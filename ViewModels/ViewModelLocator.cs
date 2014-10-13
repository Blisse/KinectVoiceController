using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using KinectMusicControl.Services;
using Microsoft.Practices.ServiceLocation;

namespace KinectMusicControl.ViewModels
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the Locator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            if (ViewModelBase.IsInDesignModeStatic)
            {
                // Create design time view services and models
            }
            else
            {
                // Create run time view services and models

            }

            SimpleIoc.Default.Register<IKinectSpeechEngineService, KinectSpeechEngineService>();
            SimpleIoc.Default.Register<IMediaService, MediaService>();
            SimpleIoc.Default.Register<MainWindowViewModel>();
        }

        public MainWindowViewModel MainWindow
        {
            get { return ServiceLocator.Current.GetInstance<MainWindowViewModel>(); }
        }

        public static void Cleanup()
        {
            SimpleIoc.Default.Unregister<IKinectSpeechEngineService>();
            SimpleIoc.Default.Unregister<MainWindowViewModel>();
            SimpleIoc.Default.Unregister<IMediaService>();
        }
    }
}
