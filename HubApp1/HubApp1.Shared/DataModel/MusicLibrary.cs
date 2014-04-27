using HubApp1.Common;
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
using System.Xml.Serialization;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace ModernMusic.MusicLibrary
{
    [DataContract]
    public class MusicLibrary
    {
        private static MusicLibrary _instance;
        public static MusicLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MusicLibrary();
                    _instance.LoadLibrary();
                }
                return _instance;
            }
        }

        private int _hasLoadedArtists;

        [DataMember]
        public ObservableCollection<Artist> Artists { get; private set; }
        [DataMember]
        public ObservableCollection<Album> Albums { get; private set; }
        [DataMember]
        public ObservableCollection<Song> Songs { get; private set; }

        private MusicLibraryCache _cache;

        private MusicLibrary()
        {
            Artists = new ObservableCollection<Artist>();
            Albums = new ObservableCollection<Album>();
            Songs = new ObservableCollection<Song>();
            _cache = new MusicLibraryCache();
        }

        public async void LoadLibrary()
        {
            if (Interlocked.CompareExchange(ref _hasLoadedArtists, 1, 0) == 1)
                return;

            try
            {
                await Windows.Storage.ApplicationData.Current.LocalFolder.GetItemAsync("cache");

                {
                    await _cache.Deserialize();

                    foreach (Artist artist in _cache.Artists.Values)
                        Artists.Add(artist);
                    foreach (List<Album> albums in _cache.Albums.Values)
                    {
                        foreach (Album album in albums)
                            Albums.Add(album);
                    }
                    foreach (List<Song> songs in _cache.Songs.Values)
                    {
                        foreach (Song song in songs)
                            Songs.Add(song);
                    }
                }
            }
            catch { }

            await TraverseFolder(KnownFolders.MusicLibrary);

            await _cache.Serialize();

            Serialize();
        }

        public void Serialize()
        {
            /*using (MemoryStream ms = new MemoryStream())
            {
                var _Serializer = new DataContractJsonSerializer(GetType());
                _Serializer.WriteObject(ms, this);
                ms.Position = 0;
                
                using(StreamReader reader = new StreamReader(ms))
                {
                    string cache = reader.ReadToEnd();
                }
            }*/
        }

        private async Task TraverseFolder(StorageFolder folder)
        {
            foreach (StorageFile file in await folder.GetFilesAsync())
            {
                MusicProperties songProperties = await file.Properties.GetMusicPropertiesAsync();

                Artist artist = _cache.CreateIfNotExist(FixArtistName(songProperties.Artist));
                Album album = _cache.CreateIfNotExist(FixArtistName(songProperties.Artist), FixAlbumName(songProperties.Album));
                Song song = _cache.CreateIfNotExist(songProperties.Album, songProperties.Title, file.Path, songProperties);

                if (artist != null)
                    Artists.Add(artist);
                if (album != null)
                    Albums.Add(album);
                if (song != null)
                    Songs.Add(song);
            }
            foreach (StorageFolder childFolder in await folder.GetFoldersAsync())
            {
                await TraverseFolder(childFolder);
            }
        }

        private string FixArtistName(string artist)
        {
            if (artist == "")
                return "Unknown Artist";
            return artist;
        }

        private string FixAlbumName(string album)
        {
            if (album == "")
                return "Unknown Album";
            return album;
        }

        public void DownloadAlbumArt(Album album)
        {

        }

        public async void DownloadAlbumArt(Artist artist)
        {
            JsonArray array = await XboxMusicConnection.GetAllAlbumData(artist);
            for(int i = 0; i < array.Count; i++)
            {

            }
        }

        public List<Album> GetAlbums(Artist artist)
        {
            return _cache.GetAlbums(artist.ArtistName);
        }

        public List<Song> GetSongs(Album album)
        {
            return _cache.GetSongs(album.AlbumName);
        }

        public List<Song> GetSongs(Artist artist)
        {
            return _cache.GetSongsForArtist(artist.ArtistName);
        }
    }

    [DataContract]
    public class MusicLibraryCache
    {
        [DataMember]
        public Dictionary<string, Artist> Artists { get; private set; }
        [DataMember]
        public Dictionary<string, List<Album>> Albums { get; private set; }
        [DataMember]
        public Dictionary<string, List<Song>> Songs { get; private set; }

        public MusicLibraryCache()
        {
            Artists = new Dictionary<string, Artist>();
            Albums = new Dictionary<string, List<Album>>();
            Songs = new Dictionary<string, List<Song>>();
        }

        public Artist CreateIfNotExist(string artistName)
        {
            Artist artist;
            if (!Artists.TryGetValue(artistName, out artist))
            {
                artist = new Artist(artistName);
                Artists.Add(artistName, artist);
                return artist;
            }
            return null;
        }

        public Album CreateIfNotExist(string artistName, string albumName)
        {
            Album album = new Album(artistName, albumName);
            List<Album> albums;
            if (!Albums.TryGetValue(artistName, out albums))
            {
                albums = new List<Album>();
                albums.Add(album);
                Albums.Add(artistName, albums);
                return album;
            }
            else
            {
                if(albums.FirstOrDefault((a)=>a.AlbumName == albumName) == null)
                {
                    albums.Add(album);
                    return album;
                }
            }
            return null;
        }

        public Song CreateIfNotExist(string albumName, string songName, string filePath, MusicProperties props)
        {
            Song song = new Song(filePath, songName, props);
            List<Song> songs;
            if (!Songs.TryGetValue(albumName, out songs))
            {
                songs = new List<Song>();
                Songs.Add(albumName, songs);
            }
            if (songs.FirstOrDefault((s) => s.FilePath == song.FilePath) == null)
            {
                songs.Add(song);
                return song;
            }
            return null;
        }

        public Artist Get(string artistName)
        {
            Artist artist;
            if (!Artists.TryGetValue(artistName, out artist))
                return null;
            return artist;
        }

        public Album Get(string artistName, string albumName)
        {
            List<Album> albums;
            if (!Albums.TryGetValue(artistName, out albums))
                return null;
            return albums.FirstOrDefault((a) => a.AlbumName == albumName);
        }

        public List<Album> GetAlbums(string artistName)
        {
            List<Album> albums;
            if (!Albums.TryGetValue(artistName, out albums))
                return null;
            return albums;
        }

        public List<Song> GetSongs(string albumName)
        {
            List<Song> song;
            if (!Songs.TryGetValue(albumName, out song))
                return null;
            return song;
        }

        public List<Song> GetSongsForArtist(string artist)
        {
            List<Song> songs = new List<Song>();
            foreach (Album album in GetAlbums(artist))
            {
                List<Song> song;
                if (!Songs.TryGetValue(album.AlbumName, out song))
                    continue;
                songs.AddRange(song);
            }
            return songs;
        }
        
        public async Task Deserialize()
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("cache");
            using (IInputStream inStream = await file.OpenSequentialReadAsync())
            {
                var serializer = new DataContractJsonSerializer(typeof(MusicLibraryCache));
                var cache = (MusicLibraryCache)serializer.ReadObject(inStream.AsStreamForRead());

                this.Artists = cache.Artists;
                this.Albums = cache.Albums;
                this.Songs = cache.Songs;
            }
        }

        public async Task Serialize()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var _Serializer = new DataContractJsonSerializer(GetType());
                _Serializer.WriteObject(ms, this);
                ms.Position = 0;

                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("cache", CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await file.OpenStreamForWriteAsync())
                {
                    await ms.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                }
            }
        }
    }
}
