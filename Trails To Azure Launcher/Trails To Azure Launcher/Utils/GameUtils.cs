using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Trails_To_Azure_Launcher.Models;

namespace Trails_To_Azure_Launcher.Utils
{
    class GameUtils
    {
        public volatile static bool checkingForUpdate = false;

        public static bool isTypeInstalled(ContentInfo.Types type)
        {
            //Base game
            if ((Directory.Exists("data") || File.Exists("ED_AO.exe")) && (type == ContentInfo.Types.Game || type == ContentInfo.Types.GeoLiteEdits))
            {
                return true;
            }
            //Voice mod
            else if ((Directory.Exists("voice") && File.Exists("dinput8.dll")) && type == ContentInfo.Types.Voice)
            {
                return true;
            }
            //BGM mod
            else if ((Directory.Exists("data/bgm_org")) && type == ContentInfo.Types.Evo_BGM)
            {
                return true;
            }
            //HD mod
            else if ((File.Exists("d3d9.dll")) && type == ContentInfo.Types.HDPack)
            {
                return true;
            }

            return false;
        }

        public static void checkForUpdates(MainWindow window)
        {
            if (checkingForUpdate == true)
            {
                return;
            }

            checkingForUpdate = true;

            //Disable all buttons while checking for update
            window.Dispatcher.Invoke(() =>
            {
                foreach (String btnName in MainWindow.buttonNames)
                {
                    ((Button)window.FindName(btnName)).IsEnabled = false;
                }

                ((Label)window.FindName("stat_update")).Content = "Checking for updates...";
            });

            //Check if there's an update for the launcher first
            #region launcher
            LiveVersion launcherVersion = null;
            bool launcherNeedsUpdated = false;

            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("Authorization", MyGitCredentials.token);
                    launcherVersion = JsonConvert.DeserializeObject<LiveVersion>(client.DownloadString("https://raw.githubusercontent.com/geofrontlite/TtALauncher/master/version.json"));
                }
            }
            catch (WebException e)//No internet connection
            {
                window.Dispatcher.Invoke(() =>
                {
                    for (byte i = 0; i < MainWindow.buttonNames.Length; i++)
                    {
                        if ((ContentInfo.Types)i != ContentInfo.Types.GeoLiteEdits)
                        {
                            ((Button)window.FindName(MainWindow.buttonNames[i])).IsEnabled = (i == 0) ? true : GameUtils.isTypeInstalled((int)ContentInfo.Types.Game);
                        }
                    }

                    ((Label)window.FindName("stat_update")).Content = "Failed to connect to the internet";
                });
                checkingForUpdate = false;
                return;
            }

            String ver = window.GetType().Assembly.GetName().Version.ToString();
            String[] currentVersion = ver.Substring(0, ver.Length - 2).Split('.', StringSplitOptions.RemoveEmptyEntries);
            String[] liveVersion = launcherVersion.version.Split('.', StringSplitOptions.RemoveEmptyEntries);

            for (byte j = 0; j < liveVersion.Length; j++)
            {
                if ((j >= currentVersion.Length && liveVersion[j] != "0") || String.Compare(liveVersion[j], currentVersion[j]) > 0)//Behind in update(s)
                {
                    launcherNeedsUpdated = true;
                    break;
                }
            }

            window.updateURL = "https://github.com/geofrontlite/TtALauncher/releases/download/v" + launcherVersion.version +  "/TtA-Updater.exe";

            if (launcherNeedsUpdated == true)
            {
                window.Dispatcher.Invoke(() =>
                {
                    window.toggleUpdatePrompt(true);

                    if (launcherVersion.required == true)
                    {
                        ((Label)window.FindName("update_msg_aggressive")).Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ((Label)window.FindName("update_msg_passive")).Visibility = Visibility.Visible;
                    }
                });
            }


            if (launcherVersion != null && launcherVersion.required == true && launcherNeedsUpdated == true)
            {
                window.Dispatcher.Invoke(() =>
                {
                    ((Label)window.FindName("stat_update")).Content = "Cannot update components until\nlauncher is updated.";
                });

                checkingForUpdate = false;
                return;
            }
            #endregion

            //Nothing is installed yet
            if ((File.Exists("manifest.json") == false || new FileInfo("manifest.json").Length == 0) && GameUtils.isTypeInstalled(ContentInfo.Types.Game) == false)
            {
                window.Dispatcher.Invoke(() =>
                {
                    for (byte i = 0; i < MainWindow.buttonNames.Length; i++)
                    {
                        if ((ContentInfo.Types)i != ContentInfo.Types.GeoLiteEdits)
                        {
                            ((Button)window.FindName(MainWindow.buttonNames[i])).IsEnabled = (i == 0) ? true : GameUtils.isTypeInstalled((int)ContentInfo.Types.Game);
                        }
                    }

                    ((Label)window.FindName("stat_update")).Content = "";
                });
                checkingForUpdate = false;
                return;
            }
            //Game installed but no manifest. Will create manifest and add game to it
            else if (File.Exists("manifest.json") == false && GameUtils.isTypeInstalled(ContentInfo.Types.Game) == true)
            {
                new FileInfo("manifest.json").Create().Close();

                using (StreamWriter file = File.CreateText("manifest.json"))
                {
                    file.Write(JsonConvert.SerializeObject(new Manifest()
                    {
                        type = ContentInfo.TypesAsString[0],
                        version = ContentInfo.bakedVersions[0]
                    },
                    Formatting.Indented));
                }
            }

            List<Manifest> manifest;

