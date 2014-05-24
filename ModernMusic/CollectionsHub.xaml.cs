using ModernMusic.Library;
using ModernMusic.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace ModernMusic
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CollectionsHub : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public CollectionsHub()
        {
            this.InitializeComponent();
            NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;

            this.navigationHelper = new NavigationHelper(this) { OnBackClearState = true };
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
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
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if (e.NavigationParameter == null)
                return;

            this.DefaultViewModel["MusicLibrary"] = MusicLibrary.Instance;

            if(e.NavigationParameter.ToString() == "Artists")
            {
                pivot.SelectedIndex = 0;
            }
            else if (e.NavigationParameter.ToString() == "Albums")
            {
                pivot.SelectedIndex = 1;
            }
            else if (e.NavigationParameter.ToString() == "Songs")
            {
                pivot.SelectedIndex = 2;
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
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
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void ArtistPlay_Tapped(Artist artist)
        {
            this.Frame.Navigate(typeof(NowPlaying), artist);
        }

        private void ArtistItem_Tapped(Artist artist)
        {
            this.Frame.Navigate(typeof(ArtistView), artist);
        }

        private void AlbumArt_Tapped(Album album)
        {
            this.Frame.Navigate(typeof(NowPlaying), album);
        }

        private void AlbumItem_Tapped(Album album)
        {
            this.Frame.Navigate(typeof(AlbumView), album);
        }

        private void SongItem_Tapped(Song song)
        {
            Playlist playlist = new Playlist();
            int idx = 0;
            foreach(GroupInfoList<Song> songs in MusicLibrary.Instance.SongGroupDictionary)
            {
                foreach (Song s in songs)
                {
                    if (s == song)
                        idx = playlist.Songs.Count;
                    playlist.Songs.Add(s);
                }
            }
            this.Frame.Navigate(typeof(NowPlaying), new KeyValuePair<Playlist, int>(playlist, idx));
        }

        private void nowPlaying_click(object sender, RoutedEventArgs e)
        {
            if (!Frame.Navigate(typeof(NowPlaying), null))
            {
                var resourceLoader = ResourceLoader.GetForCurrentView("Resources");
                throw new Exception(resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        private void addToNowPlaying_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            if (item != null)
            {
                Song song = item.DataContext as Song;
                Album album = item.DataContext as Album;
                Artist artist = item.DataContext as Artist;

                if (song != null)
                {
                    NowPlayingManager.AddToNowPlaying(song);
                }
                else if (album != null)
                {
                    NowPlayingManager.AddToNowPlaying(album);
                }
                else if (artist != null)
                {
                    NowPlayingManager.AddToNowPlaying(artist);
                }
                if (Settings.Instance.AddToNowPlayingSwitchesView)
                    this.Frame.Navigate(typeof(NowPlaying), null);
            }
        }

        private async void pinItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            if (item != null)
            {
                Album album = item.DataContext as Album;
                Artist artist = item.DataContext as Artist;
                if (album != null)
                {
                    await album.PinToStart();
                }
                else if (artist != null)
                {
                    await artist.PinToStart();
                }
            }
        }

        private void control_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);

            flyoutBase.ShowAt(senderElement);
        }

        private async void deleteSong_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            if (item != null)
            {
                Song song = item.DataContext as Song;

                if (song != null)
                {
                    await song.Delete();
                }
            }
        }

        private void Border_Tapped(object sender, TappedRoutedEventArgs e)
        {
            pivot.Opacity = 0.5;
            Border border = (Border)sender;
            if (border.DataContext is GroupInfoList<Artist>)
            {
                Popup.DataContext = MusicLibrary.Instance.ArtistsCollection.View;
                Popup.Tag = "Artist";
                Popup.IsOpen = true;
            }
            else if (border.DataContext is GroupInfoList<Album>)
            {
                Popup.DataContext = MusicLibrary.Instance.AlbumsCollection.View;
                Popup.Tag = "Album";
                Popup.IsOpen = true;
            }
            else if (border.DataContext is GroupInfoList<Song>)
            {
                Popup.DataContext = MusicLibrary.Instance.SongsCollection.View;
                Popup.Tag = "Song";
                Popup.IsOpen = true;
            }
            if (Popup.IsOpen)
                navigationHelper.BlockBackStateTemporarily = new Action(() => { Popup.IsOpen = false; pivot.Opacity = 1.0; });
        }

        private void Header_Tapped(object sender, TappedRoutedEventArgs e)
        {
            pivot.Opacity = 1.0;

            string header = ((TextBlock)((Border)sender).Child).Text;
            Popup.IsOpen = false;
            navigationHelper.BlockBackStateTemporarily = null;

            if ((string)Popup.Tag == "Artist")
            {
                GroupInfoList<Artist> group = MusicLibrary.GetItemGroup(MusicLibrary.Instance.ArtistGroupDictionary, header);

                lvArtists.ScrollIntoView(group[0], ScrollIntoViewAlignment.Leading);
            }
            else if ((string)Popup.Tag == "Album")
            {
                GroupInfoList<Album> group = MusicLibrary.GetItemGroup(MusicLibrary.Instance.AlbumGroupDictionary, header);

                lvAlbums.ScrollIntoView(group[0], ScrollIntoViewAlignment.Leading);
            }
            else if ((string)Popup.Tag == "Song")
            {
                GroupInfoList<Song> group = MusicLibrary.GetItemGroup(MusicLibrary.Instance.SongGroupDictionary, header);

                lvSongs.ScrollIntoView(group[0], ScrollIntoViewAlignment.Leading);
            }
        }
    }
}
