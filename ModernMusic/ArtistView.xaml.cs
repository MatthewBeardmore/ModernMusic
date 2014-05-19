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
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;

// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

namespace ModernMusic
{
    /// <summary>
    /// A page that displays an overview of a single group, including a preview of the items
    /// within the group.
    /// </summary
    public sealed partial class ArtistView : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private Artist _currentArtist = null;

        public ArtistView()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
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
            if (commandBar.Visibility != Windows.UI.Xaml.Visibility.Visible)
                commandBar.Visibility = Windows.UI.Xaml.Visibility.Visible;

            _currentArtist = (Artist)e.NavigationParameter;
            this.DefaultViewModel["Artist"] = _currentArtist;
            this.DefaultViewModel["Albums"] = MusicLibrary.Instance.GetAlbums(_currentArtist);
            this.DefaultViewModel["Songs"] = MusicLibrary.Instance.GetSongs(_currentArtist);

            Task.Run(new Action(() =>
            {
                Task t = MusicLibrary.Instance.DownloadAlbumArt(_currentArtist);
                t.Wait();
                if(_currentArtist.ImagePath == null)
                    return;

                Uri source = new Uri(_currentArtist.ImagePath);
                if(!source.IsFile)
                {
                    var a = Dispatcher.RunIdleAsync((o) =>
                    {
                        rect.Opacity = 0.5;
                        rect.Fill = new ImageBrush
                        {
                            Stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill,
                            ImageSource = new BitmapImage { UriSource = source }
                        };
                    });
                }
            }));
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

        private void SongItem_Tapped(Song song)
        {
            SwitchToNowPlayingView(song);
        }

        private void AlbumArt_Tapped(Album album)
        {
            SwitchToNowPlayingView(album);
        }

        private void SwitchToNowPlayingView(object kvp)
        {
            commandBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if (!Frame.Navigate(typeof(NowPlaying), kvp))
            {
                var resourceLoader = ResourceLoader.GetForCurrentView("Resources");
                throw new Exception(resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        private void AlbumItem_Tapped(Album album)
        {
            if (!Frame.Navigate(typeof(AlbumView), album))
            {
                var resourceLoader = ResourceLoader.GetForCurrentView("Resources");
                throw new Exception(resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        private void addToNowPlaying_Click(object sender, RoutedEventArgs e)
        {
            NowPlayingManager.AddToNowPlaying(_currentArtist);
            if (Settings.Instance.AddToNowPlayingSwitchesView)
                SwitchToNowPlayingView(null);
        }

        private async void pin_Click(object sender, RoutedEventArgs e)
        {
            await _currentArtist.PinToStart();
        }

        private async void pinAlbum_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            if (item != null)
            {
                Album album = item.DataContext as Album;
                if (album != null)
                {
                    await album.PinToStart();
                }
            }
        }

        private void control_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);

            flyoutBase.ShowAt(senderElement);
        }

        private void addSongToNowPlaying_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            if (item != null)
            {
                Song song = item.DataContext as Song;
                Album album = item.DataContext as Album;

                if (song != null)
                {
                    NowPlayingManager.AddToNowPlaying(song);
                }
                else if (album != null)
                {
                    NowPlayingManager.AddToNowPlaying(album);
                }
                if (Settings.Instance.AddToNowPlayingSwitchesView)
                    SwitchToNowPlayingView(null);
            }
        }

        private async void deleteSong_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            if (item != null)
            {
                Song song = item.DataContext as Song;

                if (song != null)
                {
                    DeletionResult result = await song.Delete();
                    if (result == DeletionResult.Artist)
                    {
                        if (!Frame.Navigate(typeof(CollectionsHub), null))
                        {
                            var resourceLoader = ResourceLoader.GetForCurrentView("Resources");
                            throw new Exception(resourceLoader.GetString("NavigationFailedExceptionMessage"));
                        }
                    }
                }
            }
        }
    }
}