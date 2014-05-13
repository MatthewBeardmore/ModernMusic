using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using ModernMusic.Library;

namespace ModernMusic.Helpers
{
    [DataContract]
    public class Settings
    {
        private static Settings _settings;
        public static Settings Instance
        {
            get
            {
                if (_settings == null)
                    _settings = Load();
                return _settings;
            }
        }

        [DataMember]
        public bool ClearCacheOnNextStart { get; set; }

        [DataMember]
        public bool AllowXboxMusicIntegration { get; set; }

        public static Settings Load()
        {
            if(!Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("Settings"))
                return new Settings();

            var obj = Windows.Storage.ApplicationData.Current.LocalSettings.Values["Settings"];
            return JsonSerialization.Deserialize<Settings>(obj.ToString());
        }

        public void Save()
        {
            Windows.Storage.ApplicationData.Current.LocalSettings.Values["Settings"] = JsonSerialization.Serialize(this);
        }
    }
}
