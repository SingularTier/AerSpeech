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
    /// Represents a way to Handle the incoming Requests, works with AerTalk and AerDB. Has State.
    /// </summary>
    /// <remarks>
    /// TODO: I would LOVE to get the dependency on AerDB REMOVED from this class, but that may be a pipe dream -SingularTier
    /// </remarks>
    public class Personality
    {
        private AerTalk _Talk;
        private AerRSS _GalnetRSS;
        private AerRSS _JokeRSS;
        private AerWiki _Wikipedia;
        private AerDB _Data;
        private AerKeyboard _Keyboard;

        #region State Variables
        private string _CommanderName;
        public string CommanderName
        {
            get
            {
                if (_CommanderName == null)
                {
                    _CommanderName = Settings.Load("CommanderName");
                }

                return _CommanderName;
            }
            set
            {
                Settings.Store("CommanderName", value);
                _CommanderName = value;
            }
        }

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

            _RegisterDefaultHandlers();
        }

        /// <summary>
        /// Handles input form the AerHandler
        /// </summary>
        /// <param name="input"></param>
        public void RecognizedInput(AerRecognitionResult input)
        {
            if (input.Confidence < input.RequiredConfidence)
            {
                return;
            }

            if (input.Command != null)
            { 
                if (_EventRegistry.ContainsKey(input.Command))
                {
                    //If we haven't said 'stop listening'
                    if (!_Squelched)
                    {
                        TimeSpan elapsed = DateTime.UtcNow.Subtract(_LastQueryTime);
                        if (input.Command.Equals("AerEndQuery"))
                        {
                            //Do nothing until Aer is addressed again...
                            //This makes (_LastQueryTime + elapsed time) > _StopListeningTime
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
                        //If we said 'start listening' to end squelch state
                        if (input.Command.Equals("StartListening"))
                        {
                            _EventRegistry[input.Command](input);
                        }
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

        /// <summary>
        /// Uses reflection to pull out all SpeechHandlerAttribute tags and creates a Rule->Delegate dictionary
        /// from the data.
        /// </summary>
        private void _RegisterDefaultHandlers()
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
                    //AerDebug.Log("Found Handler: " + ((SpeechHandlerAttribute)attribs[i]).GrammarRule);
                    if (mi.MemberType == MemberTypes.Method)
                    {
                        AerInputHandler Call = (AerInputHandler)Delegate.CreateDelegate(typeof(AerInputHandler), this, mi.Name);


                        RegisterHandler(((SpeechHandlerAttribute)attribs[i]).GrammarRule, Call);
                    }
                }
            }
        }

        /// <summary>
        /// Registers a new handler for a command
        /// </summary>
        /// <param name="Command"></param>
        /// <param name="handler"></param>
        public void RegisterHandler(string Command, AerInputHandler handler)
        {
            if (!_EventRegistry.ContainsKey(Command))
                _EventRegistry.Add(Command, handler);
            else
            {
                AerDebug.LogError("Re-registered command to new handler, command=" + Command);
                _EventRegistry[Command] = handler; //Maybe it should just be thrown away?
            }
        }

        /// <summary>
        /// An ugly mess. This is used by AerInput spaghetti code to notify the user when the grammar is loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GrammarLoaded_Handler(object sender, LoadGrammarCompletedEventArgs e)
        {
            _Talk.SayReady();
            AerDebug.Log("Initialization Complete.");
        }

        #region Grammar Rule Handlers

        #region State Machine
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
        [SpeechHandlerAttribute("AerCloseTerminal")]
        public void AerCloseTerminal_Handler(AerRecognitionResult result)
        {
            Environment.Exit(0);
        }
        [SpeechHandlerAttribute("CancelSpeech")]
        public void CancelSpeech_Handler(AerRecognitionResult result)
        {
            _Talk.RandomAck();
        }
        #endregion

        #region Galnet
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
        #endregion

        #region Other
        [SpeechHandlerAttribute("BasicCalculate")]
        public void BasicCalculate_Handler(AerRecognitionResult result)
        {
            Calculator calculator = new Calculator();
            string answer = calculator.CalculationResult(result.Data);
            if (answer == null)
                _Talk.RandomUnknownAck();
            else
                _Talk.Say(answer);
        }
        [SpeechHandlerAttribute("SearchWiki")]
        public void SearchWiki_Handler(AerRecognitionResult result)
        {
            string word = result.Data;
            _Talk.SayBlocking("Searching the wiki for " + word);
            _Talk.Say(_Wikipedia.Query(word));
        }
        [SpeechHandlerAttribute("CurrentTime")]
        public void CurrentTime_Handler(AerRecognitionResult result)
        {
            _Talk.SayCurrentTime();
        }
        [SpeechHandlerAttribute("Greetings")]
        public void Greetings_Handler(AerRecognitionResult result)
        {
            _Talk.RandomGreetings(CommanderName);
        }
        [SpeechHandlerAttribute("TellJoke")]
        public void TellJoke_Handler(AerRecognitionResult result)
        {
            _Talk.Say(_JokeRSS.Entries[_JokeEntry].Description);
            _JokeEntry++;
            if (_JokeEntry >= _JokeRSS.Entries.Count)
                _JokeEntry = 0;
        }
        #endregion

        #region Settings
        [SpeechHandlerAttribute("Set-CommanderName")]
        public void SetCommanderName_Handler(AerRecognitionResult result)
        {
            _Talk.Say("Setting Commander Name to " + result.Data);
            CommanderName = result.Data;
        }
        #endregion

        #region EDDB
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
        [SpeechHandlerAttribute("PriceCheck")]
        public void PriceCheck_Handler(AerRecognitionResult result)
        {
            if (result.Commodity.AveragePrice <= 0)
            {
                _Talk.RandomUnknownAck();
            }
            else
            {
                _Talk.SayAveragePrice(result.Commodity);
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
        [SpeechHandlerAttribute("SayCurrentSystem")]
        public void SayCurrentSystem_Handler(AerRecognitionResult result)
        {
            if (LocalSystem != null)
                _Talk.Say(LocalSystem.Name);
            else
                _Talk.SayUnknownLocation();

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

        #region Type
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
        #endregion

        #region Aer Info
        [SpeechHandlerAttribute("Instructions")]
        public void Instruction_Handler(AerRecognitionResult result)
        {
            _Talk.SayInstructions();
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
        [SpeechHandlerAttribute("AerCapabilities")]
        public void AerCapabilities_Handler(AerRecognitionResult result)
        {
            _Talk.SayCapabilities();
        }
        [SpeechHandlerAttribute("SayCurrentVersion")]
        public void SayCurrentVersion_Handler(AerRecognitionResult result)
        {
            _Talk.Say(AerDebug.VERSION_NUMBER);

        }
        #endregion



      


        #endregion
    }
}
