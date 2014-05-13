﻿using ModernMusic.Library;
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
            _currentArtist = (Artist)e.NavigationParameter;
            this.DefaultViewModel["Artist"] = _currentArtist;
            this.DefaultViewModel["Albums"] = MusicLibrary.Instance.GetAlbums(_currentArtist);
            this.DefaultViewModel["Songs"] = MusicLibrary.Instance.GetSongs(_currentArtist);

            /*Task.Run(new Action(() =>
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
                        this.Background = new ImageBrush
                        {
                            Stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill,
                            ImageSource = new BitmapImage { UriSource = source }
                        };
                    });
                }
            }));*/
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
            if (!Frame.Navigate(typeof(NowPlaying), song))
            {
                var resourceLoader = ResourceLoader.GetForCurrentView("Resources");
                throw new Exception(resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        private void AlbumArt_Tapped(Album album)
        {
            if (!Frame.Navigate(typeof(NowPlaying), album))
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
        }

        private async void pin_Click(object sender, RoutedEventArgs e)
        {
            string activationArguments = "Artist:" + _currentArtist.ID.ToString();
            string appbarTileId = "ModernMusic." + activationArguments.Replace(':', '.');

            await MusicLibrary.Instance.DownloadAlbumArt(_currentArtist);
            Uri square150x150Logo = new Uri("ms-appx:///Assets/Square150x150Logo.scale-240.png");
            if(_currentArtist.ImagePath != null)
            {
                Uri source = new Uri(_currentArtist.ImagePath);
                if(!source.IsFile)
                    square150x150Logo = await Utilities.ResizeImage(new Uri(_currentArtist.ImagePath), 150);
            }

            SecondaryTileManager.PinSecondaryTile(appbarTileId, "Modern Music", square150x150Logo, activationArguments);
        }
    }
}