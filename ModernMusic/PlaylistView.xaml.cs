using ModernMusic.Controls;
using ModernMusic.Library;
using ModernMusic.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

namespace ModernMusic
{
    /// <summary>
    /// A page that displays an overview of a single group, including a preview of the items
    /// within the group.
    /// </summary
    public sealed partial class PlaylistView : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private Playlist _currentPlaylist = null;
        private int _currentSongIndex;
        private ObservableCollection<Song> Songs { get; set; }

        public PlaylistView()
        {
            this.InitializeComponent();

            NowPlayingManager.OnChangedTrack += nowPlayingManger_onChangedTrack;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            Songs = new ObservableCollection<Song>();
            this.DefaultViewModel["Songs"] = Songs;
        }

        void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            commandBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            KeyValuePair<Playlist, int> playlist;
            if (e.NavigationParameter is Playlist)
                playlist = new KeyValuePair<Playlist, int>((Playlist)e.NavigationParameter, 0);
            else
                playlist = (KeyValuePair<Playlist, int>)e.NavigationParameter;
            _currentPlaylist = playlist.Key;
            _currentSongIndex = playlist.Value;
            foreach(Song song in _currentPlaylist.Songs)
                Songs.Add(song);

            ResetCommandBarVisibility();
        }

        private void songView_Loaded(object sender, RoutedEventArgs e)
        {
            songView.ScrollIntoView(_currentPlaylist.Songs[Math.Max(0, _currentSongIndex - 1)], ScrollIntoViewAlignment.Leading);
        }

        private void nowPlayingManger_onChangedTrack()
        {
        }

        private void songItem_Tapped(Song song)
        {
            KeyValuePair<Playlist, int> kvp = new KeyValuePair<Playlist, int>(_currentPlaylist,
                _currentPlaylist.Songs.IndexOf(song));
            if (!Frame.Navigate(typeof(NowPlaying), kvp))
            {
                var resourceLoader = ResourceLoader.GetForCurrentView("Resources");
                throw new Exception(resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        private void savePlaylist_Click(object sender, RoutedEventArgs e)
        {
            songView.ReorderMode = ListViewReorderMode.Disabled;
            if (!Frame.Navigate(typeof(SavePlaylist), _currentPlaylist))
            {
                var resourceLoader = ResourceLoader.GetForCurrentView("Resources");
                throw new Exception(resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void SongItemControl_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);

            flyoutBase.ShowAt(senderElement);
        }

        private async void removeSong_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            if (item != null)
            {
                Song song = item.DataContext as Song;

                if (song != null)
                {
                    _currentPlaylist.Songs.Remove(song);
                    Songs.Remove(song);

                    await PlaylistManager.Instance.Serialize();
                }
            }
        }

        private async void removeSelectedSong_Click(object sender, RoutedEventArgs e)
        {
            List<object> songs = new List<object>(songView.SelectedItems);
            foreach (object song in songs)
            {
                _currentPlaylist.Songs.Remove((Song)song);
                Songs.Remove((Song)song);
            }

            await PlaylistManager.Instance.Serialize();
        }

        private void reorder_Click(object sender, RoutedEventArgs e)
        {
            if (songView.ReorderMode == ListViewReorderMode.Disabled)
                songView.ReorderMode = ListViewReorderMode.Enabled;
            else
                songView.ReorderMode = ListViewReorderMode.Disabled;
        }

        private void selectSongs_Click(object sender, RoutedEventArgs e)
        {
            songView.ReorderMode = ListViewReorderMode.Disabled;
            if (songView.SelectionMode == ListViewSelectionMode.Multiple)
            {
                navigationHelper.BlockBackStateTemporarily = null;
                songView.SelectionMode = ListViewSelectionMode.Single;
                ResetCommandBarVisibility();
            }
            else
            {
                navigationHelper.BlockBackStateTemporarily = new Action(() => selectSongs_Click(null, null));
                songView.SelectionMode = ListViewSelectionMode.Multiple;
                savePlaylist.Visibility = reorderSongs.Visibility = selectSongs.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                removeSong.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private void ResetCommandBarVisibility()
        {
            savePlaylist.Visibility = string.IsNullOrEmpty(_currentPlaylist.Name) ? Visibility.Visible : Visibility.Collapsed;
            reorderSongs.Visibility = selectSongs.Visibility = Windows.UI.Xaml.Visibility.Visible;
            removeSong.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            commandBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }
    }
}