using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Web.Http;

namespace ModernMusic.MusicLibrary
{
    public class XboxMusicConnection
    {
        private const string CLIENT_ID = "TestUniversalMusicPlayer";
        private const string CLIENT_SECRET = "6UwK02pmg5B6Ni9qYZbze+rpB5KiXobK6c/YEli1u64=";
        private const string SERVICE_URL = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
        private const string SCOPE = "http://music.xboxlive.com";
        private static string Token;
        private static DateTime? TokenExpirationDate = null;

        private static async void CreateConnection()
        {
            if(Token != null && (TokenExpirationDate.Value - DateTime.Now).TotalSeconds > 0)
                return;

            // Define the data needed to request an authorization token.
            var grantType = "client_credentials";

            // Create the request data.
            var requestData = new Dictionary<string, string>();
            requestData["client_id"] = CLIENT_ID;
            requestData["client_secret"] = CLIENT_SECRET;
            requestData["scope"] = SCOPE;
            requestData["grant_type"] = grantType;

            var client = new HttpClient();
            var response = await client.PostAsync(new Uri(SERVICE_URL), new HttpFormUrlEncodedContent(requestData));
            var responseString = await response.Content.ReadAsStringAsync();

            var token = Regex.Match(responseString, ".*\"access_token\":\"(.*?)\".*", RegexOptions.IgnoreCase).Groups[1].Value;

        }

        public static async Task<JsonObject> GetArtistData(Artist artist)
        {
            CreateConnection();

            var client = new HttpClient();
            string service = "https://music.xboxlive.com/1/content/music/search?q=" + WebUtility.UrlEncode(artist.ArtistName) + "&accessToken=Bearer+";
            var response = await client.GetAsync(new Uri(service + WebUtility.UrlEncode(Token)));
            string responseString = await response.Content.ReadAsStringAsync();

            JsonObject jsonData = JsonObject.Parse(responseString);

            if (jsonData.ContainsKey("Artists"))
            {
                JsonObject artistItems = jsonData["Artists"].GetObject();
                JsonArray artistsArray = artistItems["Items"].GetArray();
                JsonObject artistInfo = artistsArray.GetObjectAt(0);

                return artistInfo;
            }
            return null;
        }

        public static async Task<JsonObject> GetAlbumData(Album album)
        {
            CreateConnection();

            var client = new HttpClient();
            string service = "https://music.xboxlive.com/1/content/music/search?q=" + WebUtility.UrlEncode(album.Artist + " " + album.AlbumName) + "&accessToken=Bearer+";
            var response = await client.GetAsync(new Uri(service + WebUtility.UrlEncode(Token)));
            string responseString = await response.Content.ReadAsStringAsync();

            JsonObject jsonData = JsonObject.Parse(responseString);

            if (jsonData.ContainsKey("Albums"))
            {
                JsonObject artistItems = jsonData["Albums"].GetObject();
                JsonArray artistsArray = artistItems["Items"].GetArray();
                JsonObject albumInfo = artistsArray.GetObjectAt(0);

                return albumInfo;
            }
            return null;
        }

        public static async Task<JsonArray> GetAllAlbumData(Artist artist)
        {
            CreateConnection();

            var client = new HttpClient();
            string service = "https://music.xboxlive.com/1/content/music/search?q=" + WebUtility.UrlEncode(artist.ArtistName) + "&accessToken=Bearer+";
            var response = await client.GetAsync(new Uri(service + WebUtility.UrlEncode(Token)));
            string responseString = await response.Content.ReadAsStringAsync();

            JsonObject jsonData = JsonObject.Parse(responseString);

            if (jsonData.ContainsKey("Albums"))
            {
                JsonObject artistItems = jsonData["Albums"].GetObject();
                return artistItems["Items"].GetArray();
            }
            return null;
        }
    }
}
