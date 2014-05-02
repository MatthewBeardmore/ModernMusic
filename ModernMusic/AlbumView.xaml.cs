﻿using ModernMusic.Library;
using ModernMusic.Library.Helpers;
using System;
using System.Collections.Generic;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

namespace ModernMusic
{
    /// <summary>
    /// A page that displays an overview of a single group, including a preview of the items
    /// within the group.
    /// </summary
    public sealed partial class AlbumView : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private Album _currentAlbum = null;

        public AlbumView()
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
            _currentAlbum = (Album)e.NavigationParameter;

            this.DefaultViewModel["Artist"] = MusicLibrary.Instance.GetArtist(_currentAlbum);
            this.DefaultViewModel["Album"] = _currentAlbum;
            this.DefaultViewModel["Songs"] = MusicLibrary.Instance.GetSongs(_currentAlbum);
        }

        private void songView_ItemClick(object sender, ItemClickEventArgs e)
        {
            KeyValuePair<Album, int> kvp = new KeyValuePair<Album, int>(_currentAlbum,
                MusicLibrary.Instance.GetSongs(_currentAlbum).IndexOf(((Song)e.ClickedItem)));
            if (!Frame.Navigate(typeof(NowPlaying), kvp))
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

        private void addToNowPlaying_Click(object sender, RoutedEventArgs e)
        {
            NowPlayingManager.AddToNowPlaying(_currentAlbum);
        }

        private void pin_Click(object sender, RoutedEventArgs e)
        {
            const string appbarTileId = "SecondaryTile.ModernMusic.Album";

            Uri square150x150Logo = new Uri("ms-appx:///Assets/headphones_120.scale-240.png");

            SecondaryTileManager.PinSecondaryTile(appbarTileId, _currentAlbum.AlbumName, square150x150Logo, _currentAlbum.AlbumName);
        }
    }
}