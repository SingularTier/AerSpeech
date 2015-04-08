using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;


namespace AerSpeech
{
    /// <summary>
    /// Controls all of AER's responses. 
    /// </summary>
    /// <remarks>This needs cleaning as well. Essentially it could  use a re-design to get rid 
    ///    of the flat interface design. Maybe event driven responses?</remarks>
    public class AerTalk
    {
        private SpeechSynthesizer _synth;
        Random rand;
        public String LastSpelledWord;

        public AerTalk()
        {
            rand = new Random();
            LastSpelledWord = "";
            _synth = new SpeechSynthesizer();
            _synth.SetOutputToDefaultAudioDevice();
        }

        public void Say(string text)
        {
            _synth.SpeakAsyncCancelAll();
            _synth.SpeakAsync(stripFormatting(text));
        }

        public void SayInitializing()
        {
            this.Say("Aer. Initializing. Welcome to the Audio Expository Response Interface, Better known as: Aer. For instructions, please say 'I need instructions'. Initialization complete.");
        }
        public void SayBlocking(string text)
        {
            _synth.SpeakAsyncCancelAll();
            _synth.Speak(stripFormatting(text));
        }
        public void RandomGreetings()
        {
            int rsp = rand.Next(0, 2);
            switch (rsp)
            {
                case 0:
                    this.Say("Greetings Commander");
                    break;
                case 1:
                    this.Say("Hello Commander");
                    break;
                default:
                    break;
            }
        }
        public void RandomQueryAck()
        {
            int rsp = rand.Next(0, 3);
            switch(rsp)
            {
                case 0:
                    this.Say("Yes?");
                    break;
                case 1:
                    this.Say("What?");
                    break;
                case 2:
                    this.Say("Yes Commander?");
                    break;
                default:
                    break;
            }
        }
        public void RandomNack()
        {
            int rsp = rand.Next(0, 3);
            switch (rsp)
            {
                case 0:
                    this.Say("I can't do that");
                    break;
                case 1:
                    this.Say("Sorry, impossible");
                    break;
            }
        }
        public void RandomAck()
        {
            int rsp = rand.Next(0, 3);
            switch (rsp)
            {
                case 0:
                    this.Say("Fine");
                    break;
                case 1:
                    this.Say("Ok");
                    break;
                case 2:
                    this.Say("Sure");
                    break;
            }
        }
        public void RandomUnknownAck()
        {
            int rsp = rand.Next(0, 2);
            switch (rsp)
            {
                case 0:
                    this.Say("Unknown");
                    break;
                case 1:
                    this.Say("I do not know.");
                    break;
            }
        }
        public void SayIdentity()
        {
            this.Say("I am Aerr, or the Audio Expository Response Interface. I interpret spoken commands in to actions. Unfortunately my capabilities are limited, but as more features are added you will find me indispensable.");
        }
        public void SayCreaterInfo()
        {
            this.Say("I was developed by Commander Tei Lin in an effort to create a more robust Speech interface than what was available at the time.");
        }
        public void SayCapabilities()
        {
            this.Say("I can search for commodities, browse the galnet news, search wikipedia, and even tell jokes. For more information, ask for instructions");
        }
        public void SayInstructions()
        {
            string instructions = @"To get information on a system, say 'I need information on', followed by the system name, either spoken or spelled in the NATO alphabet.
                                    To set your local system, say 'set current system to', followed by the system name, also either spoken or spelled. 
                                    Once you set your local system, I can search for commodities and get distances to other systems.
                                    To browse Galnet, say 'browse galnet', 'next article', and 'read article'.
                                    To Search Wikipedia, say 'Search wikipedia for', followed by the NATO alphabet spelling of what you would like to search.
                                    To disable all command processing, say 'Stop Listening'. To Resume processing, say 'Start Listening'";
                                    
            this.Say(instructions);
        }
        public void SaySystem(EliteSystem es)
        {

            string str = es.Name + ", Allegiance ";
            if(es.Allegiance.Equals(""))
            {
                str+= "Unknown";
            }
            else
            {
                str += es.Allegiance;
            }
            str += ", Controlling Faction ";
            if (es.Faction.Equals(""))
            {
                str += "Unknown";
            }
            else
            {
                str += es.Faction;
            }
            str += ", Population ";
            if (es.Faction.Equals(""))
            {
                str += "Unknown";
            }
            else
            {
                str += es.Population;
            }

            Say(str);
        }
        public void SaySetSystem(EliteSystem es)
        {
            Say("Setting Current System to " + es.Name);
        }
        public void SayFoundCommodity(EliteCommodity ec, EliteStation est)
        {
            string spellName = Regex.Replace(est.System.Name, @"(?<=.)(?!$)", ",");
            this.Say("You can find " + ec.Name + ", at " + est.Name + ", in " + est.System.Name + ", Spelled " + Spell(est.System.Name));
        }

        public string Spell(string spellMe)
        {
            string spellName = Regex.Replace(spellMe, @"(?<=.)(?!$)", ",");
            LastSpelledWord = spellMe;
            return spellName;
        }

        public void SayUnknownLocation()
        {
            this.Say(@"I don't know where we are. Please set location using 'set current system'");
        }
        public void SayDistance(double distance)
        {
            Say(distance.ToString("0.##") + " light years");
        }
        public void SayCannotFindCommodity(EliteCommodity ec)
        {
            this.Say("I could not find " + ec.Name + " within 250 light years");
        }

        public void SayStartListening()
        {
            this.Say("I am now listening for commands");
        }

        public void SayStopListening()
        {
            this.Say("I will no longer respond to commands until you say, 'Start listening'");
        }

        private string stripFormatting(string input)
        {
            string output;

            output = Regex.Replace(input, "<[^<]+?>", "");
            output = Regex.Replace(output, "</[^<]+?>", "");
            output = Regex.Replace(output, "<br />", "");
            output = Regex.Replace(output, "<br>", "");
            output = Regex.Replace(output, "<b>", "");
            output = Regex.Replace(output, "&#[0-9]+;", "");

            return output;
        }
    }

}
