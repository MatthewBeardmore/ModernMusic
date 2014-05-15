using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ModernMusic.Library
{
    [ProtoContract]
    public class Song : INotifyPropertyChanged
    {
        [ProtoMember(1)]
        public Guid ID = Guid.NewGuid();

        public delegate void SongTappedHandler(Song song);

        [ProtoMember(2)]
        public string FilePath { get; set; }
        [ProtoMember(3)]
        public string SongTitle { get; set; }
        [ProtoMember(4)]
        public string Album { get; set; }
        [ProtoMember(5)]
        public string Artist { get; set; }
        [ProtoMember(6)]
        public uint TrackNumber { get; set; }

        public Song Self { get { return this; } }

        public string ArtistCaps { get { return Artist.ToUpper(); } }
        public string AlbumLower { get { return Album.ToLower(); } }
        //Caches the property of Album
        public string CachedImagePath { get; set; }

        public Song() { }

        public Song(string artist, string albumName, String filePath, String songTitle, uint trackNumber)
        {
            this.FilePath = filePath;
            this.SongTitle = songTitle;
            this.Artist = artist;
            this.Album = albumName;
            this.TrackNumber = trackNumber;
        }

        public async Task<DeletionResult> Delete()
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(FilePath);
            await file.DeleteAsync();
            await PlaylistManager.Instance.DeleteSong(this);

            return await MusicLibrary.Instance.DeleteSong(this);
        }

        public override string ToString()
        {
            return this.SongTitle;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool Selected { get; set; }
        public void FirePropertyChanged()
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("Selected"));
        }
    }
}
