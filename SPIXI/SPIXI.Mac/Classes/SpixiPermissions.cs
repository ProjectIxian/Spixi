﻿using SPIXI.Mac;
using SPIXI.Interfaces;
using Xamarin.Forms;
using AVFoundation;

[assembly: Dependency(typeof(SpixiPermissions))]
namespace SPIXI.Mac
{
    class SpixiPermissions : ISpixiPermissions
    {
        public void requestAudioRecordingPermissions()
        {
            /*AVAudioSession av_session = AVAudioSession.SharedInstance();
            if (av_session != null)
            {
                av_session.RequestRecordPermission(delegate (bool granted)
                {

                });
            }*/
        }
    }
}