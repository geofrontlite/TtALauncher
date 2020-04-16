using IWshRuntimeLibrary;
using LibGit2Sharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Trails_To_Azure_Launcher.Models;
using Trails_To_Azure_Launcher.Utils;
using File = System.IO.File;

namespace Trails_To_Azure_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public enum InstallTypes
        {
            None = -1,
            Install = 0,
            Uninstall = 1,
            Update = 2
        }

        private CloneOptions gitCredentials = new CloneOptions();

        private volatile bool inAProcess = false;
        private volatile bool cleaningTemp = false;

        private InstallTypes performingType = InstallTypes.None;//True if installing, false if uninstalling
        private ContentInfo.Types typeProcessing = ContentInfo.Types.None;

        private Task cloneRepoTask;
        private CancellationTokenSource cancelCloneRepoToken = new CancellationTokenSource();
        private Task uninstallTask;
        private CancellationTokenSource cancelUninstallToken = new CancellationTokenSource();
        private Timer installTask;
        public static Timer checkForUpdatesTimer;

        private double lastPercentage = -1D;
        private int timeTillNextStep = 4000;//4 seconds
        private int timeBuildUp = 0;
        private byte onProcess = 0;//Internal counter to track installation progress
        private ulong totalSize = 0L;//This is used in the uninstall all feature

        public static readonly String[] buttonNames = new String[] { "install_btn", "edits_btn", "voice_btn", "bgm_btn", "hd_btn" };
        public static readonly String[] statsNames = new String[] { "stat_game", "stat_edits", "stat_voice", "stat_evobgm", "stat_hdpack" };
        public static readonly String[][] buttonText = new String[][]
        {
            new String[]{ "Install Game", "Uninstall Game", "Update Game" },//Game
            new String[]{ null, null, "Update Edits" },//Edits
            new String[]{ "Install Evo Voice Mod", "Uninstall Evo Voice Mod", "Update Evo Voice Mod" },//Voice mod
            new String[]{ "Install Evo BGM Mod", "Uninstall Evo BGM Mod", "Update Evo BGM Mod" },//Evo BGM mod
            new String[]{ "Install HD Pack\n(w/ DS4 Prompts)", "Uninstall HD Pack\n(and DS4 Prompts)", "Update HD Pack\n(w/ DS4 Prompts)" }//HD Pack
        };
        public static readonly String[][] buttonTooltips = new String[][]
        {
            new String[]{ "Install game and includes latest Geofront Lite edits", "Uninstall everything, including mods (saves remain)", "Update game files" },//Game
            new String[]{ null, null, "Get the latest changes from the Geofront Lite editing team" },//Edits
            new String[]{ null, null, null},//Voice mod
            new String[]{ "Install EVO BGM (makes a backup of original BGM and data/text/t_bgm._dt", "Uninstalls the EVO BGM and restores original BGM", null },//Evo BGM mod
            new String[]{ null, null, null }//HD Pack
        };
        public static readonly String[][] statusMessages = new String[][]
        {
            new String[]{ "Game is installed", "Game is not installed", "An update is available" },//Game
            new String[]{ "", "", "An update is available" },//Edits
            new String[]{ "Evo voice mod is\n      installed", "Evo voice mod is not\n         installed", "An update is available" },//Voice mod
            new String[]{ "Evo BGM mod is\n      installed", "Evo BGM mod is not\n         installed", "An update is available" },//Evo BGM mod
            new String[]{ "HD pack is installed", "HD pack is not installed", "An update is available" }//HD Pack
        };

        public MainWindow()
        {
            InitializeComponent();

            gitCredentials.CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = MyGitCredentials.GitUserName, Password = MyGitCredentials.GitPassword };
            toggleInstallationView(false);
            installEditsIfGameIsInstalled();
            refreshInfo();

            checkForUpdatesTimer = new Timer((Object) => { GameUtils.checkForUpdates(this); }, null, 30, 21600000);;//Every 6 hours
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (inAProcess == true)
            {
                e.Cancel = true;
                ExitConfirm exit = new ExitConfirm();
                exit.Show();
            }
            else
            {
                App.Current.Shutdown();
            }

            base.OnClosing(e);
        }

        private void refreshInfo()
        {
            bool installed = GameUtils.isTypeInstalled(ContentInfo.Types.Game);
            //Base game
            ((Button)this.FindName("launch_game_btn")).IsEnabled = installed;
            ((Button)this.FindName("launch_config_btn")).IsEnabled = installed;
            ((Button)this.FindName(buttonNames[(int)ContentInfo.Types.Game])).Content = 
                        buttonText[(int)ContentInfo.Types.Game][(installed) ? (int)InstallTypes.Uninstall : (int)InstallTypes.Install];
            ((Button)this.FindName(buttonNames[(int)ContentInfo.Types.Game])).ToolTip = 
                        buttonTooltips[(int)ContentInfo.Types.Game][(installed) ? (int)InstallTypes.Uninstall : (int)InstallTypes.Install];
            ((Label)this.FindName(statsNames[(int)ContentInfo.Types.Game])).Content = 
                        statusMessages[(int)ContentInfo.Types.Game][(installed == false) ? (int)InstallTypes.Uninstall : (int)InstallTypes.Install];
            ((Label)this.FindName(statsNames[(int)ContentInfo.Types.Game])).Foreground = (installed) ? Brushes.DarkGreen : Brushes.DarkRed;
            ((Button)this.FindName(buttonNames[(int)ContentInfo.Types.GeoLiteEdits])).IsEnabled = installed;
            ((Button)this.FindName(buttonNames[(int)ContentInfo.Types.Voice])).IsEnabled = installed;
            ((Button)this.FindName(buttonNames[(int)ContentInfo.Types.Evo_BGM])).IsEnabled = installed;
            ((Button)this.FindName(buttonNames[(int)ContentInfo.Types.HDPack])).IsEnabled = installed;

            installed = GameUtils.isTypeInstalled(ContentInfo.Types.Voice);
            //Voice mod
            ((Button)this.FindName(buttonNames[(int)ContentInfo.Types.Voice])).Content = 
                        buttonText[(int)ContentInfo.Types.Voice][(installed) ? (int)InstallTypes.Uninstall : (int)InstallTypes.Install];
            ((Button)this.FindName(buttonNames[(int)ContentInfo.Types.Voice])).ToolTip = 
                        buttonTooltips[(int)ContentInfo.Types.Voice][(installed) ? (int)InstallTypes.Uninstall : (int)InstallTypes.Install];
            ((Label)this.FindName(statsNames[(int)ContentInfo.Types.Voice])).Content = 
                        statusMessages[(int)ContentInfo.Types.Voice][(installed == false) ? (int)InstallTypes.Uninstall : (int)InstallTypes.Install];
            ((Label)this.FindName(statsNames[(int)ContentInfo.Types.Voice])).Foreground = (installed) ? Brushes.DarkGreen : Brushes.DarkRed;


            installed = GameUtils.isTypeInstalled(ContentInfo.Types.Evo_BGM);
            //BGM mod
            ((Button)this.FindName(buttonNames[(int)ContentInfo.Types.Evo_BGM])).Content =
                        buttonText[(int)ContentInfo.Types.Evo_BGM][(installed) ? (int)InstallTypes.Uninstall : (int)InstallTypes.Install];
            ((Button)this.FindName(buttonNames[(int)ContentInfo.Types.Evo_BGM])).ToolTip =
                        buttonTooltips[(int)ContentInfo.Types.Evo_BGM][(installed) ? (int)InstallTypes.Uninstall : (int)InstallTypes.Install];
            ((Label)this.FindName(statsNames[(int)ContentInfo.Types.Evo_BGM])).Content =
                        statusMessages[(int)ContentInfo.Types.Evo_BGM][(installed == false) ? (int)InstallTypes.Uninstall : (int)InstallTypes.Install];
            ((Label)this.FindName(statsNames[(int)ContentInfo.Types.Evo_BGM])).Foreground = (installed) ? Brushes.DarkGreen : Brushes.DarkRed;


            installed = GameUtils.isTypeInstalled(ContentInfo.Types.HDPack);
            //HD mod
            ((Button)this.FindName(buttonNames[(int)ContentInfo.Types.HDPack])).Content =
                        buttonText[(int)ContentInfo.Types.HDPack][(installed) ? (int)InstallTypes.Uninstall : (int)InstallTypes.Install];
            ((Button)this.FindName(buttonNames[(int)ContentInfo.Types.HDPack])).ToolTip =
                        buttonTooltips[(int)ContentInfo.Types.HDPack][(installed) ? (int)InstallTypes.Uninstall : (int)InstallTypes.Install];
            ((Label)this.FindName(statsNames[(int)ContentInfo.Types.HDPack])).Content =
                        statusMessages[(int)ContentInfo.Types.HDPack][(installed == false) ? (int)InstallTypes.Uninstall : (int)InstallTypes.Install];
            ((Label)this.FindName(statsNames[(int)ContentInfo.Types.HDPack])).Foreground = (installed) ? Brushes.DarkGreen : Brushes.DarkRed;
        }

        private void installEditsIfGameIsInstalled()
        {
            //Nothing is installed yet
            if ((File.Exists("manifest.json") == false || new FileInfo("manifest.json").Length == 0) && GameUtils.isTypeInstalled(ContentInfo.Types.Game) == false)
            {
                return;
            }
            //Game installed but no manifest. 
            else if ((File.Exists("manifest.json") == false || new FileInfo("manifest.json").Length == 0) && GameUtils.isTypeInstalled(ContentInfo.Types.Game) == true)
            {
                //Install edits
                Button placeHolder = new Button();
                placeHolder.Name = buttonNames[(int)ContentInfo.Types.GeoLiteEdits];
                placeHolder.Content = buttonText[(int)ContentInfo.Types.GeoLiteEdits][(int)InstallTypes.Install];//Needed so the app knows to install it
                install(placeHolder, null);
                return;
            }

            List<Manifest> manifest;

            // deserialize JSON directly from a file
            using (StreamReader file = File.OpenText("manifest.json"))
            {
                manifest = JsonConvert.DeserializeObject<List<Manifest>>(file.ReadToEnd());
            }

            //Check if game is installed but not edits
            if (manifest.Any(m => String.Equals(m.type, ContentInfo.TypesAsString[0], StringComparison.OrdinalIgnoreCase)) == true &&//Has game
                manifest.Any(m => String.Equals(m.type, ContentInfo.TypesAsString[1], StringComparison.OrdinalIgnoreCase)) == false)//Does not have edits
            {
                Button placeHolder = new Button();
                placeHolder.Name = buttonNames[(int)ContentInfo.Types.GeoLiteEdits];
                placeHolder.Content = buttonText[(int)ContentInfo.Types.GeoLiteEdits][(int)InstallTypes.Install];//Needed so the app knows to install it
                install(placeHolder, null);
            }
        }

        private void ShowAbout(Object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.Show();
        }

        private void StartGame(Object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("ED_AO.exe");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.ShowError("100a", "File system error: Could not find the game executable.");
                    ((Label)this.FindName("stat_game")).Content = "There was an error\nstarting the game.";
                });
            }
        }

        private void StartConfig(Object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("config.exe");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.ShowError("100b", "File system error: Could not find the game config\n   executable.");
                    ((Label)this.FindName("stat_game")).Content = "There was an error\nstarting the config.";
                });
            }
        }

        private void OpenGameDirectory(Object sender, RoutedEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = ".",
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
        }

        public void CreateShortcut(Object sender, RoutedEventArgs e)
        {
            string shortcutLocation = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Trails to Azure - Launcher" + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.Description = "The launcher for Trails to Azure";   // The description of the shortcut
            shortcut.TargetPath = Assembly.GetEntryAssembly().Location;// The path of the file that will launch when the shortcut is run

            if (shortcut.TargetPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                shortcut.TargetPath = shortcut.TargetPath.Substring(0, shortcut.TargetPath.Length-4) + ".exe";
            }

            shortcut.Save();                                    // Save the shortcut
        }

        private void KillLauncher(Object sender, RoutedEventArgs e)
        {
            if (inAProcess == false)
            {
                App.Current.Shutdown();
            }
            else
            {
                ExitConfirm exit = new ExitConfirm();
                exit.Show();
            }
        }

        private void ShowError(String code, String msg)
        {
            Error error = new Error(code, msg);
            error.Show();
            cancelInstallation();
        }

        private void RestartAsAdmin(Object sender, RoutedEventArgs e)
        {
            String exePath = Assembly.GetEntryAssembly().Location;// The path of the file that will launch when the shortcut is run

            if (exePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                exePath = exePath.Substring(0, exePath.Length - 4) + ".exe";
            }

            ProcessStartInfo info = new ProcessStartInfo(exePath);
            info.UseShellExecute = true;
            info.Verb = "runas";
            Process.Start(info);

            App.Current.Shutdown();
        }

        private void CloseInsuffPrivsPrompt(Object sender, RoutedEventArgs e)
        {
            toggleInsuffPrivsPrompt(false);
        }

        private void toggleInsuffPrivsPrompt(bool show)
        {
            ((Rectangle)this.FindName("elev_hider")).Visibility = (show) ? Visibility.Visible : Visibility.Hidden;
            ((Rectangle)this.FindName("elev_panel")).Visibility = (show) ? Visibility.Visible : Visibility.Hidden;
            ((Label)this.FindName("elev_header")).Visibility = (show) ? Visibility.Visible : Visibility.Hidden;
            ((Label)this.FindName("elev_msg")).Visibility = (show) ? Visibility.Visible : Visibility.Hidden;
            ((Button)this.FindName("elev_yes_btn")).Visibility = (show) ? Visibility.Visible : Visibility.Hidden;
            ((Button)this.FindName("elev_no_btn")).Visibility = (show) ? Visibility.Visible : Visibility.Hidden;
        }

        private void toggleInstallationView(bool visible)
        {
            toggleInstallationView(visible, 5);
        }

        private void toggleInstallationView(bool visible, byte numOfProceedures)
        {
            ((Rectangle)this.FindName("inst_hider")).Visibility = (visible) ? Visibility.Visible : Visibility.Hidden;
            ((Rectangle)this.FindName("inst_panel")).Visibility = (visible) ? Visibility.Visible : Visibility.Hidden;
            ((Label)this.FindName("inst_oper")).Visibility = (visible) ? Visibility.Visible : Visibility.Hidden;
            ((ProgressBar)this.FindName("inst_prog")).Visibility = (visible) ? Visibility.Visible : Visibility.Hidden;
            ((Label)this.FindName("inst_progPerc")).Visibility = (visible) ? Visibility.Visible : Visibility.Hidden;
            ((Label)this.FindName("inst_size")).Visibility = (visible && performingType != InstallTypes.Uninstall) ? Visibility.Visible : Visibility.Hidden;
            ((Button)this.FindName("inst_cancel")).Visibility = (visible) ? Visibility.Visible : Visibility.Hidden;

            for (byte i = 0; i < numOfProceedures; i++)
            {
                ((Label)this.FindName("inst_proc" + (i + 1))).Visibility = (visible) ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void CancelInstallation(Object sender, RoutedEventArgs e)
        {
            cancelInstallation();
        }

        private void cancelInstallation()
        {
            ((Button)this.FindName("inst_cancel")).IsEnabled = false;
            ((Label)this.FindName("inst_cancelmsg")).Visibility = Visibility.Visible;
            String type = ((performingType == InstallTypes.Install) ? "installation" : ((performingType == InstallTypes.Uninstall) ? "uninstallation" : "update"));
            ((Label)this.FindName("inst_cancelmsg")).Content = "Cancelling " + type + "..." + ((performingType == InstallTypes.Install && typeProcessing == ContentInfo.Types.Game) ? " (this may take a while)" : "");

            Task.Factory.StartNew((window) =>
            {
                if (cloneRepoTask != null && cloneRepoTask.IsCompleted == false)
                {
                    cancelCloneRepoToken.Cancel();
                }
                if (uninstallTask != null && uninstallTask.IsCompleted == false)
                {
                    cancelUninstallToken.Cancel();
                }
                if (installTask != null)
                {
                    try
                    {
                        installTask.Dispose();
                    }
                    catch { }
                }
                if (uninstallTask != null)
                {
                    try
                    {
                        uninstallTask.Dispose();
                    }
                    catch { }
                }

                if (cloneRepoTask != null && cloneRepoTask.IsCompleted == false)
                {
                    cloneRepoTask.Wait();
                    cloneRepoTask.Dispose();
                }
                if (uninstallTask != null && uninstallTask.IsCompleted == false)
                {
                    uninstallTask.Wait();
                    uninstallTask.Dispose();
                }

                while (cleanTempFolder() == false)
                {
                    Thread.Sleep(20);
                }

                inAProcess = false;
                onProcess = 0;
                performingType = InstallTypes.None;

                MainWindow win = (MainWindow)window;

                win.Dispatcher.Invoke(() =>
                {
                    ((Label)this.FindName("inst_cancelmsg")).Visibility = Visibility.Hidden;
                    toggleInstallationView(false);
                    ((Button)this.FindName("inst_cancel")).IsEnabled = true;
                });
            }, this);
        }

        private void install(Object sender, RoutedEventArgs e)
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);

                if (principal.IsInRole(WindowsBuiltInRole.Administrator) == false)
                {
                    toggleInsuffPrivsPrompt(true);
                    return;
                }
            }

            Button requestSrc = ((Button)sender);

            inAProcess = true;
            onProcess = 0;
            performingType = InstallTypes.None;
            ((Label)this.FindName("inst_progPerc")).Content = "0.00%";
            ((ProgressBar)this.FindName("inst_prog")).Value = 0;

            foreach (ContentInfo.Types type in (ContentInfo.Types[])Enum.GetValues(typeof(ContentInfo.Types)))
            {
                if (type != ContentInfo.Types.None && String.Equals(requestSrc.Name, buttonNames[(int)type], StringComparison.OrdinalIgnoreCase) == true)
                {
                    typeProcessing = type;
                }
            }

            if (requestSrc.Content.ToString().StartsWith("Install", StringComparison.OrdinalIgnoreCase) == true)
            {
                performingType = InstallTypes.Install;
                timeBuildUp = 0 - new Random().Next(3000);

                //Set up the installation prompt
                toggleInstallationView(true, (byte)(ContentInfo.installMessages[(int)typeProcessing].Length - 1));
                ((Label)this.FindName("inst_size")).Content = "Size: " + FileUtils.SizeSuffix(ContentInfo.diskSize[(int)typeProcessing], 2);

                ((Label)this.FindName("inst_oper")).Content = ContentInfo.installMessages[(int)typeProcessing][0];

                Task.Run(() => GUIUtils.autoAdjustMargins(this, "inst_oper", false));

                for (byte i = 1; i < ContentInfo.installMessages[(int)typeProcessing].Length; i++)
                {
                    ((Label)this.FindName("inst_proc" + i)).Content = ContentInfo.installMessages[(int)typeProcessing][i];
                    ((Label)this.FindName("inst_proc" + i)).Foreground = (i == 1) ? Brushes.DarkGoldenrod : Brushes.DarkRed;
                }

                installTask = new Timer(watchInstallTask, null, 100, 200);
            }
            else if (requestSrc.Content.ToString().StartsWith("Uninstall", StringComparison.OrdinalIgnoreCase) == true)
            {
                performingType = InstallTypes.Uninstall;

                //Set up the uninstallation prompt
                toggleInstallationView(true, (byte)(ContentInfo.uninstallMessages[(int)typeProcessing].Length - 1));

                ((Label)this.FindName("inst_oper")).Content = ContentInfo.uninstallMessages[(int)typeProcessing][0];

                for (byte i = 1; i < ContentInfo.uninstallMessages[(int)typeProcessing].Length; i++)
                {
                    ((Label)this.FindName("inst_proc" + i)).Content = ContentInfo.uninstallMessages[(int)typeProcessing][i];
                    ((Label)this.FindName("inst_proc" + i)).Foreground = (i == 1) ? Brushes.DarkGoldenrod : Brushes.DarkRed;
                }

                if (typeProcessing == ContentInfo.Types.Game)//Uninstalling the game uninstalls everything
                {
                    uninstallTask = Task.Run(() => uninstallAllAction(this), cancelUninstallToken.Token);
                    installTask = new Timer(watchUninstallAllTask, null, 0, 200);
                }
                else
                {
                    uninstallTask = Task.Run(() => uninstallAction(this), cancelUninstallToken.Token);
                    installTask = new Timer(watchUninstallTask, null, 0, 80);
                }
            }
            else if (requestSrc.Content.ToString().StartsWith("Update", StringComparison.OrdinalIgnoreCase) == true)
            {
                performingType = InstallTypes.Update;
                timeBuildUp = 0 - new Random().Next(3000);

                //Set up the installation prompt
                toggleInstallationView(true, (byte)(ContentInfo.updateMessages[(int)typeProcessing].Length - 1));
                ((Label)this.FindName("inst_size")).Content = "Size: " + FileUtils.SizeSuffix(ContentInfo.diskSize[(int)typeProcessing], 2);

                ((Label)this.FindName("inst_oper")).Content = ContentInfo.updateMessages[(int)typeProcessing][0];

                Task.Run(() => GUIUtils.autoAdjustMargins(this, "inst_oper", false));

                for (byte i = 1; i < ContentInfo.updateMessages[(int)typeProcessing].Length; i++)
                {
                    ((Label)this.FindName("inst_proc" + i)).Content = ContentInfo.updateMessages[(int)typeProcessing][i];
                    ((Label)this.FindName("inst_proc" + i)).Foreground = (i == 1) ? Brushes.DarkGoldenrod : Brushes.DarkRed;
                }

                uninstallTask = Task.Run(() => uninstallAction(this), cancelUninstallToken.Token);//Uninstall task because it uninstalls first
                installTask = new Timer(watchUpdateTask, null, 0, 120);
            }
            else
            {
                ShowError("101", "Invalid installation type");
                return;
            }
        }

        private Action<MainWindow, CloneOptions> cloneGameRepo = (MainWindow window, CloneOptions privateCloneOps) =>
        {
            try
            {
                Repository.Clone(ContentInfo.repoURLs[(int)window.typeProcessing], "_temp", privateCloneOps);
            }
            catch
            {
                window.Dispatcher.Invoke(() =>
                {
                    window.ShowError("400" + ContentInfo.numToLetter((int)window.typeProcessing, false), "Network Error: Connection to the internet was lost\n   during download.");
                    ((Label)window.FindName("stat_game")).Content = "There was an error\ninstalling the game.";
                });
            }
        };

        private void watchInstallTask(Object state)
        {
            //Clean up before running
            if (onProcess == 0)
            {
                if (cleanTempFolder() == true)
                {
                    onProcess = 1;
                    cloneRepoTask = Task.Run(() => cloneGameRepo(this, gitCredentials), cancelCloneRepoToken.Token);
                }
                else
                {
                    return;
                }
            }

            //This MUST be done to affect the UI elements in a thread off the main thread
            this.Dispatcher.Invoke(() =>
            {
                //Report on the size
                double percentageDone = Decimal.ToDouble(((Decimal.Divide(GetDirectorySize("_temp"), ContentInfo.repoSizes[(int)typeProcessing]))) * 100);
                percentageDone = (percentageDone > 100 || onProcess > 5) ? 100 : percentageDone;//Prevent overflows and backflows, just in case
                ((Label)this.FindName("inst_progPerc")).Content = percentageDone.ToString("#0.00") + "%";
                ((ProgressBar)this.FindName("inst_prog")).Value = percentageDone;

                if (onProcess < 3)
                {
                    if (percentageDone == lastPercentage)
                    {
                        timeBuildUp += 200;//This MUST be equal to the amount of time that passes between timer intervals
                    }
                    else
                    {
                        timeBuildUp = 0 - new Random().Next(3000);
                    }

                    lastPercentage = percentageDone;
                }

                //The second part is just in case it takes over 16 seconds, we know it moved on
                //Allocating...
                if ((timeBuildUp >= timeTillNextStep && onProcess == 1 && percentageDone > 1) || (timeBuildUp >= timeTillNextStep * 4 && onProcess == 1)
                    || (percentageDone > 6 && onProcess == 1))
                {
                    ((Label)this.FindName("inst_proc1")).Foreground = Brushes.DarkGreen;
                    ((Label)this.FindName("inst_proc2")).Foreground = Brushes.DarkGoldenrod;
                    timeBuildUp = 0 - new Random().Next(3000);
                    onProcess = 2;
                }
                //Preparing to download...
                else if ((percentageDone > 6 && onProcess == 2) || (timeBuildUp >= timeTillNextStep * 5 && onProcess == 2))//The second part is just in case it takes over 36+ seconds, we know it moved on
                {
                    ((Label)this.FindName("inst_proc1")).Foreground = Brushes.DarkGreen;//Add this here just in case
                    ((Label)this.FindName("inst_proc2")).Foreground = Brushes.DarkGreen;
                    ((Label)this.FindName("inst_proc3")).Foreground = Brushes.DarkGoldenrod;
                    onProcess = 3;
                }
                //Fetching...
                else if (percentageDone > 30 && onProcess == 3)
                {
                    ((Label)this.FindName("inst_proc1")).Foreground = Brushes.DarkGreen;//Add this here again just in case
                    ((Label)this.FindName("inst_proc2")).Foreground = Brushes.DarkGreen;//Add this here just in case
                    ((Label)this.FindName("inst_proc3")).Foreground = Brushes.DarkGreen;
                    ((Label)this.FindName("inst_proc4")).Foreground = Brushes.DarkGoldenrod;
                    onProcess = 4;
                }
                else if (percentageDone > 99.6)
                {
                    ((Label)this.FindName("inst_proc4")).Foreground = Brushes.DarkGreen;
                    ((Label)this.FindName("inst_proc5")).Foreground = Brushes.DarkGoldenrod;
                    onProcess = (onProcess > 5) ? onProcess : (byte)5;
                }
            });

            if (cloneRepoTask.IsCompleted == true)
            {
                if (onProcess == 5)
                {
                    cloneRepoTask.Dispose();
                    onProcess++;
                    return;
                }
                else if (onProcess == 6)
                {
                    onProcess = 7;

                    Debug.WriteLine("Size of _temp folder: " + GetDirectorySize("_temp"));

                    //Do copy operations
                    for (int i = 0; i < ContentInfo.copy_dirs[(int)typeProcessing].Length; i++)
                    {
                        if (ContentInfo.copy_dirs[(int)typeProcessing][i] == null)
                        {
                            continue;
                        }

                        FileUtils.DirectoryCopy(ContentInfo.copy_dirs[(int)typeProcessing][i], ContentInfo.dirs[(int)typeProcessing][i], true, true);
                    }

                    for (int i = 0; i < ContentInfo.copy_files[(int)typeProcessing].Length; i++)
                    {
                        if (ContentInfo.copy_files[(int)typeProcessing][i] == null)
                        {
                            continue;
                        }

                        File.Copy(ContentInfo.copy_files[(int)typeProcessing][i], ContentInfo.files[(int)typeProcessing][i], true);
                    }

                    //Do move operations
                    for (int i = 0; i < ContentInfo.move_dirs_src[(int)typeProcessing].Length; i++)
                    {
                        if (ContentInfo.move_dirs_src[(int)typeProcessing][i] == null || Directory.Exists(ContentInfo.move_dirs_src[(int)typeProcessing][i]) == false
                            || ContentInfo.move_dirs_dest[(int)typeProcessing][i] == null)
                        {
                            continue;
                        }

                        FileUtils.DirectoryMove(ContentInfo.move_dirs_src[(int)typeProcessing][i], ContentInfo.move_dirs_dest[(int)typeProcessing][i], true);
                    }

                    for (int i = 0; i < ContentInfo.move_files_src[(int)typeProcessing].Length; i++)
                    {
                        if (ContentInfo.move_files_src[(int)typeProcessing][i] == null || File.Exists(ContentInfo.move_files_src[(int)typeProcessing][i]) == false
                            || ContentInfo.move_files_dest[(int)typeProcessing][i] == null)
                        {
                            continue;
                        }

                        File.Move(ContentInfo.move_files_src[(int)typeProcessing][i], ContentInfo.move_files_dest[(int)typeProcessing][i], true);
                    }

                    //Movie files out of _temp
                    for (int i = 0; i < ContentInfo.git_dirs[(int)typeProcessing].Length; i++)
                    {
                        FileUtils.DirectoryMove(ContentInfo.git_dirs[(int)typeProcessing][i], ContentInfo.dirs[(int)typeProcessing][i], true);
                    }

                    for (int i = 0; i < ContentInfo.git_files[(int)typeProcessing].Length; i++)
                    {
                        File.Move(ContentInfo.git_files[(int)typeProcessing][i], ContentInfo.files[(int)typeProcessing][i], true);
                    }

                    //Amend split files
                    DirectoryInfo[] bigDirs = new DirectoryInfo(".").GetDirectories("__BIG_*", SearchOption.AllDirectories);

                    foreach (DirectoryInfo dir in bigDirs)
                    {
                        FileUtils.CombineFiles(dir.FullName, dir.FullName.Substring(0, dir.FullName.Length - dir.Name.Length) + dir.Name.Substring(6));
                        Directory.Delete(dir.FullName, true);
                    }

                    Manifest verDownloaded = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText("_temp/version.json"));
                    Manifest manifestToAdd = new Manifest
                    {
                        type = ContentInfo.TypesAsString[(int)typeProcessing],
                        version = verDownloaded.version
                    };

                    if (File.Exists("manifest.json") == false)
                    {
                        new FileInfo("manifest.json").Create().Close(); ;
                    }

                    List<Manifest> manifest;
                    // deserialize JSON directly from a file
                    using (StreamReader file = File.OpenText("manifest.json"))
                    {
                        manifest = JsonConvert.DeserializeObject<List<Manifest>>(file.ReadToEnd());
                    }

                    if (manifest == null)
                    {
                        manifest = new List<Manifest>();
                    }

                    manifest.Add(manifestToAdd);

                    // serialize JSON directly to a file
                    using (StreamWriter file = File.CreateText("manifest.json"))
                    {
                        file.Write(JsonConvert.SerializeObject(manifest, Formatting.Indented));
                    }

                    onProcess = 8;
                }

                //This check is to prevent a thread running up on the other and breaking it
                if (onProcess == 8)
                {
                    if (cleanTempFolder() == false)
                    {
                        return;
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        toggleInstallationView(false);
                        refreshInfo();
                    });

                    if (typeProcessing == ContentInfo.Types.Game)
                    {
                        typeProcessing = ContentInfo.Types.None;//It's important that this is here instead of out of the branch
                        installTask.Dispose();

                        this.Dispatcher.Invoke(() =>
                        {
                            Button placeHolder = new Button();
                            placeHolder.Name = buttonNames[(int)ContentInfo.Types.GeoLiteEdits];
                            placeHolder.Content = buttonText[(int)ContentInfo.Types.GeoLiteEdits][(int)InstallTypes.Install];//Needed so the app knows to install it
                            install(placeHolder, null);
                        });
                    }
                    else
                    {
                        inAProcess = false;
                        typeProcessing = ContentInfo.Types.None;
                        installTask.Dispose();
                    }
                }
            }
        }

        private Action<MainWindow> uninstallAction = (MainWindow window) =>
        {
            //Delete the directories first
            foreach (String dir in ContentInfo.dirs[(int)window.typeProcessing])
            {
                if (Directory.Exists(dir) == true)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        //Can ignore this exception
                    }
                    catch (DirectoryNotFoundException e)
                    { 
                        //Can ignore this exception
                    }
                }
            }

            //Delete files next
            foreach (String file in ContentInfo.files[(int)window.typeProcessing])
            {
                if (File.Exists(file) == true)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        //Can ignore this exception
                    }
                }
            }
        };

        private void watchUninstallTask(Object state)
        {
            ulong size = 0L;

            foreach (String dir in ContentInfo.dirs[(int)this.typeProcessing])
            {
                if (Directory.Exists(dir) == true)
                {
                    size += GetDirectorySize(dir);
                }
            }

            foreach (String file in ContentInfo.files[(int)this.typeProcessing])
            {
                try
                {
                    if (File.Exists(file) == true)
                    {
                        size += (ulong)new FileInfo(file).Length;
                    }
                }
                catch (FileNotFoundException e)
                { 
                    //Can ignore this exception
                }
            }

            //This MUST be done to affect the UI elements in a thread off the main thread
            this.Dispatcher.Invoke(() =>
            {
                //Report on the size
                double percentageDone = Math.Abs((Decimal.ToDouble(((Decimal.Divide(size, ContentInfo.diskSize[(int)typeProcessing]))) * 100))-100);
                ((Label)this.FindName("inst_progPerc")).Content = percentageDone.ToString("#0.00") + "%";
                ((ProgressBar)this.FindName("inst_prog")).Value = percentageDone;

                if (percentageDone >= 98)
                {
                    ((Label)this.FindName("inst_proc1")).Foreground = Brushes.DarkGreen;
                    ((Label)this.FindName("inst_proc2")).Foreground = Brushes.DarkGoldenrod;

                    if (uninstallTask.IsCompleted == true)
                    {
                        bool runAgain = false;

                        //Delete the directories first
                        foreach (String dir in ContentInfo.dirs[(int)typeProcessing])
                        {
                            if (Directory.Exists(dir) == true)
                            {
                                runAgain = true;
                            }
                        }

                        //Delete files next
                        foreach (String file in ContentInfo.files[(int)typeProcessing])
                        {
                            if (File.Exists(file) == true)
                            {
                                runAgain = true;
                            }
                        }

                        if (runAgain == true)
                        {
                            uninstallTask = Task.Run(() => uninstallAction(this), cancelUninstallToken.Token);
                        }
                    }
                    return;
                }
            });

            if (size == 0 && uninstallTask.IsCompleted)
            {
                uninstallTask.Dispose();//The task
                installTask.Dispose();//The timer

                //Reverse any moves
                for (int i = 0; i < ContentInfo.move_dirs_src[(int)typeProcessing].Length; i++)
                {
                    if (ContentInfo.move_dirs_src[(int)typeProcessing][i] == null || ContentInfo.move_dirs_dest[(int)typeProcessing][i] == null//It's okay if src doesn't exist since it's a reversal
                        || Directory.Exists(ContentInfo.move_dirs_dest[(int)typeProcessing][i]) == false)
                    {
                        continue;
                    }

                    FileUtils.DirectoryMove(ContentInfo.move_dirs_dest[(int)typeProcessing][i], ContentInfo.move_dirs_src[(int)typeProcessing][i], true);
                }

                for (int i = 0; i < ContentInfo.move_files_src[(int)typeProcessing].Length; i++)
                {
                    if (ContentInfo.move_files_src[(int)typeProcessing][i] == null || ContentInfo.move_files_dest[(int)typeProcessing][i] == null//It's okay if src doesn't exist since it's a reversal
                        || File.Exists(ContentInfo.move_files_dest[(int)typeProcessing][i]) == false)
                    {
                        continue;
                    }

                    File.Move(ContentInfo.move_files_dest[(int)typeProcessing][i], ContentInfo.move_files_src[(int)typeProcessing][i], true);
                }

                if (File.Exists("manifest.json") == true)
                {
                    List<Manifest> manifest;
                    // deserialize JSON directly from a file
                    using (StreamReader file = File.OpenText("manifest.json"))
                    {
                        manifest = JsonConvert.DeserializeObject<List<Manifest>>(file.ReadToEnd());
                    }

                    if (manifest != null && manifest.Count > 0 && manifest.Any(m => String.Equals(m.type, ContentInfo.TypesAsString[(int)typeProcessing])) == true)
                    {
                        Manifest manifestToDelete = manifest.Single(m => String.Equals(m.type, ContentInfo.TypesAsString[(int)typeProcessing]) == true);

                        manifest.Remove(manifestToDelete);

                        // serialize JSON directly to a file
                        // serialize JSON directly to a file
                        using (StreamWriter file = File.CreateText("manifest.json"))
                        {
                            file.Write(JsonConvert.SerializeObject(manifest, Formatting.Indented));
                        }
                    }
                }

                this.Dispatcher.Invoke(() =>
                {
                    toggleInstallationView(false);
                    refreshInfo();
                });
                inAProcess = false;
                typeProcessing = ContentInfo.Types.None;
            }
        }

        private void watchUpdateTask(Object state)
        {
            #region blocker
            if (onProcess == 1 || (onProcess >= 11 && onProcess <= 17 && onProcess != 16))//Blocker
            {
                return;
            }
            else if(onProcess == 0 || onProcess == 10)
            {
                onProcess += 1;//Would make it 1 or 11
            }
            #endregion

            #region uninstall_part
            if (onProcess < 10)//The uninstall part is reserved the use of onProcess 0-10
            {
                ulong size = 0L;

                foreach (String dir in ContentInfo.dirs[(int)this.typeProcessing])
                {
                    if (Directory.Exists(dir) == true)
                    {
                        size += GetDirectorySize(dir);
                    }
                }

                foreach (String file in ContentInfo.files[(int)this.typeProcessing])
                {
                    try
                    {
                        if (File.Exists(file) == true)
                        {
                            size += (ulong)new FileInfo(file).Length;
                        }
                    }
                    catch (FileNotFoundException e)
                    {
                        //Can ignore this exception
                    }
                }

                //This MUST be done to affect the UI elements in a thread off the main thread
                this.Dispatcher.Invoke(() =>
                {
                    //Report on the size
                    double percentageDone = (Math.Abs((Decimal.ToDouble(((Decimal.Divide(size, ContentInfo.diskSize[(int)typeProcessing]))) * 100)) - 100)) * 0.2;//The uninstallation will only make up 20%
                    ((Label)this.FindName("inst_progPerc")).Content = percentageDone.ToString("#0.00") + "%";
                    ((ProgressBar)this.FindName("inst_prog")).Value = percentageDone;

                    if (percentageDone >= 19.9)
                    {
                        ((Label)this.FindName("inst_proc1")).Foreground = Brushes.DarkGreen;
                        ((Label)this.FindName("inst_proc2")).Foreground = Brushes.DarkGoldenrod;

                        if (uninstallTask.IsCompleted == true)
                        {
                            bool runAgain = false;

                            //Delete the directories first
                            foreach (String dir in ContentInfo.dirs[(int)typeProcessing])
                            {
                                if (Directory.Exists(dir) == true)
                                {
                                    runAgain = true;
                                }
                            }

                            //Delete files next
                            foreach (String file in ContentInfo.files[(int)typeProcessing])
                            {
                                if (File.Exists(file) == true)
                                {
                                    runAgain = true;
                                }
                            }

                            if (runAgain == true)
                            {
                                uninstallTask = Task.Run(() => uninstallAction(this), cancelUninstallToken.Token);
                                return;
                            }
                        }
                    }
                });

                if (size == 0 && uninstallTask.IsCompleted)
                {
                    uninstallTask.Dispose();//The task

                    //Reverse any moves
                    for (int i = 0; i < ContentInfo.move_dirs_src[(int)typeProcessing].Length; i++)
                    {
                        if (ContentInfo.move_dirs_src[(int)typeProcessing][i] == null || ContentInfo.move_dirs_dest[(int)typeProcessing][i] == null//It's okay if src doesn't exist since it's a reversal
                            || Directory.Exists(ContentInfo.move_dirs_dest[(int)typeProcessing][i]) == false)
                        {
                            continue;
                        }

                        FileUtils.DirectoryMove(ContentInfo.move_dirs_dest[(int)typeProcessing][i], ContentInfo.move_dirs_src[(int)typeProcessing][i], true);
                    }

                    for (int i = 0; i < ContentInfo.move_files_src[(int)typeProcessing].Length; i++)
                    {
                        if (ContentInfo.move_files_src[(int)typeProcessing][i] == null || ContentInfo.move_files_dest[(int)typeProcessing][i] == null//It's okay if src doesn't exist since it's a reversal
                            || File.Exists(ContentInfo.move_files_dest[(int)typeProcessing][i]) == false)
                        {
                            continue;
                        }

                        File.Move(ContentInfo.move_files_dest[(int)typeProcessing][i], ContentInfo.move_files_src[(int)typeProcessing][i], true);
                    }

                    if (File.Exists("manifest.json") == true)
                    {
                        List<Manifest> manifest;
                        // deserialize JSON directly from a file
                        using (StreamReader file = File.OpenText("manifest.json"))
                        {
                            manifest = JsonConvert.DeserializeObject<List<Manifest>>(file.ReadToEnd());
                        }

                        if (manifest != null && manifest.Count > 0 && manifest.Any(m => String.Equals(m.type, ContentInfo.TypesAsString[(int)typeProcessing])) == true)
                        {
                            Manifest manifestToDelete = manifest.Single(m => String.Equals(m.type, ContentInfo.TypesAsString[(int)typeProcessing]) == true);

                            manifest.Remove(manifestToDelete);

                            // serialize JSON directly to a file
                            // serialize JSON directly to a file
                            using (StreamWriter file = File.CreateText("manifest.json"))
                            {
                                file.Write(JsonConvert.SerializeObject(manifest, Formatting.Indented));
                            }
                        }
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        refreshInfo();
                    });
                }

                onProcess = 10;
            }
            #endregion

            #region install_update
            if (onProcess >= 10)
            {
                //Clean up before running
                if (onProcess == 10 || onProcess == 11)
                {
                    onProcess = 11;//Lock the method
                    if (Directory.Exists("_temp") && cleanTempFolder() == true)
                    {
                        onProcess = 12;
                        timeBuildUp = 0 - new Random().Next(3000);
                        cloneRepoTask = Task.Run(() => cloneGameRepo(this, gitCredentials), cancelCloneRepoToken.Token);
                    }
                    else
                    {
                        onProcess = 10;
                        return;
                    }
                }

                //This MUST be done to affect the UI elements in a thread off the main thread
                this.Dispatcher.Invoke(() =>
                {
                    //Report on the size
                    double percentageDone = Decimal.ToDouble(((Decimal.Divide(GetDirectorySize("_temp"), ContentInfo.repoSizes[(int)typeProcessing]))) * 100);
                    percentageDone = (percentageDone > 100 || onProcess > 5) ? 100 : percentageDone;//Prevent overflows and backflows, just in case
                    percentageDone = (percentageDone * 0.8) + 0.2;//Should be between 20%-100%
                    ((Label)this.FindName("inst_progPerc")).Content = percentageDone.ToString("#0.00") + "%";
                    ((ProgressBar)this.FindName("inst_prog")).Value = percentageDone;

                    if (onProcess < 13)
                    {
                        if (percentageDone == lastPercentage)
                        {
                            timeBuildUp += 200;//This MUST be equal to the amount of time that passes between timer intervals
                        }
                        else
                        {
                            timeBuildUp = 0 - new Random().Next(3000);
                        }

                        lastPercentage = percentageDone;
                    }

                    //Done preparing to update...
                    if ((percentageDone > 24 && onProcess == 12) || (timeBuildUp >= timeTillNextStep * 5 && onProcess == 12))//The second part is just in case it takes over 36+ seconds, we know it moved on
                    {
                        ((Label)this.FindName("inst_proc1")).Foreground = Brushes.DarkGreen;//Add this here just in case
                        ((Label)this.FindName("inst_proc2")).Foreground = Brushes.DarkGreen;
                        ((Label)this.FindName("inst_proc3")).Foreground = Brushes.DarkGoldenrod;
                        onProcess = 13;
                    }
                    //Done downloading...
                    else if (percentageDone > 38 && onProcess == 13)
                    {
                        ((Label)this.FindName("inst_proc1")).Foreground = Brushes.DarkGreen;//Add this here again just in case
                        ((Label)this.FindName("inst_proc2")).Foreground = Brushes.DarkGreen;//Add this here just in case
                        ((Label)this.FindName("inst_proc3")).Foreground = Brushes.DarkGreen;
                        ((Label)this.FindName("inst_proc4")).Foreground = Brushes.DarkGoldenrod;
                        onProcess = 14;
                    }
                    //Done decompressing...
                    else if (percentageDone > 99.4)
                    {
                        ((Label)this.FindName("inst_proc4")).Foreground = Brushes.DarkGreen;
                        ((Label)this.FindName("inst_proc5")).Foreground = Brushes.DarkGoldenrod;
                        onProcess = (onProcess > 15) ? onProcess : (byte)15;
                    }
                });

                if (cloneRepoTask.IsCompleted == true)
                {
                    if (onProcess == 15)
                    {
                        cloneRepoTask.Dispose();
                        onProcess = 16;
                        return;
                    }
                    #region perform directory and file moving/copying + manifest
                    else if (onProcess == 16)
                    {
                        onProcess = 17;

                        Debug.WriteLine("Size of _temp folder: " + GetDirectorySize("_temp"));

                        //Do copy operations
                        for (int i = 0; i < ContentInfo.copy_dirs[(int)typeProcessing].Length; i++)
                        {
                            if (ContentInfo.copy_dirs[(int)typeProcessing][i] == null)
                            {
                                continue;
                            }

                            FileUtils.DirectoryCopy(ContentInfo.copy_dirs[(int)typeProcessing][i], ContentInfo.dirs[(int)typeProcessing][i], true, true);
                        }

                        for (int i = 0; i < ContentInfo.copy_files[(int)typeProcessing].Length; i++)
                        {
                            if (ContentInfo.copy_files[(int)typeProcessing][i] == null)
                            {
                                continue;
                            }

                            File.Copy(ContentInfo.copy_files[(int)typeProcessing][i], ContentInfo.files[(int)typeProcessing][i], true);
                        }

                        //Do move operations
                        for (int i = 0; i < ContentInfo.move_dirs_src[(int)typeProcessing].Length; i++)
                        {
                            if (ContentInfo.move_dirs_src[(int)typeProcessing][i] == null || Directory.Exists(ContentInfo.move_dirs_src[(int)typeProcessing][i]) == false
                                || ContentInfo.move_dirs_dest[(int)typeProcessing][i] == null)
                            {
                                continue;
                            }

                            FileUtils.DirectoryMove(ContentInfo.move_dirs_src[(int)typeProcessing][i], ContentInfo.move_dirs_dest[(int)typeProcessing][i], true);
                        }

                        for (int i = 0; i < ContentInfo.move_files_src[(int)typeProcessing].Length; i++)
                        {
                            if (ContentInfo.move_files_src[(int)typeProcessing][i] == null || File.Exists(ContentInfo.move_files_src[(int)typeProcessing][i]) == false
                                || ContentInfo.move_files_dest[(int)typeProcessing][i] == null)
                            {
                                continue;
                            }

                            File.Move(ContentInfo.move_files_src[(int)typeProcessing][i], ContentInfo.move_files_dest[(int)typeProcessing][i], true);
                        }

                        //Movie files out of _temp
                        for (int i = 0; i < ContentInfo.git_dirs[(int)typeProcessing].Length; i++)
                        {
                            FileUtils.DirectoryMove(ContentInfo.git_dirs[(int)typeProcessing][i], ContentInfo.dirs[(int)typeProcessing][i], true);
                        }

                        for (int i = 0; i < ContentInfo.git_files[(int)typeProcessing].Length; i++)
                        {
                            File.Move(ContentInfo.git_files[(int)typeProcessing][i], ContentInfo.files[(int)typeProcessing][i], true);
                        }

                        //Amend split files
                        DirectoryInfo[] bigDirs = new DirectoryInfo(".").GetDirectories("__BIG_*", SearchOption.AllDirectories);

                        foreach (DirectoryInfo dir in bigDirs)
                        {
                            FileUtils.CombineFiles(dir.FullName, dir.FullName.Substring(0, dir.FullName.Length - dir.Name.Length) + dir.Name.Substring(6));
                            Directory.Delete(dir.FullName, true);
                        }

                        Manifest verDownloaded = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText("_temp/version.json"));
                        Manifest manifestToAdd = new Manifest
                        {
                            type = ContentInfo.TypesAsString[(int)typeProcessing],
                            version = verDownloaded.version
                        };

                        if (File.Exists("manifest.json") == false)
                        {
                            new FileInfo("manifest.json").Create().Close(); ;
                        }

                        List<Manifest> manifest;
                        // deserialize JSON directly from a file
                        using (StreamReader file = File.OpenText("manifest.json"))
                        {
                            manifest = JsonConvert.DeserializeObject<List<Manifest>>(file.ReadToEnd());
                        }

                        if (manifest == null)
                        {
                            manifest = new List<Manifest>();
                        }

                        manifest.Add(manifestToAdd);

                        // serialize JSON directly to a file
                        using (StreamWriter file = File.CreateText("manifest.json"))
                        {
                            file.Write(JsonConvert.SerializeObject(manifest, Formatting.Indented));
                        }

                        onProcess = 18;
                    }
                    #endregion

                    //This check is to prevent a thread running up on the other and breaking it
                    if (onProcess == 18)
                    {
                        if (cleanTempFolder() == false)
                        {
                            return;
                        }

                        this.Dispatcher.Invoke(() =>
                        {
                            toggleInstallationView(false);
                            refreshInfo();
                        });


                        inAProcess = false;
                        typeProcessing = ContentInfo.Types.None;
                        installTask.Dispose();
                    }
                }
            }
            #endregion
        }

        private Action<MainWindow> uninstallAllAction = (MainWindow window) =>
        {
            foreach (String[] dirs in ContentInfo.dirs)
            {
                foreach (String dir in dirs)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        //Can ignore this exception
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        //Can ignore this exception
                    }
                }
            }

            foreach (String[] files in ContentInfo.files)
            {
                foreach (String file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        //Can ignore this exception
                    }
                    catch (FileNotFoundException e)
                    { 
                        //Can ignore this exception
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        //Can ignore this exception
                    }
                }
            }
        };

        private void watchUninstallAllTask(Object state)
        {
            //Block threads getting backed up by only allowing one at a time
            if (onProcess >= 1 && onProcess != 2)
            {
                return;
            }
            else if (onProcess == 0)
            {
                onProcess = 1;

                foreach (String[] dirs in ContentInfo.dirs)
                {
                    foreach (String dir in dirs)
                    {
                        if (Directory.Exists(dir) == true)
                        {
                            totalSize += GetDirectorySize(dir);
                        }
                    }
                }

                foreach (String[] files in ContentInfo.files)
                {
                    foreach (String file in files)
                    {
                        try
                        {
                            if (File.Exists(file) == true)
                            {
                                totalSize += (ulong)new FileInfo(file).Length;
                            }
                        }
                        catch (FileNotFoundException e)
                        {
                            //Can ignore this exception
                        }
                    }
                }

                onProcess = 2;
            }

            onProcess = 3;

            ulong size = 0L;

            for (int i = 0; i < ContentInfo.dirs.Length; i++)
            {
                foreach (String dir in ContentInfo.dirs[i])
                {
                    if (Directory.Exists(dir) == true)
                    {
                        size += GetDirectorySize(dir);
                    }
                }
            }

            for (int i = 0; i < ContentInfo.files.Length; i++)
            {
                foreach (String file in ContentInfo.files[i])
                {
                    try
                    {
                        if (File.Exists(file) == true)
                        {
                            size += (ulong)new FileInfo(file).Length;
                        }
                    }
                    catch (FileNotFoundException e)
                    {
                        //Can ignore this exception
                    }
                }
            }

            //This MUST be done to affect the UI elements in a thread off the main thread
            this.Dispatcher.Invoke(() =>
            {
                //Report on the size
                double percentageDone = Math.Abs((Decimal.ToDouble(((Decimal.Divide(size, totalSize))) * 100)) - 100);
                ((Label)this.FindName("inst_progPerc")).Content = percentageDone.ToString("#0.00") + "%";
                ((ProgressBar)this.FindName("inst_prog")).Value = percentageDone;
            });

            if (uninstallTask.IsCompleted && size > 0)
            {
                uninstallTask = Task.Run(() => uninstallAllAction(this), cancelUninstallToken.Token);
            }
            else if(uninstallTask.IsCompleted)//Make sure the task was completed
            {
                File.Delete("manifest.json");
                this.Dispatcher.Invoke(() =>
                {
                    toggleInstallationView(false);
                    refreshInfo();
                });
                inAProcess = false;
                typeProcessing = ContentInfo.Types.None;

                uninstallTask.Dispose();//The task
                installTask.Dispose();//The timer
                return;
            }

            onProcess = 2;
        }

        private bool cleanTempFolder()
        {
            if (cleaningTemp == true)//Lock to prevent an IO overflow
            {
                return false;
            }

            cleaningTemp = true;

            if (Directory.Exists("_temp"))//Make sure the directory is clean first
            {
                try
                {
                    setAttributesNormal(new DirectoryInfo("_temp"));
                    Directory.Delete("_temp", true);
                    cleaningTemp = false;
                    return Directory.Exists("_temp");
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                cleaningTemp = false;
                return true;
            }
        }

        private void setAttributesNormal(DirectoryInfo dir)
        {
            try
            {
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    setAttributesNormal(subDir);

                    try
                    {
                        subDir.Attributes = FileAttributes.Normal;
                    }
                    catch
                    {
                    }
                }
            }
            catch (DirectoryNotFoundException e)
            {
                return;
                //This is an acceptable exception
            }

            foreach (FileInfo file in dir.GetFiles())
            {
                try
                {
                    file.Attributes = FileAttributes.Normal;
                }
                catch
                {
                }
            }
        } 

        //This method is needed to delete those pesky git files
        private ulong GetDirectorySize(string path)
        {
            ulong size = 0;

            if (Directory.Exists(path) == false)
            {
                return size;
            }

            DirectoryInfo dirInfo = new DirectoryInfo(path);

            try
            {
                foreach (FileInfo fi in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    size += (ulong)fi.Length;
                }
            }
            catch (UnauthorizedAccessException e)
            {
                //This error can be ignored
            }
            catch (DirectoryNotFoundException e)
            { 
                //Can ignore thie exception
            }

            return size;
        }
    }
}
