﻿using ModernMusic.Helpers;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;

namespace ModernMusic.Library
{
    [ProtoContract]
    public class Artist : INotifyPropertyChanged
    {
        [ProtoMember(1)]
        public Guid ID = Guid.NewGuid();

        public delegate void ArtistTappedHandler(Artist artist);

        private string _imagePath = "ms-appx:///Assets/DarkGray.png";

        [ProtoMember(2)]
        public string ArtistName { get; set; }
        public string ArtistNameCaps { get { return ArtistName.ToUpper(); } }
        [ProtoMember(3)]
        public bool HasDownloadedArtistData { get; set; }
        [ProtoMember(4)]
        public string ImagePath
        {
            get { return _imagePath; }
            set { _imagePath = value; OnPropertyChanged<string>(); }
        }

        public Artist Self { get { return this; } }

        public Artist() { }

        public Artist(String artistName)
        {
            this.ArtistName = artistName;
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

        public async Task PinToStart()
        {
            string activationArguments = "Artist:" + ID.ToString();
            string appbarTileId = "ModernMusic." + activationArguments.Replace(':', '.');

            await MusicLibrary.Instance.DownloadAlbumArt(this);
            Uri square150x150Logo = new Uri("ms-appx:///Assets/Square150x150Logo.scale-240.png");
            if (ImagePath != null)
            {
                Uri source = new Uri(ImagePath);
                if (!source.IsFile)
                    square150x150Logo = await Utilities.ResizeImage(new Uri(ImagePath), 150);
            }

            SecondaryTileManager.PinSecondaryTile(appbarTileId, "Modern Music", square150x150Logo, activationArguments);
        }
    }
}
