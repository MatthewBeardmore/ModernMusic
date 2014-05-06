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
    public sealed partial class SongItemControl : UserControl
    {
        public event Song.SongTappedHandler OnItemTapped;

        public bool AllowSelection { get; set; }

        public SongItemControl()
        {
            this.InitializeComponent();
        }

        public void Select()
        {
            border.BorderBrush = songText.Foreground = artistText.Foreground = icon.Foreground =
                (SolidColorBrush)App.Current.Resources["PhoneAccentBrush"];
        }

        public void Deselect()
        {
            border.BorderBrush = songText.Foreground = artistText.Foreground = icon.Foreground =
                new SolidColorBrush(Colors.White);
        }

        private void StackPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (OnItemTapped != null)
            {
                e.Handled = true;
                OnItemTapped((Song)DataContext);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext != null && AllowSelection)
            {
                ((Song)DataContext).PropertyChanged += SongItemControl_PropertyChanged;
                SongItemControl_PropertyChanged(null, null);
            }
        }

        void SongItemControl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (DataContext != null && ((Song)DataContext).Selected)
            {
                ((Song)DataContext).ClearSelection(false);
                Select();
            }
            else
                Deselect();
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
    }
}
