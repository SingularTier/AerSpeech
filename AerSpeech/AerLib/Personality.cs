using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech;
using System.Speech.Recognition;
using System.Reflection;

namespace AerSpeech
{
    /// <summary>
    /// Handler for speech recognition commands. Associates directly with a topLevel rule (out.Command)
    /// </summary>
    /// <param name="result"></param>
    public delegate void AerInputHandler(AerRecognitionResult result);

    /// <summary>
    /// Represents a way to Handle the incomming Requests, works with AerTalk.
    /// </summary>
    public class Personality
    {
        private AerTalk _Talk;
        private AerRSS _GalnetRSS;
        private AerRSS _JokeRSS;
        private AerWiki _Wikipedia;
        private AerDB _Data;
        private AerKeyboard _Keyboard;

        #region State Variables
        private EliteSystem _LocalSystem; //Our current local system
        public EliteSystem LocalSystem
        {
            get
            {
                if (_LocalSystem == null)
                {
                    string localSystem = Settings.Load("localsystem");
                    _LocalSystem = _Data.GetSystem(localSystem);
                }

                return _LocalSystem;
            }
            set
            {
                Settings.Store("localsystem", value.Name);
                _LocalSystem = value;
            }
        }
        public EliteSystem LastSystem;
        public EliteStation LastStation;

        private int _GalnetEntry; //Current galnet entry index
        private int _JokeEntry;   //Current joke entry index
        private bool _Squelched;
        private int _StopListeningTime;
        DateTime _LastQueryTime;
        private Dictionary<string, AerInputHandler> _EventRegistry;
        #endregion

        public Personality(AerTalk voiceSynth, AerDB data)
        {

            _Data = data;
            _Talk = voiceSynth;

            _Talk.SayInitializing();
            _EventRegistry = new Dictionary<string, AerInputHandler>();
            _Keyboard = new AerKeyboard();
            _GalnetRSS = new AerRSS("http://www.elitedangerous.com/en/news/galnet/rss");
            _JokeRSS = new AerRSS("http://www.jokes2go.com/jspq.xml");
            _Wikipedia = new AerWiki();
            _StopListeningTime = 30; //30 seconds

            RegisterDefaultHandlers();
        }

        public void RecognizedInput(AerRecognitionResult input)
        {
            if (input.Confidence < input.RequiredConfidence)
            {
                //Do homophone handling
                return;
            }

            if (input.Command != null)
            {
                if (_EventRegistry.ContainsKey(input.Command))
                {
                    TimeSpan elapsed = DateTime.UtcNow.Subtract(_LastQueryTime);
                    if (input.Command.Equals("AerEndQuery"))
                    {
                        //Do nothing until Aer is addressed again...
                        _LastQueryTime = DateTime.UtcNow.Subtract(new TimeSpan(0, 0, _StopListeningTime));
                        _EventRegistry[input.Command](input);
                    }
                    else if (elapsed.TotalSeconds < _StopListeningTime)
                    {
                        _LastQueryTime = DateTime.UtcNow;
                        _EventRegistry[input.Command](input);
                    }
                    else if (input.Command.Equals("AerQuery"))
                    {
                        _LastQueryTime = DateTime.UtcNow;
                        _EventRegistry[input.Command](input);
                    }
                    else
                    {
                        //Do nothing until Aer is addressed again...
                    }
                }
                else
                {
                    AerDebug.LogError(@"Recieved command that didn't have a handler, command=" + input.Command);
                }
            }
            else
            {
                AerDebug.LogError(@"Recieved Recognition Result that didn't have a command semantic, '" + input.ToString() + "'");
            }
        }

        public void RegisterDefaultHandlers()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            IEnumerable<MemberInfo> handlers = assembly.GetTypes().SelectMany(type => type.GetMembers())
                                                    .Union(assembly.GetTypes())
                                                    .Where(type => Attribute.IsDefined(type, typeof(SpeechHandlerAttribute)));

