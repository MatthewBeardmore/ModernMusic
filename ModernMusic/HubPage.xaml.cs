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
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Animation;
using Windows.Storage.Streams;

// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

namespace ModernMusic
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class HubPage : Page, ILaunchable
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");
        public const string appbarTileId = "SecondaryTile.ModernMusic.Hub";

        public HubPage()
        {
            this.InitializeComponent();

            // Hub is only supported in Portrait orientation
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            this.navigationHelper = new NavigationHelper(this);
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
        /// <see cref="Frame.Navigate(Type, object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            commandBar.Visibility = Windows.UI.Xaml.Visibility.Visible;

            if(e.NavigationParameter is Playlist)
            {
                pivot.SelectedIndex = 1;
            }
            DefaultViewModel["MusicLibrary"] = MusicLibrary.Instance;
            DefaultViewModel["PlaylistManager"] = PlaylistManager.Instance;

            var a = Task.Run(new Action(() =>
            {

                try
                {
                    Song currentSong = NowPlayingInformation.CurrentSong;
                    Album album = MusicLibrary.Instance.GetAlbum(currentSong);
                    if (currentSong != null && album != null && !string.IsNullOrEmpty(album.CachedImagePath))
                    {
                        Uri uri;
                        if (Uri.TryCreate(album.CachedImagePath, UriKind.RelativeOrAbsolute, out uri))
                        {
                            IRandomAccessStream ras = AsyncInline.Run(new Func<Task<IRandomAccessStream>>(() =>
                                Utilities.ResizeImageFile(uri, 180)));
                            var b = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                            {
                                nowPlayingArt.SetSource(ras);
                                nowPlayingRow.Height = new GridLength(180);
                            });
                        }
                    }
                    else
                    {
                        var b = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => nowPlayingRow.Height = new GridLength(0));
                    }
                }
                catch { }
            }));
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

        private void playlistItem_Tapped(Playlist playlist)
        {
            if (!Frame.Navigate(typeof(PlaylistView), playlist))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
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

        private void TextBlock_Artists_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!Frame.Navigate(typeof(CollectionsHub), "Artists"))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        private void TextBlock_Albums_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!Frame.Navigate(typeof(CollectionsHub), "Albums"))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        private void TextBlock_Songs_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!Frame.Navigate(typeof(CollectionsHub), "Songs"))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (!Frame.Navigate(typeof(SettingsPage), null))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        private async void pinToStart_Click(object sender, RoutedEventArgs e)
        {
            if (SecondaryTileManager.TileExists(appbarTileId))
            {
                ToggleAppBarButton(await SecondaryTileManager.UnpinSecondaryTile(appbarTileId));
            }
            else
            {
                // Prepare package images for all four tile sizes in our tile to be pinned as well as for the square30x30 logo used in the Apps view.  
                Uri square150x150Logo = new Uri("ms-appx:///Assets/Square150x150Logo.scale-240.png");

                SecondaryTileManager.PinSecondaryTile(appbarTileId, "Modern Music", square150x150Logo, showName:true);
            }
        }

        private void ToggleAppBarButton(bool showPinButton)
        {
            if (pinToStart != null)
            {
                pinToStart.Label = showPinButton ? "pin to start" : "unpin";
                pinToStart.Icon = showPinButton ? new SymbolIcon(Symbol.Pin) : new SymbolIcon(Symbol.UnPin);
            }
        }

        public void OnLaunched(Windows.ApplicationModel.Activation.LaunchActivatedEventArgs e)
        {
            var a = Dispatcher.RunIdleAsync((o) =>
            {
                ToggleAppBarButton(!SecondaryTileManager.TileExists(appbarTileId));
            });
        }

        private async void removePlaylist_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            if (item != null)
            {
                Playlist playlist = item.DataContext as Playlist;

                if (playlist != null)
                {
                    PlaylistManager.Instance.RemovePlaylist(playlist);
                    await PlaylistManager.Instance.Serialize();
                }
            }
        }

        private void nowPlayingArt_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!Frame.Navigate(typeof(NowPlaying), null))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }
    }
}