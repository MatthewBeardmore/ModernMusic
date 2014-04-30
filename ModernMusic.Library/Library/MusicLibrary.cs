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

        private CollectionViewSource collection = new CollectionViewSource() { IsSourceGrouped = true };
        public CollectionViewSource ArtistsCollection
        {
            get
            {
                if (collection == null)
                {
                    collection = new CollectionViewSource();
                    collection.Source = CreateAlphaGroupInfo(Artists, x => x.ArtistName);
                    collection.IsSourceGrouped = true;
                }
                return collection;
            }
        }
        public RealObservableCollection<GroupInfoList<Artist>> ArtistGroupDictionary { get; private set; }

        public static RealObservableCollection<GroupInfoList<TSource>> CreateObservableGroupDictionary<TSource>()
        {
            var keys = "#abcdefghijklmnopqrstuvwxyz".ToCharArray().Select(x => x.ToString()).ToList();
            keys.Add("\uD83C\uDF10");

            return new RealObservableCollection<GroupInfoList<TSource>>(
                keys.Select(x => new GroupInfoList<TSource>() { Key = x }));
        }

        public static List<GroupInfoList<TSource>> CreateAlphaGroupInfo<TSource>(
            IEnumerable<TSource> source, Func<TSource, string> sortSelector)
        {
            var keys = "#abcdefghijklmnopqrstuvwxyz".ToCharArray().Select(x => x.ToString()).ToList();
            keys.Add("\uD83C\uDF10");
            var groupDictionary = keys.Select(x => new GroupInfoList<TSource>() { Key = x }).ToDictionary(x => (string)x.Key);
            var groups = new List<GroupInfoList<TSource>>();

            var query = from item in source
                        orderby sortSelector(item)
                        select item;

            foreach (var item in query)
            {
                var sortValue = sortSelector(item);
                if (!string.IsNullOrWhiteSpace(sortValue))
                {
                    if (Char.IsDigit(sortValue[0]) || Char.IsSymbol(sortValue[0]))
                        groupDictionary["#"].Add(item);
                    else if (groupDictionary.ContainsKey(sortValue[0].ToString().ToLower()))
                        groupDictionary[sortValue[0].ToString().ToLower()].Add(item);
                    else
                        groupDictionary["\uD83C\uDF10"].Add(item);
                }
            }

            return groupDictionary.Select(x => x.Value).ToList();
        }

        private MusicLibraryCache _cache;

        private MusicLibrary()
        {
            Artists = new ObservableCollection<Artist>();
            Albums = new ObservableCollection<Album>();
            Songs = new ObservableCollection<Song>();
            ArtistGroupDictionary = CreateObservableGroupDictionary<Artist>();
            collection.Source = ArtistGroupDictionary;
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
                    {
                        Artists.Add(artist);

                        string key = artist.ArtistName[0].ToString().ToLower();
                        if (Char.IsDigit(key[0]) || Char.IsSymbol(key[0]))
                            key = "#";
                        else if (!Char.IsLetter(key[0]))
                            key = "\uD83C\uDF10";

                        GroupInfoList<Artist> list = ArtistGroupDictionary.First((g) => g.Key.ToString() == key);
                        list.Add(artist);
                    }
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

            await LoadAllArtwork();

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

                if (songProperties.Title == "" && songProperties.Album == "" && songProperties.Artist == "")
                    continue;

                Artist artist = _cache.CreateIfNotExist(FixArtistName(songProperties.Artist));
                Album album = _cache.CreateIfNotExist(FixArtistName(songProperties.Artist), FixAlbumName(songProperties.Album));
                Song song = _cache.CreateIfNotExist(FixArtistName(songProperties.Artist), FixAlbumName(songProperties.Album),
                    songProperties.Title, file.Path, songProperties);

                if (artist != null)
                {
                    Artists.Add(artist);

                    string key = artist.ArtistName[0].ToString().ToLower();
                    if (Char.IsDigit(key[0]) || Char.IsSymbol(key[0]))
                        key = "#";
                    else if (!Char.IsLetter(key[0]))
                        key = "\uD83C\uDF10";

                    GroupInfoList<Artist> list = ArtistGroupDictionary.First((g)=>g.Key.ToString() == key);
                    list.Add(artist);
                } 
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
            for (int i = 0; i < Artists.Count; i++)
            {
                Artist artist = Artists[i];
                await DownloadAlbumArt(artist);
                await Task.Yield();
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

        public Artist GetArtist(string artistName)
        {
            return _cache.Get(FixArtistName(artistName));
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
        
        public async Task Deserialize()
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("cache");
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
