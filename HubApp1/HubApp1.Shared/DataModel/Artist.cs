using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;

namespace ModernMusic.MusicLibrary
{
    [DataContract]
    public class Artist : INotifyPropertyChanged
    {
        [DataMember]
        public string ArtistName { get; private set; }

        public Artist(String artistName)
        {
            this.ArtistName = artistName;
        }

        public override string ToString()
        {
            return this.ArtistName;
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
