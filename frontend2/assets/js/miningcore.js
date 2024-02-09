/*!
 * Miningcore.js v1.02
 * Copyright 2020 Authors (https://github.com/minernl/Miningcore)
 */

// --------------------------------------------------------------------------------------------
// --------------------------------------------------------------------------------------------
// Current running domain (or ip address) url will be read from the browser url bar.
// You can check the result in you browser development view -> F12 -> Console
// -->> !! no need to change anything below here !! <<--
// --------------------------------------------------------------------------------------------
// --------------------------------------------------------------------------------------------

// read WebURL from current browser
var WebURL = "http://192.168.1.66/"; // Website URL is:  https://domain.com/
// WebURL correction if not ends with /
if (WebURL.substring(WebURL.length - 1) != "/") {
  WebURL = WebURL + "/";
  console.log("Corrected WebURL, does not end with / -> New WebURL : ", WebURL);
}
var API = "http://192.168.1.66:4000/api/"; // API address is:  https://domain.com/api/
// // API correction if not ends with /
// if (API.substring(API.length - 1) != "/") {
//   API = API + "/";
//   console.log("Corrected API, does not end with / -> New API : ", API);
// }
var stratumAddress = "192.168.1.66"; // Stratum address is:  domain.com

// --------------------------------------------------------------------------------------------
// no need to change anything below here
// --------------------------------------------------------------------------------------------
console.log("MiningCore.WebUI : ", WebURL); // Returns website URL
console.log("API address used : ", API); // Returns API URL
console.log("Stratum address  : ", "stratum+tcp://" + stratumAddress + ":"); // Returns Stratum URL
console.log("Page Load        : ", window.location.href); // Returns full URL

currentPage = "index";

// check browser compatibility
var nua = navigator.userAgent;
//var is_android = ((nua.indexOf('Mozilla/5.0') > -1 && nua.indexOf('Android ') > -1 && nua.indexOf('AppleWebKit') > -1) && !(nua.indexOf('Chrome') > -1));
var is_IE = nua.indexOf("Mozilla/5.0") > -1 && nua.indexOf("Trident") > -1 && !(nua.indexOf("Chrome") > -1);
if (is_IE) {
  console.log("Running in IE browser is not supported - ", nua);
}

// Load INDEX Page content
function loadIndex() {
  var hashList = window.location.pathname.split(/[/?=]/);
  currentPool = hashList[2];
  currentPage = hashList[3];
  currentAddress = hashList[4];
  console.log("Current pool: " + currentPool)
  console.log("Current page: " + currentPage)
  console.log("Current address: " + currentAddress)

  if (currentPool && !currentPage) {
    currentPage = "stats";
  } else if (!currentPool && !currentPage) {
    currentPage = "index";
  }

  if (currentAddress) {
    localStorage.setItem(currentPool + "-walletAddress", currentAddress);
  } else if (localStorage[currentPool + "-walletAddress"]) {
    $("#walletAddress").val(localStorage[currentPool + "-walletAddress"]);
  }

  if (currentPool) {
    switch (currentPage) {
      case "stats":
        console.log("Loading stats page content");
        loadStatsPage();
        break;
      case "dash":
        console.log("Loading dashboard page content");
        $(".nav-dashboard").addClass("active");
        loadDashboardPage();
        break;
      case "miners":
        console.log("Loading miners page content");
        $(".nav-miners").addClass("active");
        loadMinersPage();
        break;
      case "blocks":
        console.log("Loading blocks page content");
        $(".nav-blocks").addClass("active");
        loadBlocksPage();
        break;
      case "payments":
        console.log("Loading payments page content");
        $(".nav-payments").addClass("active");
        loadPaymentsPage();
        break;
      case "connect":
        console.log("Loading connect page content");
        $(".nav-connect").addClass("active");
        loadConnectPage();
        break;
      case "faq":
        console.log("Loading faq page content");
        $(".nav-faq").addClass("active");
        break;
      case "support":
        console.log("Loading support page content");
        $(".nav-support").addClass("active");
        break;
      default:
      // default if nothing above fits
    }
  } else {
    loadHomePage();
  }
}

