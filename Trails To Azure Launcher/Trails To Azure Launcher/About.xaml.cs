using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Trails_To_Azure_Launcher.Models;
using Trails_To_Azure_Launcher.Utils;

namespace Trails_To_Azure_Launcher
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();

            String ver = GetType().Assembly.GetName().Version.ToString();
            ((Label)this.FindName("ver_installer")).Content += ver.Substring(0, ver.Length-2);

            if (File.Exists("manifest.json") == true)
            {
                List<Manifest> manifest;

                // deserialize JSON directly from a file
                using (StreamReader file = File.OpenText("manifest.json"))
                {
                    manifest = JsonConvert.DeserializeObject<List<Manifest>>(file.ReadToEnd());
                }

                bool[] checks = new bool[] { true, true, true, true, true };
                String[] GUINames = new String[] { "ver_game", "ver_edits", "ver_voice", "ver_evobgm", "ver_hdpack" };

                if (manifest != null && manifest.Count > 0)
                {
                    for(int i = 0; i < manifest.Count; i++)
                    {
                        for (int j = 0; j < ContentInfo.TypesAsString.Length; j++)
                        {
                            if (String.Equals(manifest[i].type, ContentInfo.TypesAsString[j], StringComparison.OrdinalIgnoreCase) == true)
                            {
                                ((Label)this.FindName(GUINames[j])).Content += manifest[i].version;
                                checks[j] = false;
                                break;
                            }
                        }
                    }
                }

                for (int i = 0; i < checks.Length; i++)
                {
                    if (checks[i] == true)
                    {
                        if (GameUtils.isTypeInstalled((ContentInfo.Types)i) == true)//Installed - version not in manifest
                        {
                            ((Label)this.FindName(GUINames[i])).Content += "Error in manifest";
                        }
                        else//Not installed
                        {
                            ((Label)this.FindName(GUINames[i])).Content += "Not installed";
                        }
                    }
                }
            }
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);

            if (this.IsVisible == true)//Prevents an exception from hitting the x which also causes the window to lose focus
            {
                if (e.NewFocus.GetType() == typeof(Button) && String.Equals(((Button)e.NewFocus).Name, "btn_checkupdate", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return;
                }
                else
                {
                    this.Close();
                }
            }
        }

        private void checkForUpdates(Object sender, RoutedEventArgs e)
        {
            MainWindow.checkForUpdatesTimer.Change(0, 8);
            Thread.Sleep(10);
            MainWindow.checkForUpdatesTimer.Change(0, 21600000);
        }
    }
}
