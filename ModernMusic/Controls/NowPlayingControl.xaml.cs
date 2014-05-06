using ModernMusic.Library;
using ModernMusic.Helpers;
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

namespace ModernMusic.Controls
{
    public sealed partial class NowPlayingControl : UserControl
    {
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private Page Page;

        public NowPlayingControl()
        {
            this.InitializeComponent();

            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                gridBackground.Background = new SolidColorBrush(Colors.Transparent);
            }

            NowPlayingManager.OnChangedTrack += ResumeLayout;
            NowPlayingManager.OnMediaPlayerStateChanged += NowPlayingManager_OnMediaPlayerStateChanged;
        }

        public void SetupPage(Page page)
        {
            Page = page;
        }

        public void ResumeLayout()
        {
            Song previousSong, currentSong, nextSong, subsequentSong;
            NowPlayingManager.GetNowPlaying(out previousSong, out currentSong, out nextSong, out subsequentSong);

            if (currentSong == null)
                return;
            
            this.DefaultViewModel["PreviousSong"] = previousSong;
            this.DefaultViewModel["Song"] = currentSong;
            this.DefaultViewModel["NextSong"] = nextSong;
            this.DefaultViewModel["SubsequentSong"] = subsequentSong;

            LoadSongArtwork(currentSong);
        }

        private void NowPlayingManager_OnMediaPlayerStateChanged(MediaPlayer sender, object args)
        {
            var t = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (NowPlayingManager.IsAudioPlaying)
                {
                    playButton.Symbol = Symbol.Pause;
                }
                else
                    playButton.Symbol = Symbol.Play;
            });
        }

        private void nowPlayingControl_loaded(object sender, RoutedEventArgs e)
        {
            AlbumArtwork.Width = this.ActualWidth - 20 - 60;
            AlbumArtwork.Height = AlbumArtwork.Width;
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

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public void EnsurePlayback()
        {
            if (!NowPlayingManager.IsAudioPlaying)
            {
                if (NowPlayingManager.PlayCurrentSong(Dispatcher))
                    ResumeLayout();
            }
        }

        private void playButton_Click(object sender, TappedRoutedEventArgs e)
        {
            if (!NowPlayingManager.IsAudioPlaying)
            {
                if (NowPlayingManager.PlayCurrentSong(Dispatcher))
                    ResumeLayout();
            }
            else
            {
                NowPlayingManager.PauseCurrentSong();
            }
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            NowPlayingManager.SkipToNextSong(Dispatcher);
            ResumeLayout();
        }

        private void previousButton_Click(object sender, RoutedEventArgs e)
        {
            if (NowPlayingManager.SkipToPreviousSong(Dispatcher))
                ResumeLayout();
        }

        private void repeatButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NowPlayingInformation.RepeatEnabled = !NowPlayingInformation.RepeatEnabled;
            SolidColorBrush accentBrush = (SolidColorBrush)App.Current.Resources["PhoneAccentBrush"];
            ((SymbolIcon)sender).Foreground =
                NowPlayingInformation.RepeatEnabled ? accentBrush : new SolidColorBrush(Colors.White);

            ResumeLayout();
        }

        private void shuffleButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NowPlayingInformation.ShuffleEnabled = !NowPlayingInformation.ShuffleEnabled;
            SolidColorBrush accentBrush = (SolidColorBrush)App.Current.Resources["PhoneAccentBrush"];
            ((SymbolIcon)sender).Foreground =
                NowPlayingInformation.ShuffleEnabled ? accentBrush : new SolidColorBrush(Colors.White);

            ResumeLayout();
        }
        
        private void playlistButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!Page.Frame.Navigate(typeof(PlaylistView), new KeyValuePair<Playlist, int>(
                NowPlayingInformation.CurrentPlaylist, NowPlayingInformation.CurrentIndex)))
            {
                var resourceLoader = ResourceLoader.GetForCurrentView("Resources");
                throw new Exception(resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        private void icon_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Grid grid = (Grid)sender;
            Border border = (Border)grid.Children[0];
            SymbolIcon symbol = (SymbolIcon)border.Child;

            symbol.Foreground = new SolidColorBrush(Colors.Black);
            border.BorderThickness = new Thickness(0);
            border.Background = new SolidColorBrush(Colors.White);
        }

        private void icon_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            Grid grid = (Grid)sender;
            Border border = (Border)grid.Children[0];
            SymbolIcon symbol = (SymbolIcon)border.Child;

            symbol.Foreground = new SolidColorBrush(Colors.White);
            border.BorderThickness = new Thickness(3);
            border.Background = new SolidColorBrush(Colors.Transparent);
        }
    }
}