            foreach(MemberInfo mi in handlers)
            {
                Object[] attribs = mi.GetCustomAttributes(typeof(SpeechHandlerAttribute), false);
                for (int i = 0; i < attribs.Length; i++)
                {
                    AerDebug.Log("Found Handler: " + ((SpeechHandlerAttribute)attribs[i]).GrammarRule);
                    if (mi.MemberType == MemberTypes.Method)
                    {
                        AerInputHandler Call = (AerInputHandler)Delegate.CreateDelegate(typeof(AerInputHandler), this, mi.Name);


                        RegisterHandler(((SpeechHandlerAttribute)attribs[i]).GrammarRule, Call);
                    }
                }
            }
        }

        public void RegisterHandler(string Command, AerInputHandler handler)
        {
            _EventRegistry.Add(Command, handler);
        }

        public void GrammarLoaded_Handler(object sender, LoadGrammarCompletedEventArgs e)
        {
            _Talk.SayReady();
            AerDebug.Log("Initialization Complete.");
        }

        #region Grammar Rule Handlers
        [SpeechHandlerAttribute("Nearest-BlackMarket")]
        public void NearestBlackMarket_Handler(AerRecognitionResult result)
        {
            if (LocalSystem == null)
                _Talk.SayUnknownLocation();
            else
            {
                EliteStation est = _Data.FindClosestBlackMarket(LocalSystem);
                if (est != null)
                {
                    _Talk.SayFoundStation(est);
                }
                else
                {
                    _Talk.RandomUnknownAck();
                }
            }
        }
        [SpeechHandlerAttribute("StationInfo-DistanceFromStar")]
        public void StationDistance_Handler(AerRecognitionResult result)
        {
            if (result.System != null)
            {
                if (result.Station != null)
                {
                    _Talk.SayStationDistance(result.Station);
                }
                else
                {
                    _Talk.SayUnknownStation();
                }
            }
            else
            {
                _Talk.SayUnknownSystem();
            }
        }
        [SpeechHandlerAttribute("StationInfo-Allegiance")]
        public void StationInfoAllegiance_Handler(AerRecognitionResult result)
        {
            if (result.System != null)
            {
                if (result.Station != null)
                {
                    _Talk.SayStationAllegiance(result.Station);
                }
                else
                {
                    _Talk.SayUnknownStation();
                }
            }
            else
            {
                _Talk.SayUnknownSystem();
            }
        }
        [SpeechHandlerAttribute("StationInfo-MaxLandingPadSize")]
        public void StationInfoMaxLandingPadSize_Handler(AerRecognitionResult result)
        {
            if (result.System != null)
            {
                if (result.Station != null)
                {
                    _Talk.SayStationMaxLandingPadSize(result.Station);
                }
                else
                {
                    _Talk.SayUnknownStation();
                }
            }
            else
            {
                _Talk.SayUnknownSystem();
            }
        }
        [SpeechHandlerAttribute("StationInfo-KnownServices")]
        public void StationInfoKnownServices_Handler(AerRecognitionResult result)
        {
            if (result.System != null)
            {
                if (result.Station != null)
                {
                    _Talk.SayStationServices(result.Station);
                }
                else
                {
                    _Talk.SayUnknownStation();
                }
            }
            else
            {
                _Talk.SayUnknownSystem();
            }
        }
        [SpeechHandlerAttribute("AerCloseTerminal")]
        public void AerCloseTerminal_Handler(AerRecognitionResult result)
        {
            Environment.Exit(0);
        }
        [SpeechHandlerAttribute("Greetings")]
        public void Greetings_Handler(AerRecognitionResult result)
        {
            _Talk.RandomGreetings();
        }
        [SpeechHandlerAttribute("AerQuery")]
        public void AerQuery_Handler(AerRecognitionResult result)
        {
            _Talk.RandomQueryAck();
        }
        [SpeechHandlerAttribute("AerEndQuery")]
        public void AerEndQuery_Handler(AerRecognitionResult result)
        {
            _Talk.RandomQueryEndAck();
        }
        [SpeechHandlerAttribute("SearchWiki")]
        public void SearchWiki_Handler(AerRecognitionResult result)
        {
            string word = result.Data;
            _Talk.SayBlocking("Searching the wiki for " + word);
            _Talk.Say(_Wikipedia.Query(word));
        }
        [SpeechHandlerAttribute("BrowseGalnet")]
        public void BrowseGalnet_Handler(AerRecognitionResult result)
        {
            _GalnetEntry = 0;
            if (_GalnetRSS.Loaded)
                _Talk.Say(_GalnetRSS.Entries[_GalnetEntry].Title);
            else
                _Talk.RandomNack();
        }
        [SpeechHandlerAttribute("NextArticle")]
        public void NextArticle_Handler(AerRecognitionResult result)
        {
            if (_GalnetRSS.Loaded)
            {
                _GalnetEntry++;

                if (_GalnetEntry >= _GalnetRSS.Entries.Count)
                    _GalnetEntry = 0;

                _Talk.Say(_GalnetRSS.Entries[_GalnetEntry].Title);
            }
            else
                _Talk.RandomNack();
        }
        [SpeechHandlerAttribute("PreviousArticle")]
        public void PreviousArticle_Handler(AerRecognitionResult result)
        {
            if (_GalnetRSS.Loaded)
            {
                _GalnetEntry--;

                if (_GalnetEntry < 0)
                    _GalnetEntry = 0;

                _Talk.Say(_GalnetRSS.Entries[_GalnetEntry].Title);
            }
            else
                _Talk.RandomNack();
        }
        [SpeechHandlerAttribute("ReadArticle")]
        public void ReadArticle_Handler(AerRecognitionResult result)
        {
            if (_GalnetRSS.Loaded)
                _Talk.Say(_GalnetRSS.Entries[_GalnetEntry].Description);
            else
                _Talk.RandomNack();
        }
        [SpeechHandlerAttribute("TellJoke")]
        public void TellJoke_Handler(AerRecognitionResult result)
        {
            _Talk.Say(_JokeRSS.Entries[_JokeEntry].Description);
            _JokeEntry++;
            if (_JokeEntry >= _JokeRSS.Entries.Count)
                _JokeEntry = 0;
        }
        [SpeechHandlerAttribute("Instruction")]
        public void Instruction_Handler(AerRecognitionResult result)
        {
            _Talk.SayInstructions();
        }
        [SpeechHandlerAttribute("SystemInfo")]
        public void SystemInfo_Handler(AerRecognitionResult result)
        {
            if (result.System != null)
                _Talk.SaySystem(result.System);
            else
                _Talk.RandomUnknownAck();
        }
        [SpeechHandlerAttribute("LastStationInfo")]
        public void LastStationInfo_Handler(AerRecognitionResult result)
        {
            if (LastStation != null)
            {
                _Talk.SayStation(LastStation);
            }
            else
            {
                _Talk.SayUnknownStation();
            }
        }
        [SpeechHandlerAttribute("StationInfo")]
        public void StationInfo_Handler(AerRecognitionResult result)
        {

            if (result.System != null)
            {
                if (result.Station != null)
                {
                    _Talk.SayStation(result.Station);
                }
                else
                {
                    _Talk.SayUnknownStation();
                }
            }
            else
            {
                _Talk.SayUnknownSystem();
            }
        }
        [SpeechHandlerAttribute("SetLocalSystem")]
        public void SetLocalSystem_Handler(AerRecognitionResult result)
        {
            if (result.System != null)
            {
                _Talk.SaySetSystem(result.System);
                LocalSystem = result.System;
            }
            else
                _Talk.RandomUnknownAck();
        }
        [SpeechHandlerAttribute("StarDistance")]
        public void StarDistance_Handler(AerRecognitionResult result)
        {
            if ((result.System != null) && (LocalSystem != null))
            {
                _Talk.SayDistance(Math.Sqrt(_Data.DistanceSqr(result.System, LocalSystem)));
            }
            else
            {
                _Talk.RandomUnknownAck();
            }
        }
        [SpeechHandlerAttribute("StarToStarDistance")]
        public void StarToStarDistance_Handler(AerRecognitionResult result)
        {
            if ((result.FromSystem != null) && (result.ToSystem != null))
            {
                _Talk.SayDistance(Math.Sqrt(_Data.DistanceSqr(result.FromSystem, result.ToSystem)));
            }
            else
            {
                _Talk.RandomUnknownAck();
            }
        }
        [SpeechHandlerAttribute("AerSetSystem")]
        public void AerSetSystem_Handler(AerRecognitionResult result)
        {
            if (result.System != null)
            {
                LocalSystem = result.System;
            }
        }
        [SpeechHandlerAttribute("AerCapabilities")]
        public void AerCapabilities_Handler(AerRecognitionResult result)
        {
            _Talk.SayCapabilities();
        }
        [SpeechHandlerAttribute("AerCreatorInfo")]
        public void AerCreatorInfo_Handler(AerRecognitionResult result)
        {
            _Talk.SayCreaterInfo();
        }
        [SpeechHandlerAttribute("AerIdentity")]
        public void AerIdentity_Handler(AerRecognitionResult result)
        {
            _Talk.SayIdentity();
        }
        [SpeechHandlerAttribute("PriceCheck")]
        public void PriceCheck_Handler(AerRecognitionResult result)
        {
            if (result.Commodity.AveragePrice <= 0)
            {
                _Talk.RandomUnknownAck();
            }
            else
            {
                _Talk.Say(result.Commodity.AveragePrice.ToString());
            }
        }
        [SpeechHandlerAttribute("FindCommodity")]
        public void FindCommodity_Handler(AerRecognitionResult result)
        {
            if (LocalSystem != null)
            {
                EliteStation est = _Data.FindCommodity(result.Commodity.id, LocalSystem, 250);
                if (est != null)
                {
                    _Talk.SayFoundCommodity(result.Commodity, est);
                    LastStation = est;
                    LastSystem = est.System;
                }
                else
                    _Talk.SayCannotFindCommodity(result.Commodity);
            }
            else
                _Talk.SayUnknownLocation();
        }
        [SpeechHandlerAttribute("CancelSpeech")]
        public void CancelSpeech_Handler(AerRecognitionResult result)
        {
            _Talk.RandomAck();
        }
        [SpeechHandlerAttribute("StartListening")]
        public void StartListening_Handler(AerRecognitionResult result)
        {
            _Squelched = false;
            _Talk.SayStartListening();

        }
        [SpeechHandlerAttribute("StopListening")]
        public void StopListening_Handler(AerRecognitionResult result)
        {
            _Squelched = true;
            _Talk.SayStopListening();
        }
        [SpeechHandlerAttribute("TypeLastSpelled")]
        public void TypeLastSpelled_Handler(AerRecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.Type(_Talk.LastSpelledWord);
        }
        [SpeechHandlerAttribute("EraseField")]
        public void EraseField_Handler(AerRecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.ClearField();
        }
        [SpeechHandlerAttribute("TypeDictation")]
        public void TypeDictation_Handler(AerRecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.Type(result.Data);
        }
        [SpeechHandlerAttribute("TypeNato")]
        public void TypeNato_Handler(AerRecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.Type(result.Data);
        }
        [SpeechHandlerAttribute("TypeSystem")]
        public void TypeSystem_Handler(AerRecognitionResult result)
        {
            if (result.System != null)
            {
                _Talk.RandomAck();
                _Keyboard.Type(result.System.Name);
            }
            else
            {
                _Talk.RandomUnknownAck();
            }
        }
        [SpeechHandlerAttribute("TypeCurrentSystem")]
        public void TypeCurrentSystem_Handler(AerRecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.Type(LocalSystem.Name);
        }
        [SpeechHandlerAttribute("SayCurrentSystem")]
        public void SayCurrentSystem_Handler(AerRecognitionResult result)
        {
            if (LocalSystem != null)
                _Talk.Say(LocalSystem.Name);
            else
                _Talk.SayUnknownLocation();

        }
        [SpeechHandlerAttribute("SayCurrentVersion")]
        public void SayCurrentVersion_Handler(AerRecognitionResult result)
        {
            _Talk.Say(AerDebug.VERSION_NUMBER);

        }
        [SpeechHandlerAttribute("AllegianceDistance")]
        public void AllegianceDistance_Handler(AerRecognitionResult result)
        {
            if (LocalSystem == null)
                _Talk.SayUnknownLocation();
            else
            {
                EliteSystem es = _Data.FindClosestAllegiance(result.Data, LocalSystem);
                if (es != null)
                {
                    LastSystem = es;
                    _Talk.SayAndSpell(es.Name);
                }
                else
                {
                    _Talk.RandomUnknownAck();
                }
            }

        }
        #endregion
    }
}
