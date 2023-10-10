using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using IWshRuntimeLibrary;
using Microsoft.WindowsAPICodePack.Dialogs;
using Octokit;
using FileMode = System.IO.FileMode;

namespace AutobotSetup;

public partial class SetupWindow
{
    private readonly string _appDataPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Autobot";
    private string _installationPath;
    
    public SetupWindow()
    {
        InitializeComponent();

        InstallPathTxtBx.Text = _appDataPath;
        _installationPath = _appDataPath;
    }

    private void BrowseBtn_OnClick(object sender, RoutedEventArgs e)
    {
        using var folderBrowserDialog = new CommonOpenFileDialog();
        
        folderBrowserDialog.IsFolderPicker = true;
        folderBrowserDialog.Title = "Choose Autobot Installation Folder";
        folderBrowserDialog.Multiselect = false;

        if (folderBrowserDialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            
        var selectedInstallPath = folderBrowserDialog.FileName;

        if (selectedInstallPath == null) return;
        
        InstallPathTxtBx.Text = selectedInstallPath;
        _installationPath = selectedInstallPath;
    }

    private void ContinueBtn_OnClick(object sender, RoutedEventArgs e)
    {
        InstallPathGrid.Visibility = Visibility.Collapsed;
        DownloadGrid.Visibility = Visibility.Visible;
        
        new Task(DownloadLatest).Start();
    }

    private async void DownloadLatest()
    {
        var gitHubClient = new GitHubClient(new ProductHeaderValue("connor-davis"));
        var releases = await gitHubClient.Repository.Release.GetAll("connor-davis", "Autobotv4");
        var latestGithubVersion = new Version(releases[0].TagName.Replace("v", ""));
        var downloadUrl = $"https://github.com/connor-davis/Autobotv4/releases/download/v{latestGithubVersion}/Autobotv4.zip";
        
        using var httpClient = new HttpClient();

        try
        {
            await using var fileStream = new FileStream("Autobot.zip", FileMode.Create, FileAccess.Write, FileShare.None);
            
            // Send an HTTP GET request to the URL and get the response
            using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            
            // Check if the response is successful
            response.EnsureSuccessStatusCode();

            // Get the content length (file size) from the response headers
            var totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault();

            // Create a buffer for downloading data in chunks
            var buffer = new byte[8192];
            long bytesRead = 0;

            // Create a stream to read the response content
            await using var contentStream = await response.Content.ReadAsStreamAsync();
            int bytesReadThisChunk;
            
            while ((bytesReadThisChunk = await contentStream.ReadAsync(buffer)) > 0)
            {
                // Write the downloaded data to the file
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesReadThisChunk));

                // Update the progress bar and status text
                bytesRead += bytesReadThisChunk;
                
                var progress = (double)bytesRead / totalBytes * 100;

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = progress;
                    DownloadStatus.Content = $"Downloading... {progress:F2}%";
                });
            }
            
            fileStream.Close();

            Dispatcher.Invoke(() =>
            {
                DownloadStatus.Content = "Download Finished.";

                Thread.Sleep(1000);
                
                DownloadGrid.Visibility = Visibility.Collapsed;
                InstallGrid.Visibility = Visibility.Visible;
                
                new Task(Install).Start();
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void Install()
    {
        try
        {
            if (!Directory.Exists(_installationPath)) Directory.CreateDirectory(_installationPath);
            
            ZipFile.ExtractToDirectory("Autobot.zip", _installationPath, true);
            
            Dispatcher.Invoke(() =>
            {
                InstallStatus.Content = "Installation Complete.";
                
                Thread.Sleep(1000);

                InstallGrid.Visibility = Visibility.Collapsed;
                InstallCompleteGrid.Visibility = Visibility.Visible;

                // Create a desktop shortcut
                CreateDesktopShortcut("Autobot", $"{_installationPath}\\Autobot.exe");

                // Create a Start Menu shortcut
                CreateStartMenuShortcut("Autobot", $"{_installationPath}\\Autobot.exe");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void LaunchBtn_OnClick(object sender, RoutedEventArgs e)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = $"{_installationPath}\\Autobot.exe",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process.Start(startInfo);
        
        Environment.Exit(0);
    }

    private static void CreateDesktopShortcut(string shortcutName, string targetPath)
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var shortcutFilePath = Path.Combine(desktopPath, $"{shortcutName}.lnk");
        
        CreateShortcut(shortcutFilePath, targetPath);
    }

    private static void CreateStartMenuShortcut(string shortcutName, string targetPath)
    {
        var startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
        var shortcutFilePath = Path.Combine(startMenuPath, $"{shortcutName}.lnk");
        
        CreateShortcut(shortcutFilePath, targetPath);
    }

    private static void CreateShortcut(string shortcutFilePath, string targetPath)
    {
        // Create a shortcut object
        var shell = new WshShell();
        var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutFilePath);
        
        // Set the target application path
        shortcut.TargetPath = targetPath;
        
        // Save the shortcut
        shortcut.Save();
    }
}