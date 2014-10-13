using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MediaInfoLib;
using WMPLib;

namespace KinectMusicControl.Services
{
    public interface IMediaService
    {
        void PlayNextSong();
        void PlayPreviousSong();
        void PlayOrPause();
        void Stop();
        void VolumeUp();
        void VolumeDown();
        void VolumeMute();

        String GetCurrentlyPlayingSong();
    }

    public class MediaService : IMediaService
    {
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte vkCode, byte scanCode, int flags, IntPtr extraInfo);

        [DllImport("User32.dll")]
        private static extern IntPtr FindWindow(string strClassName, string strWindowName);

        public void PlayNextSong()
        {
            keybd_event(MediaNextTrackVk, VkScanCode, 0, IntPtr.Zero);
        }

        public void PlayPreviousSong()
        {
            keybd_event(MediaPreviousTrackVk, VkScanCode, 0, IntPtr.Zero);
        }

        public void PlayOrPause()
        {
            keybd_event(MediaPlayPauseVk, VkScanCode, 0, IntPtr.Zero);
        }

        public void Stop()
        {
            keybd_event(MediaStopVk, VkScanCode, 0, IntPtr.Zero);
        }

        public void VolumeUp()
        {
            keybd_event(VolumeUpVk, VkScanCode, 0, IntPtr.Zero);
        }

        public void VolumeDown()
        {
            keybd_event(VolumeDownVk, VkScanCode, 0, IntPtr.Zero);
        }

        public void VolumeMute()
        {
            keybd_event(VolumeMuteVk, VkScanCode, 0, IntPtr.Zero);
        }

        public string GetCurrentlyPlayingSong()
        {
            const String foobarMusicPlayerProcessName = "foobar2000";
            try
            {
                var foobarProcessesList = Process.GetProcessesByName(foobarMusicPlayerProcessName);
                var foobarProcess = foobarProcessesList.ElementAtOrDefault(0);
                if (foobarProcess != null)
                {
                    var foobarMainWindowTitle = foobarProcess.MainWindowTitle;
                    return foobarMainWindowTitle;
                }
            }
            catch
            {
                Debug.WriteLine("Error finding foobar process.");
            }

            return String.Empty;
        }

        private const int VkScanCode = 0x45;
        private const int MediaNextTrackVk = 0xB0;
        private const int MediaPreviousTrackVk = 0xB1;
        private const int MediaStopVk = 0xB2;
        private const int MediaPlayPauseVk = 0xB3;
        private const int VolumeMuteVk = 0xAD;
        private const int VolumeDownVk = 0xAE;
        private const int VolumeUpVk = 0xAF;
    }
}
