using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using KinectMusicControl.KinectServices;
using KinectMusicControl.Models.Kinect;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

namespace KinectMusicControl.Services
{
    public interface IKinectSpeechEngineService
    {
        KinectServiceResult InitializeKinectSensor(IEnumerable<KinectGrammarPhraseKeyValue> grammarPhrases = null);

        void DisposeKinectSensor();

        event EventHandler<String> SpeechRecognizedHandler;
        event EventHandler<String> SpeechRejectedHandler;
    }

    public enum KinectServiceResult
    {
        NoSpeechRecognizerAvailable,
        NoAudioStreamFound,
        NoSensorAvailable,
        Success
    }

    public class KinectSpeechEngineService : IKinectSpeechEngineService
    {
        private KinectSensor _kinectSensor;
        private SpeechRecognitionEngine _speechRecognitionEngine;
        private KinectAudioStream _kinectAudioStream;
        private static readonly List<KinectGrammarPhraseKeyValue> DefaultPhrases;

        private const double ConfidenceThreshold = 0.7;

        static KinectSpeechEngineService()
        {
            DefaultPhrases = new List<KinectGrammarPhraseKeyValue>();
            DefaultPhrases.Add(new KinectGrammarPhraseKeyValue("play song", "PLAY"));
            DefaultPhrases.Add(new KinectGrammarPhraseKeyValue("pause song", "PAUSE"));
            DefaultPhrases.Add(new KinectGrammarPhraseKeyValue("stop song", "STOP"));
            DefaultPhrases.Add(new KinectGrammarPhraseKeyValue("next song", "NEXT"));
            DefaultPhrases.Add(new KinectGrammarPhraseKeyValue("previous song", "PREVIOUS"));
            DefaultPhrases.Add(new KinectGrammarPhraseKeyValue("volume up", "VOLUME_UP"));
            DefaultPhrases.Add(new KinectGrammarPhraseKeyValue("volume down", "VOLUME_DOWN"));
            DefaultPhrases.Add(new KinectGrammarPhraseKeyValue("mute", "MUTE"));
        }

        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        private RecognizerInfo GetRecognizerInfo()
        {
            IEnumerable<RecognizerInfo> recognizers;
            
            // This is required to catch the case when an expected recognizer is not installed.
            // By default - the x86 Speech Runtime is always expected. 
            try
            {
                recognizers = SpeechRecognitionEngine.InstalledRecognizers();
            }
            catch (COMException)
            {
                return null;
            }

            foreach (RecognizerInfo recognizer in recognizers)
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-CA".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        private Grammar GetGrammar(RecognizerInfo recognizerInfo, IEnumerable<KinectGrammarPhraseKeyValue> phrases)
        {

            /****************************************************************
            * 
            * Use this code to create grammar programmatically rather than from
            * a grammar file.
            * 
            * var directions = new Choices();
            * directions.Add(new SemanticResultValue("forward", "FORWARD"));
            * directions.Add(new SemanticResultValue("forwards", "FORWARD"));
            * directions.Add(new SemanticResultValue("straight", "FORWARD"));
            * directions.Add(new SemanticResultValue("backward", "BACKWARD"));
            * directions.Add(new SemanticResultValue("backwards", "BACKWARD"));
            * directions.Add(new SemanticResultValue("back", "BACKWARD"));
            * directions.Add(new SemanticResultValue("turn left", "LEFT"));
            * directions.Add(new SemanticResultValue("turn right", "RIGHT"));
            *
            * var gb = new GrammarBuilder { Culture = recognizerInfo.Culture };
            * gb.Append(directions);
            *
            * var g = new Grammar(gb);
            * 
            ****************************************************************/

            // Create a grammar from grammar definition XML file.
            /*
             * using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(Properties.Resources.SpeechGrammar)))
            {
                var g = new Grammar(memoryStream);
                return g;
            }
             */

            Choices choices = new Choices();
            foreach (var phrase in phrases ?? DefaultPhrases)
            {
                choices.Add(phrase.Key, phrase.Value);
            }

            var grammarBuilder = new GrammarBuilder { Culture = recognizerInfo.Culture };
            grammarBuilder.Append(choices);
            
            var grammar = new Grammar(grammarBuilder);

            return grammar;
        }

        private KinectAudioStream GetAudioStream(KinectSensor kinectSensor)
        {
            if (kinectSensor.IsOpen)
            {
                // grab the audio stream
                IReadOnlyList<AudioBeam> audioBeamList = kinectSensor.AudioSource.AudioBeams;
                Stream audioStream = audioBeamList[0].OpenInputStream();

                // create the convert stream
                return _kinectAudioStream = new KinectAudioStream(audioStream);
            }

            return null;
        }

        public KinectServiceResult InitializeKinectSensor(IEnumerable<KinectGrammarPhraseKeyValue> grammarPhrases = null)
        {
            DisposeKinectSensor();

            _kinectSensor = KinectSensor.GetDefault();
            if (null == _kinectSensor)
            {
                return KinectServiceResult.NoSensorAvailable;
            }

            // open the sensor
            _kinectSensor.Open();

            _kinectAudioStream = GetAudioStream(_kinectSensor);
            if (null == _kinectAudioStream)
            {
                return KinectServiceResult.NoAudioStreamFound;
            }

            RecognizerInfo recognizerInfo = GetRecognizerInfo();
            if (null == recognizerInfo)
            {
                return KinectServiceResult.NoSpeechRecognizerAvailable;
            }

            _speechRecognitionEngine = new SpeechRecognitionEngine(recognizerInfo.Id);
            _speechRecognitionEngine.LoadGrammar(GetGrammar(recognizerInfo, grammarPhrases));

            _speechRecognitionEngine.SpeechRecognized += SpeechRecognized;
            _speechRecognitionEngine.SpeechRecognitionRejected += SpeechRejected;

            // let the convertStream know speech is going active
            _kinectAudioStream.SpeechActive = true;

            // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
            // This will prevent recognition accuracy from degrading over time
            _speechRecognitionEngine.UpdateRecognizerSetting("AdaptationOn", 0);

            _speechRecognitionEngine.SetInputToAudioStream(_kinectAudioStream,
                new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            _speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);

            return KinectServiceResult.Success;
        }

        public void DisposeKinectSensor()
        {
            if (null != _kinectAudioStream)
            {
                _kinectAudioStream.SpeechActive = false;
                _kinectAudioStream.Close();
            }

            if (null != _speechRecognitionEngine)
            {
                _speechRecognitionEngine.SpeechRecognized -= this.SpeechRecognized;
                _speechRecognitionEngine.SpeechRecognitionRejected -= this.SpeechRejected;
                _speechRecognitionEngine.RecognizeAsyncStop();
                _speechRecognitionEngine.Dispose();
            }

            if (null != _kinectSensor)
            {
                _kinectSensor.Close();
                _kinectSensor = null;
            }
        }


        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                String semanticResult = e.Result.Text ?? e.Result.Semantics.Value.ToString();

                var handler = SpeechRecognizedHandler;
                if (handler != null)
                {
                    handler(this, semanticResult);
                }
            }
        }

        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            var handler = SpeechRejectedHandler;
            if (handler != null)
            {
                handler(this, e.Result.Text ?? e.Result.Semantics.Value.ToString());
            }
        }

        public event EventHandler<String> SpeechRejectedHandler;

        public event EventHandler<String> SpeechRecognizedHandler;
    }
}
