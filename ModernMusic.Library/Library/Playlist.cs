using ModernMusic.Helpers;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ModernMusic.Library
{
    [ProtoContract]
    public class Playlist
    {
        #region Public properties, events and handlers

        [ProtoMember(1)]
        public Guid ID = Guid.NewGuid();

        public delegate void PlaylistTappedHandler(Playlist playlist);

        [ProtoMember(2)]
        public List<Song> Songs
        {
            get;
            set;
        }

        [ProtoMember(3)]
        public string Name { get; set; }

        [ProtoMember(4)]
        public int[] ShuffleList { get; set; }

        #endregion

        public Playlist()
        {
            Songs = new List<Song>();
            ShuffleList = new int[0];
        }

        public void PinToStart()
        {
            string activationArguments = "Playlist:" + ID.ToString();
            string appbarTileId = "ModernMusic." + activationArguments.Replace(':', '.');

            Uri square150x150Logo = new Uri("ms-appx:///Assets/Transparent.png");

            SecondaryTileManager.PinSecondaryTile(appbarTileId, Name, square150x150Logo, activationArguments, true);
        }

        public void GenerateShuffleList()
        {
            Random random = new Random();
            ShuffleList = new int[Songs.Count];

            List<int> indices = new List<int>();
            for (int i = 0; i < ShuffleList.Length; i++)
                indices.Add(i);

            for (int i = 0; i < ShuffleList.Length; i++)
            {
                int idx = random.Next(0, indices.Count);
                ShuffleList[i] = indices[idx];
                indices.RemoveAt(idx);
            }
        }

        public List<Song> GetSongList()
        {
            if(NowPlayingInformation.ShuffleEnabled)
            {
                List<Song> songs = new List<Song>();
                foreach(int idx in ShuffleList)
                {
                    songs.Add(Songs[idx]);
                }
                return songs;
            }
            return Songs;
        }

        public Song GetSong(int index)
        {
            if (index == -1)
                return null;

            if (NowPlayingInformation.ShuffleEnabled)
            {
                //Use the index as an index into the shuffled list so that we can get the next actual song
                int shuffledIdx = ShuffleList[index];
                return Songs[shuffledIdx];
            }
            return Songs[index];
        }

        public void ClearSelection()
        {
            foreach (Song song in Songs)
            {
                if (song.Selected)
                    song.Selected = false;
            }
        }

        public static Playlist DeserializeFrom(IInputStream stream)
        {
            try
            {
                return (Playlist)LibrarySerializer.Create().Deserialize(stream.AsStreamForRead(),
                        null, typeof(Playlist));
            }
            catch { return null; }
        }

        public void SerializeTo(Stream stream)
        {
            LibrarySerializer.Create().Serialize(stream, this);
        }
    }
}
