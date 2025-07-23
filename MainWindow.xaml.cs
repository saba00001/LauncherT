using DiscordRPC;
using DiscordRPC.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO.Compression;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Security;
using System.Net.NetworkInformation;
using System.Windows.Media.Animation;
using System.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using System.Collections.Generic;
using System.Linq;

namespace HRP
{
    public partial class MainWindow : Window
    {
        #region Fields and Properties

        private readonly HttpClient httpClient = new HttpClient();
        private readonly string versionUrl = "https://pub-e8ef73117d8c40baa7cb599b15297f19.r2.dev/version.txt";
        private readonly string gameZipUrl = "https://pub-e8ef73117d8c40baa7cb599b15297f19.r2.dev/bin.zip";
        private readonly string newsUrl = "https://pub-e8ef73117d8c40baa7cb599b15297f19.r2.dev/news.json";
        private readonly string gameDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "HRP Launcher");

        private readonly string versionFile = "version.txt";
        private readonly string gameZipFile = "bin.zip";
        private readonly string configFile = "launcher_config.json";
        private readonly string serverIP = "91.134.166.77";
        private readonly int serverPort = 33333;

        private string currentVersion = "2.0.0";
        private bool isUpdating = false;
        private bool isAutoCheckingUpdate = false;
        private bool isGameRunning = false;

        private DispatcherTimer autoUpdateTimer;
        private DispatcherTimer serverStatusTimer;
        private DispatcherTimer gameMonitorTimer;
        private LauncherConfig config;

        // Discord Rich Presence
        private DiscordRpcClient discordClient;
        private readonly string discordClientId = "1396609064473071697";
        private DateTime launcherStartTime = DateTime.UtcNow;

        // Animation Resources
        private Storyboard fadeInStoryboard;
        private Storyboard slideInStoryboard;

        #endregion

        #region Constructor and Initialization

        public MainWindow()
        {
            InitializeComponent();
            InitializeLauncher();
            InitializeAnimations();
            InitializeDiscordRichPresence();
        }

