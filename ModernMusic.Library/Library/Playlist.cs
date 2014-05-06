using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Storage;

namespace ModernMusic.Library
{
    [DataContract]
    public class Playlist
    {
        #region Public properties, events and handlers

        public delegate void PlaylistTappedHandler(Playlist playlist);

        [DataMember]
        public List<Song> Songs
        {
            get;
            set;
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public Guid ID = Guid.NewGuid();

        #endregion

        public Playlist()
        {
            Songs = new List<Song>();
        }
    }
}
