using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;
using System.Speech;
using System.Speech.Recognition;
using System.IO;
using AerSpeech;

namespace AerSpeechConsole
{
    class Program
    {
        static AerInput _AerInput;
        static AerHandler _AerHandler;
        static bool _RunWorker;

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the A.E.R. Interface Console");
            AerDB data = new AerDB(@"json\");
            AerTalk talk = new AerTalk();

            Personality person = new Personality(talk, data);
            _AerHandler = new AerHandler(data, person);
            _AerInput = new AerInput(@"Grammars\", person.GrammarLoaded_Handler);

            HandleInput();
        }

        static void HandleInput()
        {
            _RunWorker = true;
            Thread exeThread = new Thread(ExecuteThread);
            exeThread.Start();

            while (_RunWorker)
            {
                string input = Console.ReadLine();
                input = input.ToLower();

                if(input.Equals("quit"))
                {
                    _RunWorker = false;
                }
#if DEBUG
                if(input.Equals("compilegrammars"))
                {
                    _AerHandler.DBG_CompileGrammars();
                }
#endif
            }
        }

        public static void ExecuteThread()
        {
            while (_RunWorker)
            {
                if (_AerInput.NewInput)
                {
                    _AerInput.NewInput = false;
                    _AerHandler.InputHandler(_AerInput.LastResult);
                }
            }
        }
    }
}