// Load HOME page content
function loadHomePage() {
  console.log("Loading home page content");

    return $.ajax(API + "pools")
      .done(function (data) {
        //const poolCoinTableTemplate = "";  //$(".index-coin-table-template").html();

        var poolCoinTableTemplate = "";
        var poolCount = 0;
        var totalBlocks = 0;
        var totalCoinPaid = 0;
        var USDTPrice = 0

        $.each(data.pools, function (index, value) {
          poolCount++;
          totalBlocks += value.totalBlocks;
          totalCoinPaid += value.totalPaid;

          var coinLogo = "<img class='coinimg' src='../icon/" + value.coin.type.toLowerCase() + ".png' />";
          var coinName = value.coin.name;
          if (typeof coinName === "undefined" || coinName === null) {
            coinName = value.coin.type;
          }

          if (typeof value.coin.blockTime === "undefined" || value.coin.blockTime === null) var blocktime = 60;

          if (value.poolStats.poolHashrate > 0) {
            var ttf = ((value.networkStats.networkHashrate / value.poolStats.poolHashrate) * blocktime).toFixed(0);
          } else var ttf = "--";


          poolCoinTableTemplate += "<tr class='coin-table-row'>";
          poolCoinTableTemplate += "<td class='coin'><a href='/pool/" + value.id + "'>" + coinLogo + coinName + " (" + value.coin.type.toUpperCase() + ") </a></td>";
          poolCoinTableTemplate += "<td class='algo'>" + value.coin.algorithm + "</td>";
          poolCoinTableTemplate += "<td class='miners'>" + (value.poolStats.connectedMiners > 0 ? value.poolStats.connectedMiners : "--") + "</td>";
          poolCoinTableTemplate += "<td class='pool-hash'>" + (value.poolStats.poolHashrate > 0 ? _formatter(value.poolStats.poolHashrate, 3, "H/s") : "--") + "</td>";
          poolCoinTableTemplate += "<td class='pool-ttf'>" + readableSeconds(ttf) + "</td>";
          poolCoinTableTemplate += "<td class='fee'><small class='tag red-bg'>" + value.paymentProcessing.payoutScheme + " " + value.poolFeePercent + "% </small></td>";
          poolCoinTableTemplate += "<td class='net-hash'>" + _formatter(value.networkStats.networkHashrate, 3, "H/s") + "</td>";
          poolCoinTableTemplate += "<td class='net-diff'>" + _formatter(value.networkStats.networkDifficulty, 5, "") + "</td>";
          poolCoinTableTemplate += "<td class='gomine'><a href='pool/" + value.id + ".html'><button class='button'>Go Mine " + coinLogo + coinName + "</button></a></td>";
          poolCoinTableTemplate += "</tr>";

          var ttf2 = (value.networkStats.networkDifficulty * 2 ** 32) / value.poolStats.poolHashrate; // seconds
          console.log(ttf2);
        });

        $(".pool-coin-table").html(poolCoinTableTemplate);
        $("#poolCount").html(poolCount);
        $("#totalBlocks").html(totalBlocks);
        $("#totalCoinPaid").html(_formatter(totalCoinPaid, 0, "", false));

        var blocks = loadBlocksPage(1);
        $("#blockList").html(blocks);

        $(document).ready(function () {
          $("#pool-coins tr").click(function () {
            var href = $(this).find("a").attr("href");
            if (href) {
              window.location = href;
            }
          });
        });
      })
    .fail(function () {
      var poolCoinTableTemplate = "";

      poolCoinTableTemplate += "<tr><td class='notconnected' colspan='8'> ";
      poolCoinTableTemplate += "<div class='alert alert-danger'>";
      poolCoinTableTemplate += "	<h4><i class='splashy-error'></i> Warning!</h4>";
      poolCoinTableTemplate += "	<hr>";
      poolCoinTableTemplate += "	<p>The pool is currently down for maintenance.</p>";
      poolCoinTableTemplate += "	<p>Please try again later.</p>";
      poolCoinTableTemplate += "</div>";
      poolCoinTableTemplate += "</td></tr>";

      $(".pool-coin-table").html(poolCoinTableTemplate);
    });
}

