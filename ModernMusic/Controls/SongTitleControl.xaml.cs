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
    public sealed partial class SongTitleControl : UserControl
    {
        private Song _oldDataContext;

        public SongTitleControl()
        {
            this.InitializeComponent();
        }

        private void userControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (_oldDataContext != null)
                _oldDataContext.PropertyChanged -= SongItemControl_PropertyChanged;

            _oldDataContext = DataContext as Song;

            if (_oldDataContext != null)
            {
                if (_oldDataContext.Blank)
                {
                    textBlock.Foreground = new SolidColorBrush(Colors.Transparent);
                    textBlock.Height = textBlock.MinHeight =
                        textBlock.MaxHeight = _oldDataContext.BlankHeight;
                }
                else
                {
                    if (_oldDataContext.Selected)
                        Select();
                    else
                        Deselect();
                }
                _oldDataContext.PropertyChanged += SongItemControl_PropertyChanged;
            }
        }

        void SongItemControl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (DataContext == null)
                return;

            if (_oldDataContext.Blank)
            {
                textBlock.Foreground = new SolidColorBrush(Colors.Transparent);
                textBlock.Height = textBlock.MinHeight =
                    textBlock.MaxHeight = _oldDataContext.BlankHeight;
            }
            else
            {
                if (((Song)DataContext).Selected)
                    Select();
                else
                    Deselect();
            }
        }

        public void Select()
        {
            textBlock.Margin = new Thickness(0, -6, 0, 0);
            textBlock.Height = textBlock.MinHeight =
                textBlock.MaxHeight = 37;
            textBlock.FontSize = 27;
            textBlock.Style = (Style)this.Resources["SubheaderTextBlockStyle"];
            textBlock.Foreground = Application.Current.Resources["PhoneForegroundBrush"] as SolidColorBrush;
        }

        public void Deselect()
        {
            textBlock.Margin = new Thickness(0, 0, 0, 0);
            textBlock.Height = textBlock.MinHeight =
                textBlock.MaxHeight = 21;
            textBlock.FontSize = 16;
            textBlock.Style = (Style)this.Resources["ListViewItemSubheaderTextBlockStyle"];
            textBlock.Foreground = new SolidColorBrush((Color)Application.Current.Resources["PhoneBaseMidColor"]);
        }
    }
}
