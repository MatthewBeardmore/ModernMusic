using ModernMusic.Helpers;
using ModernMusic.Library.Helpers;
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
using Windows.UI.Xaml.Data;

namespace ModernMusic.Library
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
                    var t = _instance.LoadLibrary();
                }
                return _instance;
            }
        }

        private int _hasLoadedArtists;

        [DataMember]
        public CollectionViewSource ArtistsCollection { get; private set; }
        public RealObservableCollection<GroupInfoList<Artist>> ArtistGroupDictionary { get; private set; }

        [DataMember]
        public CollectionViewSource AlbumsCollection { get; private set; }
        public RealObservableCollection<GroupInfoList<Album>> AlbumGroupDictionary { get; private set; }

        [DataMember]
        public CollectionViewSource SongsCollection { get; private set; }
        public RealObservableCollection<GroupInfoList<Song>> SongGroupDictionary { get; private set; }

        public static RealObservableCollection<GroupInfoList<TSource>> CreateObservableGroupDictionary<TSource>()
        {
            var keys = "#abcdefghijklmnopqrstuvwxyz".ToCharArray().Select(x => x.ToString()).ToList();
            keys.Add("\uD83C\uDF10");

            return new RealObservableCollection<GroupInfoList<TSource>>(
                keys.Select(x => new GroupInfoList<TSource>() { Key = x }));
        }

        public static void AddItemToGroup<T>(RealObservableCollection<GroupInfoList<T>> group, T addition, Func<T, char> keyConversion)
        {
            string key = (keyConversion(addition).ToString().ToLower());
            if (Char.IsDigit(key[0]) || Char.IsSymbol(key[0]))
                key = "#";
            else if (!Char.IsLetter(key[0]))
                key = "\uD83C\uDF10";

            GroupInfoList<T> list = group.FirstOrDefault((g) => g.Key.ToString() == key);
            if(list == null)
            {
                key = "\uD83C\uDF10";
                list = group.First((g) => g.Key.ToString() == key);
            }
            list.Add(addition);
        }

        private MusicLibraryCache _cache;

        private MusicLibrary()
        {
            ArtistGroupDictionary = CreateObservableGroupDictionary<Artist>();
            ArtistsCollection = new CollectionViewSource() { IsSourceGrouped = true, Source = ArtistGroupDictionary };

            AlbumGroupDictionary = CreateObservableGroupDictionary<Album>();
            AlbumsCollection = new CollectionViewSource() { IsSourceGrouped = true, Source = AlbumGroupDictionary };

            SongGroupDictionary = CreateObservableGroupDictionary<Song>();
            SongsCollection = new CollectionViewSource() { IsSourceGrouped = true, Source = SongGroupDictionary };

            _cache = new MusicLibraryCache();
        }

        public async Task LoadLibrary()
        {
            if (Interlocked.CompareExchange(ref _hasLoadedArtists, 1, 0) == 1)
                return;

            try
            {
                StorageFile cacheFile = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync("cache");

                {
                    await _cache.Deserialize(cacheFile);

                    foreach (Artist artist in _cache.Artists.Values)
                    {
                        AddItemToGroup(ArtistGroupDictionary, artist, a => a.ArtistName[0]);
                    }
                    foreach (List<Album> albums in _cache.Albums.Values)
                    {
                        foreach (Album album in albums)
                            AddItemToGroup(AlbumGroupDictionary, album, a => a.AlbumName[0]);
                    }
                    foreach (List<Song> songs in _cache.Songs.Values)
                    {
                        foreach (Song song in songs)
                            AddItemToGroup(SongGroupDictionary, song, a => song.SongTitle[0]);
                    }
                }
            }
            catch { }

            await TraverseFolder(KnownFolders.MusicLibrary);

            await LoadAllArtwork();

            await _cache.Serialize();
        }

        private async Task TraverseFolder(StorageFolder folder)
        {
            foreach (StorageFile file in await folder.GetFilesAsync())
            {
                MusicProperties songProperties = await file.Properties.GetMusicPropertiesAsync();

                if (songProperties.Title == "" && songProperties.Album == "" && songProperties.Artist == "")
                    continue;

                Artist artist = _cache.CreateIfNotExist(FixArtistName(songProperties.Artist));
                Album album = _cache.CreateIfNotExist(FixArtistName(songProperties.Artist), FixAlbumName(songProperties.Album));
                Song song = _cache.CreateIfNotExist(FixArtistName(songProperties.Artist), FixAlbumName(songProperties.Album),
                    FixSongName(songProperties.Title, file.DisplayName), file.Path, songProperties);

                if (artist != null)
                    AddItemToGroup(ArtistGroupDictionary, artist, a => a.ArtistName[0]);
                if (album != null)
                    AddItemToGroup(AlbumGroupDictionary, album, a => a.AlbumName[0]);
                if (song != null)
                    AddItemToGroup(SongGroupDictionary, song, a => song.SongTitle[0]);
            }
            foreach (StorageFolder childFolder in await folder.GetFoldersAsync())
            {
                await TraverseFolder(childFolder);
            }
        }

        private string FixSongName(string songTitle, string displayName)
        {
            if (songTitle == "")
                return displayName;
            return songTitle;
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

        public async Task DownloadAlbumArt(Artist artist)
        {
            if (artist.HasDownloadedArtistData)
                return;

            JsonArray array = await XboxMusicConnection.GetAllAlbumData(artist);

            if (array == null)
                return;

            for(uint i = 0; i < array.Count; i++)
            {
                JsonObject albumInfo = array.GetObjectAt(i);
                string albumName = albumInfo["Name"].GetString();
                string imagePath = albumInfo["ImageUrl"].GetString();

                Album album = _cache.Get(artist.ArtistName, albumName);
                if (album == null)
                    continue;

                album.ImagePath = imagePath;
            }
        }

        public async Task LoadAllArtwork()
        {
            for (int i = 0; i < ArtistGroupDictionary.Count; i++)
            {
                foreach(Artist artist in ArtistGroupDictionary[i])
                {
                    await DownloadAlbumArt(artist);
                    await Task.Yield();
                }
            }
            await Task.Yield();
        }

        public List<Album> GetAlbums(Artist artist)
        {
            return _cache.GetAlbums(artist.ArtistName);
        }

        public Album GetAlbum(Song song)
        {
            return _cache.Get(song.Artist, song.Album);
        }

        public List<Song> GetSongs(Album album)
        {
            return _cache.GetSongs(album.Artist, album.AlbumName);
        }

        public List<Song> GetSongs(Artist artist)
        {
            return _cache.GetSongsForArtist(artist.ArtistName);
        }

        public Artist GetArtist(Album album)
        {
            return _cache.Get(album.Artist);
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
            if (!Artists.TryGetValue(artistName.ToLower(), out artist))
            {
                artist = new Artist(artistName);
                Artists.Add(artistName.ToLower(), artist);
                return artist;
            }
            return null;
        }

        public Album CreateIfNotExist(string artistName, string albumName)
        {
            Album album = new Album(artistName, albumName);
            List<Album> albums;
            if (!Albums.TryGetValue(artistName.ToLower(), out albums))
            {
                albums = new List<Album>();
                albums.Add(album);
                Albums.Add(artistName.ToLower(), albums);
                return album;
            }
            else
            {
                if (albums.FirstOrDefault((a) => a.AlbumName.ToLower().Replace(" ", "") == 
                    albumName.ToLower().Replace(" ", "")) == null)
                {
                    albums.Add(album);
                    return album;
                }
            }
            return null;
        }

        public Song CreateIfNotExist(string artist, string albumName, string songName, string filePath, MusicProperties props)
        {
            Song song = new Song(artist, albumName, filePath, songName, props);
            List<Song> songs;
            if (!Songs.TryGetValue(artist.ToLower() + "--" + albumName.ToLower(), out songs))
            {
                songs = new List<Song>();
                Songs.Add(artist.ToLower() + "--" + albumName.ToLower(), songs);
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
            if (!Artists.TryGetValue(artistName.ToLower(), out artist))
                return null;
            return artist;
        }

        public Album Get(string artistName, string albumName)
        {
            List<Album> albums;
            if (!Albums.TryGetValue(artistName.ToLower(), out albums))
                return null;
            return albums.FirstOrDefault((a) => a.AlbumName.ToLower().Replace(" ", "") == albumName.ToLower().Replace(" ", ""));
        }

        public List<Album> GetAlbums(string artistName)
        {
            List<Album> albums;
            if (!Albums.TryGetValue(artistName.ToLower(), out albums))
                return null;
            return albums;
        }

        public List<Song> GetSongs(string artist, string albumName)
        {
            List<Song> song;
            if (!Songs.TryGetValue(artist.ToLower() + "--" + albumName.ToLower(), out song))
                return null;
            song.Sort((a,b) => a.TrackNumber.CompareTo(b.TrackNumber));
            return song;
        }

        public List<Song> GetSongsForArtist(string artist)
        {
            List<Song> songs = new List<Song>();
            foreach (Album album in GetAlbums(artist))
            {
                List<Song> song;
                if (!Songs.TryGetValue(artist.ToLower() + "--" + album.AlbumName.ToLower(), out song))
                    continue;
                songs.AddRange(song);
            }
            return songs;
        }
        
        public async Task Deserialize(StorageFile file)
        {
            if (file == null)
                file = await ApplicationData.Current.LocalFolder.GetFileAsync("cache");
            if(Settings.Instance.ClearCacheOnNextStart)
            {
                await file.DeleteAsync();
                Settings.Instance.ClearCacheOnNextStart = false;
                Settings.Instance.Save();
                return;
            }
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
