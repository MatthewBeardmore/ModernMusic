using ModernMusic.Helpers;
using ProtoBuf;
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
using Windows.UI.Core;
using Windows.UI.Xaml;
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
                }
                return _instance;
            }
        }

        private int _hasLoadedArtists;
        private int _hasLoadedCache;

        private string _lastLoadedArtist = "";
        public event Action<string> OnLoadLibraryFromDiskProgress;

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
            GroupInfoList<T> list = GetItemGroup(group, addition, keyConversion);
            if (list.FirstOrDefault((a)=>((dynamic)a).ID == ((dynamic)addition).ID) == null)
                list.Add(addition);
        }

        internal static GroupInfoList<T> GetItemGroup<T>(RealObservableCollection<GroupInfoList<T>> group, T addition, Func<T, char> keyConversion)
        {
            string key = (keyConversion(addition).ToString().ToLower());
            if (Char.IsDigit(key[0]) || Char.IsSymbol(key[0]))
                key = "#";
            else if (!Char.IsLetter(key[0]))
                key = "\uD83C\uDF10";

            GroupInfoList<T> list = group.FirstOrDefault((g) => g.Key.ToString() == key);
            if (list == null)
            {
                key = "\uD83C\uDF10";
                list = group.First((g) => g.Key.ToString() == key);
            }
            return list;
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

        public async Task<StorageFile> HasCache()
        {
            try
            {
                var cacheFile = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync("cache");
                if (Settings.Instance.ClearCacheOnNextStart)
                {
                    await cacheFile.DeleteAsync();
                    Settings.Instance.ClearCacheOnNextStart = false;
                    Settings.Instance.Save();
                    return null;
                }
                return cacheFile;
            }
            catch
            {
                if (Settings.Instance.ClearCacheOnNextStart)
                {
                    Settings.Instance.ClearCacheOnNextStart = false;
                    Settings.Instance.Save();
                }
                return null;
            }
        }

        public async Task LoadLibraryFromDisk()
        {
            if (Interlocked.CompareExchange(ref _hasLoadedArtists, 1, 0) == 1)
                return;

            await TraverseFolder(KnownFolders.MusicLibrary);
            await LoadAllArtwork();
            await _cache.Serialize();
        }

        public Task LoadCache(CoreDispatcher dispatcher, StorageFile cacheFile = null)
        {
            if (Interlocked.CompareExchange(ref _hasLoadedCache, 1, 0) == 1)
                return null;

            return Task.Run(new Action(() =>
                {
                    try
                    {
                        _cache = AsyncInline.Run(new Func<Task<MusicLibraryCache>>(() => MusicLibraryCache.Deserialize(cacheFile)));
                        var c = dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low,
                                () =>
                                {
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
                                });
                    }
                    catch { }
                }));
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

                if(_lastLoadedArtist != songProperties.Artist)
                {
                    _lastLoadedArtist = songProperties.Artist;
                    if (OnLoadLibraryFromDiskProgress != null)
                        OnLoadLibraryFromDiskProgress(_lastLoadedArtist);
                }
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

                Uri cachedUri = await Utilities.DownloadFile(new Uri(imagePath));
                if (cachedUri != null)
                    album.CachedImagePath = cachedUri.ToString();
            }

            foreach(Album album in _cache.GetAlbums(artist.ArtistName))
            {
                if(string.IsNullOrEmpty(album.ImagePath))
                {
                    JsonObject albumInfo = await XboxMusicConnection.GetAlbumData(album);
                    if (albumInfo == null)
                        continue;

                    string imagePath = albumInfo["ImageUrl"].GetString();
                    album.ImagePath = imagePath;

                    Uri cachedUri = await Utilities.DownloadFile(new Uri(imagePath));
                    if (cachedUri != null)
                        album.CachedImagePath = cachedUri.ToString();
                }
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
                    if (OnLoadLibraryFromDiskProgress != null)
                        OnLoadLibraryFromDiskProgress(artist.ArtistName);
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
            if (song == null)
                return null;

            return _cache.Get(song.Artist, song.Album);
        }

        public Artist GetArtist(Guid id)
        {
            return _cache.GetArtist(id);
        }

        public Album GetAlbum(Guid id)
        {
            return _cache.GetAlbum(id);
        }

        public List<Song> GetAllSongs()
        {
            return _cache.GetAllSongs();
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

        public async Task<DeletionResult> DeleteSong(Song song)
        {
            DeletionResult result = _cache.DeleteSong(song, ArtistGroupDictionary, AlbumGroupDictionary, SongGroupDictionary);

            await _cache.Serialize();

            return result;
        }
    }

    public enum DeletionResult
    {
        Song,
        Album,
        Artist
    }

    [ProtoContract]
    public class MusicLibraryCache
    {
        [ProtoMember(1)]
        public Dictionary<string, Artist> Artists { get; private set; }
        [ProtoMember(2)]
        public Dictionary<string, List<Album>> Albums { get; private set; }
        [ProtoMember(3)]
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
            Song song = new Song(artist, albumName, filePath, songName, props.TrackNumber);
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

        public Album GetAlbum(Guid id)
        {
            foreach(List<Album> albums in Albums.Values)
            {
                foreach(Album album in albums)
                {
                    if (album.ID == id)
                        return album;
                }
            }

            return null;
        }

        public Artist GetArtist(Guid id)
        {
            foreach (Artist artist in Artists.Values)
            {
                if (artist.ID == id)
                    return artist;
            }
            return null;
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
                song.Sort((a, b) => a.TrackNumber.CompareTo(b.TrackNumber));
                songs.AddRange(song);
            }
            return songs;
        }

        public List<Song> GetAllSongs()
        {
            List<Song> allSongs = new List<Song>();
            foreach(string artist in Artists.Keys)
            {
                allSongs.AddRange(GetSongsForArtist(artist));
            }
            return allSongs;
        }

        public DeletionResult DeleteSong(Song song, RealObservableCollection<GroupInfoList<Artist>> artistsList,
            RealObservableCollection<GroupInfoList<Album>> albumsList,
            RealObservableCollection<GroupInfoList<Song>> songsList)
        {
            GroupInfoList<Song> songList = MusicLibrary.GetItemGroup(songsList, song, a => a.SongTitle[0]);

            List<Song> songs;
            if (Songs.TryGetValue(song.Artist.ToLower() + "--" + song.Album.ToLower(), out songs))
            {
                songList.Remove(song);
                songs.Remove(song);
            }
            songs = GetSongs(song.Artist, song.Album);
            if (songs.Count == 0)
            {
                Songs.Remove(song.Artist.ToLower() + "--" + song.Album.ToLower());
                List<Album> albums;
                if (Albums.TryGetValue(song.Artist.ToLower(), out albums))
                {
                    Album album = albums.FirstOrDefault((a) => a.AlbumName == song.Album);
                    albums.Remove(album);
                    if(albums.Count == 0)
                    {
                        GroupInfoList<Album> albumList = MusicLibrary.GetItemGroup(albumsList, album, aa => aa.AlbumName[0]);
                        Albums.Remove(song.Artist.ToLower());
                        albumList.Remove(album);

                        Artist artist = Artists[song.Artist.ToLower()];

                        GroupInfoList<Artist> artistList = MusicLibrary.GetItemGroup(artistsList, artist, a => a.ArtistName[0]);
                        Artists.Remove(song.Artist.ToLower());
                        artistList.Remove(artist);
                        return DeletionResult.Artist;
                    }
                    return DeletionResult.Album;
                }
            }
            return DeletionResult.Song;
        }
        
        public async static Task<MusicLibraryCache> Deserialize(StorageFile file)
        {
            try
            {
                if (file == null)
                    file = await ApplicationData.Current.LocalFolder.GetFileAsync("cache");

                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    return (MusicLibraryCache)LibrarySerializer.Create().Deserialize(inStream.AsStreamForRead(), 
                        null, typeof(MusicLibraryCache));
                }
                /*using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    var serializer = new DataContractJsonSerializer(typeof(MusicLibraryCache));
                    return (MusicLibraryCache)serializer.ReadObject(inStream.AsStreamForRead());
                }*/
            }
            catch { }
            return new MusicLibraryCache();
        }

        public async Task Serialize()
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("cache", CreationCollisionOption.ReplaceExisting);
            using (Stream fileStream = await file.OpenStreamForWriteAsync())
            {
                LibrarySerializer.Create().Serialize(fileStream, this);
                //Serializer.Serialize(fileStream, this);
            }
            /*using (MemoryStream ms = new MemoryStream())
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
            }*/
        }
    }
}