// Load STATS page content
function loadStatsPage() {
  //clearInterval();
  setInterval(
    (function load() {
      loadStatsData();
      return load;
    })(),
    60000
  );
  setInterval(
    (function load() {
      loadStatsChart();
      return load;
    })(),
    600000
  );
}

// Load DASHBOARD page content
function loadDashboardPage() {

  function render() {
    //clearInterval();
    setInterval(
      (function load() {
        loadStatsData();
        loadDashboardData(currentAddress);
        loadDashboardWorkerList(currentAddress);
        loadDashboardChart(currentAddress);
        return load;
      })(),
      60000
    );
  }

  if (currentAddress) {
    localStorage.setItem(currentPool + "-walletAddress", currentAddress);
    render();
  }

  if (localStorage[currentPool + "-walletAddress"]) {
    $("#walletAddress").val(localStorage[currentPool + "-walletAddress"]);
  }
}

// Load MINERS page content
function loadMinersPage() {
  return $.ajax(API + "pools/" + currentPool + "/miners?page=0&pagesize=20")
    .done(function (data) {
      loadStatsData();
      var minerList = "";
      if (data.length > 0) {
        $.each(data, function (index, value) {
          minerList += "<tr>";
          //minerList +=   "<td>" + value.miner + "</td>";
          minerList += "<td>" + value.miner.substring(0, 12) + " &hellip; " + value.miner.substring(value.miner.length - 12) + "</td>";
          //minerList += '<td><a href="' + value.minerAddressInfoLink + '" target="_blank">' + value.miner.substring(0, 12) + ' &hellip; ' + value.miner.substring(value.miner.length - 12) + '</td>';
          minerList += "<td>" + _formatter(value.hashrate, 5, "H/s") + "</td>";
          minerList += "<td>" + _formatter(value.sharesPerSecond, 5, "S/s") + "</td>";
          minerList += "</tr>";
        });
      } else {
        minerList += '<tr><td colspan="4">No miner connected</td></tr>';
      }
      $("#minerList").html(minerList);
    })
    .fail(function () {
      $.notify(
        {
          message: "Error: No response from API.<br>(loadMinersList)",
        },
        {
          type: "danger",
          timer: 3000,
        }
      );
    });
}

// Load BLOCKS page content
function loadBlocksPage(isIndex = 0) {
  var ajaxUrl = ""
  var showPoolId = 0;
  if (isIndex) {
    ajaxUrl = API + "blocks?page=0&pageSize=10"
    showPoolId = 1;
  } else {
    ajaxUrl = API + "pools/" + currentPool + "/blocks?page=0&pageSize=25"
  }

  return $.ajax(ajaxUrl)
    .done(function (data) {
      loadStatsData();
      var blockList = "";
      if (data.length > 0) {
        $.each(data, function (index, value) {
          var createDate = convertLocalDateToUTCDate(new Date(value.created), false).toISOString();
          var effort = Math.round(value.effort * 100);
          var progress = Math.round(value.confirmationProgress * 100)
          var progress_color = progress <= 50 ? "red" : progress < 100 ? "orange" : "green";

          blockList += "<tr>";
          blockList += "<td>" + createDate + "</td>";
          if (showPoolId) {
            blockList += "<td>" + value.poolId + "</td>";
          }
          blockList += "<td><a href='" + value.infoLink + "' target='_blank'>" + value.blockHeight + "</a></td>";
          if (typeof value.effort !== "undefined") {
            blockList += "<td>" + formatLuck(effort) + "</td>";
          } else {
            blockList += "<td>n/a</td>";
          }
          var statustext = "<span class='tag'>Confirmed</span>";
          if (value.status == "pending")
            statustext = "<span class='tag orange-bg'>Pending</span>";
          blockList += "<td>" + statustext + "</td>";
          blockList += "<td>" + value.reward + "</td>";
          blockList += "<td><span class='progress' style='width: 50%'><span class='progress-text'>" + Math.round(value.confirmationProgress * 100) + "%</span><span class='progress-bar " + progress_color + "-gradient glossy' style='width: " + Math.round(value.confirmationProgress * 100) + "%'><span class='progress-text'>" + Math.round(value.confirmationProgress * 100) + "%</span></span></td>";
          // "<td><span class='progress thin' style='width: 100px'><span class='progress-bar " + (Math.round(value.confirmationProgress * 100) == 100 ? "blue-gradient" : "red-gradient") + " glossy' style='width: " + Math.round(value.confirmationProgress * 100) + "%'></span></span></td>";
          blockList += "</tr>";
        });
      } else {
        blockList += '<tr><td colspan="6">No blocks found yet</td></tr>';
      }

      $("#blockList").html(blockList);
    })
    .fail(function () {
      $.notify(
        {
          message: "Error: No response from API.<br>(loadBlocksList)",
        },
        {
          type: "danger",
          timer: 3000,
        }
      );
    });
}

