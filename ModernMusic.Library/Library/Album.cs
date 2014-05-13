using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ModernMusic.Library
{
    [ProtoContract]
    public class Album : INotifyPropertyChanged
    {
        [ProtoMember(1)]
        public Guid ID = Guid.NewGuid();

        public delegate void AlbumTappedHandler(Album album);

        private string _imagePath = null;
        private string _cachedImagePath = "ms-appx:///Assets/DarkGray.png";
        
        [ProtoMember(2)]
        public string AlbumName { get; set; }
        [ProtoMember(3)]
        public string Artist { get; set; }
        [ProtoMember(4)]
        public string ImagePath
        {
            get { return _imagePath; }
            set { _imagePath = value; /*OnPropertyChanged<string>();*/ }
        }
        [ProtoMember(5)]
        public string CachedImagePath
        {
            get { return _cachedImagePath; }
            set { _cachedImagePath = value; OnPropertyChanged<string>(); }
        }

        public Album Self { get { return this; } }

        public Album() { }

        public Album(string artist, string albumName)
        {
            this.Artist = artist;
            this.AlbumName = albumName;
            this.CachedImagePath = "ms-appx:///Assets/DarkGray.png";
        }

        public override string ToString()
        {
            return this.AlbumName;
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
