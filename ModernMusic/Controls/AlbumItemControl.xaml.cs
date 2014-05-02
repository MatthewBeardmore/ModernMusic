using ModernMusic.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class AlbumItemControl : UserControl
    {
        public event Album.AlbumTappedHandler OnAlbumArtTapped;
        public event Album.AlbumTappedHandler OnItemTapped;

        public AlbumItemControl()
        {
            this.InitializeComponent();
        }
        private void AlbumItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (OnItemTapped != null)
            {
                e.Handled = true;
                OnItemTapped((Album)DataContext);
            }
        }

        private void AlbumArt_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (OnAlbumArtTapped != null)
            {
                e.Handled = true;
                OnAlbumArtTapped((Album)DataContext);
            }
        }
    }
}
