using ModernMusic.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace ModernMusic.Controls
{
    public sealed partial class SearchItemControl : UserControl
    {
        public event Action<object> OnItemTapped;
        public event Action<object> OnPlayTapped;

        public SearchItemControl()
        {
            this.InitializeComponent();

            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                stackPanel.Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        private void StackPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (OnItemTapped != null)
            {
                e.Handled = true;
                OnItemTapped(DataContext);
            }
        }

        private void border_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (OnPlayTapped != null)
            {
                e.Handled = true;
                OnPlayTapped(DataContext);
            }
        }

        private void icon_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            icon.Foreground = new SolidColorBrush(Colors.Black);
            border.BorderThickness = new Thickness(0);
            border.Background = new SolidColorBrush(Colors.White);
        }

        private void icon_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            icon.Foreground = new SolidColorBrush(Colors.White);
            border.BorderThickness = new Thickness(2.5);
            border.Background = new SolidColorBrush(Colors.Transparent);
        }

        private void userControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            artistText.Visibility = Windows.UI.Xaml.Visibility.Visible;
            if (DataContext is Song)
            {
                Song song = (Song)DataContext;
                searchText.Text = song.SongTitle;
                artistText.Text = song.Artist;
                typeText.Text = "Song";
            }
            else if (DataContext is Album)
            {
                Album album = (Album)DataContext;
                searchText.Text = album.AlbumName;
                artistText.Text = album.Artist;
                typeText.Text = "Album";
            }
            else if (DataContext is Artist)
            {
                Artist artist = (Artist)DataContext;
                searchText.Text = artist.ArtistName;
                artistText.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                typeText.Text = "Artist";
            }
        }
    }
}
