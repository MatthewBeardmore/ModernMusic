using HubApp1.Common;
using ModernMusic.Library;
using ModernMusic.Library.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace HubApp1
{
    public sealed partial class NowPlayingControl : UserControl
    {
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private bool _updatingPositionSlider = false;
        private Page Page;

        public NowPlayingControl()
        {
            this.InitializeComponent();

            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                gridBackground.Background = new SolidColorBrush(Colors.Transparent);
            }

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.5);
            timer.Start();
            timer.Tick += timer_Tick;

            NowPlayingManager.OnChangedTrack += NowPlayingManager_OnTrackChanged;
            NowPlayingManager.OnMediaPlayerStateChanged += NowPlayingManager_OnMediaPlayerStateChanged;
        }

        public void SetupPage(Page page)
        {
            Page = page;
        }

        public void ResumeLayout()
        {
            Song nextSong, subsequentSong;
            NowPlayingManager.GetSubsequentSongs(out nextSong, out subsequentSong);
            this.DefaultViewModel["Song"] = NowPlayingManager.CurrentSong;
            this.DefaultViewModel["NextSong"] = nextSong;
            this.DefaultViewModel["SubsequentSong"] = subsequentSong;
            LoadSongArtwork(NowPlayingManager.CurrentSong);
        }

        private void NowPlayingManager_OnTrackChanged(Song song, Song nextSong, Song subsequentSong)
        {
            this.DefaultViewModel["Song"] = song;
            this.DefaultViewModel["NextSong"] = nextSong;
            this.DefaultViewModel["SubsequentSong"] = subsequentSong;
            LoadSongArtwork(song);
        }

        private void NowPlayingManager_OnMediaPlayerStateChanged(MediaPlayer sender, object args)
        {
            var t = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (NowPlayingManager.IsAudioPlaying)
                {
                    playButton.Icon = new SymbolIcon(Symbol.Pause);
                }
                else
                    playButton.Icon = new SymbolIcon(Symbol.Play);
            });
        }

        private void LoadSongArtwork(Song song)
        {
            string imagePath = MusicLibrary.Instance.GetAlbum(song).ImagePath;
            Uri uri;
            if (Uri.TryCreate(imagePath, UriKind.RelativeOrAbsolute, out uri))
            {
                var t = Dispatcher.RunIdleAsync((e) => 
                {
                    try { AlbumArtworkImage.UriSource = uri; } 
                    catch { AlbumArtworkImage.UriSource = null; } 
                });
            }
        }

        void timer_Tick(object sender, object e)
        {
            _updatingPositionSlider = true;
            if (NowPlayingManager.IsAudioOpen)
            {
                positionSlider.Value = BackgroundMediaPlayer.Current.Position.TotalSeconds;
                positionSlider.Maximum = BackgroundMediaPlayer.Current.NaturalDuration.TotalSeconds;
                if (positionSlider.Maximum < 1.0f)
                    positionSlider.Maximum = 100f;
            }
            else
            {
                positionSlider.Value = 0;
                positionSlider.Maximum = 100;
            }
            _updatingPositionSlider = false;
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            if (NowPlayingManager.CurrentState == MediaPlayerState.Paused)
            {
                NowPlayingManager.Play();
            }
            else
            {
                NowPlayingManager.Pause();
            }
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            NowPlayingManager.SkipNext();
            ResumeLayout();
        }

        private void previousButton_Click(object sender, RoutedEventArgs e)
        {
            NowPlayingManager.SkipPrevious();
            ResumeLayout();
        }

        private void positionSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!_updatingPositionSlider)
                NowPlayingManager.Seek(positionSlider.Value);
        }

        private void repeatButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NowPlayingManager.RepeatEnabled = !NowPlayingManager.RepeatEnabled;
            SolidColorBrush accentBrush = (SolidColorBrush)App.Current.Resources["PhoneAccentBrush"];
            ((SymbolIcon)((ContentControl)((ContentPresenter)sender).Content).Content).Foreground = 
                NowPlayingManager.RepeatEnabled ? accentBrush : new SolidColorBrush(Colors.White);
        }
        
        private void playlistButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!Page.Frame.Navigate(typeof(PlaylistView), NowPlayingManager.CurrentPlaylist))
            {
                var resourceLoader = ResourceLoader.GetForCurrentView("Resources");
                throw new Exception(resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }
        
        private void shuffleButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NowPlayingManager.ShuffleEnabled = !NowPlayingManager.ShuffleEnabled;
            SolidColorBrush accentBrush = (SolidColorBrush)App.Current.Resources["PhoneAccentBrush"];
            ((SymbolIcon)((ContentControl)((ContentPresenter)sender).Content).Content).Foreground =
                NowPlayingManager.ShuffleEnabled ? accentBrush : new SolidColorBrush(Colors.White);
        }
    }
}
