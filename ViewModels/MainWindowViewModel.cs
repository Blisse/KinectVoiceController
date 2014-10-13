using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using KinectMusicControl.Services;

namespace KinectMusicControl.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private readonly IKinectSpeechEngineService _kinectSpeechEngineService;
        private readonly IMediaService _mediaService;

        private const int SpeechDisplayTimeInSeconds = 4;

        private String _recognizedSpeech = String.Empty;
        public String RecognizedSpeech
        {
            get
            {
                return _recognizedSpeech;
            }

            set
            {
                if (_recognizedSpeech == value)
                {
                    return;
                }

                _recognizedSpeech = value;
                RaisePropertyChanged();
            }
        }

        private String _rejectedSpeech = String.Empty;
        public String RejectedSpeech
        {
            get
            {
                return _rejectedSpeech;
            }

            set
            {
                if (_rejectedSpeech == value)
                {
                    return;
                }

                _rejectedSpeech = value;
                RaisePropertyChanged();
            }
        }

        private String _currentlyPlayingSong = String.Empty;
        public String CurrentlyPlayingSong
        {
            get
            {
                return _currentlyPlayingSong;
            }

            set
            {
                if (_currentlyPlayingSong == value)
                {
                    return;
                }

                _currentlyPlayingSong = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand StartListeningCommand { get; set; }
        public RelayCommand StopListeningCommand { get; set; }

        public MainWindowViewModel(IKinectSpeechEngineService kinectSpeechEngineService, IMediaService mediaService)
        {
            _kinectSpeechEngineService = kinectSpeechEngineService;
            _mediaService = mediaService;

            StartListeningCommand = new RelayCommand(ExecuteStart);
            StopListeningCommand = new RelayCommand(ExecuteStop);

            _kinectSpeechEngineService.SpeechRecognizedHandler += KinectSpeechEngineServiceOnSpeechRecognized;
            _kinectSpeechEngineService.SpeechRejectedHandler += KinectSpeechEngineServiceOnSpeechRejected;
        }

        private async void KinectSpeechEngineServiceOnSpeechRejected(object sender, string speech)
        {
            RejectedSpeech = speech;

            await Task.Delay(SpeechDisplayTimeInSeconds * 1000);

            RejectedSpeech = String.Empty;
        }

        private async void KinectSpeechEngineServiceOnSpeechRecognized(object sender, string speech)
        {
            RecognizedSpeech = speech;

            switch (RecognizedSpeech)
            {
                case "NEXT": _mediaService.PlayNextSong(); break;
                case "PREVIOUS": _mediaService.PlayPreviousSong(); break;
                case "STOP": _mediaService.Stop(); break;
                case "PLAY": _mediaService.PlayOrPause(); break;
                case "PAUSE": _mediaService.PlayOrPause(); break;
                case "MUTE": _mediaService.VolumeMute(); break;
                case "VOLUME_UP": _mediaService.VolumeUp(); break;
                case "VOLUME_DOWN": _mediaService.VolumeDown(); break;
            }

            await Task.Delay(250);

            CurrentlyPlayingSong = _mediaService.GetCurrentlyPlayingSong();

            await Task.Delay(SpeechDisplayTimeInSeconds * 1000);

            RecognizedSpeech = String.Empty;
        }

        private async void GetCurrentlyPlayingSongRequest()
        {
            while (true)
            {
                CurrentlyPlayingSong = _mediaService.GetCurrentlyPlayingSong();
                await Task.Delay(10*1000);
            }
        }

        private void ExecuteStart()
        {
            _kinectSpeechEngineService.InitializeKinectSensor();
            GetCurrentlyPlayingSongRequest();
        }

        private void ExecuteStop()
        {
            _kinectSpeechEngineService.DisposeKinectSensor();
        }
    }
}
