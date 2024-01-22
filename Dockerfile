FROM ubuntu:jammy as BUILDER
WORKDIR /app
RUN apt-get update && \
    apt-get -y install \
    cmake build-essential libssl-dev \
    pkg-config libboost-all-dev \
    libsodium-dev libzmq5 libzmq3-dev libgmp-dev dotnet-sdk-6.0 libzmq5 libzmq3-dev libsodium-dev libgmp-dev libboost-all-dev curl dotnet-sdk-6.0 git cmake ninja-build build-essential libssl-dev pkg-config libboost-all-dev libzmq5 libgmp-dev

COPY . .

WORKDIR /app/src/Miningcore
RUN dotnet publish -c Release --framework net6.0 -o ../../build

# Runtime image
FROM ubuntu:jammy

WORKDIR /app

RUN apt-get update && \
    apt-get install -y libzmq5 libzmq3-dev libsodium-dev libgmp-dev libboost-all-dev curl dotnet-sdk-6.0 git cmake ninja-build build-essential libssl-dev pkg-config libboost-all-dev libzmq5 libgmp-dev && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

RUN apt-get update && apt-get install -y locales && rm -rf /var/lib/apt/lists/* && localedef -i en_US -c -f UTF-8 -A /usr/share/locale/locale.alias en_US.UTF-8
ENV LANG en_US.utf8
RUN groupadd -g 10001 miningcore
RUN useradd -u 10001 -g miningcore -d /app miningcore
RUN chown -R miningcore:miningcore /app
USER miningcore

COPY --from=BUILDER /app/build ./

CMD ["./Miningcore", "-c", "config.json" ]