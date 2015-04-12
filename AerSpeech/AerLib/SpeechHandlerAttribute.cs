using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerSpeech
{
    //TODO: Allow user to tag a Manual (not heurestic) confidence level in the attribute tag -SingularTier
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class SpeechHandlerAttribute : Attribute
    {
        public readonly string GrammarRule;

        public SpeechHandlerAttribute(string name)
        {
            GrammarRule = name;
        }
    }
}
