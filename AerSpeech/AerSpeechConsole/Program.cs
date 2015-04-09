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
            _AerHandler = new AerHandler();
            _AerInput = new AerInput(_AerHandler);
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
                    _AerHandler.DefaultInput_Handler(_AerInput.LastResult);
                }
            }
        }
    }
}