        private void InitializeLauncher()
        {
            try
            {
                // HTTP Client setup
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "HRP-Launcher/2.0");

                // Load configuration
                LoadConfig();

                // Setup timers
                SetupTimers();

                // Event handlers
                Loaded += MainWindow_Loaded;
                Closing += MainWindow_Closing;
                StateChanged += MainWindow_StateChanged;

                // System tray setup
                SystemTrayIcon.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LogError($"Launcher initialization failed: {ex.Message}");
            }
        }

        private void InitializeAnimations()
        {
            fadeInStoryboard = (Storyboard)FindResource("FadeInAnimation");
            slideInStoryboard = (Storyboard)FindResource("SlideInAnimation");
        }

        #endregion

        #region Discord Rich Presence

        private void InitializeDiscordRichPresence()
        {
            if (!config.EnableDiscordRichPresence) return;

            try
            {
                discordClient = new DiscordRpcClient(discordClientId);
                discordClient.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

                discordClient.OnReady += (sender, e) =>
                {
                    LogInfo($"Discord RPC Ready for user {e.User.Username}");
                    Dispatcher.Invoke(() => UpdateDiscordStatus(true));
                };

                discordClient.OnPresenceUpdate += (sender, e) =>
                {
                    LogInfo($"Discord Presence Updated");
                };

                discordClient.OnError += (sender, e) =>
                {
                    LogError($"Discord RPC Error: {e.Message}");
                    Dispatcher.Invoke(() => UpdateDiscordStatus(false));
                };

                discordClient.Initialize();
                UpdateDiscordPresence("მთავარ მენიუში", "ლაუნჩერის გამოყენება", PlayerNameTextBox.Text);
            }
            catch (Exception ex)
            {
                LogError($"Discord RPC initialization failed: {ex.Message}");
                UpdateDiscordStatus(false);
            }
        }

        private void UpdateDiscordPresence(string state, string details, string playerName = "", string largeImageKey = "hrp_logo", string largeImageText = "HRP - High Quality Roleplay")
        {
            if (!config.EnableDiscordRichPresence || discordClient == null || !discordClient.IsInitialized) return;

            try
            {
                var presence = new RichPresence()
                {
                    Details = string.IsNullOrEmpty(playerName) ? details : $"{details} ({playerName})",
                    State = state,
                    Assets = new Assets()
                    {
                        LargeImageKey = largeImageKey,
                        LargeImageText = largeImageText,
                        SmallImageKey = isGameRunning ? "playing" : "launcher",
                        SmallImageText = isGameRunning ? "თამაშში" : "ლაუნჩერში"
                    },
                    Timestamps = new Timestamps()
                    {
                        Start = launcherStartTime
                    }
                };

                if (isGameRunning)
                {
                    presence.Party = new Party()
                    {
                        ID = "hrp_session",
                        Size = 1,
                        Max = 500
                    };

                    presence.Buttons = new DiscordRPC.Button[]
                    {
                        new DiscordRPC.Button()
                        {
                            Label = "სერვერზე შემოსვლა",
                            Url = $"mtasa://{serverIP}:{serverPort}"
                        }
                    };
                }

                discordClient.SetPresence(presence);
            }
            catch (Exception ex)
            {
                LogError($"Discord presence update failed: {ex.Message}");
            }
        }

        private void UpdateDiscordStatus(bool isConnected)
        {
            DiscordStatusIndicator.Fill = isConnected ? 
                (Brush)FindResource("SuccessBrush") : 
                (Brush)FindResource("ErrorBrush");
            
            DiscordStatusText.Text = isConnected ? 
                "Discord დაკავშირებულია" : 
                "Discord გათიშულია";
        }

        #endregion

        #region Configuration Management

        private class LauncherConfig
        {
            public string LastPlayerName { get; set; } = "";
            public string LastVersion { get; set; } = "0.0.0";
            public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;
            public bool AutoCheckUpdates { get; set; } = true;
            public int AutoCheckInterval { get; set; } = 300000; // 5 minutes
            public bool EnableDiscordRichPresence { get; set; } = true;
            public bool MinimizeToTray { get; set; } = true;
            public bool StartWithWindows { get; set; } = false;
            public bool AutoLaunchGame { get; set; } = false;
            public WindowState LastWindowState { get; set; } = WindowState.Normal;
            public double WindowWidth { get; set; } = 950;
            public double WindowHeight { get; set; } = 650;
        }

        private void LoadConfig()
        {
            try
            {
                string configPath = Path.Combine(gameDirectory, configFile);
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonSerializer.Deserialize<LauncherConfig>(json) ?? new LauncherConfig();
                }
                else
                {
                    config = new LauncherConfig();
                }

                // Apply config
                Width = config.WindowWidth;
                Height = config.WindowHeight;
            }
            catch (Exception ex)
            {
                LogError($"Config load error: {ex.Message}");
                config = new LauncherConfig();
            }
        }

        private void SaveConfig()
        {
            try
            {
                Directory.CreateDirectory(gameDirectory);
                
                // Update config with current window state
                config.WindowWidth = Width;
                config.WindowHeight = Height;
                config.LastWindowState = WindowState;

                string configPath = Path.Combine(gameDirectory, configFile);
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                LogError($"Config save error: {ex.Message}");
            }
        }

        #endregion

        #region Timer Management

        private void SetupTimers()
        {
            // Auto update timer
            autoUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(config.AutoCheckInterval)
            };
            autoUpdateTimer.Tick += async (s, e) =>
            {
                if (!isUpdating && config.AutoCheckUpdates)
                {
                    await CheckVersionAndNews();
                }
            };

            // Server status timer
            serverStatusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            serverStatusTimer.Tick += async (s, e) => await CheckServerStatus();

            // Game monitor timer
            gameMonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            gameMonitorTimer.Tick += (s, e) => CheckGameStatus();

            // Start timers
            autoUpdateTimer.Start();
            serverStatusTimer.Start();
            gameMonitorTimer.Start();
        }

        #endregion

        #region Event Handlers

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Apply animations
                BeginStoryboard(fadeInStoryboard);
                BeginStoryboard(slideInStoryboard);

                // Load player name
                LoadPlayerName();

                // Initial checks
                isAutoCheckingUpdate = true;
                await CheckVersionAndNews();
                await CheckServerStatus();
                isAutoCheckingUpdate = false;

                // Update Discord Presence
                if (config.EnableDiscordRichPresence)
                {
                    UpdateDiscordPresence("მთავარ მენიუში", "ლაუნჩერის გამოყენება", PlayerNameTextBox.Text);
                }

                // Update UI
                UpdateUIState();
                UpdateTitleStatus("მზადაა გამოყენებისთვის", (Brush)FindResource("SuccessBrush"));
            }
            catch (Exception ex)
            {
                LogError($"Window load error: {ex.Message}");
                UpdateStatus($"ჩატვირთვის შეცდომა: {ex.Message}", (Brush)FindResource("ErrorBrush"));
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (config.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
                SystemTrayIcon.Visibility = Visibility.Visible;
                ShowTrayNotification("HRP Launcher", "ლაუნჩერი სისტემურ ტრეიშია მინიმიზებული");
                return;
            }

            Cleanup();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && config.MinimizeToTray)
            {
                Hide();
                SystemTrayIcon.Visibility = Visibility.Visible;
            }
        }

        private void Cleanup()
        {
            try
            {
                SaveConfig();
                discordClient?.Dispose();
                autoUpdateTimer?.Stop();
                serverStatusTimer?.Stop();
                gameMonitorTimer?.Stop();
                httpClient?.Dispose();
            }
            catch (Exception ex)
            {
                LogError($"Cleanup error: {ex.Message}");
            }
        }

        #endregion

        #region Window Controls

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open settings window
            ShowMessage("პარამეტრების ფანჯარა მალე იქნება ხელმისაწვდომი!", "ინფორმაცია", MessageBoxImage.Information);
        }

        #endregion

        #region System Tray

        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            SystemTrayIcon.Visibility = Visibility.Collapsed;
        }

        private void ExitApplication_Click(object sender, RoutedEventArgs e)
        {
            config.MinimizeToTray = false;
            Close();
        }

        private void ShowTrayNotification(string title, string message)
        {
            SystemTrayIcon.ShowBalloonTip(title, message, BalloonIcon.Info);
        }

        #endregion

        #region Version and News Management

        private async Task CheckVersionAndNews()
        {
            try
            {
                if (!isAutoCheckingUpdate)
                {
                    UpdateStatus("ვერსიისა და სიახლეების შემოწმება...", (Brush)FindResource("WarningBrush"));
                }

                // Check version
                await CheckVersion();

                // Load news
                await LoadNews();

                config.LastUpdateCheck = DateTime.Now;
            }
            catch (Exception ex)
            {
                if (!isAutoCheckingUpdate)
                {
                    UpdateStatus($"შემოწმების შეცდომა: {ex.Message}", (Brush)FindResource("ErrorBrush"));
                }
                LogError($"Version/News check failed: {ex.Message}");
            }
        }

        private async Task CheckVersion()
        {
            try
            {
                string serverVersion = await DownloadStringAsync(versionUrl);
                string localVersionPath = Path.Combine(gameDirectory, versionFile);
                string localVersion = config.LastVersion;

                if (File.Exists(localVersionPath))
                {
                    localVersion = await File.ReadAllTextAsync(localVersionPath);
                    localVersion = localVersion.Trim();
                }

                currentVersion = serverVersion.Trim();
                CurrentVersionLabel.Text = $"v{localVersion}";
                LatestVersionLabel.Text = $"v{currentVersion}";

                if (serverVersion.Trim() != localVersion)
                {
                    UpdateStatus("განახლება საჭიროა!", (Brush)FindResource("WarningBrush"));
                    UpdateStatusIndicator(UpdateStatusIndicator, (Brush)FindResource("WarningBrush"));
                    LaunchGameButton.IsEnabled = false;
                    CheckUpdateButton.Content = "🔄 განახლების ჩამოწერა";
                    UpdateTitleStatus("განახლება საჭიროა", (Brush)FindResource("WarningBrush"));
                }
                else
                {
                    UpdateStatus("განახლებულია!", (Brush)FindResource("SuccessBrush"));
                    UpdateStatusIndicator(UpdateStatusIndicator, (Brush)FindResource("SuccessBrush"));
                    LaunchGameButton.IsEnabled = !string.IsNullOrWhiteSpace(PlayerNameTextBox.Text);
                    CheckUpdateButton.Content = "🔄 განახლების შემოწმება";
                    UpdateTitleStatus("მზადაა გამოყენებისთვის", (Brush)FindResource("SuccessBrush"));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"ვერსიის შემოწმება ვერ მოხერხდა: {ex.Message}");
            }
        }

        private async Task LoadNews()
        {
            try
            {
                string newsJson = await DownloadStringAsync(newsUrl);
                var newsItems = JsonSerializer.Deserialize<List<NewsItem>>(newsJson);

                NewsPanel.Children.Clear();

                foreach (var item in newsItems.Take(5))
                {
                    var newsCard = CreateNewsCard(item);
                    NewsPanel.Children.Add(newsCard);
                }
            }
            catch (Exception ex)
            {
                LogError($"News loading failed: {ex.Message}");
                // Keep default news if loading fails
            }
        }

        private Border CreateNewsCard(NewsItem item)
        {
            var border = new Border
            {
                Background = (Brush)FindResource("DarkBackgroundBrush"),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var stackPanel = new StackPanel();

            var titleBlock = new TextBlock
            {
                Text = item.Title,
                Foreground = GetNewsCategoryBrush(item.Category),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };

            var contentBlock = new TextBlock
            {
                Text = item.Content,
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            };

            stackPanel.Children.Add(titleBlock);
            stackPanel.Children.Add(contentBlock);
            border.Child = stackPanel;

            return border;
        }

        private Brush GetNewsCategoryBrush(string category)
        {
            return category?.ToLower() switch
            {
                "update" => (Brush)FindResource("AccentBrush"),
                "server" => (Brush)FindResource("SecondaryBrush"),
                "event" => (Brush)FindResource("PrimaryBrush"),
                _ => (Brush)FindResource("TextPrimaryBrush")
            };
        }

        private class NewsItem
        {
            public string Title { get; set; }
            public string Content { get; set; }
            public string Category { get; set; }
            public DateTime Date { get; set; }
        }

        #endregion

        #region Server Status Management

        private async Task CheckServerStatus()
        {
            try
            {
                bool isOnline = await PingServer();
                int playerCount = await GetPlayerCount();

                if (isOnline)
                {
                    UpdateServerStatus(true, playerCount);
                    UpdatePing();
                }
                else
                {
                    UpdateServerStatus(false, 0);
                }
            }
            catch (Exception ex)
            {
                LogError($"Server status check failed: {ex.Message}");
                UpdateServerStatus(false, 0);
            }
        }

        private async Task<bool> PingServer()
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(serverIP, 5000);
                
                if (reply.Status == IPStatus.Success)
                {
                    PingText.Text = $"Ping: {reply.RoundtripTime}ms";
                    PingText.Foreground = reply.RoundtripTime < 100 ? 
                        (Brush)FindResource("SuccessBrush") : 
                        (Brush)FindResource("WarningBrush");
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<int> GetPlayerCount()
        {
            try
            {
                // This would typically query the MTA server for player count
                // For now, return a mock value
                return new Random().Next(150, 300);
            }
            catch
            {
                return 0;
            }
        }

        private void UpdateServerStatus(bool isOnline, int playerCount)
        {
            if (isOnline)
            {
                UpdateStatusIndicator(ServerStatusIndicator, (Brush)FindResource("SuccessBrush"));
                ServerStatusText.Text = "ონლაინია";
                ServerStatusText.Foreground = (Brush)FindResource("SuccessBrush");
                PlayersOnlineText.Text = $"მოთამაშეები: {playerCount}/500";
            }
            else
            {
                UpdateStatusIndicator(ServerStatusIndicator, (Brush)FindResource("ErrorBrush"));
                ServerStatusText.Text = "ოფლაინია";
                ServerStatusText.Foreground = (Brush)FindResource("ErrorBrush");
                PlayersOnlineText.Text = "მოთამაშეები: 0/500";
                PingText.Text = "Ping: N/A";
                PingText.Foreground = (Brush)FindResource("ErrorBrush");
            }
        }

        private void UpdatePing()
        {
            // Additional ping logic if needed
        }

        #endregion

        #region Game Management

        private void CheckGameStatus()
        {
            try
            {
                var mtaProcesses = Process.GetProcessesByName("Multi Theft Auto");
                bool gameCurrentlyRunning = mtaProcesses.Length > 0;

                if (gameCurrentlyRunning != isGameRunning)
                {
                    isGameRunning = gameCurrentlyRunning;
                    
                    if (config.EnableDiscordRichPresence)
                    {
                        UpdateDiscordPresence(
                            isGameRunning ? "თამაშშია" : "მთავარ მენიუში",
                            isGameRunning ? "თამაშობს HRP-ზე" : "ლაუნჩერის გამოყენება",
                            PlayerNameTextBox.Text
                        );
                    }

                    UpdateTitleStatus(
                        isGameRunning ? "თამაში გაშვებულია" : "მზადაა გამოყენებისთვის",
                        isGameRunning ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("SuccessBrush")
                    );
                }

                // Cleanup process references
                foreach (var process in mtaProcesses)
                {
                    process.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogError($"Game status check failed: {ex.Message}");
            }
        }

        private async void LaunchGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PlayerNameTextBox.Text))
            {
                ShowMessage("გთხოვთ, შეიყვანოთ მოთამაშის სახელი!", "შეცდომა", MessageBoxImage.Warning);
                PlayerNameTextBox.Focus();
                return;
            }

            if (!IsValidPlayerName(PlayerNameTextBox.Text))
            {
                ShowMessage("მოთამაშის სახელი უნდა შეიცავდეს მხოლოდ ასოებს, ციფრებს და ქვედა ხაზებს!", "შეცდომა", MessageBoxImage.Warning);
                PlayerNameTextBox.Focus();
                return;
            }

            try
            {
                UpdateStatus("თამაშის გაშვება...", (Brush)FindResource("WarningBrush"));

                string mtaPath = FindMTAPath();
                if (string.IsNullOrEmpty(mtaPath))
                {
                    ShowMessage("MTA:SA ვერ მოიძებნა! გთხოვთ, დააინსტალიროთ MTA:SA.", "შეცდომა", MessageBoxImage.Error);
                    return;
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = mtaPath,
                    Arguments = $"-c \"{serverIP} {serverPort} {PlayerNameTextBox.Text}\"",
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(mtaPath)
                };

                Process.Start(processInfo);

                SavePlayerName(PlayerNameTextBox.Text);
                UpdateStatus("თამაში წარმატებით გაშვებულია!", (Brush)FindResource("SuccessBrush"));

                if (config.MinimizeToTray)
                {
                    await Task.Delay(2000);
                    WindowState = WindowState.Minimized;
                }

                ShowTrayNotification("HRP Launcher", "თამაში წარმატებით გაშვებულია!");
            }
            catch (Exception ex)
            {
                UpdateStatus($"თამაშის გაშვების შეცდომა: {ex.Message}", (Brush)FindResource("ErrorBrush"));
                ShowMessage($"თამაშის გაშვება ვერ მოხერხდა: {ex.Message}", "შეცდომა", MessageBoxImage.Error);
                LogError($"Game launch failed: {ex.Message}");
            }
        }

        private bool IsValidPlayerName(string name)
        {
            return Regex.IsMatch(name, @"^[a-zA-Z0-9_]{3,25}$");
        }

        private string FindMTAPath()
        {
            string[] possiblePaths = {
                Path.Combine(gameDirectory, "bin", "Multi Theft Auto.exe"),
                Path.Combine(gameDirectory, "Multi Theft Auto.exe"),
                @"C:\Program Files\MTA San Andreas 1.5\Multi Theft Auto.exe",
                @"C:\Program Files (x86)\MTA San Andreas 1.5\Multi Theft Auto.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MTA San Andreas 1.5", "Multi Theft Auto.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MTA San Andreas 1.5", "Multi Theft Auto.exe")
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        #endregion

        #region Download and Update

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (isUpdating) return;

            try
            {
                isUpdating = true;
                UpdateUIState();

                await CheckVersionAndNews();

                if (StatusLabel.Text.Contains("განახლება საჭიროა"))
                {
                    await DownloadAndInstallUpdate();
                }
            }
            finally
            {
                isUpdating = false;
                UpdateUIState();
            }
        }

        private async Task DownloadAndInstallUpdate()
        {
            try
            {
                Directory.CreateDirectory(gameDirectory);

                ShowProgressCard(true);
                DownloadProgressBar.Value = 0;
                DownloadProgressBar.IsIndeterminate = false;

                UpdateStatus("ფაილების ჩამოწერა...", (Brush)FindResource("WarningBrush"));
                ProgressLabel.Text = "ფაილების ჩამოწერა...";

                await DownloadFileWithProgress(gameZipUrl, Path.Combine(gameDirectory, gameZipFile));

                UpdateStatus("ფაილების ინსტალაცია...", (Brush)FindResource("WarningBrush"));
                ProgressLabel.Text = "ფაილების ინსტალაცია...";
                DownloadProgressBar.IsIndeterminate = true;

                await Task.Run(() =>
                {
                    string zipPath = Path.Combine(gameDirectory, gameZipFile);
                    using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                    {
                        archive.ExtractToDirectory(gameDirectory, overwriteFiles: true);
                    }
                });

                await File.WriteAllTextAsync(Path.Combine(gameDirectory, versionFile), currentVersion);
                config.LastVersion = currentVersion;

                UpdateStatus("განახლება წარმატებით დასრულდა!", (Brush)FindResource("SuccessBrush"));
                ProgressLabel.Text = "განახლება წარმატებით დასრულდა!";
                LaunchGameButton.IsEnabled = !string.IsNullOrWhiteSpace(PlayerNameTextBox.Text);
                CheckUpdateButton.Content = "🔄 განახლების შემოწმება";

                File.Delete(Path.Combine(gameDirectory, gameZipFile));

                await Task.Delay(3000);
                ShowProgressCard(false);

                ShowTrayNotification("HRP Launcher", "განახლება წარმატებით დასრულდა!");
            }
            catch (Exception ex)
            {
                UpdateStatus($"განახლების შეცდომა: {ex.Message}", (Brush)FindResource("ErrorBrush"));
                ShowProgressCard(false);
                LogError($"Update failed: {ex.Message}");
            }
        }

        private async Task<string> DownloadStringAsync(string url)
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        private async Task DownloadFileWithProgress(string url, string filePath)
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                long? totalBytes = response.Content.Headers.ContentLength;

                using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    byte[] buffer = new byte[8192];
                    long totalBytesRead = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;

                        if (totalBytes.HasValue)
                        {
                            double percentage = (double)totalBytesRead / totalBytes.Value * 100;

                            Dispatcher.Invoke(() =>
                            {
                                DownloadProgressBar.Value = percentage;
                                ProgressPercentText.Text = $"{percentage:F1}%";
                                ProgressLabel.Text = $"ჩამოწერა: {totalBytesRead / 1024 / 1024:F1} MB / {totalBytes.Value / 1024 / 1024:F1} MB";
                            });
                        }
                    }
                }
            }
        }

        #endregion

        #region Player Management

        private void PlayerNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            bool isValid = !string.IsNullOrWhiteSpace(textBox.Text) && IsValidPlayerName(textBox.Text);
            
            LaunchGameButton.IsEnabled = isValid && !isUpdating && !StatusLabel.Text.Contains("განახლება საჭიროა");
            
            if (!string.IsNullOrWhiteSpace(textBox.Text) && !IsValidPlayerName(textBox.Text))
            {
                textBox.BorderBrush = (Brush)FindResource("ErrorBrush");
            }
            else
            {
                textBox.BorderBrush = (Brush)FindResource("LightBackgroundBrush");
            }
        }

        private void SavePlayerName(string playerName)
        {
            try
            {
                config.LastPlayerName = playerName;
                SaveConfig();
            }
            catch (Exception ex)
            {
                LogError($"Player name save error: {ex.Message}");
            }
        }

        private void LoadPlayerName()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(config.LastPlayerName))
                {
                    PlayerNameTextBox.Text = config.LastPlayerName;
                }
            }
            catch (Exception ex)
            {
                LogError($"Player name load error: {ex.Message}");
            }
        }

        #endregion

        #region Social Media

        private void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://discord.gg/hrp",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LogError($"Discord link failed: {ex.Message}");
            }
        }

        private void WebsiteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://hrp.ge",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LogError($"Website link failed: {ex.Message}");
            }
        }

        #endregion

        #region UI Helpers

        private void UpdateStatus(string message, Brush color)
        {
            StatusLabel.Text = message;
            StatusLabel.Foreground = color;
        }

        private void UpdateTitleStatus(string message, Brush color)
        {
            TitleStatusText.Text = $"• {message}";
            TitleStatusText.Foreground = color;
        }

        private void UpdateStatusIndicator(System.Windows.Shapes.Ellipse indicator, Brush color)
        {
            indicator.Fill = color;
        }

        private void ShowProgressCard(bool show)
        {
            ProgressCard.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            
            if (!show)
            {
                DownloadProgressBar.Value = 0;
                DownloadProgressBar.IsIndeterminate = false;
                ProgressLabel.Text = "";
                ProgressPercentText.Text = "0%";
            }
        }

        private void UpdateUIState()
        {
            CheckUpdateButton.IsEnabled = !isUpdating;
            LaunchGameButton.IsEnabled = !isUpdating && 
                                       !StatusLabel.Text.Contains("განახლება საჭიროა") && 
                                       !string.IsNullOrWhiteSpace(PlayerNameTextBox.Text) &&
                                       IsValidPlayerName(PlayerNameTextBox.Text);
            PlayerNameTextBox.IsEnabled = !isUpdating;
        }

        private void ShowMessage(string message, string title, MessageBoxImage icon)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        #endregion

        #region Logging

        private void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        private void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        #endregion
    }
}