// Load PAYMENTS page content
function loadPaymentsPage() {
  return $.ajax(API + "pools/" + currentPool + "/payments?page=0&pageSize=15")
    .done(function (data) {
      loadStatsData();

      var paymentList = "";
      if (data.length > 0) {
        $.each(data, function (index, value) {
          var createDate = convertLocalDateToUTCDate(new Date(value.created), false).toISOString();
          paymentList += "<tr>";
          paymentList += "<td class='align-right'>" + createDate + "</td>";
          paymentList += '<td><a href="' + value.addressInfoLink + '" target="_blank">' + value.address.substring(0, 12) + " &hellip; " + value.address.substring(value.address.length - 12) + "</td>";
          paymentList += "<td>" + value.amount + "</td>";
          paymentList +=
            '<td colspan="2"><a href="' +
            value.transactionInfoLink +
            '" target="_blank">' +
            value.transactionConfirmationData.substring(0, 16) +
            " &hellip; " +
            value.transactionConfirmationData.substring(value.transactionConfirmationData.length - 16) +
            " </a></td>";
          paymentList += "</tr>";
        });
      } else {
        paymentList += '<tr><td colspan="4">No payments found yet</td></tr>';
      }
      $("#paymentList").html(paymentList);
    })
    .fail(function () {
      $.notify(
        {
          message: "Error: No response from API.<br>(loadPaymentsList)",
        },
        {
          type: "danger",
          timer: 3000,
        }
      );
    });
}

