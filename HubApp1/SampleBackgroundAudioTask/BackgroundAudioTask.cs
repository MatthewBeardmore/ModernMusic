/*
 * (c) Copyright Microsoft Corporation.
This source is subject to the Microsoft Public License (Ms-PL).
All other rights reserved.
 */
using System;
using System.Diagnostics;
using System.Threading;
using Windows.ApplicationModel.Background;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Foundation.Collections;
using Windows.Storage;
using ModernMusic.Library;
using System.Threading.Tasks;
using ModernMusic.Helpers;

/* This is the Sample background task that will start running the first time 
 * MediaPlayer singleton instance is accessed from foreground. When a new audio 
 * or video app comes into picture the task is expected to recieve the cancelled 
 * event. User can save state and shutdown MediaPlayer at that time. When foreground 
 * app is resumed or restarted check if your music is still playing or continue from
 * previous state.
 * 
 * This task also implements SystemMediaTransportControl apis for windows phone universal 
 * volume control. Unlike Windows 8.1 where there are different views in phone context, 
 * SystemMediaTransportControl is singleton in nature bound to the process in which it is 
 * initialized. If you want to hook up volume controls for the background task, do not 
 * implement SystemMediaTransportControls in foreground app process.
 */

namespace BackgroundAudioTask
{
    /// <summary>
    /// Enum to identify foreground app state
    /// </summary>
    enum ForegroundAppStatus
    {
        Active,
        Suspended,
        Unknown
    }

    /// <summary>
    /// Impletements IBackgroundTask to provide an entry point for app code to be run in background. 
    /// Also takes care of handling UVC and communication channel with foreground
    /// </summary>
    public sealed class BackgroundAudioTask : IBackgroundTask
    {
        #region Private fields, properties

        private SystemMediaTransportControls systemmediatransportcontrol;
        private BackgroundTaskDeferral deferral; // Used to keep task alive
        private AutoResetEvent BackgroundTaskStarted = new AutoResetEvent(false);
        private bool backgroundtaskrunning = false;
        private Playlist _currentCachedPlaylist = null;
        private CancellationTokenSource _cancellationToken = null;
        
        #endregion

        #region IBackgroundTask and IBackgroundTaskInstance Interface Members and handlers
        /// <summary>
        /// The Run method is the entry point of a background task. 
        /// </summary>
        /// <param name="taskInstance"></param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            NowPlayingInformation.DisableCaching = true;
            MusicLibrary.IsBackgroundTask = true;

            Debug.WriteLine("Background Audio Task " + taskInstance.Task.Name + " starting...");
            // Initialize SMTC object to talk with UVC. 
            //Note that, this is intended to run after app is paused and 
            //hence all the logic must be written to run in background process
            systemmediatransportcontrol = SystemMediaTransportControls.GetForCurrentView();
            systemmediatransportcontrol.ButtonPressed += systemmediatransportcontrol_ButtonPressed;
            systemmediatransportcontrol.PropertyChanged += systemmediatransportcontrol_PropertyChanged;
            systemmediatransportcontrol.IsEnabled = true;
            systemmediatransportcontrol.IsPauseEnabled = true;
            systemmediatransportcontrol.IsPlayEnabled = true;
            systemmediatransportcontrol.IsNextEnabled = true;
            systemmediatransportcontrol.IsPreviousEnabled = true;
            systemmediatransportcontrol.IsRewindEnabled = true;
            systemmediatransportcontrol.IsFastForwardEnabled = true;

            // Associate a cancellation and completed handlers with the background task.
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);
            taskInstance.Task.Completed += Taskcompleted;
            //Add handlers for MediaPlayer
            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaEnded += MediaPlayer_MediaEnded;
            BackgroundMediaPlayer.Current.MediaFailed += mediaPlayer_MediaFailed;

            //Initialize message channel 
            BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;
            
            //Send information to foreground that background task has been started if app is active
            
            ValueSet message = new ValueSet();
            message.Add(Constants.BackgroundTaskIsRunning, "");
            BackgroundMediaPlayer.SendMessageToForeground(message);

            BackgroundTaskStarted.Set();
            backgroundtaskrunning = true;

