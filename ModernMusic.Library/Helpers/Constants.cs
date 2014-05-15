/*
 * (c) Copyright Microsoft Corporation.
This source is subject to the Microsoft Public License (Ms-PL).
All other rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModernMusic.Library
{
    /// <summary>
    /// Collection of string constants used in the entire solution. This file is shared for all projects
    /// </summary>
    public class Constants
    {
        /// 
        /// Foreground -> Background
        ///  

        public const string BackgroundTaskQuery = "BackgroundTaskQuery";
        public const string StartPlaying = "StartPlaying";
        public const string ClearCache = "ClearCache";
        public const string PlayTrack = "PlayTrack";
        public const string PauseTrack = "PauseTrack";
        public const string StopPlayback = "StopPlayback";

        /// 
        /// Background -> Foreground
        ///  

        public const string ChangedTrack = "ChangedTrack";
        public const string Seek = "Seek";
        public const string BackgroundTaskIsRunning = "BackgroundTaskIsRunning";
        public const string BackgroundTaskIsStopping = "BackgroundTaskIsStopping";
    }
}