// Load CONNECTION page content
function loadConnectPage() {
  return $.ajax(API + "pools")
    .done(function (data) {
      var connectPoolConfig = "";
      $.each(data.pools, function (index, value) {
        if (currentPool === value.id) {
          defaultPort = Object.keys(value.ports)[0];
          coinName = value.coin.name;
          coinType = value.coin.type.toLowerCase();
          algorithm = value.coin.algorithm;

          // Connect Pool config table
          connectPoolConfig += "<tr><td class='align-right'><strong>Crypto Coin name</strong></td><td>" + coinName + " (" + value.coin.type + ") </td></tr>";
          //connectPoolConfig += "<tr><td>Coin Family line </td><td>" + value.coin.family + "</td></tr>";
          connectPoolConfig += "<tr><td class='align-right'><strong>Coin Algorithm</strong></td><td>" + value.coin.algorithm + "</td></tr>";
          connectPoolConfig +=
            '<tr><td class="align-right"><strong>Pool Wallet</strong></td><td><a href="' +
            value.addressInfoLink +
            '" target="_blank">' +
            value.address.substring(0, 12) +
            " &hellip; " +
            value.address.substring(value.address.length - 12) +
            "</a></td></tr>";
          connectPoolConfig += "<tr><td class='align-right'><strong>Payout Scheme</strong></td><td>" + value.paymentProcessing.payoutScheme + "</td></tr>";
          connectPoolConfig += "<tr><td class='align-right'><strong>Minimum Payment</strong></td><td>" + value.paymentProcessing.minimumPayment + " " + value.coin.type + "</td></tr>";
          if (typeof value.paymentProcessing.minimumPaymentToPaymentId !== "undefined") {
            connectPoolConfig += "<tr><td class='align-right'><strong>Minimum Payout (to Exchange)</strong></td><td>" + value.paymentProcessing.minimumPaymentToPaymentId + "</td></tr>";
          }
          connectPoolConfig += "<tr><td class='align-right'><strong>Pool Fee</strong></td><td>" + value.poolFeePercent + "%</td></tr>";
          $.each(value.ports, function (port, options) {
            connectPoolConfig += "<tr><td class='align-right'><strong>stratum+tcp://" + stratumAddress + ":" + port + "</strong></td><td>";
            if (typeof options.varDiff !== "undefined" && options.varDiff != null) {
              connectPoolConfig += "Difficulty Variable / " + options.varDiff.minDiff + " &harr; ";
              if (typeof options.varDiff.maxDiff === "undefined" || options.varDiff.maxDiff == null) {
                connectPoolConfig += "&infin; ";
              } else {
                connectPoolConfig += options.varDiff.maxDiff;
              }
            } else {
              connectPoolConfig += "Difficulty Static / " + options.difficulty;
            }
            connectPoolConfig += "</td></tr>";
          });
        }
      });
      connectPoolConfig += "</tbody>";
      $("#connectPoolConfig").html(connectPoolConfig);
      $("#algorithm").html(algorithm);

      // Connect Miner config
      $(".coinName").html(coinName);
    })
    .fail(function () {
      $.notify(
        {
          message: "Error: No response from API.<br>(loadConnectConfig)",
        },
        {
          type: "danger",
          timer: 3000,
        }
      );
    });
}

// Dashboard - load wallet stats
function loadWallet() {
  console.log("Loading wallet address:", $("#walletAddress").val());
  if ($("#walletAddress").val().length > 0) {
    localStorage.setItem(currentPool + "-walletAddress", $("#walletAddress").val());
  }
  window.location.href = "#" + currentPool + "/" + currentPage + "/" + $("#walletAddress").val();
}

// General formatter function
function _formatter(value, decimal, unit, space = true) {
  if (value === 0) {
    return "0 " + unit;
  } else {
    var si = [
      { value: 1, symbol: "" },
      { value: 1e3, symbol: "k" },
      { value: 1e6, symbol: "M" },
      { value: 1e9, symbol: "G" },
      { value: 1e12, symbol: "T" },
      { value: 1e15, symbol: "P" },
      { value: 1e18, symbol: "E" },
      { value: 1e21, symbol: "Z" },
      { value: 1e24, symbol: "Y" },
    ];
    for (var i = si.length - 1; i > 0; i--) {
      if (value >= si[i].value) {
        break;
      }
    }
    return (value / si[i].value).toFixed(decimal).replace(/\.0+$|(\.[0-9]*[1-9])0+$/, "$1") + (space == true ? " " : "") + si[i].symbol + unit;
  }
}

// Time convert Local -> UTC
function convertLocalDateToUTCDate(date, toUTC) {
  date = new Date(date);
  //Local time converted to UTC
  var localOffset = date.getTimezoneOffset() * 60000;
  var localTime = date.getTime();
  if (toUTC) {
    date = localTime + localOffset;
  } else {
    date = localTime - localOffset;
  }
  newDate = new Date(date);
  return newDate;
}

// Time convert UTC -> Local
function convertUTCDateToLocalDate(date) {
  var newDate = new Date(date.getTime() + date.getTimezoneOffset() * 60 * 1000);
  var localOffset = date.getTimezoneOffset() / 60;
  var hours = date.getUTCHours();
  newDate.setHours(hours - localOffset);
  return newDate;
}

