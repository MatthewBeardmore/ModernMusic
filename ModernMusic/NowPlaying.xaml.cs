using ModernMusic.Library;
using ModernMusic.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace ModernMusic
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NowPlaying : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();

        public NowPlaying()
        {
            this.InitializeComponent();

            nowPlayingControl.SetupPage(this);

            this.navigationHelper = new NavigationHelper(this) { OnBackClearState = true };
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            //this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if (e.NavigationParameter == null)
            {
                nowPlayingControl.EnsurePlayback();
                nowPlayingControl.ResumeLayout();
                return;
            }

            Playlist playlist = new Playlist();
            int currentSongIndex = 0;

            if (e.NavigationParameter is Artist)
            {
                Artist artist = (Artist)e.NavigationParameter;
                playlist.Songs = MusicLibrary.Instance.GetSongs(artist);
            }
            else if (e.NavigationParameter is Album)
            {
                Album album = (Album)e.NavigationParameter;
                playlist.Songs = MusicLibrary.Instance.GetSongs(album);
            }
            else if (e.NavigationParameter is Song)
            {
                Song song = (Song)e.NavigationParameter;

                playlist.Songs.Add(song);
            }
            else if (e.NavigationParameter is KeyValuePair<Album, int>)
            {
                KeyValuePair<Album, int> kvp = ((KeyValuePair<Album, int>)e.NavigationParameter);
                playlist.Songs = MusicLibrary.Instance.GetSongs(kvp.Key);
                currentSongIndex = kvp.Value;
            }
            else if (e.NavigationParameter is KeyValuePair<Playlist, int>)
            {
                KeyValuePair<Playlist, int> kvp = ((KeyValuePair<Playlist, int>)e.NavigationParameter);
                playlist = kvp.Key;
                currentSongIndex = kvp.Value;
            }

            NowPlayingManager.BeginPlaylist(Dispatcher, playlist, currentSongIndex);
            nowPlayingControl.ResumeLayout();
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
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
    }
}