            // deserialize JSON directly from a file
            using (StreamReader file = File.OpenText("manifest.json"))
            {
                manifest = JsonConvert.DeserializeObject<List<Manifest>>(file.ReadToEnd());
            }

            if (manifest == null)//window can happen if the file exists but is empty
            {
                manifest = new List<Manifest>();
            }

            bool repair = false;

            //Repair manifest if needed
            for (byte i = 0; i < ContentInfo.TypesAsString.Length; i++)
            {
                if (i == (byte)ContentInfo.Types.GeoLiteEdits)//Geofront Lite edits should NOT be added to the manifest. Rather let the installer take care of it
                {
                    continue;
                }

                //Game is installed but not in the manifest
                if (GameUtils.isTypeInstalled((ContentInfo.Types)i) == true && manifest.Any(m => String.Equals(m.type, ContentInfo.TypesAsString[i], StringComparison.OrdinalIgnoreCase)) == false)
                {
                    repair = true;
                    manifest.Add(new Manifest()
                    {
                        type = ContentInfo.TypesAsString[i],
                        version = ContentInfo.bakedVersions[i]
                    });
                }
            }

            if (repair == true)
            {
                using (StreamWriter file = File.CreateText("manifest.json"))
                {
                    file.Write(JsonConvert.SerializeObject(manifest, Formatting.Indented));
                }
            }

            byte foundUpdate = 0;
            bool updateForEdits = false;
            LiveVersion _liveVersion;

            for (byte i = 0; i < ContentInfo.liveVersionURLs.Length; i++)
            {
                if (GameUtils.isTypeInstalled((ContentInfo.Types)i) == false)
                {
                    continue;
                }

                try
                {
                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("Authorization", MyGitCredentials.token);
                        _liveVersion = JsonConvert.DeserializeObject<LiveVersion>(client.DownloadString(ContentInfo.liveVersionURLs[i]));
                    }
                }
                catch (WebException e)//No internet connection
                {
                    window.Dispatcher.Invoke(() =>
                    {
                        for (byte i = 0; i < MainWindow.buttonNames.Length; i++)
                        {
                            if ((ContentInfo.Types)i != ContentInfo.Types.GeoLiteEdits)
                            {
                                ((Button)window.FindName(MainWindow.buttonNames[i])).IsEnabled = (i == 0) ? true : GameUtils.isTypeInstalled((int)ContentInfo.Types.Game);
                            }
                        }

                        ((Label)window.FindName("stat_update")).Content = "Failed to connect to the internet";
                    });
                    checkingForUpdate = false;
                    return;
                }

                //Extra check that this is even in the manifest
                if (manifest.Any(m => String.Equals(m.type, ContentInfo.TypesAsString[i])))
                {
                    Manifest currentType = manifest.Find(m => String.Equals(m.type, ContentInfo.TypesAsString[i]));

                    currentVersion = currentType.version.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    liveVersion = _liveVersion.version.Split('.', StringSplitOptions.RemoveEmptyEntries);

                    for (byte j = 0; j < liveVersion.Length; j++)
                    {
                        if ((j >= currentVersion.Length && liveVersion[j] != "0") || String.Compare(liveVersion[j], currentVersion[j]) > 0)//Behind in update(s)
                        {
                            foundUpdate++;
                            window.Dispatcher.Invoke(() =>
                            {
                                if (String.Equals(((Button)window.FindName(MainWindow.buttonNames[i])).Content.ToString(), MainWindow.buttonText[i][(int)MainWindow.InstallTypes.Update], StringComparison.OrdinalIgnoreCase) == false)
                                {
                                    ((Button)window.FindName(MainWindow.buttonNames[i])).Content = MainWindow.buttonText[i][(int)MainWindow.InstallTypes.Update];
                                    ((Button)window.FindName(MainWindow.buttonNames[i])).ToolTip = MainWindow.buttonTooltips[i][(int)MainWindow.InstallTypes.Update];
                                    ((Label)window.FindName(MainWindow.statsNames[i])).Content = MainWindow.statusMessages[i][(int)MainWindow.InstallTypes.Update] + "\n          (" + _liveVersion.version + ")";
                                    ((Label)window.FindName(MainWindow.statsNames[i])).Foreground = Brushes.DarkGoldenrod;
                                    int multithreadRef = i;
                                    Task.Run(() => GUIUtils.autoAdjustMargins(window, MainWindow.statsNames[multithreadRef], true));
                                }
                            });

                            if ((ContentInfo.Types)i == ContentInfo.Types.GeoLiteEdits)
                            {
                                updateForEdits = true;
                            }
                        }
                    }
                }
            }

            window.Dispatcher.Invoke(() =>
            {
                for (byte i = 0; i < MainWindow.buttonNames.Length; i++)
                {
                    if ((ContentInfo.Types)i != ContentInfo.Types.GeoLiteEdits)
                    {
                        ((Button)window.FindName(MainWindow.buttonNames[i])).IsEnabled = (i == 0) ? true : GameUtils.isTypeInstalled((int)ContentInfo.Types.Game);
                    }
                    else if(updateForEdits == true)//If this is the edits and there is an update available for it...
                    {
                        ((Button)window.FindName(MainWindow.buttonNames[i])).IsEnabled = GameUtils.isTypeInstalled((int)ContentInfo.Types.Game);
                    }
                }

                ((Label)window.FindName("stat_update")).Content = (foundUpdate > 0) ? ((foundUpdate == 1) ? "An update is available." : "Updates are available.") : "Everything is up to date!";
            });
            checkingForUpdate = false;
        }
    }
}
