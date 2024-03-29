﻿using ModernMusic.Library;
using ModernMusic.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core;
using System.IO;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.Media;

namespace ModernMusic.Library
{
    public static class NowPlayingManager
    {
        #region Public Properties

        public static Action OnChangedTrack;
        public static Action OnSeek;
        public static TypedEventHandler<MediaPlayer, object> OnMediaPlayerStateChanged;

        public static TimeSpan? CurrentPosition
        {
            get
            {
                if (NowPlayingManager.IsAudioOpen)
                    return BackgroundMediaPlayer.Current.Position;
                return null;
            }
        }

        public static TimeSpan? CurrentTrackDuration
        {
            get
            {
                try
                {
                    return BackgroundMediaPlayer.Current.NaturalDuration;
                }
                catch { return null; }
            }
        }

        public static bool IsAudioPlaying
        {
            get
            {
                try
                {
                    MediaPlayerState state = BackgroundMediaPlayer.Current.CurrentState;
                    return state == MediaPlayerState.Playing ||
                        state == MediaPlayerState.Buffering ||
                        state == MediaPlayerState.Opening;
                }
                catch { return false; }
            }
        }

        public static bool IsAudioOpen
        {
            get
            {
                try
                {
                    MediaPlayerState state = BackgroundMediaPlayer.Current.CurrentState;
                    return state == MediaPlayerState.Playing ||
                        state == MediaPlayerState.Paused ||
                        state == MediaPlayerState.Buffering ||
                        state == MediaPlayerState.Opening;
                }
                catch { return false; }
            }
        }

        #endregion

        #region Private Fields and Properties

        private static ManualResetEvent ServerInitialized = new ManualResetEvent(false);

        #endregion

        #region Public Methods

        #region New Background Management Methods

        public static void BeginPlaylist(CoreDispatcher dispatcher, Playlist playlist, int index = 0)
        {
            NowPlayingInformation.CurrentIndex = index;
            NowPlayingInformation.CurrentPlaylist = playlist;

            var t = Task.Run(new Action(() =>
            {
                StartBackgroundAudioTask(dispatcher);

                ValueSet message = new ValueSet();
                message.Add(Constants.StartPlaying, true);
                BackgroundMediaPlayer.SendMessageToBackground(message);
            }));
        }

        public static bool PlayCurrentSong(CoreDispatcher dispatcher)
        {
            bool needsUpdated = true;
            if (ServerInitialized.WaitOne(0))
            {
                ValueSet message = new ValueSet();
                if (NowPlayingInformation.CurrentIndex == -1)
                {
                    NowPlayingInformation.CurrentIndex = 0;
                    message.Add(Constants.StartPlaying, null);
                }
                else
                {
                    message.Add(Constants.PlayTrack, null);
                    needsUpdated = false;
                }
                BackgroundMediaPlayer.SendMessageToBackground(message);
            }
            else
            {
                if (NowPlayingInformation.CurrentIndex == -1)
                    NowPlayingInformation.CurrentIndex = 0;

                //TODO: Keep audio position when the background task is killed
                StartBackgroundAudioTask(dispatcher);
                if(IsAudioOpen)
                {
                    ValueSet message = new ValueSet();
                    message.Add(Constants.PlayTrack, null);
                    BackgroundMediaPlayer.SendMessageToBackground(message);
                }
                else
                {
                    ValueSet message = new ValueSet();
                    message.Add(Constants.StartPlaying, null);
                    BackgroundMediaPlayer.SendMessageToBackground(message);
                }
            }
            return needsUpdated;
        }

        public static void PauseCurrentSong()
        {
            ValueSet message = new ValueSet();
            message.Add(Constants.PauseTrack, null);
            BackgroundMediaPlayer.SendMessageToBackground(message);
        }

        public static void Seek(double time)
        {
            BackgroundMediaPlayer.Current.Position = TimeSpan.FromSeconds(time);
        }

        public static async void AddToNowPlaying(Song song)
        {
            Playlist currentPlaylist = NowPlayingInformation.CurrentPlaylist;
            if (currentPlaylist != null)
            {
                currentPlaylist.Songs.Add(song);
                NowPlayingInformation.CurrentPlaylist = currentPlaylist;

                if (!string.IsNullOrEmpty(currentPlaylist.Name))
                    await PlaylistManager.Instance.Serialize();
            }
        }

        public static async void AddToNowPlaying(Album album)
        {
            Playlist currentPlaylist = NowPlayingInformation.CurrentPlaylist;
            if (currentPlaylist != null)
            {
                foreach (Song song in MusicLibrary.Instance.GetSongs(album))
                    currentPlaylist.Songs.Add(song);
                NowPlayingInformation.CurrentPlaylist = currentPlaylist;

                if (!string.IsNullOrEmpty(currentPlaylist.Name))
                    await PlaylistManager.Instance.Serialize();
            }
        }

