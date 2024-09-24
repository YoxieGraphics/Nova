using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace yt_dlp_GUI
{
    public partial class MainWindow : Window
    {
        private string downloadFolder = string.Empty;
        private string configFilePath = "appsettings.json";

        public MainWindow()
        {
            InitializeComponent();
            LoadDownloadFolder();
        }

        // Load the download folder from the JSON config file
        private void LoadDownloadFolder()
        {
            if (File.Exists(configFilePath))
            {
                var config = new ConfigurationBuilder()
                    .AddJsonFile(configFilePath)
                    .Build();

                downloadFolder = config["DownloadFolder"];

                if (!string.IsNullOrEmpty(downloadFolder))
                {
                    downloadFolderTextBlock.Text = $"Download Location: {downloadFolder}";
                }
                else
                {
                    downloadFolderTextBlock.Text = "Download Location: Not selected";
                }
            }
        }

        // Save the download folder to the JSON config file
        private void SaveDownloadFolder(string folderPath)
        {
            var json = JsonSerializer.Serialize(new { DownloadFolder = folderPath });
            File.WriteAllText(configFilePath, json);
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                downloadFolder = dialog.SelectedPath;
                downloadFolderTextBlock.Text = $"Download Location: {downloadFolder}";

                // Save the chosen folder path to appsettings.json
                SaveDownloadFolder(downloadFolder);
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            string videoUrl = videoUrlTextBox.Text;

            if (string.IsNullOrEmpty(videoUrl))
            {
                var messageBox = new CustomMessageBox("Please enter a video URL.", "Input Error");
                messageBox.ShowDialog();
                return;
            }

            if (string.IsNullOrEmpty(downloadFolder))
            {
                var messageBox = new CustomMessageBox("Please select a download location.", "Input Error");
                messageBox.ShowDialog();
                return;
            }

            string format = videoRadioButton.IsChecked == true ? "-f best" : "-x --audio-format mp3";

            // Set the status to "Downloading" and run the download process asynchronously
            statusTextBlock.Text = "Status: Downloading...";
            await DownloadVideoAsync(videoUrl, format);
        }

        private async Task DownloadVideoAsync(string videoUrl, string format)
        {
            await Task.Run(() =>
            {
                try
                {
                    string outputFilePath = Path.Combine(downloadFolder, "%(title)s.%(ext)s");

                    ProcessStartInfo processInfo = new ProcessStartInfo
                    {
                        FileName = "yt-dlp",
                        Arguments = $" {format} -o \"{outputFilePath}\" {videoUrl}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Process process = new Process
                    {
                        StartInfo = processInfo
                    };

                    process.OutputDataReceived += (sender, args) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (args.Data != null)
                            {
                                statusTextBlock.Text = $"Status: {args.Data}";
                            }
                        });
                    };

                    process.ErrorDataReceived += (sender, args) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (args.Data != null)
                            {
                                statusTextBlock.Text = $"Error: {args.Data}";
                            }
                        });
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    Dispatcher.Invoke(() =>
                    {
                        statusTextBlock.Text = "Status: Download Complete";
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        var messageBox = new CustomMessageBox($"An error occurred: {ex.Message}", "Error");
                        messageBox.ShowDialog();
                        statusTextBlock.Text = "Status: Error occurred";
                    });
                }
            });
        }

        private void videoRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // Handle any logic when videoRadioButton is checked, if needed
        }
    }
}
