using HubApp1.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Web.Http;

namespace HubApp1.Data
{
    public sealed class MusicDataSource
    {
        private static MusicDataSource _sampleDataSource = new MusicDataSource();

        private ObservableCollection<ArtistDataGroup> _groups = new ObservableCollection<ArtistDataGroup>();
        public ObservableCollection<ArtistDataGroup> Groups
        {
            get { return this._groups; }
        }

        public static async Task<ArtistListGroup> GetGroupsAsync()
        {
            await _sampleDataSource.GetSampleDataAsync();

            return new ArtistListGroup(_sampleDataSource.Groups);
        }

        public static async Task<ArtistDataGroup> GetArtistAsync(string uniqueId)
        {
            await _sampleDataSource.GetSampleDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1)
            {
                ArtistDataGroup grp = matches.First();
                grp.LoadAlbums();
                return grp;
            }
            return null;
        }

        public static async Task<AlbumDataGroup> GetAlbumAsync(string artistId, string albumId)
        {
            await _sampleDataSource.GetSampleDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.Groups.Where((group) => group.UniqueId.Equals(artistId));
            if (matches.Count() == 1)
            {
                ArtistDataGroup grp = matches.First();
                grp.LoadAlbums();
                var albumMatches = grp.Items.Where((group) => group.UniqueId.Equals(albumId));
                if (albumMatches.Count() == 1)
                {
                    AlbumDataGroup album = albumMatches.First();
                    album.LoadSongs();
                    return album;
                }
            }
            return null;
        }

        public static async Task<AlbumDataGroup> GetItemAsync(string uniqueId)
        {
            await _sampleDataSource.GetSampleDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        private async Task GetSampleDataAsync()
        {
            if (this._groups.Count != 0)
                return;

            foreach(StorageFolder directory in await KnownFolders.MusicLibrary.GetFoldersAsync())
            {
                ArtistDataGroup group = new ArtistDataGroup(directory.Path,
                                                            directory.DisplayName,
                                                            "",
                                                            directory.DateCreated.ToString());

                this.Groups.Add(group);
            }
        }
    }

    public class ArtistListGroup
    {
        public RealObservableCollection<ArtistDataGroup> Items { get; private set; }

        public ArtistListGroup(IEnumerable<ArtistDataGroup> items)
        {
            Items = new RealObservableCollection<ArtistDataGroup>();
            foreach (ArtistDataGroup artist in items)
                Items.Add(artist);
        }
    }

    public class ArtistDataGroup : INotifyPropertyChanged
    {
        public ArtistDataGroup(String uniqueId, String artistName, String imagePath, String description)
        {
            this.UniqueId = uniqueId;
            this.ArtistName = artistName;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Items = new ObservableCollection<AlbumDataGroup>();

            LoadArtistData();
        }