        public static async void AddToNowPlaying(Artist artist)
        {
            Playlist currentPlaylist = NowPlayingInformation.CurrentPlaylist;
            if (currentPlaylist != null)
            {
                foreach (Song song in MusicLibrary.Instance.GetSongs(artist))
                    currentPlaylist.Songs.Add(song);
                NowPlayingInformation.CurrentPlaylist = currentPlaylist;

                if (!string.IsNullOrEmpty(currentPlaylist.Name))
                    await PlaylistManager.Instance.Serialize();
            }
        }

        public static async void AddToNowPlaying(Playlist playlist)
        {
            Playlist currentPlaylist = NowPlayingInformation.CurrentPlaylist;
            if (currentPlaylist != null)
            {
                foreach (Song song in new List<Song>(playlist.Songs))
                    currentPlaylist.Songs.Add(song);
                NowPlayingInformation.CurrentPlaylist = currentPlaylist;

                if (!string.IsNullOrEmpty(currentPlaylist.Name))
                    await PlaylistManager.Instance.Serialize();
            }
        }

        public static void StopPlayback()
        {
            ValueSet message = new ValueSet();
            message.Add(Constants.StopPlayback, null);
            BackgroundMediaPlayer.SendMessageToBackground(message);
        }

        public static void SkipToNextSong(CoreDispatcher dispatcher, Playlist playlist = null)
        {
            NowPlayingInformation.SkipToNextSong(true, playlist);

            var t = Task.Run(new Action(() =>
            {
                if (!ServerInitialized.WaitOne(0))
                    StartBackgroundAudioTask(dispatcher);

                ValueSet message = new ValueSet();
                message.Add(Constants.StartPlaying, null);
                BackgroundMediaPlayer.SendMessageToBackground(message);
            }));
        }

        public static bool SkipToSong(int idx, CoreDispatcher dispatcher)
        { 
            if (idx == NowPlayingInformation.CurrentIndex)
                return false;

            NowPlayingInformation.CurrentIndex = idx;

            var t = Task.Run(new Action(() =>
            {
                if (!ServerInitialized.WaitOne(0))
                    StartBackgroundAudioTask(dispatcher);

                ValueSet message = new ValueSet();
                message.Add(Constants.StartPlaying, null);
                BackgroundMediaPlayer.SendMessageToBackground(message);
            }));
            return true;
        }

        public static void ReplaySong(CoreDispatcher dispatcher)
        {
            var t = Task.Run(new Action(() =>
            {
                if (!ServerInitialized.WaitOne(0))
                    StartBackgroundAudioTask(dispatcher);

                if (IsAudioPlaying)
                {
                    ValueSet message = new ValueSet();
                    message.Add(Constants.StartPlaying, true);
                    BackgroundMediaPlayer.SendMessageToBackground(message);
                }
                else
                {
                    ValueSet message = new ValueSet();
                    message.Add(Constants.ClearCache, true);
                    BackgroundMediaPlayer.SendMessageToBackground(message);
                }
            }));
        }

        public static bool SkipToPreviousSong(CoreDispatcher dispatcher, Playlist playlist = null)
        {
            if(BackgroundMediaPlayer.Current.Position.TotalSeconds > 5)
            {
                BackgroundMediaPlayer.Current.Position = TimeSpan.FromSeconds(0);
                //Just restart the song if it is more than 5 seconds into the song
                return false;
            }
            NowPlayingInformation.SkipToPreviousSong(true, playlist);

            var t = Task.Run(new Action(() =>
            {
                if (!ServerInitialized.WaitOne(0))
                    StartBackgroundAudioTask(dispatcher);

                ValueSet message = new ValueSet();
                message.Add(Constants.StartPlaying, null);
                BackgroundMediaPlayer.SendMessageToBackground(message);
            }));
            return true;
        }

        public static void GetNowPlaying(out Song previousSong, out Song currentSong, out Song nextSong, out Song subsequentSong)
        {
            Playlist playlist = NowPlayingInformation.CurrentPlaylist;

            int previousIndex, currentIndex, nextIndex, subsequentIndex;
            NowPlayingInformation.GetNowPlayingIndices(out previousIndex, out currentIndex, out nextIndex, out subsequentIndex, playlist);

            previousSong = playlist.GetSong(previousIndex);
            currentSong = playlist.GetSong(currentIndex);
            nextSong = playlist.GetSong(nextIndex);
            subsequentSong = playlist.GetSong(subsequentIndex);
        }

        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize Background Media Player Handlers and starts playback
        /// </summary>
        private static void StartBackgroundAudioTask(CoreDispatcher dispatcher)
        {
            if (ServerInitialized.WaitOne(0))
                return;

            RemoveMediaPlayerEventHandlers();
            AddMediaPlayerEventHandlers();
            QueryForBackgroundTask();

            bool result = ServerInitialized.WaitOne(2000);
            if (!result)
            {
                throw new Exception("Background Audio Task didn't start in expected time");
            }
        }

