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
    public sealed partial class ArtistItemControl : UserControl
    {
        public event Artist.ArtistTappedHandler OnPlay_Tapped;
        public event Artist.ArtistTappedHandler OnItem_Tapped;


        public ArtistItemControl()
        {
            this.InitializeComponent();

            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                stackPanel.Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        private void ArtistPlay_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (OnPlay_Tapped != null)
            {
                e.Handled = true;
                OnPlay_Tapped((Artist)DataContext);
            }
        }

        private void ArtistItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (OnItem_Tapped != null)
            {
                e.Handled = true;
                OnItem_Tapped((Artist)DataContext);
            }
        }
    }
}
