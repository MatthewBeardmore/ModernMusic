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
using ModernMusic.Library.Helpers;

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
        private Song _currentSong;

        #endregion

        #region IBackgroundTask and IBackgroundTaskInstance Interface Members and handlers
        /// <summary>
        /// The Run method is the entry point of a background task. 
        /// </summary>
        /// <param name="taskInstance"></param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {
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
            // You get some time here to save your state before process and resources are reclaimed
            Debug.WriteLine("MyBackgroundAudioTask " + (sender == null ? "null" : sender.Task.TaskId.ToString()) + " Cancel Requested...");
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

        #endregion

        #region SysteMediaTransportControls related functions and handlers
        /// <summary>
        /// Update UVC using SystemMediaTransPortControl apis
        /// </summary>
        private async void UpdateUVCOnNewTrack(Song song)
        {
            systemmediatransportcontrol.PlaybackStatus = MediaPlaybackStatus.Playing;
            /*systemmediatransportcontrol.DisplayUpdater.Type = MediaPlaybackType.Music;
            systemmediatransportcontrol.DisplayUpdater.MusicProperties.Title = song.SongTitle;
            systemmediatransportcontrol.DisplayUpdater.MusicProperties.Artist = song.Artist;*/
            await systemmediatransportcontrol.DisplayUpdater.CopyFromFileAsync(MediaPlaybackType.Music,
                await StorageFile.GetFileFromPathAsync(song.FilePath));
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
            switch (args.Button)
            {
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
                    StartPlayback(null);
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
                    SkipToNext();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    Debug.WriteLine("UVC previous button pressed");
                    SkipToPrevious();
                    break;
            }
        }

        #endregion

        #region Playlist management functions and handlers

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
            BackgroundMediaPlayer.Current.Pause();
        }

        /// <summary>
        /// Start playlist and change UVC state
        /// </summary>

        private async void StartPlayback(Song song)
        {
            try
            {
                if (song != null)
                {
                    _currentSong = song;
                    BackgroundMediaPlayer.Current.AutoPlay = true;
                    BackgroundMediaPlayer.Current.SetFileSource(await StorageFile.GetFileFromPathAsync(song.FilePath));
                    UpdateUVCOnNewTrack(song);
                }
                else
                {
                    BackgroundMediaPlayer.Current.Play();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Skip track and update UVC via SMTC
        /// </summary>
        private void SkipToPrevious()
        {
            //systemmediatransportcontrol.PlaybackStatus = MediaPlaybackStatus.;
            ValueSet message = new ValueSet();
            message.Add(Constants.SkipPrevious, null);
            BackgroundMediaPlayer.SendMessageToForeground(message);
        }

        /// <summary>
        /// Skip track and update UVC via SMTC
        /// </summary>
        private void SkipToNext()
        {
            //systemmediatransportcontrol.PlaybackStatus = MediaPlaybackStatus.Changing;
            ValueSet message = new ValueSet();
            message.Add(Constants.SkipNext, null);
            BackgroundMediaPlayer.SendMessageToForeground(message);
        }

        private void Seek(double position)
        {
            BackgroundMediaPlayer.Current.Position = TimeSpan.FromSeconds(position);
        }

        #endregion

        #region Background Media Player Handlers
        void Current_CurrentStateChanged(MediaPlayer sender, object args)
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

        private void mediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Debug.WriteLine("Failed with error code " + args.ExtendedErrorCode.ToString());
        }

        /// <summary>
        /// Handler for MediaPlayer Media Ended
        /// </summary>
        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            SkipToNext();
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
                    case Constants.BackgroundTaskQuery:
                        //Basically a ping to make sure that the background task is running
                        ValueSet message = new ValueSet();
                        message.Add(Constants.BackgroundTaskIsRunning, "");
                        BackgroundMediaPlayer.SendMessageToForeground(message);
                        break;
                    case Constants.KillBackgroundTask:
                        OnCanceled(null, BackgroundTaskCancellationReason.Abort);
                        break;
                    case Constants.StartPlayback: //Foreground App process has signalled that it is ready for playback
                        Debug.WriteLine("Starting to play a track");
                        Song song = JsonSerialization.Deserialize<Song>(e.Data[key].ToString());
                        StartPlayback(song);
                        break;
                    case Constants.PlayTrack:
                        Debug.WriteLine("Play");
                        Play();
                        break;
                    case Constants.PauseTrack:
                        Debug.WriteLine("Paused");
                        Pause();
                        break;
                    case Constants.StopPlayback:
                        StopPlayback();
                        break;
                    case Constants.Seek:
                        Seek((double)e.Data[key]);
                        break;
                }
            }
        }

        #endregion

    }
}
