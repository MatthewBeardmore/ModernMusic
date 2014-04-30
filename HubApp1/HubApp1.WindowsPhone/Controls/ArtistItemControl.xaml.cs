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

namespace HubApp1.Controls
{
    public sealed partial class ArtistItemControl : UserControl
    {
        public event TappedEventHandler OnPlay_Tapped;
        public event TappedEventHandler OnItem_Tapped;


        public ArtistItemControl()
        {
            this.InitializeComponent();
        }

        private void ArtistPlay_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (OnPlay_Tapped != null)
                OnPlay_Tapped(sender, e);
        }

        private void ArtistItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (OnItem_Tapped != null)
                OnItem_Tapped(sender, e);
        }
    }
}