// Check if file exits
function doesFileExist(urlToFile) {
  var xhr = new XMLHttpRequest();
  xhr.open("HEAD", urlToFile, false);
  xhr.send();

  if (xhr.status == "404") {
    return false;
  } else {
    return true;
  }
}

// STATS page data
function loadStatsData() {
  return $.ajax(API + "pools")
    .done(function (data) {
      console.log("Stats method");
      $.each(data.pools, function (index, value) {
        if (currentPool === value.id) {
          console.log(data);
          $("#blockchainHeight").text(value.networkStats.blockHeight);
          $("#connectedPeers").text(value.networkStats.connectedPeers);
          $("#minimumPayment").text(value.paymentProcessing.minimumPayment + " " + value.coin.type);
          $("#payoutScheme").text(value.paymentProcessing.payoutScheme);
          $("#poolFeePercent").text(value.poolFeePercent + " %");
          $("#paymentProcessing").text(value.paymentProcessing.enabled);
          $("#canonicalName").text(value.coin.canonicalName);
          $("#algorithm").text(value.coin.algorithm);
          $("#website").html("<a href='" + value.coin.website + "' target='_blank'>" + value.coin.website + "</a>");
          $("#discord").html("<a href='" + value.coin.discord + "' target='_blank'>" + value.coin.discord + "</a>");
          $("#poolAddress").html("<a href='" + value.addressInfoLink + "' target='_blank'>" + value.address + "</a>");
          $("#poolHashRate").text(_formatter(value.poolStats.poolHashrate, 2, "H/s"));
          $("#poolMiners").text(value.poolStats.connectedMiners + " Miner(s)");
          $("#networkHashRate").text(_formatter(value.networkStats.networkHashrate, 2, "H/s"));
          $("#networkDifficulty").text(_formatter(value.networkStats.networkDifficulty, 4, ""));
          $("#poolEffort").text(_formatter(value.poolEffort * 100, 0, "%"));
          $("#lastNetworkBlockFound").text(value.networkStats.lastNetworkBlockTime);
          $("#lastBlockFound").text(value.lastPoolBlockTime);
          $("#totalBlocksFound").text(value.totalBlocks);
          $("#totalPaid").text(value.totalPaid);
          $("#coinname").text(value.coin.name);

          var priceURL = "https://api.xeggex.com/api/v2/ticker/" + value.coin.type.toLowerCase() + "%2Fusdt"
          console.log(priceURL);

          $.ajax({
            url: priceURL,
            async: false,
            success: function (pricedata) {
              console.log("Getting price data...");
              console.log(pricedata.last_price);
              $("#lastPrice").text("$" + pricedata.last_price);
            }
          });
        }
      });
    })
    .fail(function () {
      $.notify(
        {
          message: "Error: No response from API.<br>(loadStatsData)",
        },
        {
          type: "danger",
          timer: 3000,
        }
      );
    });
}

