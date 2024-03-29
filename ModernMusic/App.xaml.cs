﻿using ModernMusic.Helpers;
using ModernMusic.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Pivot Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641

namespace ModernMusic
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private TransitionCollection transitions;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active.
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page.
                rootFrame = new Frame();

                // Associate the frame with a SuspensionManager key.
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

                // TODO: Change this value to a cache size that is appropriate for your application.
                rootFrame.CacheSize = 1;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Restore the saved session state only when appropriate.
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        // Something went wrong restoring state.
                        // Assume there is no state and continue.
                    }
                }

                // Place the frame in the current Window.
                Window.Current.Content = rootFrame;
            }

            if (Settings.Instance.AlwaysScanAtStartup)
            {
                var b = MusicLibrary.Instance.LoadLibraryFromDisk();
            }

            MusicLibrary.Dispatcher = Window.Current.Dispatcher;

            if (rootFrame.Content == null)
            {
                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;

                await GoToFirstFrame(e, rootFrame);
            }
            else
            {
                if (!CommandArgumentsExist(e) || !(await ParseLaunchArgument(e)))
                {
                    if (!(rootFrame.Content is LoadingScreen) && !(rootFrame.Content is HubPage))
                    {
                        Window.Current.Content = rootFrame = null;
                        await GoToFirstFrame(e, rootFrame);
                        rootFrame = (Frame)Window.Current.Content;
                    }
                }
            }

            //Just to make sure that the cache is loading
            var a = MusicLibrary.Instance.LoadCache(Window.Current.Dispatcher);

            if (rootFrame.Content is ILaunchable)
                ((ILaunchable)rootFrame.Content).OnLaunched(e);

            // Ensure the current window is active.
            Window.Current.Activate();
        }

        private async Task GoToFirstFrame(LaunchActivatedEventArgs e, Frame rootFrame)
        {
            StorageFile file = await ModernMusic.Library.MusicLibrary.Instance.HasCache();
            if (file != null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter.
                if (!CommandArgumentsExist(e) || !(await ParseLaunchArgument(e)))
                {
                    var a = MusicLibrary.Instance.LoadCache(Window.Current.Dispatcher, file);
                    if(rootFrame == null)
                    {
                        rootFrame = new Frame();
                        Window.Current.Content = rootFrame;
                    }
                    if (!rootFrame.Navigate(typeof(HubPage), e.Arguments))
                    {
                        throw new Exception("Failed to create initial page");
                    }
                }
            }
            else
            {
                if (rootFrame == null)
                {
                    rootFrame = new Frame();
                    Window.Current.Content = rootFrame;
                }
                if (!rootFrame.Navigate(typeof(LoadingScreen), e.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
        }

        private bool CommandArgumentsExist(LaunchActivatedEventArgs e)
        {
            return !string.IsNullOrEmpty(e.Arguments) && e.Arguments != "none";
        }

        private async Task<bool> ParseLaunchArgument(LaunchActivatedEventArgs e)
        {
            string[] param = e.Arguments.Split(':');

            string type = param[0];
            string guid = param[1];

            object kvp = null;

            Task t = MusicLibrary.Instance.LoadCache(Window.Current.Dispatcher);
            if (t != null)
                await t;
            //Load playlists as well
            PlaylistManager manager = PlaylistManager.Instance;

            if (type == "Artist")
            {
                Artist artist = MusicLibrary.Instance.GetArtist(new Guid(guid));

                kvp = artist;
            }
            else if (type == "Album")
            {
                Album album = MusicLibrary.Instance.GetAlbum(new Guid(guid));

                kvp = album;
            }
            else if (type == "Playlist")
            {
                Playlist playlist = PlaylistManager.Instance.GetPlaylist(new Guid(guid));

                if (playlist == null)
                    return false;

                kvp = new KeyValuePair<Playlist, int>(playlist, 0);
            }

            if (kvp == null)
                return false;

            if (e.PreviousExecutionState == ApplicationExecutionState.Running)
            {
                Window.Current.Content = null;
            }

            Frame rootFrame = new Frame();
            Window.Current.Content = rootFrame;
            if (!rootFrame.Navigate(typeof(NowPlaying), kvp))
                return false;

            return true;
        }

        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }
    }
}
