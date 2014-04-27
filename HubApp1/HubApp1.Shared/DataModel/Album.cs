﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ModernMusic.MusicLibrary
{
    [DataContract]
    public class Album : INotifyPropertyChanged
    {
        [DataMember]
        public string AlbumName { get; private set; }
        [DataMember]
        public string Artist { get; private set; }
        public string ImagePath { get; set; }

        public Album(string artist, string albumName)
        {
            this.Artist = artist;
            this.AlbumName = albumName;
            ImagePath = "Assets/LightGray.png";
        }

        public override string ToString()
        {
            return this.AlbumName;
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
}
