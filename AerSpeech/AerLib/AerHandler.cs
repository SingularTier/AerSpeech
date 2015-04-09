using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Speech;
using System.Speech.Recognition;
using System.IO;

namespace AerSpeech
{
    /// <summary>
    /// Handler for speech recognition commands. Associates directly with a topLevel rule (out.Command)
    /// </summary>
    /// <param name="result"></param>
    public delegate void AerInputHandler(AerRecognitionResult result);

    public class AerRecognitionResult
    {
        public double RequiredConfidence;
        public double Confidence;
        public string Command;
        public string Data;
        public EliteStation Station;
        public EliteSystem System;
        public EliteSystem FromSystem;
        public EliteSystem ToSystem;
        public EliteCommodity Commodity;
        public SemanticValue Semantics;

    }

    /// <summary>
    /// Default Handler for incomming speech results. Maintains a registry of handlers keyed by command.
    /// TODO: Make an interface.
    /// </summary>
    public class AerHandler
    {
#region privates
        private AerRSS _GalnetRSS;
        private AerRSS _JokeRSS;
        private AerWiki _Wikipedia;
        private AerDB _Eddb;
        private AerTalk _Talk;
        private AerKeyboard _Keyboard;

        private Dictionary<string, AerInputHandler> _EventRegistry;
#endregion

#region State Variables
        private EliteSystem _LocalSystem; //Our current local system
        private int _GalnetEntry; //Current galnet entry index
        private int _JokeEntry;   //Current joke entry index
        public bool _Squelched;
#endregion

#region Constructors

