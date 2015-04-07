using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AerSpeech
{
    /// <summary>
    /// Debug encapsulation.
    /// </summary>
    public static class AerDebug
    {
        static bool _Init = false;
        static StreamWriter _LogFile;

        public static void Init()
        {
            _LogFile = new StreamWriter(Application.UserAppDataPath + "\\aer_output.log", false);
            _Init = true;
        }

        public static void LogError(string error)
        {
            if (!_Init)
                Init();

            if(_LogFile != null)
            {
                _LogFile.Write("ERROR: " + error + Environment.NewLine);
                _LogFile.Flush();
            }

            //Dirty hack to support VA and console.
            try
            {
                Console.WriteLine("ERROR: " + error);
            }
            catch (Exception e) { }
        }

        public static void LogException(Exception e)
        {
            LogError(@"Caught unhandled exception: " + e.Message);
            foreach (DictionaryEntry de in e.Data)
            {
                LogError("    Key: " + de.Key.ToString() + "      Value: " + de.Value + Environment.NewLine);
            }
        }

        public static void Log(string text)
        {
            if (!_Init)
                Init();

            if (_LogFile != null)
            {
                _LogFile.Write("LOG  : " + text + Environment.NewLine);
                _LogFile.Flush();
            }

            //Dirty hack to support VA and console.
            try
            {
                Console.WriteLine("LOG  : " + text);
            }
            catch (Exception e) { }
        }

        public static void LogSpeech(string text)
        {
            //Dirty hack to support VA and console.
            try
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("LOG  : " + text);
                Console.ResetColor();
            }
            catch (Exception e) { }

            if (!_Init)
                Init();

            if (_LogFile != null)
            {
                _LogFile.WriteAsync("SPEECH: " + text + Environment.NewLine);
                _LogFile.Flush();
            }
        }
    }
}
