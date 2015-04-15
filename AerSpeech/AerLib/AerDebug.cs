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
        public static string VERSION_NUMBER = "1.3p1";

        static bool _Init = false;
        static StreamWriter _LogFile;

        public static EventHandler<DebugLogEventArgs> OnLogSpeech;
        public static EventHandler<DebugLogEventArgs> OnLogError;
        public static EventHandler<DebugLogEventArgs> OnLog;
        public static EventHandler<DebugLogEventArgs> OnLogSay;

        static public string GetUserDataPath()
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            dir = System.IO.Path.Combine(dir, "AER Interface");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }
        
        public static void Init()
        {
            _LogFile = new StreamWriter(GetUserDataPath() + "\\aer_output.log", false);
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
            catch { }

            if (OnLogError != null)
            {
                OnLogError(null, new DebugLogEventArgs(error));
            }
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
            catch { }

            if (OnLog != null)
            {
                OnLog(null, new DebugLogEventArgs(text));
            }
        }

        public static void LogSpeech(string text, double confidence, bool accepted = false)
        {
            //Dirty hack to support VA and console.
            try
            {
                if (confidence > 0.9f)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Red;
                }
                else if (confidence > 0.75f)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Yellow;
                }
                Console.WriteLine("SPEECH  : " + text + " \t\t " + confidence);
                Console.ResetColor();
            }
            catch { }

            if (!_Init)
                Init();

            if (_LogFile != null)
            {
                _LogFile.WriteAsync("SPEECH: " + text + " \t\t " + confidence + Environment.NewLine);
                _LogFile.Flush();
            }

            if(OnLogSpeech != null)
            {
                OnLogSpeech(null, new DebugLogEventArgs(text, accepted));
            }

        }

        public static void LogSay(string text)
        {
            if (!_Init)
                Init();

            if (_LogFile != null)
            {
                _LogFile.Write("SAY  : " + text + Environment.NewLine);
                _LogFile.Flush();
            }

            //Dirty hack to support VA and console.
            try
            {
                Console.WriteLine("SAY  : " + text);
            }
            catch { }

            if (OnLogSay != null)
            {
                OnLogSay(null, new DebugLogEventArgs(text));
            }
        }
    }

    public class DebugLogEventArgs : EventArgs
    {
        public bool Accepted { get; set; }
        public string Text { get; set; }

        public DebugLogEventArgs(string text, bool accepted = false)
        {
            Accepted = accepted;
            Text = text;
        }
    }
}