        private static void QueryForBackgroundTask()
        {
            ValueSet message = new ValueSet();
            message.Add(Constants.BackgroundTaskQuery, null);
            BackgroundMediaPlayer.SendMessageToBackground(message);
        }

        /// <summary>
        /// Unsubscribes to MediaPlayer events. Should run only on suspend
        /// </summary>
        private static void RemoveMediaPlayerEventHandlers()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged -= MediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground -= BackgroundMediaPlayer_MessageReceivedFromBackground;
        }

        /// <summary>
        /// Subscribes to MediaPlayer events
        /// </summary>
        private static void AddMediaPlayerEventHandlers()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged += MediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
        }

        #region Background MediaPlayer Event handlers

        /// <summary>
        /// MediaPlayer state changed event handlers. 
        /// Note that we can subscribe to events even if Media Player is playing media in background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (OnMediaPlayerStateChanged != null)
                OnMediaPlayerStateChanged(sender, args);
        }

        /// <summary>
        /// This event fired when a message is recieved from Background Process
        /// </summary>
        private static void BackgroundMediaPlayer_MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                switch (key)
                {
                    case Constants.BackgroundTaskIsRunning:
                        //Wait for Background Task to be initialized before starting playback
                        Debug.WriteLine("Background Task Is Running");
                        ServerInitialized.Set();
                        break;
                    case Constants.BackgroundTaskIsStopping:
                        Debug.WriteLine("Background Task Is Stopping");

                        ServerInitialized.Reset();
                        if (OnMediaPlayerStateChanged != null)
                            OnMediaPlayerStateChanged(null, null);
                        break;
                    case Constants.ChangedTrack:
                        FireOnChangedTrack();
                        break;
                    case Constants.Seek:
                        FireOnSeek();
                        break;
                }
            }
        }

        private static void FireOnChangedTrack()
        {
            if (OnChangedTrack != null)
                OnChangedTrack();
        }

        public static void FireOnSeek()
        {
            if (OnSeek != null)
                OnSeek();
        }

        #endregion

        #endregion
    }

    public static class NowPlayingInformation
    {
        public static event Action OnCurrentPlaylistUpdated;
        private static Playlist _cachedPlaylist = null;
        private static int _lastIndex = -1;

        public static bool DisableCaching { get; set; }

        public static Playlist CurrentPlaylist
        {
            get
            {
                if (!DisableCaching && _cachedPlaylist != null)
                    return _cachedPlaylist;

                _cachedPlaylist = AsyncInline.Run<Playlist>(new Func<Task<Playlist>>(() => GetPlaylist("CurrentPlaylist")));
                return _cachedPlaylist;
            }
            set
            {
                if (_cachedPlaylist != null)
                    _cachedPlaylist.ClearSelection();

                _cachedPlaylist = value;

                if (value != null)
                {
                    int idx = CurrentIndex;
                    value.GenerateShuffleList(idx);
                    value.ClearSelection();
                    if (idx != -1)
                        value.GetSong(idx).Selected = true;
                }

                AsyncInline.Run(new Func<Task>(() => SetPlaylist("CurrentPlaylist", value)));
                if (OnCurrentPlaylistUpdated != null)
                    OnCurrentPlaylistUpdated();
            }
        }

        public static int CurrentIndex
        {
            get
            {
                string val = GetSetting("CurrentIndex");
                if (val == null)
                    return -1;
                int retVal = int.Parse(val);
                if (!DisableCaching && retVal != _lastIndex && _cachedPlaylist != null)
                {
                    _cachedPlaylist.ClearSelection();
                    Song song = _cachedPlaylist.GetSong(retVal);
                    if (song != null)
                        song.Selected = true;
                }
                return _lastIndex = retVal;
            }
            set
            {
                SetSetting("CurrentIndex", value.ToString());
                if (!DisableCaching && _cachedPlaylist != null)
                {
                    _lastIndex = value;
                    _cachedPlaylist.ClearSelection();
                    Song song = _cachedPlaylist.GetSong(value);
                    if(song != null)
                        song.Selected = true;
                }
            }
        }

        public static bool RepeatEnabled
        {
            get
            {
                string val = GetSetting("RepeatEnabled");
                if (val == null)
                    return false;
                return bool.Parse(val);
            }
            set
            {
                SetSetting("RepeatEnabled", value.ToString());
            }
        }

        public static bool ShuffleEnabled
        {
            get
            {
                string val = GetSetting("ShuffleEnabled");
                if (val == null)
                    return false;
                return bool.Parse(val);
            }
            set
            {
                SetSetting("ShuffleEnabled", value.ToString());
            }
        }

        public static Song CurrentSong
        {
            get
            {
                return GetCurrentSong();
            }
        }

        public static Song GetCurrentSong(Playlist playlist = null)
        {
            if (playlist == null)
                playlist = CurrentPlaylist;
            int index = CurrentIndex;
            if (index < 0 || playlist == null || index >= playlist.Songs.Count)
                return null;

            return playlist.GetSong(index);
        }

        public static void GetNowPlayingIndices(out int previousIndex, out int currentIndex, out int nextIndex, out int subsequentIndex, Playlist playlist = null)
        {
            currentIndex = NowPlayingInformation.CurrentIndex;
            if (playlist == null)
                playlist = NowPlayingInformation.CurrentPlaylist;

            if (playlist == null)
            {
                previousIndex = currentIndex = nextIndex = subsequentIndex = -1;
                return;
            }


            previousIndex = currentIndex - 1;
            if (previousIndex < 0)
            {
                if (RepeatEnabled)
                    previousIndex += playlist.Songs.Count;
                else
                    previousIndex = -1;
            }
            nextIndex = currentIndex + 1;
            if (nextIndex >= playlist.Songs.Count)
            {
                if (RepeatEnabled)
                    nextIndex -= playlist.Songs.Count;
                else
                    nextIndex = -1;
            }
            subsequentIndex = currentIndex + 2;
            if (subsequentIndex >= playlist.Songs.Count)
            {
                if (RepeatEnabled)
                    subsequentIndex -= playlist.Songs.Count;
                else
                    subsequentIndex = -1;
            }
        }

        private static void SetSetting(string key, object val)
        {
            if (val == null)
                ApplicationData.Current.LocalSettings.Values.Remove("CurrentPlaylist");
            else
                ApplicationData.Current.LocalSettings.Values[key] = val;
        }

        private static async Task<Playlist> GetPlaylist(string key)
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalCacheFolder.GetFileAsync(key);
                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    Playlist playlist = Playlist.DeserializeFrom(inStream);
                    
                    if(!MusicLibrary.IsBackgroundTask)
                    {
                        Task t = MusicLibrary.Instance.LoadCache(MusicLibrary.Dispatcher);
                        if (t != null)
                            await t;
                        else if (MusicLibrary.CacheLoadTask != null && MusicLibrary.CacheLoadTask.IsCompleted)
                            await MusicLibrary.CacheLoadTask;

                        for (int i = 0; i < playlist.Songs.Count; i++)
                        {
                            Song song = MusicLibrary.Instance.GetSong(playlist.Songs[i].ID);
                            if (song != null)
                                playlist.Songs[i] = song;
                        }
                    }

                    return playlist;
                }
            }
            catch
            {
                return null;
            }
        }

        private static async Task SetPlaylist(string key, Playlist val)
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting);
                if (val == null)
                    await file.DeleteAsync();
                else
                {
                    using (Stream fileStream = await file.OpenStreamForWriteAsync())
                        val.SerializeTo(fileStream);
                }
            }
            catch { }
        }

        private static string GetSetting(string key)
        {
            object var;
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var))
                return var.ToString();
            return null;
        }

        public static int SkipToNextSong(bool force = false, Playlist playlist = null)
        {
            int currentIndex = NowPlayingInformation.CurrentIndex;
            if (playlist == null)
                playlist = NowPlayingInformation.CurrentPlaylist;

            int nextIndex = currentIndex + 1;
            if (nextIndex >= playlist.Songs.Count)
            {
                if (RepeatEnabled || force)
                    nextIndex = 0;
                else
                    nextIndex = -1;
            }

            NowPlayingInformation.CurrentIndex = nextIndex;

            return nextIndex;
        }

        public static int SkipToPreviousSong(bool force = false, Playlist playlist = null)
        {
            int currentIndex = NowPlayingInformation.CurrentIndex;
            if(playlist == null)
                playlist = NowPlayingInformation.CurrentPlaylist;

            int previousIndex = currentIndex - 1;
            if (previousIndex < 0)
            {
                if (RepeatEnabled || force)
                    previousIndex = playlist.Songs.Count - 1;
                else
                    previousIndex = -1;
            }

            NowPlayingInformation.CurrentIndex = previousIndex;

            return previousIndex;
        }
    }
}
