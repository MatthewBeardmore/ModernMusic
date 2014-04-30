using ModernMusic.Library;
using ModernMusic.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Core;

namespace ModernMusic.Library
{
    public static class NowPlayingManager
    {
        #region Public Properties

        public static Action<Song, Song, Song> OnChangedTrack;
        public static TypedEventHandler<MediaPlayer, object> OnMediaPlayerStateChanged;

        public static Playlist CurrentPlaylist { get; private set; }

        public static Song CurrentSong { get { if (CurrentSongIndex >= 0) return CurrentPlaylist.Songs[CurrentSongIndex]; return null; } }

        public static int CurrentSongIndex { get; private set; }

        public static bool RepeatEnabled { get; set; }

        public static bool ShuffleEnabled { get; set; }

        public static MediaPlayerState CurrentState
        {
            get { return BackgroundMediaPlayer.Current.CurrentState; }
        }

        public static bool IsAudioPlaying
        {
            get
            {
                return BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing ||
                    BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Buffering ||
                    BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Opening;
            }
        }

        public static bool IsAudioOpen
        {
            get
            {
                return BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing ||
                    BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Paused ||
                    BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Buffering ||
                    BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Opening;
            }
        }

        #endregion

        #region Private Fields and Properties

        private static ManualResetEvent ServerInitialized = new ManualResetEvent(false);

        #endregion

        #region Public Methods

        public static void SetPlaylist(Playlist playlist, int index = 0)
        {
            CurrentPlaylist = playlist;
            CurrentSongIndex = index;
        }

        public static async void StartAudio(CoreDispatcher dispatcher)
        {
            StartBackgroundAudioTask(dispatcher);

            await PlaySong();
        }

        public static void Play()
        {
            ValueSet message = new ValueSet();
            message.Add(Constants.PlayTrack, null);
            BackgroundMediaPlayer.SendMessageToBackground(message);
        }

        public static void Pause()
        {
            ValueSet message = new ValueSet();
            message.Add(Constants.PauseTrack, null);
            BackgroundMediaPlayer.SendMessageToBackground(message);
        }

        public static async void SkipNext()
        {
            int nextIndex, subsequentIndex;
            GetSubsequentIndices(out nextIndex, out subsequentIndex);

            if (nextIndex == -1)
                StopMusic();
            else
            {
                CurrentSongIndex = nextIndex;
                await PlaySong();
            }
        }

        public static async void SkipPrevious()
        {
            int previousIndex;
            GetPreviousIndices(out previousIndex);

            if (previousIndex == -1)
                StopMusic();
            else
            {
                CurrentSongIndex = previousIndex;
                await PlaySong();
            }
        }

        public static void Seek(double time)
        {
            ValueSet message = new ValueSet();
            message.Add(Constants.Seek, time);
            BackgroundMediaPlayer.SendMessageToBackground(message);
        }

        public static void AddToNowPlaying(Song song)
        {
            if (CurrentPlaylist != null)
                CurrentPlaylist.Songs.Add(song);
        }

        public static void AddToNowPlaying(Album album)
        {
            if (CurrentPlaylist != null)
            {
                foreach (Song song in MusicLibrary.Instance.GetSongs(album))
                    CurrentPlaylist.Songs.Add(song);
            }
        }

        public static void AddToNowPlaying(Artist artist)
        {
            if (CurrentPlaylist != null)
            {
                foreach (Song song in MusicLibrary.Instance.GetSongs(artist))
                    CurrentPlaylist.Songs.Add(song);
            }
        }

        #endregion

        #region Private Methods

        private static async Task PlaySong()
        {
            await Task.Run(new Action(() =>
            {
                ValueSet message = new ValueSet();
                message.Add(Constants.StartPlayback, JsonSerialization.Serialize(CurrentPlaylist.Songs[CurrentSongIndex]));
                BackgroundMediaPlayer.SendMessageToBackground(message);

                if (OnChangedTrack != null)
                {
                    Song nextSong, subsequentSong;
                    GetSubsequentSongs(out nextSong, out subsequentSong);

                    OnChangedTrack(CurrentPlaylist.Songs[CurrentSongIndex], nextSong, subsequentSong);
                }
            }));
        }

        private static void StopMusic()
        {
            ValueSet message = new ValueSet();
            message.Add(Constants.StopPlayback, null);
            BackgroundMediaPlayer.SendMessageToBackground(message);
        }

        private static void GetPreviousIndices(out int previousIndex)
        {
            previousIndex = CurrentSongIndex - 1;
            if (previousIndex >= CurrentPlaylist.Songs.Count)
            {
                if (RepeatEnabled)
                    previousIndex += CurrentPlaylist.Songs.Count;
                else
                    previousIndex = -1;
            }
        }

        public static void GetSubsequentIndices(out int nextIndex, out int subsequentIndex)
        {
            nextIndex = CurrentSongIndex + 1;
            if (nextIndex >= CurrentPlaylist.Songs.Count)
            {
                if (RepeatEnabled)
                    nextIndex -= CurrentPlaylist.Songs.Count;
                else
                    nextIndex = -1;
            }
            subsequentIndex = CurrentSongIndex + 2;
            if (subsequentIndex >= CurrentPlaylist.Songs.Count)
            {
                if (RepeatEnabled)
                    subsequentIndex -= CurrentPlaylist.Songs.Count;
                else
                    subsequentIndex = -1;
            }
        }

        public static void GetSubsequentSongs(out Song nextSong, out Song subsequentSong)
        {
            int nextIndex, subsequentIndex;
            GetSubsequentIndices(out nextIndex, out subsequentIndex);

            nextSong = nextIndex == -1 ? null : CurrentPlaylist.Songs[nextIndex];
            subsequentSong = subsequentIndex == -1 ? null : CurrentPlaylist.Songs[subsequentIndex];
        }

        /// <summary>
        /// Initialize Background Media Player Handlers and starts playback
        /// </summary>
        private static void StartBackgroundAudioTask(CoreDispatcher dispatcher)
        {
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

        private static void BackgroundTaskInitializationCompleted(IAsyncAction action, AsyncStatus status)
        {
            if (status == AsyncStatus.Completed)
            {
                Debug.WriteLine("Background Audio Task initialized");
            }
            else if (status == AsyncStatus.Error)
            {
                Debug.WriteLine("Background Audio Task could not initialized due to an error ::" + action.ErrorCode.ToString());
            }
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
                    case Constants.SkipNext:
                        SkipNext();
                        break;
                    case Constants.SkipPrevious:
                        SkipPrevious();
                        break;
                }
            }
        }

        #endregion

        #endregion

        public static void KillBackgroundTask()
        {
            ValueSet message = new ValueSet();
            message.Add(Constants.KillBackgroundTask, null);
            BackgroundMediaPlayer.SendMessageToBackground(message);
        }
    }
}
