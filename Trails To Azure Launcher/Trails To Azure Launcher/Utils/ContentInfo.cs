using System;
using System.Collections.Generic;
using System.Text;

namespace Trails_To_Azure_Launcher.Models
{
    public static class ContentInfo
    {
        public enum Types
        { 
            None = -1,
            Game = 0,
            GeoLiteEdits = 1,
            Voice = 2,
            Evo_BGM = 3,
            HDPack = 4
        }

        public static char numToLetter(int num, bool upper)
        {
            const String lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const String upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            return (upper == true) ? upperCase[num] : lowerCase[num];
        }

        public static readonly String[] TypesAsString = new String[] { "game", "geoLiteEdits", "voice", "evo_bgm", "HD_pack" };
        public static readonly String[] bakedVersions = new string[] { "1.0",    "0.0.1",       "1.0",    "1.0",     "1.0" };

        //----------------------------------------------------------game--------edits--------voice-------evoBGM-------HD Pack---
        public static readonly ulong[] repoSizes = new ulong[] { 4415081520L, 842888996L, 3711028156L, 1120976905L, 294114734L };
        public static readonly ulong[] diskSize = new ulong[] {  2384078091L, 441084118L, 1869770695L,  565030136L, 241150157L };

        public static readonly String[] repoURLs = new String[] 
        { 
            "https://github.com/geofrontlite/TtA_game", "https://github.com/geofrontlite/AoNoKisekiEdits", "https://github.com/geofrontlite/TtA_EvoVoice", "https://github.com/geofrontlite/TtA_EvoBGM",
            "https://github.com/geofrontlite/TtA_HDPack"
        };

        public static readonly String[] liveVersionURLs = new String[] 
        {
            "https://raw.githubusercontent.com/geofrontlite/TtA_game/master/version.json?token=APD63P5HLUTNUEMU2P3UQN26SUZ4U",//Game
            "https://raw.githubusercontent.com/geofrontlite/AoNoKisekiEdits/master/version.json",//Edits
            "https://raw.githubusercontent.com/geofrontlite/TtA_EvoVoice/master/version.json?token=APD63P2LBCHO24VGQTPQ6726SUZ76",//Voice
            "https://raw.githubusercontent.com/geofrontlite/TtA_EvoBGM/master/version.json?token=APD63P4BDB3HW3ZBYG7QQL26SU2BO",//Evo BGM
            "https://raw.githubusercontent.com/geofrontlite/TtA_HDPack/master/version.json?token=APD63P2E3TUZD73X7KZYPYS6SUZZA"//HD pack
        };

        //ALL GENERATED FILES SHOULD BE THE LAST THINGS IN THE ARRAYS
        public static readonly String[][] git_dirs =
            {
                new String[]{ "_temp/Trails To Azure/data", "_temp/Trails To Azure/dll", "_temp/Trails To Azure/savefile" },//base game
                new String[]{ "_temp/data", "_temp/scena"},//edits
                new String[]{ "_temp/voice"},//voices
                new String[]{ "_temp/data/bgm"},//evo bgm
                new String[]{ "_temp/CEGUI", "_temp/ReShade", "_temp/SK_Res" }//HD pack
            };

        public static readonly String[][] git_files =
            {
                new String[]{ "_temp/Trails To Azure/config.exe", "_temp/Trails To Azure/ed_ao.dll", "_temp/Trails To Azure/ED_AO.exe", "_temp/Trails To Azure/user.ft" },//base game
                new String[]{ "_temp/config.exe", "temp/ED_AO.exe" },//edits
                new String[]{ "_temp/dinput8.dll" },//voices
                new String[]{ "_temp/data/text/t_bgm._dt"},//evo bgm
                new String[]{ "_temp/d3d9.dll" }//HD pack
            };

        public static readonly String[][] dirs =
            {
                //Keeping save data. Maybe want to give the user an option at some point
                new String[]{ "data", "dll", "savefile"/*, "savedata"*/ },//base game
                new String[]{ "data", "data/scena" },//edits
                new String[]{ "voice", "voice/scena"},//voices
                new String[]{ "data/bgm"},//evo bgm
                new String[]{ "CEGUI", "ReShade", "SK_Res", "logs" }//HD pack
            };

        public static readonly String[][] files =
            {
                new String[]{ "config.exe", "ed_ao.dll", "ED_AO.exe", "user.ft", "config.ini" },//base game
                new String[]{ "config.exe", "ED_AO.exe" },//edits
                new String[]{ "dinput8.dll"},//voices
                new String[]{ "data/text/t_bgm._dt"},//evo bgm
                new String[]{ "d3d9.dll", "d3d9.ini" }//HD pack
            };

        //This is the source from which the copy happens. The destination is the corropsonding path in the dirs array
        public static readonly String[][] copy_dirs =
            {
                new String[]{ },//base game (Empty on purpose)
                new String[]{ },//edits (Empty on purpose)
                new String[]{ null, "data/scena" },//voices
                new String[]{ },//evo bgm (Empty on purpose)
                new String[]{ }//HD pack (Empty on purpose)
            };

