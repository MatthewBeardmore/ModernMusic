﻿using ModernMusic.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
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
    public sealed partial class TimeSliderControl : UserControl
    {
        private bool _userScanning = false;

        public TimeSliderControl()
        {
            this.InitializeComponent();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.5);
            timer.Start();
            timer.Tick += timer_Tick;

            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                grid.Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        void timer_Tick(object sender, object e)
        {
            if (_userScanning)
                return;

            try
            {
                if (NowPlayingManager.IsAudioOpen)
                {
                    TimeSpan position = BackgroundMediaPlayer.Current.Position;
                    TimeSpan duration = BackgroundMediaPlayer.Current.NaturalDuration;
                    TimeSpan durationLeft = duration - position;

                    positionSlider.Value = position.TotalSeconds;
                    if (duration.TotalSeconds < 1.0f)
                        positionSlider.Maximum = 100f;
                    else if (positionSlider.Maximum != duration.TotalSeconds)
                        positionSlider.Maximum = duration.TotalSeconds;

                    string currentTimeText = position.Minutes.ToString("D2") + ":" + position.Seconds.ToString("D2");
                    if (position.Hours > 0)
                        currentTimeText = position.Hours.ToString("D2") + ":" + currentTimeText;
                    currentTime.Text = currentTimeText;

                    string timeLeftText = durationLeft.Minutes.ToString("D2") + ":" + durationLeft.Seconds.ToString("D2");
                    if (durationLeft.Hours > 0)
                        timeLeftText = durationLeft.Hours.ToString("D2") + ":" + timeLeftText;
                    timeLeft.Text = timeLeftText + "-";
                }
                else
                {
                    positionSlider.Value = 0;
                    positionSlider.Maximum = 100;

                    currentTime.Text = "0:00";
                    timeLeft.Text = "0:00-";
                }
            }
            catch
            {
                positionSlider.Value = 0;
                positionSlider.Maximum = 100;

                currentTime.Text = "0:00";
                timeLeft.Text = "0:00-";
            }
        }

        private void positionSlider_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _userScanning = true;
        }

        private void positionSlider_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _userScanning = false;
            NowPlayingManager.Seek(positionSlider.Value);
        }
    }
}
