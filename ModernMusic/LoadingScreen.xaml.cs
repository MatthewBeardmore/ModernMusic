using ModernMusic.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
    public sealed partial class LoadingScreen : Page
    {
        private readonly NavigationHelper navigationHelper;
        private bool _skipped = false;
        private static Task _loadingTask = null;

        public LoadingScreen()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            //this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            //this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            if (_loadingTask == null)
            {
                _loadingTask = MusicLibrary.Instance.LoadLibraryFromDisk();
            }
            _loadingTask.ContinueWith(LibraryLoaded);

            MusicLibrary.Instance.OnLoadLibraryFromDiskProgress += Instance_OnLoadLibraryFromDiskProgress;
        }

        void Instance_OnLoadLibraryFromDiskProgress(string artistName)
        {
            currentlyLoadingText.Text = "Currently loading: " + artistName;
        }

        private void LibraryLoaded(Task t)
        {
            if (_skipped)
                return;

            var a = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (!Frame.Navigate(typeof(HubPage), null))
                {
                    throw new Exception("Failed to create initial page");
                }
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LibraryLoaded(null);
            _skipped = true;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_loadingTask.IsCompleted)
                Frame.GoBack();
        }
    }
}
