<!DOCTYPE html>

<!--[if IEMobile 7]><html class="no-js iem7 oldie"><![endif]-->
<!--[if (IE 7)&!(IEMobile)]><html class="no-js ie7 oldie" lang="en"><![endif]-->
<!--[if (IE 8)&!(IEMobile)]><html class="no-js ie8 oldie" lang="en"><![endif]-->
<!--[if (IE 9)&!(IEMobile)]><html class="no-js ie9" lang="en"><![endif]-->
<!--[if (gt IE 9)|(gt IEMobile 7)]><!--><html class="no-js" lang="en"><!--<![endif]-->
  <head>
    <meta charset="utf-8" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge,chrome=1" />

    <title>Developr</title>
    <meta name="description" content="" />
    <meta name="author" content="" />

    <!-- http://davidbcalhoun.com/2010/viewport-metatag -->
    <meta name="HandheldFriendly" content="True" />
    <meta name="MobileOptimized" content="320" />

    <!-- http://www.kylejlarson.com/blog/2012/iphone-5-web-design/ and http://darkforge.blogspot.fr/2010/05/customize-android-browser-scaling-with.html -->
    <meta name="viewport" content="user-scalable=0, initial-scale=1.0, target-densitydpi=115" />

    <!-- For all browsers -->
    <link rel="stylesheet" href="css/reset.css?v=1" />
    <link rel="stylesheet" href="css/style.css?v=1" />
    <link rel="stylesheet" href="css/colors.css?v=1" />
    <link rel="stylesheet" media="print" href="css/print.css?v=1" />
    <!-- For progressively larger displays -->
    <link rel="stylesheet" media="only all and (min-width: 480px)" href="css/480.css?v=1" />
    <link rel="stylesheet" media="only all and (min-width: 768px)" href="css/768.css?v=1" />
    <link rel="stylesheet" media="only all and (min-width: 992px)" href="css/992.css?v=1" />
    <link rel="stylesheet" media="only all and (min-width: 1200px)" href="css/1200.css?v=1" />
    <!-- For Retina displays -->
    <link
      rel="stylesheet"
      media="only all and (-webkit-min-device-pixel-ratio: 1.5), only screen and (-o-min-device-pixel-ratio: 3/2), only screen and (min-device-pixel-ratio: 1.5)"
      href="css/2x.css?v=1"
    />

    <!-- Webfonts -->
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=Roboto+Condensed&display=swap" rel="stylesheet" />
    <link rel="stylesheet" href="js/libs/DataTables/jquery.dataTables.css?v=1" />

    <!-- Additional styles -->
    <link rel="stylesheet" href="css/styles/agenda.css?v=1" />
    <link rel="stylesheet" href="css/styles/dashboard.css?v=1" />
    <link rel="stylesheet" href="css/styles/form.css?v=1" />
    <link rel="stylesheet" href="css/styles/table.css?v=1" />
    <link rel="stylesheet" href="css/styles/progress-slider.css?v=1" />
    <link rel="stylesheet" href="css/styles/switches.css?v=1" />
    <link rel="stylesheet" href="css/miningcore.css" />

    <!-- JavaScript at bottom except for Modernizr -->
    <script src="js/libs/modernizr.custom.js"></script>

    <!-- For Modern Browsers -->
    <link rel="shortcut icon" href="img/favicons/favicon.png" />
    <!-- For everything else -->
    <link rel="shortcut icon" href="img/favicons/favicon.ico" />

    <script src="js/miningcore.js"></script>

    <!-- Microsoft clear type rendering -->
    <meta http-equiv="cleartype" content="on" />
  </head>

  <body class="clearfix with-menu with-shortcuts">
    <!-- Prompt IE 6 users to install Chrome Frame -->
    <!--[if lt IE 7
      ]><p class="message red-gradient simpler">
        Your browser is <em>ancient!</em> <a href="http://browsehappy.com/">Upgrade to a different browser</a> or
        <a href="http://www.google.com/chromeframe/?redirect=true">install Google Chrome Frame</a> to experience this site.
      </p><!
    [endif]-->

    <!-- Title bar -->
    <header role="banner" id="title-bar">
      <h2>Developr</h2>
    </header>

    <!-- Button to open/hide menu -->
    <a href="#" id="open-menu"><span>Menu</span></a>

    <!-- Button to open/hide shortcuts -->
    <a href="#" id="open-shortcuts"><span class="icon-thumbs"></span></a>

    <!-- Main content -->
    <section role="main" id="main">
      <noscript class="message black-gradient simpler">Your browser does not support JavaScript! Some features won't work as expected...</noscript>

      <hgroup id="main-title" class="thin">
        <h1>Dashboard</h1>
        <h2>nov <strong>10</strong></h2>
      </hgroup>

      <div class="dashboard">
        <div class="columns">
          <div class="one-column">
            <ul class="stats split-on-mobile">
              <li>
                <a href="#"> <strong id="poolCount"></strong> mining <br />pools </a>
              </li>
              <li>
                <a href="#"> <strong id="totalBlocks"></strong> blocks <br />found </a>
              </li>
              <li><strong id="totalCoinPaid"></strong> Coins <br />paid</li>
              <li><strong>1</strong> cunt <br />coin</li>
            </ul>
          </div>
          <div class="one-column">
            <h2>Info</h2>
          </div>

          <div class="eight-columns ">
            <span class="progress anthracite thin" style="width: 100px">
              <span class="progress-bar" style="width: 45%"></span>
            </span>
          </div>
          <div class="two-column">
            <div class="widget">
              <div class="widget-header">
                <h3>Server load</h3>
              </div>
              <div class="widget-content"></div>
            </div>
          </div>
        </div>
      </div>

      <div class="with-padding">
        <div class="columns">
          <div class="two-columns">
            <div class="block margin-bottom">
              <span class="ribbon tiny"><span class="ribbon-inner">Hey!</span></span>
              <h3 class="block-title">Block title</h3>

              <div class="with-padding">
                <div class="facts clearfix">
                  <div class="fact">
                    <span class="fact-value"> 50 <span class="fact-unit">Min</span> </span>
                    Average time per session<br />
                    <span class="fact-progress red">-5% ▼</span>
                  </div>

                  <div class="fact">
                    <span class="fact-value"> 25 <span class="fact-unit">%</span> </span>
                    Traffic growth over 30 days<br />
                    <span class="fact-progress green">+7.1% ▲</span>
                  </div>
                </div>
              </div>
              <div class="with-padding">
                <div class="facts clearfix">
                  <div class="fact">
                    <span class="fact-value"> 50 <span class="fact-unit">Min</span> </span>
                    Average time per session<br />
                    <span class="fact-progress red">-5% ▼</span>
                  </div>

                  <div class="fact">
                    <span class="fact-value"> 25 <span class="fact-unit">%</span> </span>
                    Traffic growth over 30 days<br />
                    <span class="fact-progress green">+7.1% ▲</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div class="eight-columns">
            <div class="block margin-bottom">
              <span class="ribbon"><span class="ribbon-inner red-gradient glossy">New!</span></span>
              <h3 class="block-title glossy licarbonen">Current Pools</h3>
              <table class="simple-table responsive-table" id="pool-coins">
                <thead>
                  <tr>
                    <th class="coin border-0">Pool coin</th>
                    <th class="algo border-0">Algorithm</th>
                    <th class="miniers border-0">Miners</th>
                    <th class="pool-hash border-0">Pool Hashrate</th>
                    <th class="pool-ttf border-0">TTF</th>
                    <th class="fee border-0">Fee</th>
                    <th class="net-hash border-0">Network Hashrate</th>
                    <th class="net-diff border-0">Network Difficulty</th>
                    <th class="col-hide border-0"></th>
                  </tr>
                </thead>

                <tbody class="pool-coin-table"></tbody>
              </table>
            </div>
          </div>
          <div class="two-columns">
            <h4 class="green underline">Paragraphs</h4>
            <p class="left-border">Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam.</p>

            <p class="inline-medium-label">
              <span class="label">Large with stripes</span>

              <span class="progress" style="width: 100%">
                <!-- inner progression marks -->
                <span class="inner-mark" style="left: 25%"></span>
                <span class="inner-mark" style="left: 50%"></span>
                <span class="inner-mark" style="left: 75%"></span>

                <!-- top and bottom progression marks -->
                <span class="top-mark" style="left: 1px"><span class="mark-label align-right">0%</span></span>
                <span class="top-mark" style="left: 25%"><span class="mark-label">25%</span></span>
                <span class="top-mark" style="left: 50%"><span class="mark-label">50%</span></span>
                <span class="top-mark" style="left: 75%"><span class="mark-label">75%</span></span>
                <span class="top-mark" style="left: 100%"><span class="mark-label align-left">100%</span></span>

                <!-- background-text, revealed when progress bar is too small -->
                <span class="progress-text">35%</span>

                <!-- progress bar with foreground text -->
                <span class="progress-bar red-gradient glossy" style="width: 35%">
                  <span class="progress-text">35%</span>
                </span>
              </span>
            </p>
          </div>
        </div>
      </div>
    </section>

    <!-- End main content -->

    <!-- Side tabs shortcuts with legends under the icons -->
    <ul id="shortcuts" role="complementary" class="children-tooltip tooltip-right with-legend">
      <li class="current">
        <a href="./" class="shortcut-dashboard" title="Dashboard"><span class="shortcut-legend">Dashboard</span></a>
      </li>
      <!-- <li>
        <a href="inbox.html" class="shortcut-messages" title="Messages"><span class="shortcut-legend">Messages</span></a>
      </li>
      <li>
        <a href="agenda.html" class="shortcut-agenda" title="Agenda"><span class="shortcut-legend">Agenda</span></a>
      </li>
      <li>
        <a href="tables.html" class="shortcut-contacts" title="Contacts"><span class="shortcut-legend">Contacts</span></a>
      </li>
      <li>
        <a href="explorer.html" class="shortcut-medias" title="Medias"><span class="shortcut-legend">Medias</span></a>
      </li>
      <li>
        <a href="sliders.html" class="shortcut-stats" title="Stats"><span class="shortcut-legend">Stats</span></a>
      </li>
      <li class="at-bottom">
        <a href="form.html" class="shortcut-settings" title="Settings"><span class="shortcut-legend">Settings</span></a>
      </li>
      <li>
        <span class="shortcut-notes" title="Notes"><span class="shortcut-legend">Notes</span></span>
      </li> -->
    </ul>

    <!-- Sidebar/drop-down menu -->
    <section id="menu" role="complementary">
      <!-- This wrapper is used by several responsive layouts -->
      <div id="menu-content">
        <header>Administrator</header>

        <div id="profile">
          <img src="img/user.png" width="64" height="64" alt="User name" class="user-icon" />
          <span class="icon-user icon-size5 black large-text-shadow"></span>
          Hello
          <span class="name">John <b>Doe</b></span>
        </div>

        <section class="navigable">
          <ul class="big-menu">
            <li class="with-right-arrow">
              <span><span class="list-count">11</span>Main styles</span>
              <ul class="big-menu">
                <li><a href="typography.html">Typography</a></li>
                <li><a href="columns.html">Columns</a></li>
                <li><a href="tables.html">Tables</a></li>
                <li><a href="colors.html">Colors &amp; backgrounds</a></li>
                <li><a href="icons.html">Icons</a></li>
                <li><a href="files.html">Files &amp; Gallery</a></li>
                <li class="with-right-arrow">
                  <span><span class="list-count">4</span>Forms &amp; buttons</span>
                  <ul class="big-menu">
                    <li><a href="buttons.html">Buttons</a></li>
                    <li><a href="form.html">Form elements</a></li>
                    <li><a href="textareas.html">Textareas &amp; WYSIWYG</a></li>
                    <li><a href="form-layouts.html">Form layouts</a></li>
                    <li><a href="wizard.html">Wizard</a></li>
                  </ul>
                </li>
                <li class="with-right-arrow">
                  <span><span class="list-count">2</span>Agenda &amp; Calendars</span>
                  <ul class="big-menu">
                    <li><a href="agenda.html">Agenda</a></li>
                    <li><a href="calendars.html">Calendars</a></li>
                  </ul>
                </li>
                <li><a href="blocks.html">Blocks &amp; infos</a></li>
              </ul>
            </li>
            <li class="with-right-arrow">
              <span><span class="list-count">8</span>Main features</span>
              <ul class="big-menu">
                <li><a href="auto-setup.html">Automatic setup</a></li>
                <li><a href="responsive.html">Responsiveness</a></li>
                <li><a href="tabs.html">Tabs</a></li>
                <li><a href="sliders.html">Slider &amp; progress</a></li>
                <li><a href="modals.html">Modal windows</a></li>
                <li class="with-right-arrow">
                  <span><span class="list-count">3</span>Messages &amp; notifications</span>
                  <ul class="big-menu">
                    <li><a href="messages.html">Messages</a></li>
                    <li><a href="notifications.html">Notifications</a></li>
                    <li><a href="tooltips.html">Tooltips</a></li>
                  </ul>
                </li>
              </ul>
            </li>
            <li class="with-right-arrow">
              <a href="ajax-demo/submenu.html" class="navigable-ajax" title="Menu title">With ajax sub-menu</a>
            </li>
          </ul>
        </section>

        <ul class="unstyled-list">
          <li class="title-menu">New messages</li>
          <li>
            <ul class="message-menu">
              <li>
                <span class="message-status">
                  <a href="#" class="starred" title="Starred">Starred</a>
                  <a href="#" class="new-message" title="Mark as read">New</a>
                </span>
                <span class="message-info">
                  <span class="blue">17:12</span>
                  <a href="#" class="attach" title="Download attachment">Attachment</a>
                </span>
                <a href="#" title="Read message">
                  <strong class="blue">John Doe</strong><br />
                  <strong>Mail subject</strong>
                </a>
              </li>
              <li>
                <a href="#" title="Read message">
                  <span class="message-status">
                    <span class="unstarred">Not starred</span>
                    <span class="new-message">New</span>
                  </span>
                  <span class="message-info">
                    <span class="blue">15:47</span>
                  </span>
                  <strong class="blue">May Starck</strong><br />
                  <strong>Mail subject a bit longer</strong>
                </a>
              </li>
              <li>
                <span class="message-status">
                  <span class="unstarred">Not starred</span>
                </span>
                <span class="message-info">
                  <span class="blue">15:12</span>
                </span>
                <strong class="blue">May Starck</strong><br />
                Read message
              </li>
            </ul>
          </li>
        </ul>
      </div>
      <!-- End content wrapper -->

      <!-- This is optional -->
      <footer id="menu-footer">
        <p class="button-height">
          <input type="checkbox" name="auto-refresh" id="auto-refresh" checked="checked" class="switch float-right" />
          <label for="auto-refresh">Auto-refresh</label>
        </p>
      </footer>
    </section>
    <!-- End sidebar/drop-down menu -->

    <!-- JavaScript at the bottom for fast page loading -->

    <!-- Scripts -->
    <script src="js/libs/jquery-1.10.2.min.js"></script>
    <script src="js/setup.js"></script>

    <!-- Template functions -->
    <script src="js/developr.input.js"></script>
    <script src="js/developr.message.js"></script>
    <script src="js/developr.modal.js"></script>
    <script src="js/developr.navigable.js"></script>
    <script src="js/developr.notify.js"></script>
    <script src="js/developr.scroll.js"></script>
    <script src="js/developr.progress-slider.js"></script>
    <script src="js/developr.tooltip.js"></script>
    <script src="js/developr.confirm.js"></script>
    <script src="js/developr.agenda.js"></script>
    <script src="js/developr.tabs.js"></script>
    <!-- Must be loaded last -->

    <!-- Plugins -->
    <script src="js/libs/jquery.tablesorter.min.js"></script>
    <script src="js/libs/DataTables/jquery.dataTables.min.js"></script>

    <script>



      // Call template init (optional, but faster if called manually)
      $.template.init();



      // If the browser support the Notification API, ask user for permission (with a little delay)
      if (notify.hasNotificationAPI() && !notify.isNotificationPermissionSet())
      {
      	setTimeout(function()
      	{
      		notify.showNotificationPermission('Your browser supports desktop notification, click here to enable them.', function()
      		{
      			// Confirmation message
      			if (notify.hasNotificationPermission())
      			{
      				notify('Notifications API enabled!', 'You can now see notifications even when the application is in background', {
      					icon: 'img/demo/icon.png',
      					system: true
      				});
      			}
      			else
      			{
      				notify('Notifications API disabled!', 'Desktop notifications will not be used.', {
      					icon: 'img/demo/icon.png'
      				});
      			}
      		});

      	}, 2000);
      }
    </script>

    <script>
        $(document).ready(function() {
        loadIndex();
      });
    </script>
  </body>
</html>
