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
using System.Threading;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media.Animation;
using Windows.Storage.Streams;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace ModernMusic.Controls
{
    public sealed partial class NowPlayingControl : UserControl
    {
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private Page Page;
        private ManualResetEventSlim _programChangingScrollViewer = new ManualResetEventSlim();
        private const int SIZE_OF_ALBUM_ART = 450;
        private ScrollViewer songListScroller;

        public NowPlayingControl()
        {
            this.InitializeComponent();

            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                grid.Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        public void SetupPage(Page page)
        {
            Page = page;

            NowPlayingManager.OnChangedTrack += () =>
            {
                var a = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, ResumeLayout);
            };
            NowPlayingManager.OnMediaPlayerStateChanged += NowPlayingManager_OnMediaPlayerStateChanged;
            NowPlayingInformation.OnCurrentPlaylistUpdated += NowPlayingInformation_OnCurrentPlaylistUpdated;
        }

        public void ResumeLayout()
        {
            if (songList.Items.Count == 0)
                return;

            Song song = NowPlayingInformation.CurrentSong;
            this.DefaultViewModel["Song"] = song;

            ScrollToCorrectSongLocation(song);
        }

        private void ScrollToCorrectSongLocation(Song song, bool disableAnimation = false)
        {
            //songList.ScrollIntoView(songList.Items[currentIndex - 1], ScrollIntoViewAlignment.Leading);
            int currentIndex = NowPlayingInformation.CurrentIndex;
            if (songListScroller != null)
                songListScroller.ChangeView(null, Math.Max(0, (currentIndex + 1)), null, disableAnimation);
            //songList.ScrollIntoView(songList.Items[Math.Max(0, (currentIndex - 1))]);

            albumArtList.ScrollIntoView(albumArtList.Items[currentIndex], ScrollIntoViewAlignment.Leading);
            //albumArtScroller.ChangeView(currentIndex * SIZE_OF_ALBUM_ART, null, null, disableAnimation);
        }

        internal void NowPlayingInformation_OnCurrentPlaylistUpdated()
        {
            SolidColorBrush accentBrush = (SolidColorBrush)App.Current.Resources["PhoneAccentBrush"];
            repeatButton.Foreground =
                NowPlayingInformation.RepeatEnabled ? accentBrush : new SolidColorBrush(Colors.White);

            shuffleButton.Foreground =
                NowPlayingInformation.ShuffleEnabled ? accentBrush : new SolidColorBrush(Colors.White);

            Song song = NowPlayingInformation.CurrentSong;
            this.DefaultViewModel["Song"] = song;

            List<Song> songs = NowPlayingInformation.CurrentPlaylist.GetSongList();
            songList.ItemsSource = songs;

            foreach (Song s in NowPlayingInformation.CurrentPlaylist.GetSongList())
            {
                if (string.IsNullOrEmpty(s.CachedImagePath))
                    s.CachedImagePath = MusicLibrary.Instance.GetAlbum(s).CachedImagePath;
            }
            albumArtList.ItemsSource = songs;

            var aa = Dispatcher.RunIdleAsync((o) =>
            {
                ScrollToCorrectSongLocation(song, true);
            });
        }

        private void songListScroller_Loaded(object sender, RoutedEventArgs e)
        {
            songListScroller = (ScrollViewer)sender;
            //This keeps the previous song/selected song at the top of the songList even at the bottom of the songList
            /*double height = (songList.ActualHeight - 21 - 36);//This is supposed to be 36, it removes a hanging pixel at the bottom
            var block = new TextBlock()
            {
                MinHeight = height,
                Height = height,
                Width = 320,
                Text = "None",
                Foreground = new SolidColorBrush(Colors.Transparent)
            };*/
        }

        private void NowPlayingManager_OnMediaPlayerStateChanged(MediaPlayer sender, object args)
        {
            var aa = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                if (NowPlayingManager.IsAudioPlaying)
                {
                    playButton.Symbol = Symbol.Pause;
                }
                else
                    playButton.Symbol = Symbol.Play;
            });
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
            NowPlayingManager.SkipToNextSong(Dispatcher, NowPlayingInformation.CurrentPlaylist);
            ResumeLayout();
        }

        private void previousButton_Click(object sender, RoutedEventArgs e)
        {
            if (NowPlayingManager.SkipToPreviousSong(Dispatcher, NowPlayingInformation.CurrentPlaylist))
                ResumeLayout();
        }

        private void repeatButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NowPlayingInformation.RepeatEnabled = !NowPlayingInformation.RepeatEnabled;
            SolidColorBrush accentBrush = (SolidColorBrush)App.Current.Resources["PhoneAccentBrush"];
            ((SymbolIcon)sender).Foreground =
                NowPlayingInformation.RepeatEnabled ? accentBrush : new SolidColorBrush(Colors.White);

            NowPlayingInformation_OnCurrentPlaylistUpdated();
        }

        private void shuffleButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NowPlayingInformation.ShuffleEnabled = !NowPlayingInformation.ShuffleEnabled;
            SolidColorBrush accentBrush = (SolidColorBrush)App.Current.Resources["PhoneAccentBrush"];
            ((SymbolIcon)sender).Foreground =
                NowPlayingInformation.ShuffleEnabled ? accentBrush : new SolidColorBrush(Colors.White);

            NowPlayingInformation.CurrentPlaylist = NowPlayingInformation.CurrentPlaylist;

            NowPlayingManager.ReplaySong(Dispatcher);

            NowPlayingInformation_OnCurrentPlaylistUpdated();
        }

        private void songList_tapped(object sender, TappedRoutedEventArgs e)
        {
            playlistButton_Tapped(sender, e);
        }
        
        private void playlistButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!Page.Frame.Navigate(typeof(PlaylistView), NowPlayingInformation.CurrentPlaylist))
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

        private void albumArtScroller_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _programChangingScrollViewer.Set();
        }

        private void albumArtScroller_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (!e.IsIntermediate && _programChangingScrollViewer.IsSet)
            {
                _programChangingScrollViewer.Reset();
                ScrollViewer viewer = (ScrollViewer)sender;
                int idx = (int)(viewer.HorizontalOffset / SIZE_OF_ALBUM_ART);
                if (NowPlayingManager.SkipToSong(idx, Dispatcher))
                    ResumeLayout();
            }
        }
    }
}
