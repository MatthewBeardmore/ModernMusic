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

        private Song _oldDataContext;

        public bool AllowSelection { get; set; }

        public SolidColorBrush ForegroundStyle
        {
            get
            {
                if (DataContext != null && ((Song)DataContext).Selected)
                    return (SolidColorBrush)App.Current.Resources["PhoneAccentBrush"];
                return new SolidColorBrush(Colors.White);
            }
        }

        public SongItemControl()
        {
            this.InitializeComponent();

            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                stackPanel.Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        private void userControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (_oldDataContext != null)
                _oldDataContext.PropertyChanged -= SongItemControl_PropertyChanged;

            _oldDataContext = DataContext as Song;

            if (_oldDataContext != null)
            {
                songText.Foreground = ForegroundStyle;
                artistText.Foreground = ForegroundStyle;
                _oldDataContext.PropertyChanged += SongItemControl_PropertyChanged;
            }
        }

        private void userControl_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        void SongItemControl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (DataContext == null)
                return;

            songText.Foreground = ForegroundStyle;
            artistText.Foreground = ForegroundStyle;
        }

        private void StackPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (OnItemTapped != null)
            {
                e.Handled = true;
                OnItemTapped((Song)DataContext);
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
    }
}
