using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;

namespace Arcade.Pages
{
    /// <summary>
    /// Interaction logic for GameBrowser.xaml
    /// </summary>
    public partial class GameBrowser : Page
    {
        public Console Console
        {
            get { return (Console)GetValue(ConsoleProperty); }
            set { SetValue(ConsoleProperty, value); }
        }

        public static readonly DependencyProperty ConsoleProperty =
            DependencyProperty.Register("Console", typeof(Console), typeof(ConsoleBrowser), new PropertyMetadata(null));


        public ObservableCollection<Game> Games
        {
            get { return (ObservableCollection<Game>)GetValue(GamesProperty); }
            set { SetValue(GamesProperty, value); }
        }

        public static readonly DependencyProperty GamesProperty =
            DependencyProperty.Register("Games", typeof(ObservableCollection<Game>), typeof(ConsoleBrowser), new PropertyMetadata(null));

        public GameBrowser(Console inConsole)
        {
            InitializeComponent();

            Console = inConsole;
            Games = new ObservableCollection<Game>();
            DataContext = this;

            ReloadGames();

            if (Games.Count > 0)
                ctGamesListBox.SelectedItem = Games.First();

            ctGamesListBox.Focus();
            ctGamesListBox.KeyDown += ctGamesListBox_KeyDown;
        }

        void ctGamesListBox_KeyDown(object sender, KeyEventArgs e)
        {
            String tempPath = null;

            if (e.Key == Key.Enter && ctGamesListBox.SelectedItem != null)
            {
                try
                {
                    var game = (Game)ctGamesListBox.SelectedItem;
                    var path = game.FullPath;
                    var configPath = Path.Combine(Console.FullPath, "!config.txt");

                    if (!File.Exists(configPath))
                    {
                        MessageBox.Show(String.Format("Configuration file not found for this directory. Expected to find one at '{0}'.", configPath), "Missing Configuration");
                        return;
                    }

                    if (path.EndsWith(".7z"))
                    {
                        tempPath = Path.Combine(Path.GetTempPath(), "Arcade", Guid.NewGuid().ToString());
                        Directory.CreateDirectory(tempPath);

                        Decompress(path, tempPath);

                        path = PickBestRom(tempPath);

                        if (String.IsNullOrWhiteSpace(path))
                        {
                            MessageBox.Show("Couldn't find any ROM files in the extracted directory.");
                            return;
                        }
                    }

                    var config = File.ReadAllLines(configPath);
                    var command = config[0];
                    var args = String.Format(config[1], path);

                    var processStartInfo = new ProcessStartInfo();
                    processStartInfo.FileName = command;
                    processStartInfo.Arguments = args;

                    var process = Process.Start(processStartInfo);
                    App.CurrentlyRunningEmulator = process;
                    process.WaitForExit();
                    App.CurrentlyRunningEmulator = null;
                }
                finally
                {
                    if (!String.IsNullOrWhiteSpace(tempPath))
                        try { Directory.Delete(tempPath, true); }
                        catch { }
                }
            }
        }

        private String PickBestRom(String inPath)
        {
            return Directory.EnumerateFiles(inPath).OrderByDescending(X => GetRomScore(X)).FirstOrDefault();
        }

        private Int32 GetRomScore(String inPath)
        {
            var score = 0;
            var fileName = Path.GetFileNameWithoutExtension(inPath);

            if (fileName.Contains("(U)") || fileName.Contains("(UE)") || fileName.Contains("(JUE)") || fileName.Contains("(UK)"))
                score += 1000;

            if (fileName.Contains("(E)") || fileName.Contains("(JE)"))
                score += 500;

            if (fileName.Contains("(J)"))
                score -= 500;

            if (fileName.Contains("[!]"))
                score += 100;

            if (fileName.Contains("[b"))
                score -= 1000;

            if (fileName.Contains("[h"))
                score -= 1000;

            if (fileName.Contains("[a"))
                score -= 1000;

            return score;
        }

        private void Decompress(String path, String tempPath)
        {
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.CreateNoWindow = true;
            processStartInfo.FileName = "7za.exe";
            processStartInfo.Arguments = "x -o\"" + tempPath + "\" \"" + path + "\"";

            var process = Process.Start(processStartInfo);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                MessageBox.Show("Failed to extract 7zip archive.", "Extract failed.");
                return;
            }
        }

        private void ReloadGames()
        {
            Games.Clear();

            foreach (var romPackage in Directory.EnumerateFiles(Console.FullPath).Where(X => !Path.GetFileName(X).StartsWith("!")).Where(X => !X.EndsWith(".srm")).Where(X => !X.EndsWith(".nfo")).OrderBy(X => X))
                Games.Add(new Game(Path.GetFileNameWithoutExtension(romPackage), romPackage));
        }
    }
}
