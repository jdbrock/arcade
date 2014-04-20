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

namespace Arcade.Pages
{
    /// <summary>
    /// Interaction logic for ConsoleBrowser.xaml
    /// </summary>
    public partial class ConsoleBrowser : Page
    {
        public ObservableCollection<Console> Consoles
        {
            get { return (ObservableCollection<Console>)GetValue(ConsolesProperty); }
            set { SetValue(ConsolesProperty, value); }
        }

        public static readonly DependencyProperty ConsolesProperty =
            DependencyProperty.Register("Consoles", typeof(ObservableCollection<Console>), typeof(ConsoleBrowser), new PropertyMetadata(null));

        public ConsoleBrowser()
        {
            InitializeComponent();

            Consoles = new ObservableCollection<Console>();
            DataContext = this;

            ReloadConsoles();

            if (Consoles.Count > 0)
                ctConsolesListBox.SelectedItem = Consoles.First();

            ctConsolesListBox.Focus();
            ctConsolesListBox.KeyDown += ctConsolesListBox_KeyDown;
        }

        void ctConsolesListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ctConsolesListBox.SelectedItem != null)
            {
                var selectedItem = (Console)ctConsolesListBox.SelectedItem;
                NavigationService.Navigate(new GameBrowser(selectedItem));
            }
        }

        private void ReloadConsoles()
        {
            Consoles.Clear();

            foreach (var directory in Directory.EnumerateDirectories(App.c_BaseDirectory).Where(X => !Path.GetFileName(X).StartsWith("!")).OrderBy(X => X))
            {
                if (!File.Exists(Path.Combine(directory, "!config.txt")))
                    continue;

                Consoles.Add(new Console(Path.GetFileName(directory), directory));
            }
        }
    }
}
