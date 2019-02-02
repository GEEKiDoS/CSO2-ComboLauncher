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

namespace CSO2_ComboLauncher
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Log = new Logger { LogDisplay = logger };
        }

        Logger Log;
        string currentLang = "";

        private class Logger
        {
            public RichTextBox LogDisplay;

            public void Error(string text)
            {
                ((Paragraph)(LogDisplay.Document.Blocks.First())).Inlines.Add(new Run { Text = $"{text}\r\n", Foreground = Brushes.Red });
            }

            public void Write(string text)
            {
                ((Paragraph)(LogDisplay.Document.Blocks.First())).Inlines.Add(new Run { Text = $"{text}\r\n", Foreground = Brushes.Black });
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Init language list
            languageList.Items.Add("简体中文 - Chinese(Simplified)");
            languageList.Items.Add("繁體中文 - Chinese(Traditional)");
            languageList.Items.Add("日本語 - Japanese");
            languageList.Items.Add("한국어 - Koeran");

            languageList.SelectedItem = "한국어 - Koeran";
        }

        private void LanguageList_SelectionChangedAsync(object sender, SelectionChangedEventArgs e)
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
            }

            if (CheckLangExists(langfile))
            {
                try
                {
                    Log.Write($"File of {languageList.SelectedItem} not exists, Downloading from github...");
                    var downloader = new WebClient();
                    downloader.DownloadDataAsync(new Uri($"https://raw.githubusercontent.com/GEEKiDoS/CSO2-ComboLauncher/langfiles/{langfile}.zip"));
                    downloader.DownloadDataCompleted += (se, ee) =>
                    {
                        Log.Write("Language file downloaded, Extracting...");
                        using (var file = new ZipArchive(new MemoryStream(ee.Result)))
                        {
                            try
                            {
                                file.ExtractToDirectory("Data");
                                Log.Write("Done!");
                                currentLang = langfile;
                            }
                            catch(Exception ex)
                            {
                                Log.Error($"Extract language file error: {ex.Message}");
                            }
                            
                        }
                    };
                }
                catch (Exception ex)
                {
                    Log.Error($"Download language file error: {ex.Message}");
                }
            }

        }


        private bool CheckLangExists(string langfile)
        {
            throw new NotImplementedException();
        }
    }
}
