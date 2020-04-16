using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Trails_To_Azure_Launcher
{
    /// <summary>
    /// Interaction logic for ExitConfirm.xaml
    /// </summary>
    public partial class ExitConfirm : Window
    {
        public ExitConfirm()
        {
            InitializeComponent();
        }

        private void CloseWindow(Object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void KillApp(Object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }
    }
}
