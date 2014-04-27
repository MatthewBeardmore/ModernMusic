using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;

namespace ModernMusic.MusicLibrary
{
    [DataContract]
    public class Song : INotifyPropertyChanged
    {
        [DataMember]
        public string FilePath { get; private set; }
        [DataMember]
        public string SongTitle { get; private set; }
        [DataMember]
        public string Album { get; set; }
        [DataMember]
        public string Artist { get; set; }

        public Song(String filePath, String songTitle, MusicProperties songProperties)
        {
            this.FilePath = filePath;
            this.SongTitle = songTitle;
            this.Album = songProperties.Album;
            this.Artist = songProperties.Artist;
        }

        public override string ToString()
        {
            return this.SongTitle;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged<T>([CallerMemberName]string caller = null)
        {
            // make sure only to call this if the value actually changes

            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(caller));
            }
        }
    }
}
