using ModernMusic.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace ModernMusic
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SearchView : Page
    {
        private int _currentSearchIndex = 0;
        private readonly NavigationHelper navigationHelper;

        public SearchView()
        {
            this.InitializeComponent();
            NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
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

        private void searchText_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
            {
                _currentSearchIndex++;

                listView.Items.Clear();

                searchText.Text = searchText.Text.TrimEnd(Environment.NewLine.ToCharArray());

                e.Handled = true;
                bool focused = listView.Focus(Windows.UI.Xaml.FocusState.Pointer);
                MusicLibrary.Instance.Search(searchText.Text, _currentSearchIndex, OnArtist, OnAlbum, OnSong);
            }
        }

        private void OnArtist(int idx, Artist artist)
        {
            if (idx != _currentSearchIndex)
                return;

            var a = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => listView.Items.Add(artist));
        }

        private void OnAlbum(int idx, Album album)
        {
            if (idx != _currentSearchIndex)
                return;

            var a = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => listView.Items.Add(album));
        }

        private void OnSong(int idx, Song song)
        {
            if (idx != _currentSearchIndex)
                return;

            var a = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => listView.Items.Add(song));
        }

        private void SearchItemControl_OnItemTapped(object obj)
        {
            if (obj is Artist)
            {
                if (!Frame.Navigate(typeof(ArtistView), obj))
                {
                    var resourceLoader = ResourceLoader.GetForCurrentView("Resources");
                    throw new Exception(resourceLoader.GetString("NavigationFailedExceptionMessage"));
                }
            }
            else if (obj is Album)
            {
                if (!Frame.Navigate(typeof(AlbumView), obj))
                {
                    var resourceLoader = ResourceLoader.GetForCurrentView("Resources");
                    throw new Exception(resourceLoader.GetString("NavigationFailedExceptionMessage"));
                }
            }
            else if (obj is Song)
            {
                if (!Frame.Navigate(typeof(NowPlaying), obj))
                {
                    var resourceLoader = ResourceLoader.GetForCurrentView("Resources");
                    throw new Exception(resourceLoader.GetString("NavigationFailedExceptionMessage"));
                }
            }
        }

        private void SearchItemControl_OnPlayTapped(object obj)
        {
            if (!Frame.Navigate(typeof(NowPlaying), obj))
            {
                var resourceLoader = ResourceLoader.GetForCurrentView("Resources");
                throw new Exception(resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }
    }
}
