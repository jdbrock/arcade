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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Arcade.Pages
{
    /// <summary>
    /// Interaction logic for SplashPage.xaml
    /// </summary>
    public partial class SplashPage : Page
    {
        public SplashPage()
        {
            InitializeComponent();

            DispatcherTimer x = null;
            
            x = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.ApplicationIdle,
                (S, E) => { NavigationService.Navigate(new Uri("Pages/ConsoleBrowser.xaml", UriKind.Relative));  x.Stop(); }, Dispatcher);
        }

        //private void ClearBackStack()
        //{
        //    try
        //    {
        //        while (true)
        //        {
        //            var entry = NavigationService.RemoveBackEntry();

        //            if (entry == null)
        //                return;
        //        }
        //    }
        //    catch { }
        //}
    }
}
