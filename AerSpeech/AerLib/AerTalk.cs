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

        public AerTalk()
        {
            rand = new Random();

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
            this.Say("I am initializing.");
        }

        public void SayReady()
        {
            this.Say("I am ready.");
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
                    this.Say("I don't know.");
                    break;
                case 1:
                    this.Say("I do not know.");
                    break;
            }
        }
        public void SayIdentity()
        {
            this.Say("I am Aer, the Audio Expository Response Interface.");
        }
        public void SayCreaterInfo()
        {
            this.Say("I was developed by Commander Tei Lin. Commander Win-Epic modified my behaviour to make me less annoying.");
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
            this.Say("You can find " + ec.Name + ", at " + est.Name + ", in " + est.System.Name + ", Spelled " + spellName);
        }
        public void SayMoreInfo(EliteStation est)
        {
            StringBuilder output = new StringBuilder();

            string spellName = Regex.Replace(est.System.Name, @"(?<=.)(?!$)", ",");

            output.Append(est.Name + " is " + est.DistanceFromStar + " light seconds away from its star. ");
            if (est.HasBlackmarket)
                output.Append("It has a black market. ");
            if (est.HasOutfitting)
                output.Append("It has outfitting. ");
            if (est.HasRefuel)
                output.Append("It has refueling. ");
            if (est.HasShipyard)
                output.Append("It has a shipyard. ");

            this.Say(output.ToString());
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
            this.Say("I am now ignoring your commands.");
        }
        public void SaySelectSystem(EliteSystem es)
        {
            this.Say("Selected " + es.Name);
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
