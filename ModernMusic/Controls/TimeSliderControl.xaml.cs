using ModernMusic.Library;
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
        private bool _beforeScanWasPlaying = false;

        public TimeSliderControl()
        {
            this.InitializeComponent();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.05);
            timer.Start();
            timer.Tick += timer_Tick;

            NowPlayingManager.OnSeek += () =>
            {
                var a = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => timer_Tick(null, null));
            };

            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                grid.Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        void timer_Tick(object sender, object e)
        {
            try
            {
                TimeSpan? position = NowPlayingManager.CurrentPosition;
                TimeSpan? duration;
                if (position.HasValue && (duration = NowPlayingManager.CurrentTrackDuration).HasValue)
                {
                    TimeSpan durationLeft = duration.Value - position.Value;

                    if (!_userScanning)
                    {
                        positionSlider.Value = position.Value.TotalSeconds;
                        if (duration.Value.TotalSeconds < 1.0f)
                            positionSlider.Maximum = 100f;
                        else if (positionSlider.Maximum != duration.Value.TotalSeconds)
                            positionSlider.Maximum = duration.Value.TotalSeconds;
                    }
                    else
                    {
                        position = TimeSpan.FromSeconds(positionSlider.Value);
                        durationLeft = duration.Value - position.Value;
                    }

                    string currentTimeText = position.Value.Minutes.ToString("D2") + ":" + position.Value.Seconds.ToString("D2");
                    if (position.Value.Hours > 0)
                        currentTimeText = position.Value.Hours.ToString("D2") + ":" + currentTimeText;
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
            if (!_userScanning)
            {
                _userScanning = true;
                _beforeScanWasPlaying = BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing;
                BackgroundMediaPlayer.Current.Pause();
            }
        }

        private void positionSlider_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            _userScanning = false;
            if (_beforeScanWasPlaying)
                BackgroundMediaPlayer.Current.Play();
            NowPlayingManager.Seek(positionSlider.Value);
        }
    }
}
