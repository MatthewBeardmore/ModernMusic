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
        private TextBlock _currentlyPlayingTextBlock;
        private const int SIZE_OF_ALBUM_ART = 450;

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

            SolidColorBrush accentBrush = (SolidColorBrush)App.Current.Resources["PhoneAccentBrush"];
            repeatButton.Foreground =
                NowPlayingInformation.RepeatEnabled ? accentBrush : new SolidColorBrush(Colors.White);

            shuffleButton.Foreground =
                NowPlayingInformation.ShuffleEnabled ? accentBrush : new SolidColorBrush(Colors.White);
        }

        public void ResumeLayout()
        {
            if (songList.Children.Count == 0)
                return;

            int currentIndex = NowPlayingInformation.CurrentIndex;
            this.DefaultViewModel["Song"] = NowPlayingInformation.CurrentSong;

            if (_currentlyPlayingTextBlock != null)
            {
                _currentlyPlayingTextBlock.Margin = new Thickness(0, 0, 0, 0);
                _currentlyPlayingTextBlock.Height = _currentlyPlayingTextBlock.MinHeight =
                    _currentlyPlayingTextBlock.MaxHeight = 21;
                _currentlyPlayingTextBlock.SetValue(VariableSizedWrapGrid.RowSpanProperty, 21);
                _currentlyPlayingTextBlock.FontSize = 16;
                _currentlyPlayingTextBlock.Style = (Style)this.Resources["ListViewItemSubheaderTextBlockStyle"];
            }

            _currentlyPlayingTextBlock = (TextBlock)songList.Children[currentIndex];

            if (_currentlyPlayingTextBlock != null)
            {
                _currentlyPlayingTextBlock.Margin = new Thickness(0, -6, 0, 0);
                _currentlyPlayingTextBlock.Height = _currentlyPlayingTextBlock.MinHeight =
                    _currentlyPlayingTextBlock.MaxHeight = 37;
                _currentlyPlayingTextBlock.SetValue(VariableSizedWrapGrid.RowSpanProperty, 37);
                _currentlyPlayingTextBlock.FontSize = 27;
                _currentlyPlayingTextBlock.Style = (Style)this.Resources["SubheaderTextBlockStyle"];
            }

            ScrollToCorrectSongLocation(currentIndex);
        }

        private void ScrollToCorrectSongLocation(int currentIndex, bool disableAnimation = false)
        {
            //songList.ScrollIntoView(songList.Items[currentIndex - 1], ScrollIntoViewAlignment.Leading);
            songListScroller.ChangeView(null, Math.Max(0, (currentIndex - 1) * 21), null, disableAnimation);
            albumArtList.ScrollIntoView(albumArtList.Items[currentIndex], ScrollIntoViewAlignment.Leading);
            //albumArtScroller.ChangeView(currentIndex * SIZE_OF_ALBUM_ART, null, null, disableAnimation);
        }

        internal void NowPlayingInformation_OnCurrentPlaylistUpdated()
        {
            int currentIndex = NowPlayingInformation.CurrentIndex;
            this.DefaultViewModel["Song"] = NowPlayingInformation.CurrentSong;

            int idx = 0;

            _currentlyPlayingTextBlock = null;
            songList.Children.Clear();
            foreach (Song song in NowPlayingInformation.CurrentPlaylist.GetSongList())
            {
                TextBlock block = new TextBlock()
                {
                    Height = 21,
                    MinHeight = 21,
                    MaxHeight = 21,
                    Width = 320,
                    Text = song.SongTitle,
                    TextWrapping = TextWrapping.NoWrap,
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch,
                    Style = (Style)this.Resources["ListViewItemSubheaderTextBlockStyle"]
                };
                if (idx == currentIndex)
                {
                    block.Margin = new Thickness(0, -6, 0, 0);
                    block.Height = block.MinHeight = block.MaxHeight = 37;
                    block.SetValue(VariableSizedWrapGrid.RowSpanProperty, 37);
                    block.FontSize = 27;
                    block.Style = (Style)this.Resources["SubheaderTextBlockStyle"];
                    _currentlyPlayingTextBlock = block;
                }
                else
                    block.SetValue(VariableSizedWrapGrid.RowSpanProperty, 21);
                songList.Children.Add(block);
                idx++;
            }
            
            double height = (songListScroller.ActualHeight - 21 - 36);//This is supposed to be 36, it removes a hanging pixel at the bottom
            if(height > 0)
            {
                var endblock = new TextBlock()
                {
                    MinHeight = height,
                    Height = height,
                    Width = 320,
                    Text = "None",
                    Foreground = new SolidColorBrush(Colors.Transparent)
                };
                endblock.SetValue(VariableSizedWrapGrid.RowSpanProperty, height);
                songList.Children.Add(endblock);
            }

            albumArtList.Items.Clear();

            foreach (Song song in NowPlayingInformation.CurrentPlaylist.GetSongList())
            {
                if (string.IsNullOrEmpty(song.CachedImagePath))
                    song.CachedImagePath = MusicLibrary.Instance.GetAlbum(song).CachedImagePath;
                albumArtList.Items.Add(song);
            }

            var aa = Dispatcher.RunIdleAsync((o) =>
            {
                ScrollToCorrectSongLocation(currentIndex, true);
            });
        }

        private void songListScroller_Loaded(object sender, RoutedEventArgs e)
        {
            //This keeps the previous song/selected song at the top of the songList even at the bottom of the songList
            double height = (songListScroller.ActualHeight - 21 - 36);//This is supposed to be 36, it removes a hanging pixel at the bottom
            var block = new TextBlock()
            {
                MinHeight = height,
                Height = height,
                Width = 320,
                Text = "None",
                Foreground = new SolidColorBrush(Colors.Transparent)
            };
            block.SetValue(VariableSizedWrapGrid.RowSpanProperty, height);
            songList.Children.Add(block);
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
            bool needToUpdateShuffleList;
            NowPlayingInformation.ShuffleEnabled = needToUpdateShuffleList = !NowPlayingInformation.ShuffleEnabled;
            SolidColorBrush accentBrush = (SolidColorBrush)App.Current.Resources["PhoneAccentBrush"];
            ((SymbolIcon)sender).Foreground =
                NowPlayingInformation.ShuffleEnabled ? accentBrush : new SolidColorBrush(Colors.White);

            if (needToUpdateShuffleList)//This changes the shuffle list
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