        public AerHandler(string pathToJsonFiles = @"json\")
            : this(new AerTalk(), pathToJsonFiles)
        {

        }

        public AerHandler(AerTalk voiceSynth, string pathToJsonFiles = @"json\" )
        {
            string systemsJson = pathToJsonFiles + @"systems.json";
            string commoditiesJson = pathToJsonFiles + @"commodities.json";
            string stationsJson = pathToJsonFiles + @"stations_lite.json";

            _Squelched = false;
            _Talk = voiceSynth;
            _Talk.SayInitializing();
            _GalnetEntry = 0;
            _JokeEntry = 0;
            _Keyboard = new AerKeyboard();
            _GalnetRSS = new AerRSS("http://www.elitedangerous.com/news/galnet/rss");
            _JokeRSS = new AerRSS("http://www.jokes2go.com/jspq.xml");
            _Wikipedia = new AerWiki();
            _Eddb = new AerDB(systemsJson, stationsJson, commoditiesJson);
            _EventRegistry = new Dictionary<string, AerInputHandler>();

            RegisterDefaultHandlers();
        }
#endregion

        public AerRecognitionResult BuildAerRecognition(RecognitionResult input)
        {
            int numberOfSemantics = 0;
            AerRecognitionResult output = new AerRecognitionResult();
            output.Confidence = input.Confidence;
            try
            {
                if (input.Semantics.ContainsKey("Command") && input.Semantics["Command"] != null)
                {
                    output.Command = input.Semantics["Command"].Value.ToString();
                    numberOfSemantics++;
                }

                if (input.Semantics.ContainsKey("Data") && input.Semantics["Data"] != null)
                {
                    output.Data = input.Semantics["Data"].Value.ToString();
                    numberOfSemantics++;
                }

                if (input.Semantics.ContainsKey("CommodityId") && input.Semantics["CommodityId"] != null)
                {
                    int commodityId = int.Parse(input.Semantics["CommodityId"].Value.ToString());
                    output.Commodity = _Eddb.GetCommodity(commodityId);
                    numberOfSemantics++;
                }

                if (input.Semantics.ContainsKey("SystemName") && input.Semantics["SystemName"] != null)
                {
                    string systemName = input.Semantics["SystemName"].Value.ToString();

                    if (systemName.Equals("__local__"))
                        output.System = _LocalSystem;
                    else
                        output.System = _Eddb.GetSystem(systemName);

                    numberOfSemantics++;
                }

                if (input.Semantics.ContainsKey("StationName") && input.Semantics["StationName"] != null)
                {
                    if (output.System != null)
                    {
                        output.Station = _Eddb.GetStation(output.System,
                                                input.Semantics["StationName"].Value.ToString());
                    }

                    numberOfSemantics++;
                }
            }
            catch (Exception e)
            {
                AerDebug.LogError("Could not parse grammar semantics, " + e.Message);
                AerDebug.LogException(e);
            }

            output.RequiredConfidence = 0.92f - 0.02 * (numberOfSemantics * numberOfSemantics);

            return output;
        }


        /// <summary>
        /// Handles all speech results.
        /// This should be called in a thread that doesn't mind blocking for a long time.
        /// </summary>
        /// <param name="result"></param>
        public void DefaultInput_Handler(RecognitionResult result)
        {
            AerRecognitionResult input = BuildAerRecognition(result);
            if(input.Confidence < input.RequiredConfidence)
            {
                return;
            }

            if (input.Command != null)
            {
                if (_EventRegistry.ContainsKey(input.Command))
                {

                    if (!_Squelched)
                        _EventRegistry[input.Command](input);
                    else if (input.Command.Equals("AerStartListening"))
                        _EventRegistry[input.Command](input);
                }
                else
                {
                    AerDebug.LogError(@"Recieved command that didn't have a handler, '" + result.Text + "', command=" + input.Command);
                }
            }
            else
            {
                AerDebug.LogError(@"Recieved Recognition Result that didn't have a command semantic, '" + result.Text + "'");
            }
        }

        /// <summary>
        /// Creates default handlers for Default.xml grammar
        /// </summary>
        public void RegisterDefaultHandlers()
        {
            //TODO: Maybe use Reflection and Attribute Tags to pull in this data manually
            _EventRegistry.Add("Greetings", Greetings_Handler);
            _EventRegistry.Add("AerQuery", AerQuery_Handler);
            _EventRegistry.Add("SearchWiki", SearchWiki_Handler);
            _EventRegistry.Add("BrowseGalnet", BrowseGalnet_Handler);
            _EventRegistry.Add("NextArticle", NextArticle_Handler);
            _EventRegistry.Add("ReadArticle", ReadArticle_Handler);
            _EventRegistry.Add("TellJoke", TellJoke_Handler);
            _EventRegistry.Add("Instruction", Instruction_Handler);
            _EventRegistry.Add("SystemInfo", SystemInfo_Handler);
            _EventRegistry.Add("StationInfo", StationInfo_Handler);
            _EventRegistry.Add("SetLocalSystem", SetLocalSystem_Handler);
            _EventRegistry.Add("StarDistance", StarDistance_Handler);
            _EventRegistry.Add("AerCapabilities", AerCapabilities_Handler);
            _EventRegistry.Add("AerCreatorInfo", AerCreatorInfo_Handler);
            _EventRegistry.Add("AerIdentity", AerIdentity_Handler);
            _EventRegistry.Add("PriceCheck", PriceCheck_Handler);
            _EventRegistry.Add("FindCommodity", FindCommodity_Handler);
            _EventRegistry.Add("CancelSpeech", CancelSpeech_Handler);
            _EventRegistry.Add("Instructions", Instruction_Handler);
            _EventRegistry.Add("StartListening", StartListening_Handler);
            _EventRegistry.Add("StopListening", StopListening_Handler);
            _EventRegistry.Add("TypeLastSpelled", TypeLastSpelled_Handler);
            _EventRegistry.Add("EraseField", EraseField_Handler);
            _EventRegistry.Add("TypeDictation", TypeDictation_Handler);
            _EventRegistry.Add("TypeNato", TypeNato_Handler);
            _EventRegistry.Add("TypeSystem", TypeSystem_Handler);
            _EventRegistry.Add("TypeCurrentSystem", TypeCurrentSystem_Handler);
            _EventRegistry.Add("StationDistance", StationDistance_Handler);
            _EventRegistry.Add("SayCurrentSystem", SayCurrentSystem_Handler);
            _EventRegistry.Add("SayCurrentVersion", SayCurrentVersion_Handler); 
        }
#region DebugPriceCheck
        public void DBG_CompileGrammars()
        {
            _Eddb.DBG_CompileGrammars();
        }
#endregion

        //TODO: Abstract all the boilerplate semantic value code in to the
        // RecognitionResult class, or a subclass of it. This would include
        // values such as distance, stations, systems, ids. There's a lot of
        // copy pasting going on here. -SingularTier

#region Grammar Rule Handlers
        public void Greetings_Handler(AerRecognitionResult result)
        {
            _Talk.RandomGreetings();
        }
        public void AerQuery_Handler(AerRecognitionResult result)
        {
            _Talk.RandomQueryAck();
        }
        public void SearchWiki_Handler(AerRecognitionResult result)
        {
            string word = result.Data;
            _Talk.SayBlocking("Searching the wiki for " + word);
            _Talk.Say(_Wikipedia.Query(word));
        }
        public void BrowseGalnet_Handler(AerRecognitionResult result)
        {
            if(_GalnetRSS.Loaded)
                _Talk.Say(_GalnetRSS.Entries[_GalnetEntry].Title);
            else
                _Talk.RandomNack();
        }
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
        public void ReadArticle_Handler(AerRecognitionResult result)
        {
            if (_GalnetRSS.Loaded)
                _Talk.Say(_GalnetRSS.Entries[_GalnetEntry].Description);
            else
                _Talk.RandomNack();
        }
        public void TellJoke_Handler(AerRecognitionResult result)
        {
            _Talk.Say(_JokeRSS.Entries[_JokeEntry].Description);
            _JokeEntry++;
            if (_JokeEntry >= _JokeRSS.Entries.Count)
                _JokeEntry = 0;
        }
        public void Instruction_Handler(AerRecognitionResult result)
        {
            _Talk.SayInstructions();
        }
        public void SystemInfo_Handler(AerRecognitionResult result)
        {
            if(result.System != null)
                _Talk.SaySystem(result.System);
            else
                _Talk.RandomUnknownAck();
        }
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
        public void SetLocalSystem_Handler(AerRecognitionResult result)
        {
            if (result.System != null)
            {
                _Talk.SaySetSystem(result.System);
                _LocalSystem = result.System;
            }
            else
                _Talk.RandomUnknownAck();
        }
        public void StarDistance_Handler(AerRecognitionResult result)
        {
            if ((result.System != null) && (_LocalSystem != null))
            {

                _Talk.SayDistance(Math.Sqrt(_Eddb.DistanceSqr(result.System, _LocalSystem)));
            }
            else
            {
                _Talk.RandomUnknownAck();
            }
        }
        public void AerCapabilities_Handler(AerRecognitionResult result)
        {
            _Talk.SayCapabilities();
        }
        public void AerCreatorInfo_Handler(AerRecognitionResult result)
        {
            _Talk.SayCreaterInfo();
        }
        public void AerIdentity_Handler(AerRecognitionResult result)
        {
            _Talk.SayIdentity();
        }
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
        public void FindCommodity_Handler(AerRecognitionResult result)
        {
            if (_LocalSystem != null)
            {
                EliteStation est = _Eddb.FindCommodity(result.Commodity.id, _LocalSystem, 250);
                if (est != null)
                    _Talk.SayFoundCommodity(result.Commodity, est);
                else
                    _Talk.SayCannotFindCommodity(result.Commodity);
            }
            else
                _Talk.SayUnknownLocation();
        }
        public void CancelSpeech_Handler(AerRecognitionResult result)
        {
            _Talk.RandomAck();
        }
        public void StartListening_Handler(AerRecognitionResult result)
        {
            _Squelched = false;
            _Talk.SayStartListening();

        }
        public void StopListening_Handler(AerRecognitionResult result)
        {
            _Squelched = true;
            _Talk.SayStopListening();
        }
        public void TypeLastSpelled_Handler(AerRecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.Type(_Talk.LastSpelledWord);
        }
        public void EraseField_Handler(AerRecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.ClearField();
        }
        public void TypeDictation_Handler(AerRecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.Type(result.Data);
        }
        public void TypeNato_Handler(AerRecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.Type(result.Data);
        }
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
        public void TypeCurrentSystem_Handler(AerRecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.Type(_LocalSystem.Name);
        }
        public void StationDistance_Handler(AerRecognitionResult result)
        {
            if(result.System != null)
            {
                if(result.Station != null)
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
        public void SayCurrentSystem_Handler(AerRecognitionResult result)
        {
            if (_LocalSystem != null)
                _Talk.Say(_LocalSystem.Name);
            else
                _Talk.SayUnknownLocation();

        }
        public void SayCurrentVersion_Handler(AerRecognitionResult result)
        {
            _Talk.Say("1.1");

        }
#endregion
    }
}