        //This is the source from which the copy happens. The destination is the corropsonding path in the files array
        public static readonly String[][] copy_files =
            {
                new String[]{ },//base game (Empty on purpose)
                new String[]{ },//edits (Empty on purpose)
                new String[]{ },//voices (Empty on purpose)
                new String[]{ },//evo bgm (Empty on purpose)
                new String[]{ }//HD pack (Empty on purpose)
            };

        //Moving works as follows: src[i] -> dest[i] (MUST BE 1:1 with dest!)
        public static readonly String[][] move_dirs_src =
            {
                new String[]{ },//base game (Empty on purpose)
                new String[]{ },//edits (Empty on purpose)
                new String[]{ },//voices (Empty on purpose)
                new String[]{ "data/bgm" },//evo bgm
                new String[]{ }//HD pack (Empty on purpose)
            };

        //Moving works as follows: src[i] -> dest[i] (MUST BE 1:1 with src!)
        public static readonly String[][] move_dirs_dest =
            {
                new String[]{ },//base game (Empty on purpose)
                new String[]{ },//edits (Empty on purpose)
                new String[]{ },//voices (Empty on purpose)
                new String[]{ "data/bgm_org" },//evo bgm
                new String[]{ }//HD pack (Empty on purpose)
            };

        //Moving works as follows: src[i] -> dest[i] (MUST BE 1:1 with dest!)
        public static readonly String[][] move_files_src =
            {
                new String[]{ },//base game (Empty on purpose)
                new String[]{ },//edits (Empty on purpose)
                new String[]{ },//voices (Empty on purpose)
                new String[]{ "data/text/t_bgm._dt" },//evo bgm
                new String[]{ }//HD pack (Empty on purpose)
            };

        //Moving works as follows: src[i] -> dest[i] (MUST BE 1:1 with src!)
        public static readonly String[][] move_files_dest =
            {
                new String[]{ },//base game (Empty on purpose)
                new String[]{ },//edits (Empty on purpose)
                new String[]{ },//voices (Empty on purpose)
                new String[]{ "data/text/t_bgm._dt.bak" },//evo bgm
                new String[]{ }//HD pack (Empty on purpose)
            };


        public static readonly String[][] installMessages =
            {
                //base game
                new String[6]{ "Installing: Trails to Azure", "Allocating space.", "Preparing to download.", "Downloading files (this may take a few minutes).", "Decompressing files.", "Cleaning up." },
                //edits
                new String[6]{ "Installing: Trails to Azure -\n\t Geofront Lite Edit", "Allocating space.", "Preparing to download.", "Downloading files (this may take a few minutes).", "Decompressing files.", "Cleaning up." },
                //voice mod
                new String[6]{ "Installing: Evo Voice Mod", "Allocating space.", "Preparing to download.", "Downloading files (this may take a few minutes).", "Decompressing files.", "Cleaning up." },
                //evo bgm mod
                new String[6]{ "Installing: Evo BGM Mod", "Allocating space.", "Preparing to download.", "Downloading files (this may take a few minutes).", "Decompressing files.", "Cleaning up." },
                //HD mod
                new String[6]{ "Installing: HD Pack", "Allocating space.", "Preparing to download.", "Downloading files (this may take a few minutes).", "Decompressing files.", "Cleaning up." }
            };

        public static readonly String[][] uninstallMessages =
            {
                new String[3]{ "Uninstalling: Trails to Azure", "Removing files.", "Cleaning up." },//base game
                new String[]{ },//edits (Empty on purpose)
                new String[3]{ "Uninstalling: Evo Voice Mod", "Removing files.", "Cleaning up." },//voice mod
                new String[3]{ "Uninstalling: Evo BGM Mod", "Removing files.", "Cleaning up." },//evo bgm mod
                new String[3]{ "Uninstalling: HD Pack", "Removing files.", "Cleaning up." }//HD mod
            };

        public static readonly String[][] updateMessages =
            {
                //base game
                new String[6]{ "Updating: Trails to Azure", "Removing files.", "Preparing to update.", "Downloading files (this may take a few minutes).", "Decompressing files.", "Cleaning up." },
                //edits
                new String[6]{ "Updating: Trails to Azure -\n\t Geofront Lite Edit", "Removing files.", "Preparing to update.", "Downloading files (this may take a few minutes).", "Decompressing files.", "Cleaning up." },
                //voice mod
                new String[6]{ "Updating: Evo Voice Mod", "Removing files.", "Preparing to update.", "Downloading files (this may take a few minutes).", "Decompressing files.", "Cleaning up." },
                //evo bgm mod
                new String[6]{ "Updating: Evo BGM Mod", "Removing files.", "Preparing to update.", "Downloading files (this may take a few minutes).", "Decompressing files.", "Cleaning up." },
                //HD mod
                new String[6]{ "Updating: HD Pack", "Removing files.", "Preparing to update.", "Downloading files (this may take a few minutes).", "Decompressing files.", "Cleaning up." },
            };
    }
}