        public string UniqueId { get; private set; }
        public string ArtistName { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public ObservableCollection<AlbumDataGroup> Items { get; private set; }
        private JsonObject XboxMusicData { get; set; }

        public async void LoadArtistData()
        {
            if (XboxMusicData != null)
                return;

            // Define the data needed to request an authorization token.
            var service = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
            string clientId = "TestUniversalMusicPlayer";
            string clientSecret = "6UwK02pmg5B6Ni9qYZbze+rpB5KiXobK6c/YEli1u64=";
            var scope = "http://music.xboxlive.com";
            var grantType = "client_credentials";

            // Create the request data.
            var requestData = new Dictionary<string, string>();
            requestData["client_id"] = clientId;
            requestData["client_secret"] = clientSecret;
            requestData["scope"] = scope;
            requestData["grant_type"] = grantType;

            var client = new HttpClient();
            var response = await client.PostAsync(new Uri(service), new HttpFormUrlEncodedContent(requestData));
            var responseString = await response.Content.ReadAsStringAsync();
            var token = Regex.Match(responseString, ".*\"access_token\":\"(.*?)\".*", RegexOptions.IgnoreCase).Groups[1].Value;

            service = "https://music.xboxlive.com/1/content/music/search?q=" + WebUtility.UrlEncode(ArtistName) + "&accessToken=Bearer+";
            response = await client.GetAsync(new Uri(service + WebUtility.UrlEncode(token)));
            responseString = await response.Content.ReadAsStringAsync();

            XboxMusicData = JsonObject.Parse(responseString);

            if (XboxMusicData.ContainsKey("Artists"))
            {
                JsonObject artistItems = XboxMusicData["Artists"].GetObject();
                JsonArray artistsArray = artistItems["Items"].GetArray();
                JsonObject artistInfo = artistsArray.GetObjectAt(0);

                ImagePath = artistInfo["ImageUrl"].GetString();

                OnPropertyChanged<string>(ImagePath);
            }
        }

        public async void LoadAlbums()
        {
            if (this.Items.Count != 0)
                return;

            JsonObject albumItems = XboxMusicData != null && XboxMusicData.ContainsKey("Albums") ? 
                XboxMusicData["Albums"].GetObject() : null;
            JsonArray albumsArray = albumItems == null ? null : albumItems["Items"].GetArray();

            StorageFolder directory = await StorageFolder.GetFolderFromPathAsync(UniqueId);
            foreach (StorageFolder albumFolder in await directory.GetFoldersAsync())
            {
                string imageUrl = "Assets/LightGray.png";
                if (albumsArray != null)
                {
                    for (uint i = 0; i < albumsArray.Count; i++)
                    {
                        JsonObject albumInfo = albumsArray.GetObjectAt(i);

                        if (albumInfo["Name"].GetString().ToLower() == albumFolder.DisplayName.ToLower())
                        {
                            imageUrl = albumInfo["ImageUrl"].GetString();
                            break;
                        }
                    }
                }

                AlbumDataGroup album = new AlbumDataGroup(albumFolder.Path,
                    UniqueId,
                    albumFolder.DisplayName,
                    ArtistName,
                    imageUrl,
                    XboxMusicData);
                Items.Add(album);
            }
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

    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class AlbumDataGroup
    {
        public AlbumDataGroup(String uniqueId, String artistId, String albumTitle, String artistName, String imagePath, JsonObject xboxMusicData)
        {
            this.UniqueId = uniqueId;
            this.ArtistId = artistId;
            this.AlbumTitle = albumTitle;
            this.ArtistName = artistName;
            this.ImagePath = imagePath;
            this.XboxMusicData = xboxMusicData;
            this.Items = new ObservableCollection<SongDataItem>();
        }

        public string ArtistId { get; private set; }
        public string UniqueId { get; private set; }
        public string AlbumTitle { get; private set; }
        public string ArtistName { get; private set; }
        public string ImagePath { get; private set; }
        public ObservableCollection<SongDataItem> Items { get; private set; }
        private JsonObject XboxMusicData { get; set; }

        public async void LoadSongs()
        {
            if (this.Items.Count != 0)
                return;

            StorageFolder directory = await StorageFolder.GetFolderFromPathAsync(UniqueId);
            foreach (IStorageItem songItem in await directory.GetItemsAsync())
            {
                SongDataItem song = new SongDataItem(songItem.Path,
                    UniqueId,
                    ArtistId,
                    Path.GetFileNameWithoutExtension(songItem.Name),
                    AlbumTitle,
                    ArtistName,
                    "Assets/LightGray.png");
                Items.Add(song);
            }
        }

        public override string ToString()
        {
            return this.AlbumTitle;
        }
    }

    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class SongDataItem
    {
        public SongDataItem(String uniqueId, String albumId, String artistId, String songTitle, String albumTitle, String artistName, String imagePath)
        {
            this.UniqueId = uniqueId;
            this.AlbumId = albumId;
            this.ArtistId = artistId;
            this.SongTitle = songTitle;
            this.AlbumTitle = albumTitle;
            this.ArtistName = artistName;
            this.ImagePath = imagePath;
        }

        public string ArtistId { get; private set; }
        public string AlbumId { get; private set; }
        public string UniqueId { get; private set; }
        public string ArtistName { get; private set; }
        public string AlbumTitle { get; private set; }
        public string SongTitle { get; private set; }
        public string ImagePath { get; private set; }

        public override string ToString()
        {
            return this.SongTitle;
        }
    }
}
