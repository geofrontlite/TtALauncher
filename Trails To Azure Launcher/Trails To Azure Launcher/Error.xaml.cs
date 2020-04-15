using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Trails_To_Azure_Launcher.Utils;

namespace Trails_To_Azure_Launcher
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class Error : Window
    {
        String code = "-1";
        String msg = "An unknown error occurred!";

        Timer checkLinesTimer;

        public Error()
        {
            InitializeComponent();
        }

        public Error(String code, String msg) : this()
        {
            this.code = code;
            this.msg = msg;

            ((Label)this.FindName("err_code")).Content = "Error code: " + code;
            ((Label)this.FindName("err_msg")).Content = "Error message: " + msg;

            checkLinesTimer = new Timer(checkLines, null, 100, 10000);
        }

        private void checkLines(Object state)
        {
            Task.Run(() => GUIUtils.autoAdjustMargins(this, "err_msg", false));

            checkLinesTimer.Dispose();
        }

        private void OpenBrowserToGitHub(object sender, RoutedEventArgs e)
        {
            String url = "https://github.com/geofrontlite/TtA_LauncherReleases";

            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
