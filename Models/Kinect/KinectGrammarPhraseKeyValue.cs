using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectMusicControl.Models.Kinect
{
    public class KinectGrammarPhraseKeyValue
    {
        public String Key { get; set; }
        public String Value { get; set; }

        public KinectGrammarPhraseKeyValue(String key, String value)
        {
            Key = key;
            Value = value;
        }
    }
}
