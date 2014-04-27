using HubApp1.Common;
using HubApp1.Data;
using ModernMusic.MusicLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace HubApp1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NowPlaying : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private Song _currentSong;

        public NowPlaying()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            //this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            _currentSong = (Song)e.NavigationParameter;
            this.DefaultViewModel["Song"] = _currentSong;

            var sourceFile = await StorageFile.GetFileFromPathAsync(_currentSong.FilePath);
            if(sourceFile != null)
            {
                var stream = await sourceFile.OpenAsync(Windows.Storage.FileAccessMode.Read);

                // mediaControl is a MediaElement defined in XAML
                if (null != stream)
                {
                    mediaControl.SetSource(stream, sourceFile.ContentType);

                    mediaControl.Play();
                }
            }
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

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            if(mediaControl.CurrentState == MediaElementState.Paused)
            {
                playButton.Icon = new SymbolIcon(Symbol.Pause);
                mediaControl.Play();
            }
            else
            {
                playButton.Icon = new SymbolIcon(Symbol.Play);
                mediaControl.Pause();
            }
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        { 
        }

        private void previousButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
