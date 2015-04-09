using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Speech;
using System.Speech.Synthesis;
using System.Speech.Recognition;
using AerSpeech;

namespace AerSpeech
{
    /// <summary>
    /// Simple class that handles speech input.
    /// </summary>
    public class AerInput
    {
        protected SpeechRecognitionEngine RecognitionEngine;
        public RecognitionResult LastResult;
        public bool NewInput;


        public AerInput(string pathToGrammar = @"Grammars\")
        {
            RecognitionEngine = new SpeechRecognitionEngine(new CultureInfo("en-US"));
            RecognitionEngine.SetInputToDefaultAudioDevice();
            LoadGrammar(pathToGrammar);
            RecognitionEngine.SpeechRecognized += this.SpeechRecognized_Handler;
            RecognitionEngine.UpdateRecognizerSetting("ResponseSpeed", 750);
            NewInput = false;
            RecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
        }

        /// <summary>
        /// Loads the grammar in to the recognition engine.
        /// </summary>
        protected virtual void LoadGrammar(string pathToGrammar)
        {

            AerDebug.Log("Loading Grammar...");
            Grammar grammar = new Grammar(pathToGrammar + @"Default.xml");
            RecognitionEngine.LoadGrammarAsync(grammar);

        }

        /// <summary>
        /// Handles the SpeechRecognized event from the Recognition Engine
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void SpeechRecognized_Handler(object sender, SpeechRecognizedEventArgs e)
        {
            string text = e.Result.Text;
            SemanticValue semantics = e.Result.Semantics;

            NewInput = true;
            LastResult = e.Result;
            AerDebug.LogSpeech(e.Result.Text, e.Result.Confidence);
        }

    }
}
