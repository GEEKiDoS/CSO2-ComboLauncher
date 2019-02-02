using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Compression;
using System.IO;
using System.Diagnostics;
using Path = System.IO.Path;
using System.Text.RegularExpressions;
using System.Management;
using System.Globalization;

namespace CSO2_ComboLauncher
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        string localLang = "english";

        public MainWindow()
        {
            LocalizedStrings.InitLocalizedStrings();
            InitializeComponent();

            Log = new Logger { LogDisplay = logger };
        }

        private void LocalifyControl(UIElementCollection uiControls)
        {
            foreach (UIElement element in uiControls)
            {
                if (element is TextBlock)
                {
                    var textBlock = element as TextBlock;
                    textBlock.Text = LocalizedStrings.GetLocalizedString(textBlock.Text ,localLang);
                }
                else if(element is CheckBox)
                {
                    var textBlock = element as CheckBox;
                    textBlock.Content = LocalizedStrings.GetLocalizedString((textBlock.Content as string), localLang);
                }
                else if (element is Grid)
                {
                    LocalifyControl((element as Grid).Children);
                }
                else if (element is GroupBox)
                {
                    var groupBox = element as GroupBox;
                    groupBox.Header = LocalizedStrings.GetLocalizedString((groupBox.Header as string), localLang);
                    LocalifyControl(((element as GroupBox).Content as Grid).Children);
                }
            }
        }

        Logger Log;
        string currentLang = "";

        private class Logger
        {
            public RichTextBox LogDisplay;

            public void Error(string text)
            {
                LogDisplay.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ((Paragraph)(LogDisplay.Document.Blocks.First())).Inlines.Add(new Run { Text = $"{text}\r\n", Foreground = Brushes.Red });
                    LogDisplay.ScrollToEnd();
                }));
            }

            public void Write(string text)
            {
                LogDisplay.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ((Paragraph)(LogDisplay.Document.Blocks.First())).Inlines.Add(new Run { Text = $"{text}\r\n", Foreground = Brushes.Black });
                    LogDisplay.ScrollToEnd();
                }));
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists("Bin\\engine.dll"))
            {
                MessageBox.Show("Please place this launcher in the cso2 root folder!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Init language list
            languageList.Items.Add("简体中文 - Chinese(Simplified)");
            /* I dont have these language files
            languageList.Items.Add("繁體中文 - Chinese(Traditional)");
            languageList.Items.Add("日本語 - Japanese");
            */
            languageList.Items.Add("한국어 - Koeran");

            languageList.SelectedItem = "한국어 - Koeran";

            switch (System.Threading.Thread.CurrentThread.CurrentCulture.Name)
            {
                case "zh-CN":
                case "zh-Hans":
                case "zh-SG":
                    this.localLang = "schinese";
                    languageList.SelectedItem = "简体中文 - Chinese(Simplified)";
                    break;
                case "zh-TW":
                case "zh-MO":
                case "zh-HK":
                case "zh-Hant":
                    this.localLang = "tchinese";
                    break;
            }

            LocalifyControl(mainGrid.Children);
        }

        private void LanguageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var langfile = "";

            switch (languageList.SelectedItem)
            {
                case "简体中文 - Chinese(Simplified)":
                    langfile = "schinese";
                    break;
                /*
            case "繁體中文 - Chinese(Traditional)":
                langfile = "tchinese";
                break;
            case "日本語 - Japanese":
                langfile = "japanese";
                break;
                */
                default:
                    return;
            }

            if (!CheckLangExists(langfile))
            {

                Log.Write($"File of {languageList.SelectedItem} not exists, Downloading from github...\r\n");
                using (var downloader = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    downloader.DownloadDataAsync(new Uri($"https://raw.githubusercontent.com/GEEKiDoS/CSO2-ComboLauncher/master/{langfile}.zip"));
                    downloader.DownloadDataCompleted += (se, ee) =>
                    {

                        try
                        {
                            using (var file = new ZipArchive(new MemoryStream(ee.Result)))
                            {
                                try
                                {
                                    Log.Write("Language file downloaded, Extracting...\r\n");
                                    file.ExtractToDirectory("Data");
                                    Log.Write("Done!");
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Extract language file error: {ex.Message}\r\n");
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Download language file error: {ex.Message}\r\n");
                        }
                    };
                }

            }
            currentLang = langfile;

        }

        private bool CheckLangExists(string langfile)
        {
            if (File.Exists($"Data\\cstrike\\resource\\cso2_{langfile}.txt"))
                return true;

            return false;
        }

        private void IsHost_Click(object sender, RoutedEventArgs e)
        {
            if (isHost.IsChecked == true)
            {
                autoDetect.IsEnabled = true;
                holypunchPort.IsEnabled = true;
                if (!IsNodeJsInstalled)
                {
                    MessageBox.Show("Node.JS is not installed, \r\nYou can't start the master server until you installed the Node.JS", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning); ;
                }
            }
            else
            {
                autoDetect.IsEnabled = false;
                holypunchPort.IsEnabled = false;
            }
        }

        public static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(';'))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }

        private bool IsNodeJsInstalled { get { return GetFullPath("node.exe") != null; } }

        Process CurrentNodeJsProcess = null;

        private Process StartNPMProcess(string command, string workingDir = null, DataReceivedEventHandler customHandler = null)
        {
            CurrentNodeJsProcess?.WaitForExit();

            var nodePath = GetFullPath("node.exe");
            if (nodePath != null)
            {
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = nodePath,
                        Arguments = $"\"{GetFullPath("node_modules\\npm\\bin\\npm-cli.js")}\" {command}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        StandardErrorEncoding = Encoding.UTF8,
                        StandardOutputEncoding = Encoding.UTF8,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WorkingDirectory = workingDir != null ? workingDir : Environment.CurrentDirectory
                    },


                };

                p.OutputDataReceived += customHandler != null ? customHandler : (se, ee) =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() => Log.Write(ee.Data)));
                };

                p.ErrorDataReceived += (se, ee) =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() => Log.Error(ee.Data)));
                };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                CurrentNodeJsProcess = p;

                p.Exited += (se, ee) =>
                {
                    CurrentNodeJsProcess = null;
                };

                return p;
            }
            else
            {
                throw new Exception("Node.js is not installed!");
            }
        }

        private void StartMasterServer(string ip = null, string masterport = null, string holypunchPort = null, DataReceivedEventHandler customHandler = null)
        {
            if (File.Exists("server\\package.json"))
            {
                if (!Directory.Exists("server\\dist"))
                {
                    Log.Write("Master server is source code, building... \r\nInstalling dev dependencies...");
                    StartNPMProcess("install", Path.Combine(Environment.CurrentDirectory, "server")).WaitForExit();
                    Log.Write("Building...");
                    StartNPMProcess("run build", Path.Combine(Environment.CurrentDirectory, "server")).WaitForExit();
                }

                Log.Write("Checking required dependencies...");
                StartNPMProcess("install --only=production", Path.Combine(Environment.CurrentDirectory, "server")).WaitForExit();
                Log.Write("Start server...");
                var arg = "";
                arg += (ip != null ? $"-i {ip}" : "");
                arg += (ip != null ? $"-p {masterport}" : "");
                arg += (ip != null ? $"-P {holypunchPort}" : "");

                StartNPMProcess($"run start --{arg}", Path.Combine(Environment.CurrentDirectory, "server"), customHandler);
            }
            else
            {
                Log.Write($"Master Server not exists, Downloading from github...\r\n");
                using (var downloader = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    try
                    {
                        var data = downloader.DownloadData("https://github.com/Ochii/cso2-master-server/archive/master.zip");
                        using (var file = new ZipArchive(new MemoryStream(data)))
                        {
                            try
                            {
                                Log.Write("Master Server downloaded, Extracting...\r\n");
                                file.ExtractToDirectory(".");
                                if (Directory.Exists("server"))
                                    Directory.Delete("server");

                                Directory.Move("cso2-master-server-master", "server");

                                Log.Write("Done!");

                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Extract Master Server error: {ex.Message}\r\n");
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Download Master Server error: {ex.Message}\r\n");
                    }
                }

                StartMasterServer(ip, masterport, holypunchPort);
            }
        }

        private async void BtnGo_Click(object sender, RoutedEventArgs e)
        {
            Match ip = null;

            if (isHost.IsChecked == true)
                if (IsNodeJsInstalled)
                {
                    var serverAdd = autoDetect.IsChecked == true ? null : serverAddr.Text;
                    var mPort = masterPort.Text;
                    var hPort = holypunchPort.Text;


                    if (autoDetect.IsChecked == true)
                    {
                        var regex = new Regex("(((\\d{1,2})|(1\\d{2})|(2[0-4]\\d)|(25[0-5]))\\.){3}((\\d{1,2})|(1\\d{2})|(2[0-4]\\d)|(25[0-5]))");
                        await Task.Run(() => StartMasterServer(serverAdd, mPort, hPort, (se, ee) =>
                        {
                            Log.Write(ee.Data);

                            if (regex != null)
                                if (regex.Matches(ee.Data).Count > 0)
                                {
                                    ip = regex.Match(ee.Data);
                                    serverAddr.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        serverAddr.Text = ip.Value;
                                        regex = null;
                                    }));
                                }
                                else return;
                        }));
                    }
                    else await Task.Run(() => StartMasterServer(serverAdd, mPort, hPort));
                }
                else
                {
                    Log.Error("Node.Js is not installed, please download(https://nodejs.org/en/) and install nodejs before you want to be a host.");
                    return;
                }

            if (!File.Exists("Bin\\launcher.exe"))
            {
                await Task.Run(() =>
                {
                    Log.Write("CSO2 Launcher not exists, Downloading from github...\r\n");

                    try
                    {
                        using (var downloader = new WebClient())
                        {
                            Log.Write("Getting download link...\r\n");
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                            var relPage = downloader.DownloadString("https://github.com/Ochii/cso2-launcher/releases");
                            var regex = new Regex("(?<=href=\").+?(?=\")|(?<=href=\').+?(?=\')");
                            var links = regex.Matches(relPage);
                            var downloadLink = "";
                            foreach (Match link in links)
                            {
                                if (link.Value.EndsWith("binaries.zip"))
                                {
                                    downloadLink = "https://github.com/" + link.Value;

                                    break;
                                }
                            }

                            if (downloadLink != "")
                            {
                                Log.Write($"Got download link: {downloadLink} , Downloading...\r\n");

                                try
                                {
                                    var data = downloader.DownloadData(downloadLink);
                                    using (var file = new ZipArchive(new MemoryStream(data)))
                                    {
                                        try
                                        {
                                            Log.Write("CSO2 Launcher downloaded, Extracting...\r\n");
                                            file.ExtractToDirectory("Bin");

                                            Log.Write("Done!");
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error($"Extract Master Server error: {ex.Message}\r\n");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Download CSO2 Launcher error: {ex.Message}\r\n");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Getting CSO2 Launcher download link error: {ex.Message}\r\n");
                    }
                });

            }

            if (autoDetect.IsChecked == true)
                for(; ; )
                {
                    await Task.Delay(100);
                    if (ip != null)
                    {
                        await Task.Delay(100);
                        break;
                    }
                }

            var arg = $"-masterip {serverAddr.Text} -masterport {masterPort.Text}";
            arg += currentLang != "" ? $" -lang {currentLang}" : "";

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "Bin\\launcher.exe",
                    Arguments = arg,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = Path.Combine(Environment.CurrentDirectory, "Bin")
                },


            };

            p.OutputDataReceived += (se, ee) =>
            {
                try
                {
                    if (ee.Data.Contains("Error 12002 has occurred."))
                    {
                        p.Close();
                    }
                }
                catch { }
            };

            p.Start();
            p.BeginOutputReadLine();
        }


        // from https://www.codeproject.com/KB/shell/ManageProcessShellAPI.aspx?fid=275098&df=90&mpp=25&noise=3&sort=Position&view=Quick&select=2638740#xx2638740xx
        private static bool killProcess(int pid)
        {
            Process[] procs = Process.GetProcesses();
            for (int i = 0; i < procs.Length; i++)
            {
                if (getParentProcess(procs[i].Id) == pid)
                    killProcess(procs[i].Id);
            }

            try
            {
                Process myProc = Process.GetProcessById(pid);
                myProc.Kill();
            }
            // process already quited
            catch (ArgumentException)
            {
                ;
            }

            return true;
        }

        private static int getParentProcess(int Id)
        {
            int parentPid = 0;
            using (ManagementObject mo = new ManagementObject("win32_process.handle='" + Id.ToString(CultureInfo.InvariantCulture) + "'"))
            {
                try
                {
                    mo.Get();
                }
                catch (ManagementException)
                {
                    return -1;
                }
                parentPid = Convert.ToInt32(mo["ParentProcessId"], CultureInfo.InvariantCulture);
            }
            return parentPid;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CurrentNodeJsProcess != null)
            {
                Log.Write("Killing running master server...\r\n");
                killProcess(CurrentNodeJsProcess.Id);
            }
        }
    }
}