            deferral = taskInstance.GetDeferral();           
        }

        /// <summary>
        /// Indicate that the background task is completed.
        /// </summary>       
        void Taskcompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            SendBackgroundTaskIsStopping();

            NowPlayingInformation.CurrentIndex = -1;
            NowPlayingInformation.CurrentPlaylist = null;

            Debug.WriteLine("MyBackgroundAudioTask " + sender.TaskId + " Completed...");
            deferral.Complete();
        }

        /// <summary>
        /// Handles background task cancellation. Task cancellation happens due to :
        /// 1. Another Media app comes into foreground and starts playing music 
        /// 2. Resource pressure. Your task is consuming more CPU and memory than allowed.
        /// In either case, save state so that if foreground app resumes it can know where to start.
        /// </summary>
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            SendBackgroundTaskIsStopping();

            try
            {
                backgroundtaskrunning = false;
                //unsubscribe event handlers
                systemmediatransportcontrol.ButtonPressed -= systemmediatransportcontrol_ButtonPressed;
                systemmediatransportcontrol.PropertyChanged -= systemmediatransportcontrol_PropertyChanged;
                
                BackgroundMediaPlayer.Shutdown(); // shutdown media pipeline
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            deferral.Complete(); // signals task completion. 
            Debug.WriteLine("MyBackgroundAudioTask Cancel complete...");
        }

        private void SendBackgroundTaskIsStopping()
        {
            ValueSet message = new ValueSet();
            message.Add(Constants.BackgroundTaskIsStopping, "");
            BackgroundMediaPlayer.SendMessageToForeground(message);
        }

        #endregion

        #region SysteMediaTransportControls related functions and handlers
        /// <summary>
        /// Update UVC using SystemMediaTransPortControl apis
        /// </summary>
        private async void UpdateUVCOnNewTrack(Song song, StorageFile file)
        {
            if(file == null)
                file = await StorageFile.GetFileFromPathAsync(song.FilePath);
            systemmediatransportcontrol.PlaybackStatus = MediaPlaybackStatus.Playing;
            await systemmediatransportcontrol.DisplayUpdater.CopyFromFileAsync(MediaPlaybackType.Music, file);
            systemmediatransportcontrol.DisplayUpdater.Update();
        }

        /// <summary>
        /// Fires when any SystemMediaTransportControl property is changed by system or user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void systemmediatransportcontrol_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            //If soundlevel turns to muted, app can choose to pause the music
        }

        /// <summary>
        /// This function controls the button events from UVC.
        /// This code if not run in background process, will not be able to handle button pressed events when app is suspended.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void systemmediatransportcontrol_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            int index;

            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.FastForward:
                    BackgroundMediaPlayer.Current.Pause();
                    
                    _cancellationToken = new CancellationTokenSource();
                    Utilities.CreateSeekingTask(1, _cancellationToken.Token, FireOnSeek);
                    break;
                case SystemMediaTransportControlsButton.Rewind:
                    BackgroundMediaPlayer.Current.Pause();

                    _cancellationToken = new CancellationTokenSource();
                    Utilities.CreateSeekingTask(-1, _cancellationToken.Token, FireOnSeek);
                    break;
                case SystemMediaTransportControlsButton.Play: 
                    Debug.WriteLine("UVC play button pressed");
                    // If music is in paused state, for a period of more than 5 minutes, 
                    //app will get task cancellation and it cannot run code. 
                    //However, user can still play music by pressing play via UVC unless a new app comes in clears UVC.
                    //When this happens, the task gets re-initialized and that is asynchronous and hence the wait
                    if (!backgroundtaskrunning)
                    {
                        bool result = BackgroundTaskStarted.WaitOne(2000);
                        if (!result)
                            throw new Exception("Background Task didnt initialize in time");
                    }
                    if (_cancellationToken != null)
                    {
                        _cancellationToken.Cancel();
                        _cancellationToken = null;
                    }
                    
                    BackgroundMediaPlayer.Current.Play();
                    break;
                case SystemMediaTransportControlsButton.Pause: 
                    Debug.WriteLine("UVC pause button pressed");
                    try
                    {
                        BackgroundMediaPlayer.Current.Pause();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                    break;
                case SystemMediaTransportControlsButton.Next:
                    Debug.WriteLine("UVC next button pressed");

                    index = NowPlayingInformation.SkipToNextSong(true);
                    if (index < 0)
                        StopPlayback();
                    else
                    {
                        Task.Run(new Action(FireOnChangedTrack));
                        BeginPlaying();
                    }
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    Debug.WriteLine("UVC previous button pressed");
                    
                    index = NowPlayingInformation.SkipToPreviousSong(true);
                    if (index < 0)
                        StopPlayback();
                    else
                    {
                        Task.Run(new Action(FireOnChangedTrack));
                        BeginPlaying();
                    }
                    break;
            }
        }

        #endregion

        #region Background Media Player Handlers

        void Current_CurrentStateChanged(MediaPlayer sender, object args)
        {
            try
            {
                if (sender.CurrentState == MediaPlayerState.Playing)
                {
                    systemmediatransportcontrol.PlaybackStatus = MediaPlaybackStatus.Playing;
                }
                else if (sender.CurrentState == MediaPlayerState.Paused)
                {
                    systemmediatransportcontrol.PlaybackStatus = MediaPlaybackStatus.Paused;
                }
            }
            catch { }
        }

        private void mediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Debug.WriteLine("Failed with error code " + args.ExtendedErrorCode.ToString());
        }

        /// <summary>
        /// Handler for MediaPlayer Media Ended
        /// </summary>
        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            NowPlayingInformation.SkipToNextSong();
            Task.Run(new Action(FireOnChangedTrack));
            BeginPlaying();
        }

        /// <summary>
        /// Fires when a message is recieved from the foreground app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BackgroundMediaPlayer_MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                switch (key)
                {
                    //New management
                    case Constants.BackgroundTaskQuery:
                        //Basically a ping to make sure that the background task is running
                        ValueSet message = new ValueSet();
                        message.Add(Constants.BackgroundTaskIsRunning, "");
                        BackgroundMediaPlayer.SendMessageToForeground(message);
                        break;
                    case Constants.StartPlaying:
                        if (e.Data[key] != null)
                        {
                            //If it's not null, then the playlist changed and we need to recache it
                            _currentCachedPlaylist = null;
                        }
                        BeginPlaying();
                        break;
                    case Constants.ClearCache:
                        _currentCachedPlaylist = null;
                        BeginPlaying(false);
                        break;
                    case Constants.PlayTrack:
                        Play();
                        break;
                    case Constants.PauseTrack:
                        Pause();
                        break;
                    case Constants.StopPlayback:
                        StopPlayback();
                        break;
                }
            }
        }

        #region New Background Manager Methods

        private void FireOnChangedTrack()
        {
            ValueSet value = new ValueSet();
            value.Add(Constants.ChangedTrack, null);
            BackgroundMediaPlayer.SendMessageToForeground(value);
        }

        private void FireOnSeek()
        {
            ValueSet value = new ValueSet();
            value.Add(Constants.Seek, null);
            BackgroundMediaPlayer.SendMessageToForeground(value);
        }

        private async void BeginPlaying(bool autoPlay = true)
        {
            if (_currentCachedPlaylist == null)
                _currentCachedPlaylist = NowPlayingInformation.CurrentPlaylist;
            Song currentSong = NowPlayingInformation.GetCurrentSong(_currentCachedPlaylist);

            if (currentSong == null)
            {
                StopPlayback();
                return;
            }

            StorageFile file = await StorageFile.GetFileFromPathAsync(currentSong.FilePath);
            UpdateUVCOnNewTrack(currentSong, file);

            BackgroundMediaPlayer.Current.PlaybackRate = 1;
            BackgroundMediaPlayer.Current.AutoPlay = autoPlay;
            BackgroundMediaPlayer.Current.SetFileSource(file);
        }

        private void Play()
        {
            BackgroundMediaPlayer.Current.Play();
        }

        private void Pause()
        {
            BackgroundMediaPlayer.Current.Pause();
        }

        private void StopPlayback()
        {
            BackgroundMediaPlayer.Current.PlaybackRate = 0;
            BackgroundMediaPlayer.Current.Position = TimeSpan.FromSeconds(0);
            systemmediatransportcontrol.PlaybackStatus = MediaPlaybackStatus.Closed;
        }

        #endregion

        #endregion

    }
}
