using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Speech;
using System.Speech.Recognition;
using System.IO;
using AerSpeech;

namespace AerSpeechConsole
{
    class Program
    {
        static AerInput input;
        static AerHandler inputHandler;

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the A.E.R. Interface Console");
            input = new AerInput();
            inputHandler = new AerHandler();
            HandleInput();
        }

        static void HandleInput()
        {
            bool quit = false;
            while(!quit)
            {
                if(input.NewInput)
                {
                    input.NewInput = false;
                    inputHandler.DefaultInput_Handler(input.LastResult);
                }

                if(Console.KeyAvailable)
                {
                    quit = true;
                }
            }
        }
    }
}
