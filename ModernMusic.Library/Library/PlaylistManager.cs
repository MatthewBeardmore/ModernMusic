using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ModernMusic.Library
{
    public class PlaylistManager
    {
        private static PlaylistManager _instance;
        public static PlaylistManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PlaylistManager();
                return _instance;
            }
        }

        private int _hasLoadedPlaylist;

        [DataMember]
        public List<Playlist> PlaylistDataMember { get; set; }

        public ObservableCollection<Playlist> Playlists { get; private set; }

        public PlaylistManager()
        {
            PlaylistDataMember = new List<Playlist>();
            Playlists = new ObservableCollection<Playlist>();
        }

        public void AddPlaylist(Playlist playlist)
        {
            PlaylistDataMember.Add(playlist);
            Playlists.Add(playlist);
        }

        public void RemovePlaylist(Playlist playlist)
        {
            PlaylistDataMember.Remove(playlist);
            Playlists.Remove(playlist);
        }

        public async Task LoadPlaylists()
        {
            if (Interlocked.CompareExchange(ref _hasLoadedPlaylist, 1, 0) == 1)
                return;

            try
            {
                StorageFile cacheFile = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync("playlistcache");
                await Deserialize(cacheFile);
            }
            catch { }
        }

        public async Task Deserialize(StorageFile file)
        {
            if (file == null)
                file = await ApplicationData.Current.LocalFolder.GetFileAsync("playlistcache");

            using (IInputStream inStream = await file.OpenSequentialReadAsync())
            {
                var serializer = new DataContractJsonSerializer(typeof(PlaylistManager));
                var cache = (PlaylistManager)serializer.ReadObject(inStream.AsStreamForRead());

                foreach (var playlist in cache.PlaylistDataMember)
                    this.AddPlaylist(playlist);
            }
        }

        public async Task Serialize()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var _Serializer = new DataContractJsonSerializer(GetType());
                _Serializer.WriteObject(ms, this);
                ms.Position = 0;

                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("playlistcache", CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await file.OpenStreamForWriteAsync())
                {
                    await ms.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                }
            }
        }
    }
}
