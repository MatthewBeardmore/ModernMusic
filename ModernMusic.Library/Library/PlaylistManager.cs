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
using ModernMusic.Helpers;
using ProtoBuf;

namespace ModernMusic.Library
{
    [ProtoContract]
    public class PlaylistManager
    {
        private static PlaylistManager _instance;
        public static PlaylistManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PlaylistManager();
                    AsyncInline.Run(new Func<Task>(() => _instance.LoadPlaylists()));
                }
                return _instance;
            }
        }

        private int _hasLoadedPlaylist;

        [ProtoMember(1)]
        public List<Playlist> PlaylistDataMember { get; set; }

        public ObservableCollection<Playlist> Playlists { get; private set; }

        public PlaylistManager()
        {
            PlaylistDataMember = new List<Playlist>();
            Playlists = new ObservableCollection<Playlist>();
        }

        public async Task AddPlaylist(Playlist playlist)
        {
            PlaylistDataMember.Add(playlist);
            Playlists.Add(playlist);
            await PlaylistManager.Instance.Serialize();
        }

        public Playlist GetPlaylist(Guid id)
        {
            foreach (Playlist playlist in PlaylistDataMember)
            {
                if (playlist.ID == id)
                    return playlist;
            }

            return null;
        }

        public async Task RemovePlaylist(Playlist playlist)
        {
            PlaylistDataMember.Remove(playlist);
            Playlists.Remove(playlist);
            await PlaylistManager.Instance.Serialize();
        }

        public async Task DeleteSong(Song song)
        {
            foreach (Playlist playlist in PlaylistDataMember)
            {
                if (playlist.Songs.Contains(song))
                {
                    playlist.Songs.Remove(song);
                    if (NowPlayingInformation.CurrentPlaylist.ID == playlist.ID)
                        NowPlayingInformation.CurrentPlaylist = playlist;
                }
            }
            await PlaylistManager.Instance.Serialize();
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

        private async Task Deserialize(StorageFile file)
        {
            if (file == null)
                file = await ApplicationData.Current.LocalFolder.GetFileAsync("playlistcache");

            using (IInputStream inStream = await file.OpenSequentialReadAsync())
            {
                var cache = (PlaylistManager)LibrarySerializer.Create().Deserialize(inStream.AsStreamForRead(),
                    null, typeof(PlaylistManager));
                if (cache != null)
                {
                    foreach (var playlist in cache.PlaylistDataMember)
                        await this.AddPlaylist(playlist);
                }
            }
        }

        public async Task Serialize()
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("playlistcache", CreationCollisionOption.ReplaceExisting);
            using (Stream fileStream = await file.OpenStreamForWriteAsync())
            {
                LibrarySerializer.Create().Serialize(fileStream, this);
            }
        }
    }
}
