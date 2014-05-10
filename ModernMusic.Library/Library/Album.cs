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
    [DataContract]
    public class Album : INotifyPropertyChanged
    {
        [DataMember]
        public Guid ID = Guid.NewGuid();

        public delegate void AlbumTappedHandler(Album album);

        private string _imagePath = "ms-appx:///Assets/MediumGray.png";
        
        [DataMember]
        public string AlbumName { get; private set; }
        [DataMember]
        public string Artist { get; private set; }
        [DataMember]
        public string ImagePath
        {
            get { return _imagePath; }
            set { _imagePath = value; OnPropertyChanged<string>(); }
        }

        public Album Self { get { return this; } }

        public Album(string artist, string albumName)
        {
            this.Artist = artist;
            this.AlbumName = albumName;
            this.ImagePath = "ms-appx:///Assets/MediumGray.png";
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
