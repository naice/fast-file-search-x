using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ffsx
{
    public class Settings
    {
        public string SlittePath{get;set;}
        public Dictionary<string, string> Favorites { get; set; }
        public string SearchPath{get;set;}
        public string SearchToken{get;set;}
        public string SelectedFileMask { get; set; }
        public List<string> FileMasks{get;set;}

        public Settings()
        {
        }

        public void Default()
        {
            SlittePath = @"\\STORAGE\Slitte";
            Favorites = new Dictionary<string, string>();
            SelectedFileMask = "*.*";
            FileMasks = new List<string>() { 
                "*.*",
                "*.cs; *.xaml;",
                "*.csproj; *.sln",
                "*.html; *.htm; *.php; *.css",
                "*.cpp; *.h; *.dfm",
            };
        }

        #region Storage
        [Newtonsoft.Json.JsonIgnore]
        public string Path { get; set; }

        public Settings Load()
        {
            return LoadFrom(Path);
        }
        public static Settings LoadFrom(string path)
        {
            Settings settings = null;

            try
            {
                settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));
            }
            catch { }

            if (settings == null)
            {
                settings = new Settings();
                settings.Default();
            }

            settings.Path = path;

            return settings;
        }

        public void Save()
        {
            SaveTo(Path);
        }
        public void SaveTo(string path)
        {
            File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
        }
        #endregion
    }

}