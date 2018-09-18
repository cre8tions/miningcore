﻿/*
Copyright 2017 Coin Foundry (coinfoundry.org)
Authors: Oliver Weichhold (oliver@weichhold.com)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MiningCore.Configuration;
using MiningCore.JsonRpc;
using MiningCore.Mining;
using MiningCore.Time;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;
using Contract = MiningCore.Contracts.Contract;

namespace MiningCore.Stratum
{
    public class StratumClient
    {
        public StratumClient(Socket socket, IMasterClock clock, IPEndPoint endpointConfig, string connectionId)
        {
            this.socket = socket;

            receivePipe = new Pipe(PipeOptions.Default);

            this.clock = clock;
            PoolEndpoint = endpointConfig;
            ConnectionId = connectionId;
        }

        public StratumClient()
        {
            // For unit testing only
        }

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IMasterClock clock;

        private const int MaxInboundRequestLength = 0x8000;
        private const int MaxOutboundRequestLength = 0x8000;

        private readonly Socket socket;
        private readonly Pipe receivePipe;

        private bool isAlive = true;
        private WorkerContextBase context;
        private bool expectingProxyProtocolHeader = false;

        private static readonly JsonSerializer serializer = new JsonSerializer
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        #region API-Surface

        public void Run((IPEndPoint IPEndPoint, TcpProxyProtocolConfig ProxyProtocol) endpointConfig,
            Func<StratumClient, JsonRpcRequest, Task> onNext, Action<StratumClient> onCompleted, Action<StratumClient, Exception> onError)
        {
            PoolEndpoint = endpointConfig.IPEndPoint;
            RemoteEndpoint = (IPEndPoint) socket.RemoteEndPoint;

            expectingProxyProtocolHeader = endpointConfig.ProxyProtocol?.Enable == true;

            Task.Run(async () =>
            {
                try
                {
                    using(socket)
                    {
                        await Task.WhenAll(
                            FillReceivePipeAsync(),
                            ProcessReceivePipeAsync(endpointConfig.ProxyProtocol, onNext));

                        isAlive = false;
                        onCompleted(this);
                    }
                }

                catch(Exception ex)
                {
                    isAlive = false;
                    onError(this, ex);
                }
            });
        }

        public string ConnectionId { get; }
        public IPEndPoint PoolEndpoint { get; private set; }
        public IPEndPoint RemoteEndpoint { get; private set; }
        public DateTime? LastReceive { get; set; }
        public bool IsAlive { get; set; } = true;

        public void SetContext<T>(T value) where T : WorkerContextBase
        {
            context = value;
        }

        public T ContextAs<T>() where T : WorkerContextBase
        {
            return (T) context;
        }

        public Task RespondAsync<T>(T payload, object id)
        {
            Contract.RequiresNonNull(payload, nameof(payload));
            Contract.RequiresNonNull(id, nameof(id));

            return RespondAsync(new JsonRpcResponse<T>(payload, id));
        }

        public Task RespondErrorAsync(StratumError code, string message, object id, object result = null, object data = null)
        {
            Contract.RequiresNonNull(message, nameof(message));

            return RespondAsync(new JsonRpcResponse(new JsonRpcException((int) code, message, null), id, result));
        }

        public Task RespondAsync<T>(JsonRpcResponse<T> response)
        {
            Contract.RequiresNonNull(response, nameof(response));

            return SendAsync(response);
        }

        public Task NotifyAsync<T>(string method, T payload)
        {
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(method), $"{nameof(method)} must not be empty");

            return NotifyAsync(new JsonRpcRequest<T>(method, payload, null));
        }

        public Task NotifyAsync<T>(JsonRpcRequest<T> request)
        {
            Contract.RequiresNonNull(request, nameof(request));

            return SendAsync(request);
        }

        public async Task SendAsync<T>(T payload)
        {
            Contract.RequiresNonNull(payload, nameof(payload));

            if (isAlive)
            {
                var buf = Serialize(payload);

                logger.Trace(() => $"[{ConnectionId}] Sending: {StratumConstants.Encoding.GetString(buf)}");

                try
                {

                    await socket.SendAsync(buf, SocketFlags.None);
                }

                catch(ObjectDisposedException)
                {
                    // ignored
                }
            }
        }

        public void Disconnect()
        {
            socket.Close();

            IsAlive = false;
        }

        public void RespondErrorAsync(object id, int code, string message)
        {
            Contract.RequiresNonNull(id, nameof(id));
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(message), $"{nameof(message)} must not be empty");

            RespondAsync(new JsonRpcResponse(new JsonRpcException(code, message, null), id));
        }

        public void RespondUnsupportedMethod(object id)
        {
            Contract.RequiresNonNull(id, nameof(id));

            RespondErrorAsync(id, 20, "Unsupported method");
        }

        public void RespondUnauthorized(object id)
        {
            Contract.RequiresNonNull(id, nameof(id));

            RespondErrorAsync(id, 24, "Unauthorized worker");
        }

        public byte[] Serialize(object payload)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream, StratumConstants.Encoding))
                {
                    serializer.Serialize(writer, payload);
                    writer.Flush();

                    // append newline
                    stream.WriteByte(0xa);
                }

                return stream.ToArray();
            }
        }

        public T Deserialize<T>(string json)
        {
            using (var jreader = new JsonTextReader(new StringReader(json)))
            {
                return serializer.Deserialize<T>(jreader);
            }
        }

        #endregion // API-Surface

        private async Task FillReceivePipeAsync()
        {
            while (true)
            {
                var memory = receivePipe.Writer.GetMemory(MaxInboundRequestLength + 1);

                try
                {

                    var cb = await socket.ReceiveAsync(memory, SocketFlags.None);

                    if (cb == 0)
                        break;  // EOF

                    LastReceive = clock.Now;

                    receivePipe.Writer.Advance(cb);
                }

                catch (Exception)
                {
                    // Ensure that ProcessPipeAsync completes as well
                    receivePipe.Writer.Complete();

                    throw;
                }

                var result = await receivePipe.Writer.FlushAsync();

                if (result.IsCompleted)
                    break;
            }

            receivePipe.Writer.Complete();
        }

        private async Task ProcessReceivePipeAsync(TcpProxyProtocolConfig proxyProtocol, Func<StratumClient, JsonRpcRequest, Task> onNext)
        {
            while(true)
            {
                var result = await receivePipe.Reader.ReadAsync();

                var buffer = result.Buffer;
                SequencePosition? position = null;

                if (buffer.Length > MaxInboundRequestLength)
                {
                    Disconnect();
                    throw new InvalidDataException($"Incoming data exceeds maximum of {MaxInboundRequestLength}");
                }

                do
                {
                    // Look for a EOL in the buffer
                    position = buffer.PositionOf((byte) '\n');

                    if (position != null)
                    {
                        var slice = buffer.Slice(0, position.Value);
                        var line = StratumConstants.Encoding.GetString(slice.ToArray());

                        logger.Trace(() => $"[{ConnectionId}] Received data: {line}");

                        // Process Input
                        if (!expectingProxyProtocolHeader)
                        {
                            var request = Deserialize<JsonRpcRequest>(line);

                            if (request == null)
                            {
                                Disconnect();
                                throw new JsonException("Unable to deserialize request");
                            }

                            await onNext(this, request);
                        }

                        else
                        {
                            // Handle proxy header
                            if (!ProcessProxyHeader(line, proxyProtocol))
                            {
                                Disconnect();
                                throw new InvalidDataException($"Expected proxy header. Got something else.");
                            }
                        }

                        // Skip the line + the \n character (basically position)
                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    }
                } while(position != null);

                receivePipe.Reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                    break;
            }
        }

        private bool ProcessProxyHeader(string line, TcpProxyProtocolConfig proxyProtocol)
        {
            expectingProxyProtocolHeader = false;
            var peerAddress = RemoteEndpoint.Address;

            if (line.StartsWith("PROXY "))
            {
                //var proxyAddresses = proxyProtocol.ProxyAddresses?.Select(x => IPAddress.Parse(x)).ToArray();
                //if (proxyAddresses == null || !proxyAddresses.Any())
                //    proxyAddresses = new[] { IPAddress.Loopback };

                //if (proxyAddresses.Any(x => x.Equals(peerAddress)))
                {
                    logger.Debug(() => $"[{ConnectionId}] Received Proxy-Protocol header: {line}");

                    // split header parts
                    var parts = line.Split(" ");
                    var remoteAddress = parts[2];
                    var remotePort = parts[4];

                    // Update client
                    RemoteEndpoint = new IPEndPoint(IPAddress.Parse(remoteAddress), int.Parse(remotePort));
                    logger.Info(() => $"[{ConnectionId}] Real-IP via Proxy-Protocol: {RemoteEndpoint.Address}");

                    return true;
                }

                //else
                //{
                //    logger.Error(() => $"[{ConnectionId}] Received spoofed Proxy-Protocol header from {peerAddress}");
                //    return false;
                //}
            }

            if (proxyProtocol.Mandatory)
            {
                logger.Error(() => $"[{ConnectionId}] Missing mandatory Proxy-Protocol header from {peerAddress}. Closing connection.");
            }

            return false;
        }
    }
}
