using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SecretKeyUI
{
    public sealed partial class MainWindow : Window
    {
        // Absolute isolated engine pointer location within the deployment package
        private readonly string exePath;
        private string CurrentWorkspaceDir = string.Empty;

        public MainWindow()
        {
            this.InitializeComponent();

            // Safely assign the execution engine path inside the constructor
            exePath = Path.Combine(AppContext.BaseDirectory, "secretkey.exe");

            // Set a default workspace safety fall-back path
            CurrentWorkspaceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "secretkey");

            // Hook into the Activated event to force the correct window sizing on launch
            this.Activated += MainWindow_Activated;
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            // Unhook the event immediately so this only fires once on startup
            this.Activated -= MainWindow_Activated;

            // Get the native window handle (HWND)
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                // Force the window to launch at 700 pixels wide by 850 pixels tall
                // This gives your vertical stack plenty of breathing room to show the buttons
                appWindow.Resize(new Windows.Graphics.SizeInt32(700, 850));
            }
        }

        // Helper to dynamically shift the Save Config button color based on data integrity
        private void MarkConfigValid(bool isValid)
        {
            if (isValid)
            {
                // Turn the button a clear, dark forest green
                BtnSaveConfig.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 139, 34));
                BtnSaveConfig.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
            }
            else
            {
                // Reset to standard system theme brush
                BtnSaveConfig.ClearValue(Button.BackgroundProperty);
                BtnSaveConfig.ClearValue(Button.ForegroundProperty);
            }
        }

        // BUTTON 1: Manually commit current UI screen data to config.json
        private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CurrentWorkspaceDir))
            {
                UpdateStatus("Error: Choose a Workspace Directory first before trying to commit a configuration.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                return;
            }

            try
            {
                var configData = new SecretKeyConfig
                {
                    InputPath = TxtInputPath.Text ?? string.Empty,
                    OutputPath = TxtOutputPath.Text ?? string.Empty,
                    PasswordMask = TxtMask.Text ?? string.Empty,
                    RootKey = TxtRootKey.Text ?? string.Empty
                };

                string targetConfig = Path.Combine(CurrentWorkspaceDir, "config.json");
                string updatedJson = JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(targetConfig, updatedJson);

                UpdateStatus($"Success: config.json updated inside {CurrentWorkspaceDir}.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);

                // Config is written cleanly, flip the button state to green
                MarkConfigValid(true);
            }
            catch (Exception ex)
            {
                MarkConfigValid(false);
                UpdateStatus($"Write Failure: {ex.Message}", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
            }
        }

        // BUTTON 2: Spawns secretkey.exe directly using OS shell execution. No save required.
        private void BtnRunEngine_Click(object sender, RoutedEventArgs e)
        {
            string dateCode = TxtDateCode.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dateCode) || dateCode.Length != 4)
            {
                UpdateStatus("Validation Failure: Active Runtime Date Code parameter must be exactly 4 digits.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentWorkspaceDir) || !Directory.Exists(CurrentWorkspaceDir))
            {
                UpdateStatus("Error: Choose a valid Workspace Directory first before running the engine.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                return;
            }

            // If external exe is missing, continue with in-process call but notify the user
            if (!File.Exists(exePath))
            {
                UpdateStatus($"Note: external engine executable not found at {exePath}. Running in-process using SecretKey.Core.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational);
            }

            string targetConfig = Path.Combine(CurrentWorkspaceDir, "config.json");
            if (!File.Exists(targetConfig))
            {
                UpdateStatus($"Configuration missing: {targetConfig}. Create or save a config.json in the workspace first.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                return;
            }

            try
            {
                string jsonString = File.ReadAllText(targetConfig);
                var existingConfig = JsonSerializer.Deserialize<SecretKeyConfig>(jsonString);

                if (existingConfig == null)
                {
                    UpdateStatus("Config parse error: config.json deserialized to null.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(existingConfig.InputPath) || !File.Exists(existingConfig.InputPath))
                {
                    UpdateStatus($"Input file not found or not specified: {existingConfig?.InputPath}", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(existingConfig.OutputPath))
                {
                    UpdateStatus("Output path is empty in config.json.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                    return;
                }

                try
                {
                    var outDir = Path.GetDirectoryName(existingConfig.OutputPath);
                    if (!string.IsNullOrWhiteSpace(outDir) && !Directory.Exists(outDir))
                    {
                        Directory.CreateDirectory(outDir);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    UpdateStatus($"Unable to prepare output directory: {ex.Message}", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                    return;
                }

                UpdateStatus("Starting in-process engine run...", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational);

                try
                {
                    var masterKey = SecretKey.Core.Crypto.DeriveMonthlyMasterKey(existingConfig.RootKey ?? string.Empty, dateCode);
                    SecretKey.Core.Processor.Process(existingConfig.InputPath, existingConfig.OutputPath, masterKey, existingConfig.PasswordMask ?? "XxxxxNSxxxNN");

                    UpdateStatus($"Engine run completed. Output written to {existingConfig.OutputPath}", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    UpdateStatus($"Engine run failed: {ex.Message}. See debug output for details.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                }
            }
            catch (JsonException jex)
            {
                Debug.WriteLine(jex.ToString());
                UpdateStatus($"Configuration JSON parse error: {jex.Message}", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                UpdateStatus($"Unexpected error while preparing engine run: {ex.Message}", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
            }
        }

        // BUTTON 3: Permanent explicit application window shutdown control
        private void BtnCloseApp_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Handle workspace directory selection and automatically load existing config.json properties
        private async void BtnBrowseWorkspace_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                CurrentWorkspaceDir = folder.Path;
                TxtWorkspacePath.Text = folder.Path;

                string targetConfigFile = Path.Combine(CurrentWorkspaceDir, "config.json");

                if (File.Exists(targetConfigFile))
                {
                    try
                    {
                        string jsonString = File.ReadAllText(targetConfigFile);
                        var existingConfig = JsonSerializer.Deserialize<SecretKeyConfig>(jsonString);

                        if (existingConfig != null)
                        {
                            TxtInputPath.Text = existingConfig.InputPath ?? string.Empty;
                            TxtOutputPath.Text = existingConfig.OutputPath ?? string.Empty;
                            TxtMask.Text = existingConfig.PasswordMask ?? string.Empty;
                            TxtRootKey.Text = existingConfig.RootKey ?? string.Empty;

                            UpdateStatus($"Workspace loaded. Existing configuration parsed successfully.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);

                            // Found an existing file that parsed perfectly on folder change, light it green immediately
                            MarkConfigValid(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        MarkConfigValid(false);
                        UpdateStatus($"Workspace set, but existing config.json structure is unreadable: {ex.Message}", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                    }
                }
                else
                {
                    // No config exists yet in this specific target folder, reset button look back to standard
                    MarkConfigValid(false);
                    UpdateStatus($"Workspace folder established. No config.json found; ready for layout initialization.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational);
                }
            }
        }

        // Handle input source 1Password export file selection
        private async void BtnBrowseInput_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            picker.FileTypeFilter.Add(".csv");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                TxtInputPath.Text = file.Path;
            }
        }

        // Handle target output vault file path destination configuration
        private async void BtnBrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("CSV File", new System.Collections.Generic.List<string>() { ".csv" });
            picker.SuggestedFileName = "SecretKey_Vault_Export";

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                TxtOutputPath.Text = file.Path;
            }
        }

        // Generate a cryptographically strong root key sequence for the state machine seed
        private void BtnGenerateRootKey_Click(object sender, RoutedEventArgs e)
        {
            byte[] randomBytes = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            TxtRootKey.Text = Convert.ToHexString(randomBytes);
            UpdateStatus("Fresh 256-bit cryptographic root key sequence generated natively.", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);
        }

        // Helper method to safely update the InfoBar status display banner on the UI
        private void UpdateStatus(string message, Microsoft.UI.Xaml.Controls.InfoBarSeverity severity)
        {
            StatusDisplay.Message = message;
            StatusDisplay.Severity = severity;
        }
    }

    // Data contract matching the structure written to your workspace config.json file
    public class SecretKeyConfig
    {
        public string InputPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public string PasswordMask { get; set; } = string.Empty;
        public string RootKey { get; set; } = string.Empty;
    }
}