// STATS page charts
function loadStatsChart() {
  return $.ajax(API + "pools/" + currentPool + "/performance")
    .done(function (data) {
      console.log(data);
      labels = [];

      poolHashRate = [];
      networkHashRate = [];
      networkDifficulty = [];
      connectedMiners = [];
      connectedWorkers = [];

      $.each(data.stats, function (index, value) {
        if (labels.length === 0 || (labels.length + 1) % 4 === 1) {
          var createDate = convertLocalDateToUTCDate(new Date(value.created), false);
          labels.push(createDate.getHours() + ":00");
        } else {
          labels.push("");
        }
        poolHashRate.push(value.poolHashrate);
        networkHashRate.push(value.networkHashrate);
        networkDifficulty.push(value.networkDifficulty);
        connectedMiners.push(value.connectedMiners);
        // connectedWorkers.push(value.connectedWorkers);
      });

      var dataPoolHash = { labels: labels, series: [poolHashRate] };
      var dataNetworkHash = { labels: labels, series: [networkHashRate] };
      var dataNetworkDifficulty = { labels: labels, series: [networkDifficulty] };
      var dataMiners = { labels: labels, series: [connectedMiners, connectedWorkers] };

      var options = {
        height: "200px",
        showArea: true,
        seriesBarDistance: 1,
        showPoint: false,
        // low:Math.min.apply(null,networkHashRate)/1.1,
        axisX: {
          showGrid: true,
        },
        axisY: {
          offset: 47,
          scale: "logcc",
          labelInterpolationFnc: function (value) {
            return _formatter(value, 1, "");
          },
        },
        lineSmooth: Chartist.Interpolation.simple({
          divisor: 2,
        }),
      };

      var responsiveOptions = [
        [
          "screen and (max-width: 320px)",
          {
            axisX: {
              labelInterpolationFnc: function (value) {
                return value[1];
              },
            },
          },
        ],
      ];
      Chartist.Line("#chartStatsHashRate", dataNetworkHash, options, responsiveOptions);
      Chartist.Line("#chartStatsHashRatePool", dataPoolHash, options, responsiveOptions);
      Chartist.Line("#chartStatsDiff", dataNetworkDifficulty, options, responsiveOptions);
      Chartist.Line("#chartStatsMiners", dataMiners, options, responsiveOptions);
    })
    .fail(function () {
      $.notify(
        {
          message: "Error: No response from API.<br>(loadStatsChart)",
        },
        {
          type: "danger",
          timer: 3000,
        }
      );
    });
}

// DASHBOARD page data
function loadDashboardData(walletAddress) {
  return $.ajax(API + "pools/" + currentPool + "/miners/" + walletAddress)
    .done(function (data) {
      $("#pendingShares").text(data.pendingShares.toFixed(2));
      var workerHashRate = 0;
      if (data.performance) {
        $.each(data.performance.workers, function (index, value) {
          workerHashRate += value.hashrate;
        });
      }
      $("#minerHashRate").text(_formatter(workerHashRate, 4, 'H/s'));
      $("#pendingBalance").text(data.pendingBalance);
      $("#paidBalance").text(data.todayPaid);
      $("#lifetimeBalance").text(data.pendingBalance + data.totalPaid);
    })
    .fail(function () {
      $.notify(
        {
          message: "Error: No response from API.<br>(loadDashboardData)",
        },
        {
          type: "danger",
          timer: 3000,
        }
      );
    });
}

// DASHBOARD page Miner table
function loadDashboardWorkerList(walletAddress) {
  return $.ajax(API + "pools/" + currentPool + "/miners/" + walletAddress)
    .done(function (data) {
      var workerList = "";
      if (data.performance) {
        var workerCount = 0;
        $.each(data.performance.workers, function (index, value) {
          workerCount++;
          workerList += "<tr>";
          workerList += "<td>" + workerCount + "</td>";
          if (index.length === 0) {
            workerList += "<td>Unnamed</td>";
          } else {
            workerList += "<td>" + index + "</td>";
          }
          workerList += "<td>" + _formatter(value.hashrate, 5, "H/s") + "</td>";
          workerList += "<td>" + _formatter(value.sharesPerSecond, 5, "S/s") + "</td>";
          workerList += "</tr>";
        });
      } else {
        workerList += '<tr><td colspan="4">None</td></tr>';
      }
      $("#workerCount").text(workerCount);
      $("#workerList").html(workerList);
    })
    .fail(function () {
      $.notify(
        {
          message: "Error: No response from API.<br>(loadDashboardWorkerList)",
        },
        {
          type: "danger",
          timer: 3000,
        }
      );
    });
}

