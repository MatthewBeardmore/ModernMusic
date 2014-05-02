using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;

namespace ModernMusic
{
    public interface ILaunchable
    {
        void OnLaunched(LaunchActivatedEventArgs e);
    }
}
