using ffsx.Class;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ffsx
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Settings Settings { get; set; }
        public static string DIR_APPDATA { get; private set; }
        public static string DIR_SETUPS { get; private set; }
        public static string FILE_LASTSETUP { get; private set; }        

        public App()
        {
            DIR_APPDATA = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ffsx");
            if (!Directory.Exists(DIR_APPDATA))
                Directory.CreateDirectory(DIR_APPDATA);
            DIR_SETUPS = Path.Combine(DIR_APPDATA, "Settings");
            if (!Directory.Exists(DIR_SETUPS))
                Directory.CreateDirectory(DIR_SETUPS);
            FILE_LASTSETUP = Path.Combine(DIR_APPDATA, "_.config");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            string defaultSetupFile = Path.Combine(DIR_SETUPS, "default.xml");
            if (!File.Exists(FILE_LASTSETUP)) File.WriteAllText(FILE_LASTSETUP, defaultSetupFile);
            if (!Directory.Exists(DIR_SETUPS)) Directory.CreateDirectory(DIR_SETUPS);
            if (!File.Exists(defaultSetupFile)) { var d = new Settings(); d.Default(); d.SaveTo(defaultSetupFile); }
            string lastSetupFile = File.ReadAllText(FILE_LASTSETUP);
            if (!File.Exists(lastSetupFile)) lastSetupFile = defaultSetupFile;
            Settings = Settings.LoadFrom(lastSetupFile);
            Resources["Settings"] = Settings;

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Settings.Save();
            MouseHook.StopHook();
            base.OnExit(e);
        }
    }
}
