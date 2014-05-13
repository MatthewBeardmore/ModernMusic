using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ModernMusic.Library
{
    [ProtoContract]
    public class Song : INotifyPropertyChanged
    {
        [ProtoMember(1)]
        public Guid ID = Guid.NewGuid();

        public delegate void SongTappedHandler(Song song);

        [ProtoMember(2)]
        public string FilePath { get; set; }
        [ProtoMember(3)]
        public string SongTitle { get; set; }
        [ProtoMember(4)]
        public string Album { get; set; }
        [ProtoMember(5)]
        public string Artist { get; set; }
        [ProtoMember(6)]
        public uint TrackNumber { get; set; }

        public Song Self { get { return this; } }
        public bool Selected { get; private set; }
        public string ArtistCaps { get { return Artist.ToUpper(); } }
        public string AlbumLower { get { return Album.ToLower(); } }

        public Song() { }

        public Song(string artist, string albumName, String filePath, String songTitle, uint trackNumber)
        {
            this.FilePath = filePath;
            this.SongTitle = songTitle;
            this.Artist = artist;
            this.Album = albumName;
            this.TrackNumber = trackNumber;
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
