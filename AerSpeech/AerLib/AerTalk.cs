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
            this.Say("Aer initializing.");
        }

        public void SayReady()
        {
            this.Say("I am ready. If you want me to respond to your commands, say 'Start Listening'.");
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
                case 2:
                    this.Say("I cannot do that");
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
            this.Say("I was developed by Commander Tei Lin in an effort to create a more robust Speech interface than what was available at the time. Some features were programmed by Commander Win-Epic.");
        }
        public void SayCapabilities()
        {
            this.Say("I can search for commodities, browse the galnet news, search wikipedia, and even tell jokes. For more information, ask for instructions");
        }
        public void SayInstructions()
        {
            string instructions = @"To get information on a system, say 'I need information on', followed by the system name, either spoken or spelled in the NATO alphabet.
                                    To get information on a station, say 'Information on', followed by the station name and the system it is in, spoken or spelled.
                                    To set your local system, say 'set current system to', followed by the system name, also either spoken or spelled. 
                                    You can also say 'that system' or 'that station' to reuse the last system or station mentioned.
                                    Once you set your local system, I can search for commodities and get distances to other systems.
                                    To browse Galnet, say 'browse galnet', 'next article', and 'read article'.
                                    To Search Wikipedia, say 'Search wikipedia for', followed by the NATO alphabet spelling of what you would like to search.
                                    To disable all command processing, say 'Stop Listening'. To Resume processing, say 'Start Listening'.
                                    You can customize my commands in the default.xml file, found in my Grammars folder.
    ";
                                    
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
            this.Say("I am now ignoring all commands.");
        }
        public void SaySelectSystem(EliteSystem es)
        {
            this.Say("Selected " + es.Name);
        }

        public void SayStationDistance(EliteStation est)
        {
            this.Say(est.Name + " is " + est.DistanceFromStar + "light seconds from" + est.System.Name);
        }
        public void SayUnknownStation()
        {
            this.Say("Unknown Station");
        }
        public void SayUnknownSystem()
        {
            this.Say("Unknown System");
        }

        private string _BlanksToUnknown(string input)
        {
            if (input == null || input.Equals(""))
            {
                return "Unknown";
            }
            else
                return input;
        }

        public double DaysSince(long timeSinceEpoch)
        {
            DateTime currentTime = DateTime.UtcNow;
            DateTime updatedAt = FromEpoch(timeSinceEpoch);
            TimeSpan elapsed = currentTime.Subtract(updatedAt);
            return elapsed.TotalDays;
        }
        public DateTime FromEpoch(long epochTime)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(epochTime);
        }

        public void SayStation(EliteStation est)
        {
            //This could be faster, it is also more elegant
            StringBuilder stationInfo = new StringBuilder();

            double daysSinceUpdate = DaysSince(est.UpdatedAt);

            if(daysSinceUpdate > 7)
            {
                stationInfo.Append("This information was last updated, " + daysSinceUpdate.ToString("0") + " days ago, ,");
            }

            stationInfo.Append(@"" + _BlanksToUnknown(est.Name) + 
                ", Faction, " + _BlanksToUnknown(est.Faction) +
                ", Allegiance, " + _BlanksToUnknown(est.Allegiance) +
                ", Government, " +  _BlanksToUnknown(est.Government) +
                ", State, " +  _BlanksToUnknown(est.State) +
                ", StarportType, " +  _BlanksToUnknown(est.StarportType));

            stationInfo.Append(". Its distance from the star is" + est.DistanceFromStar + "light seconds. ");

            stationInfo.Append("Maximum Landing Pad Size, ");
            switch(est.MaxPadSize)
            {
                case ("S"):
                    stationInfo.Append("Small");
                    break;
                case ("M"):
                    stationInfo.Append("Medium");
                    break;
                case ("L"):
                    stationInfo.Append("Large");
                    break;
                default:
                    stationInfo.Append("Unknown");
                    break;
            }

            stationInfo.Append(", Known Available Services");
            if (est.HasCommodities)
                stationInfo.Append(", Commodities");
            if (est.HasRefuel)
                stationInfo.Append(", Refuel");
            if (est.HasRepair)
                stationInfo.Append(", Repair");
            if (est.HasRearm)
                stationInfo.Append(", Rearm");
            if (est.HasOutfitting)
                stationInfo.Append(", Outfitting");
            if (est.HasShipyard)
                stationInfo.Append(", Shipyard");
            if (est.HasBlackmarket)
                stationInfo.Append(", Black Market");

            stationInfo.Append(".");

            this.Say(stationInfo.ToString());
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