// DASHBOARD page chart
function loadDashboardChart(walletAddress) {
  return $.ajax(API + "pools/" + currentPool + "/miners/" + walletAddress + "/performance")
    .done(function (data) {
      labels = [];
      minerHashRate = [];

      $.each(data, function (index, value) {
        if (labels.length === 0 || (labels.length + 1) % 4 === 1) {
          var createDate = convertLocalDateToUTCDate(new Date(value.created), false);
          labels.push(createDate.getHours() + ":00");
        } else {
          labels.push("");
        }
        var workerHashRate = 0;
        $.each(value.workers, function (index2, value2) {
          workerHashRate += value2.hashrate;
        });
        minerHashRate.push(workerHashRate);
      });
      var data = { labels: labels, series: [minerHashRate] };
      console.log(data);
      var options = {
        height: "200px",
        showArea: true,
        seriesBarDistance: 1,
        axisX: {
          showGrid: false,
        },
        axisY: {
          offset: 47,
          labelInterpolationFnc: function (value) {
            return _formatter(value, 1, "");
          },
        },
        lineSmooth: Chartist.Interpolation.simple({
          divisor: 2,
        }),
      };
      var responsiveOptions = [
        [
          "screen and (max-width: 320px)",
          {
            axisX: {
              labelInterpolationFnc: function (value) {
                return value[0];
              },
            },
          },
        ],
      ];
      Chartist.Line("#chartDashboardHashRate", data, options, responsiveOptions);
    })
    .fail(function () {
      $.notify(
        {
          message: "Error: No response from API.<br>(loadDashboardChart)",
        },
        {
          type: "danger",
          timer: 3000,
        }
      );
    });
}

function formatLuck(percent) {
  if (!percent) {
    return;
  } else if (percent <= 50) {
    return '<span class="tag" style="background-color: #7FDBFF;">' + percent + "%</span>";
  } else if (percent <= 90) {
    return '<span class="tag" style="background-color: #39CCCC;">' + percent + "%</span>";
  } else if (percent <= 115) {
    return '<span class="tag" style="background-color: #0074D9";>' + percent + "%</span>";
  } else if (percent <= 200) {
    return '<span class="tag" style="background-color: #EF851B";>' + percent + "%</span>";
  } else {
    return '<span class="tag" style="background-color: #FF4136;">' + percent + "%</span>";
  }
}

// String Convert -> Seconds
function readableSeconds(t) {
  var seconds = Math.floor((t % 3600) % 60);
  var minutes = Math.floor((t % 3600) / 60);
  var hours = Math.floor((t % 86400) / 3600);
  var days = Math.floor((t % 604800) / 86400);
  var weeks = Math.floor((t % 2629799.8272) / 604800);
  var months = Math.floor((t % 31557597.9264) / 2629799.8272);
  var years = Math.floor(t / 31557597.9264);

  var sYears = years > 0 ? years + (years == 1 ? "y" : "y") : "";
  var sMonths = months > 0 ? (years > 0 ? " " : "") + months + (months == 1 ? "mo" : "mo") : "";
  var sWeeks = weeks > 0 ? (years > 0 || months > 0 ? " " : "") + weeks + (weeks == 1 ? "w" : "w") : "";
  var sDays = days > 0 ? (years > 0 || months > 0 || weeks > 0 ? " " : "") + days + (days == 1 ? "d" : "d") : "";
  var sHours = hours > 0 ? (years > 0 || months > 0 || weeks > 0 || days > 0 ? " " : "") + hours + (hours == 1 ? "h" : "h") : "";
  var sMinutes = minutes > 0 ? (years > 0 || months > 0 || weeks > 0 || days > 0 || hours > 0 ? " " : "") + minutes + (minutes == 1 ? "m" : "m") : "";
  var sSeconds =
    seconds > 0
      ? (years > 0 || months > 0 || weeks > 0 || days > 0 || hours > 0 || minutes > 0 ? " " : "") + seconds + (seconds == 1 ? "s" : "s")
      : years < 1 && months < 1 && weeks < 1 && days < 1 && hours < 1 && minutes < 1
      ? " Few milliseconds"
      : "";
  if (seconds > 0) return sYears + sMonths + sWeeks + sDays + sHours + sMinutes + sSeconds;
  else return "&#8734;";
}
