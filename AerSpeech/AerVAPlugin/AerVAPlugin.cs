using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using AerSpeech;

namespace AerVAPlugin
{
    public class AerVoiceAttackPlugin
    {
        static AerInput _AerInput;
        static AerHandler _AerHandler;
        static bool _RunWorker;


        public static string VA_DisplayName()
        {
            return "A.E.R. Interface";
        }

        public static string VA_DisplayInfo()
        {
            return "A.E.R. by CMDR Tei Lin (SingularTier)";
        }

        public static Guid VA_Id()
        {
            return new Guid("{ec7e5005-591c-486f-9b0c-07be23285ba6}");;
        }

        public static void VA_Init1(ref Dictionary<string, object> state, ref Dictionary<string, Int16?> conditions, ref Dictionary<string, string> textValues, ref Dictionary<string, object> extendedValues)
        {
            _RunWorker = true;
            Thread exeThread = new Thread(ExecuteThread);
            exeThread.Start();
        }

        public static void VA_Exit1(ref Dictionary<string, object> state)
        {
            _RunWorker = false;
        }

        public static void VA_Invoke1(String context, ref Dictionary<string, object> state, ref Dictionary<string, Int16?> conditions, ref Dictionary<string, string> textValues, ref Dictionary<string, object> extendedValues)
        {
           
        }

        public static void ExecuteThread()
        {
            _AerHandler = new AerHandler(AppDomain.CurrentDomain.BaseDirectory + @"\Apps\AER\json\");
            _AerInput = new AerInput(_AerHandler, AppDomain.CurrentDomain.BaseDirectory + @"\Apps\AER\Grammars\");

            while(_RunWorker)
            {
                if (_AerInput.NewInput)
                {
                    _AerInput.NewInput = false;
                    _AerHandler.DefaultInput_Handler(_AerInput.LastResult);
                }
            }
        }
    }
}

