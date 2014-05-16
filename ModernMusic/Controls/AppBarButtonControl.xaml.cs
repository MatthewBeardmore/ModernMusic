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
    public sealed partial class AppBarButtonControl : UserControl
    {
        public event Action<HoldingRoutedEventArgs> OnButtonHolding;
        public event Action OnButtonPressed;

        public Symbol Symbol
        {
            get
            {
                return symbolIcon.Symbol;
            }
            set
            {
                symbolIcon.Symbol = value;
            }
        }

        public AppBarButtonControl()
        {
            this.InitializeComponent();
        }

        private void icon_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            symbolIcon.Foreground = new SolidColorBrush(Colors.Black);
            border.BorderThickness = new Thickness(0);
            border.Background = new SolidColorBrush(Colors.White);
        }

        private void icon_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            symbolIcon.Foreground = new SolidColorBrush(Colors.White);
            border.BorderThickness = new Thickness(3);
            border.Background = new SolidColorBrush(Colors.Transparent);
        }

        private void nextButton_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (OnButtonHolding != null)
                OnButtonHolding(e);
        }

        private void nextButton_Click(object sender, TappedRoutedEventArgs e)
        {
            if (OnButtonPressed != null)
                OnButtonPressed();
        }
    }
}
