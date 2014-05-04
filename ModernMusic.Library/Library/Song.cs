using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;

namespace ModernMusic.Library
{
    [DataContract]
    public class Song : INotifyPropertyChanged
    {
        public delegate void SongTappedHandler(Song song);

        [DataMember]
        public string FilePath { get; private set; }
        [DataMember]
        public string SongTitle { get; private set; }
        [DataMember]
        public string Album { get; set; }
        [DataMember]
        public string Artist { get; set; }
        [DataMember]
        public uint TrackNumber { get; set; }

        public Song Self { get { return this; } }
        public bool Selected { get; private set; }

        public Song(string artist, string albumName, String filePath, String songTitle, MusicProperties songProperties)
        {
            this.FilePath = filePath;
            this.SongTitle = songTitle;
            this.Artist = artist;
            this.Album = albumName;
            this.TrackNumber = songProperties.TrackNumber;
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

        public void ClearSelection(bool fireProperty = true)
        {
            Selected = false;
            if (fireProperty)
                this.OnPropertyChanged<bool>();
        }

        public void Select()
        {
            Selected = true;
            this.OnPropertyChanged<bool>();
        }
    }
}
