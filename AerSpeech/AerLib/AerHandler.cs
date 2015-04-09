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
    public delegate void AerInputHandler(RecognitionResult result);

    public class AerRecognitionResult
    {
        public double Confidence;
        public EliteStation Station;
        public EliteSystem System;
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

        /// <summary>
        /// Handles all speech results.
        /// This should be called in a thread that doesn't mind blocking for a long time.
        /// </summary>
        /// <param name="result"></param>
        public void DefaultInput_Handler(RecognitionResult result)
        {
            SemanticValue semantics = result.Semantics;
            if (semantics["Command"] != null)
            {
                string command = semantics["Command"].Value.ToString();
                if(_EventRegistry.ContainsKey(command))
                {
                    if (!_Squelched)
                        _EventRegistry[command](result);
                    else if(command.Equals("AerStartListening"))
                        _EventRegistry[command](result);
                }
                else
                {
                    AerDebug.LogError(@"Recieved command that didn't have a handler, '" + result.Text + "', command=" + command);
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
        public void Greetings_Handler(RecognitionResult result)
        {
            _Talk.RandomGreetings();
        }
        public void AerQuery_Handler(RecognitionResult result)
        {
            _Talk.RandomQueryAck();
        }
        public void SearchWiki_Handler(RecognitionResult result)
        {
            string word = result.Semantics["Data"].Value.ToString();
            _Talk.SayBlocking("Searching the wiki for " + word);
            _Talk.Say(_Wikipedia.Query(word));
        }
        public void BrowseGalnet_Handler(RecognitionResult result)
        {
            if(_GalnetRSS.Loaded)
                _Talk.Say(_GalnetRSS.Entries[_GalnetEntry].Title);
            else
                _Talk.RandomNack();
        }
        public void NextArticle_Handler(RecognitionResult result)
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
        public void ReadArticle_Handler(RecognitionResult result)
        {
            if (_GalnetRSS.Loaded)
                _Talk.Say(_GalnetRSS.Entries[_GalnetEntry].Description);
            else
                _Talk.RandomNack();
        }
        public void TellJoke_Handler(RecognitionResult result)
        {
            _Talk.Say(_JokeRSS.Entries[_JokeEntry].Description);
            _JokeEntry++;
            if (_JokeEntry >= _JokeRSS.Entries.Count)
                _JokeEntry = 0;
        }
        public void Instruction_Handler(RecognitionResult result)
        {
            _Talk.SayInstructions();
        }
        public void SystemInfo_Handler(RecognitionResult result)
        {
            EliteSystem es = null;
            try
            {
                string systemName = result.Semantics["SystemName"].Value.ToString();
                AerDebug.Log("SystemName=" + systemName);

                if (systemName.Equals("__local__"))
                    es = _LocalSystem;
                else
                    es = _Eddb.GetSystem(systemName);

                if (es != null)
                    _Talk.SaySystem(es);
                else
                    _Talk.RandomUnknownAck();
            }
            catch (FormatException e)
            {
                AerDebug.LogError(@"Could not format semantic result for '" + result.Text + "', " + e.Message);
            }
            catch (Exception e)
            {
                AerDebug.LogException(e);
            }
        }
        public void StationInfo_Handler(RecognitionResult result)
        {
            try
            {
                string systemName = result.Semantics["SystemName"].Value.ToString();
                string stationName = result.Semantics["StationName"].Value.ToString();

                EliteSystem es;

                if (systemName.Equals("__local__"))
                    es = _LocalSystem;
                else
                    es = _Eddb.GetSystem(systemName);

                if (es != null)
                {
                    EliteStation est = _Eddb.GetStation(es, stationName);
                    if (est != null)
                    {
                        _Talk.SayStation(est);
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
            catch (Exception e)
            {
                AerDebug.LogException(e);
            }
        }
        public void SetLocalSystem_Handler(RecognitionResult result)
        {
            EliteSystem es = null;

            try
            {
                string systemName = result.Semantics["SystemName"].Value.ToString();
                AerDebug.Log("SystemName=" + systemName);
                es = _Eddb.GetSystem(systemName);

                if (es != null)
                {
                    _Talk.SaySetSystem(es);
                    _LocalSystem = es;
                }
                else
                    _Talk.RandomUnknownAck();
            }
            catch (FormatException e)
            {
                AerDebug.LogError(@"Could not format semantic result for '" + result.Text + "', " + e.Message);
            }
            catch (Exception e)
            {
                AerDebug.LogException(e);
            }
        }
        public void StarDistance_Handler(RecognitionResult result)
        {
            EliteSystem es = null;

            try 
            {
                string systemName = result.Semantics["SystemName"].Value.ToString();
                AerDebug.Log("SystemName=" + systemName);

                if (systemName.Equals("__local__"))
                    es = _LocalSystem;
                else
                    es = _Eddb.GetSystem(systemName);

                if ((es != null) && (_LocalSystem != null))
                {

                    _Talk.SayDistance(Math.Sqrt(_Eddb.DistanceSqr(es, _LocalSystem)));
                }
                else
                {
                    _Talk.RandomUnknownAck();
                }
            }
            catch (FormatException e)
            {
                AerDebug.LogError(@"Could not format semantic result for '" + result.Text + "', " + e.Message);
            }
            catch (Exception e)
            {
                AerDebug.LogException(e);
            }
        }
        public void AerCapabilities_Handler(RecognitionResult result)
        {
            _Talk.SayCapabilities();
        }
        public void AerCreatorInfo_Handler(RecognitionResult result)
        {
            _Talk.SayCreaterInfo();
        }
        public void AerIdentity_Handler(RecognitionResult result)
        {
            _Talk.SayIdentity();
        }
        public void PriceCheck_Handler(RecognitionResult result)
        {
            try
            {
                int commodity_id = int.Parse(result.Semantics["Data"].Value.ToString());
                int price = _Eddb.GetPrice(commodity_id);
                if (price <= 0)
                {
                    _Talk.RandomUnknownAck();
                }
                else
                {
                    _Talk.Say(price.ToString());
                }
            }
            catch (FormatException e)
            {
                AerDebug.LogError(@"Could not format semantic result for '" + result.Text + "', " + e.Message);
            }
            catch (Exception e)
            {
                AerDebug.LogException(e);
            }
        }
        public void FindCommodity_Handler(RecognitionResult result)
        {
            EliteStation est;
            try
            {
                int commodity_id = int.Parse(result.Semantics["Data"].Value.ToString());

                if (_LocalSystem != null)
                {
                    est = _Eddb.FindCommodity(commodity_id, _LocalSystem, 250);
                    if (est != null)
                        _Talk.SayFoundCommodity(_Eddb.GetCommodity(commodity_id), est);
                    else
                        _Talk.SayCannotFindCommodity(_Eddb.GetCommodity(commodity_id));
                }
                else
                    _Talk.SayUnknownLocation();
            }
            catch (FormatException e)
            {
                AerDebug.LogError(@"Could not format semantic result for '" + result.Text + "', " + e.Message);
            }
            catch (Exception e)
            {
                AerDebug.LogException(e);
            }
        }
        public void CancelSpeech_Handler(RecognitionResult result)
        {
            _Talk.RandomAck();
        }
        public void StartListening_Handler(RecognitionResult result)
        {
            _Squelched = false;
            _Talk.SayStartListening();

        }
        public void StopListening_Handler(RecognitionResult result)
        {
            _Squelched = true;
            _Talk.SayStopListening();
        }
        public void TypeLastSpelled_Handler(RecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.Type(_Talk.LastSpelledWord);
        }
        public void EraseField_Handler(RecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.ClearField();
        }
        public void TypeDictation_Handler(RecognitionResult result)
        {
            try
            {
                _Talk.RandomAck();
                string SpokenText = result.Semantics["Data"].Value.ToString();
                _Keyboard.Type(SpokenText);
            }
            catch (FormatException e)
            {
                AerDebug.LogError(@"Could not format semantic result for '" + result.Text + "', " + e.Message);
            }
            catch (Exception e)
            {
                AerDebug.LogException(e);
            }
        }
        public void TypeNato_Handler(RecognitionResult result)
        {
            try
            {
                _Talk.RandomAck();
                string SpokenText = result.Semantics["Data"].Value.ToString();
                _Keyboard.Type(SpokenText);
            }
            catch (FormatException e)
            {
                AerDebug.LogError(@"Could not format semantic result for '" + result.Text + "', " + e.Message);
            }
            catch (Exception e)
            {
                AerDebug.LogException(e);
            }
        }
        public void TypeSystem_Handler(RecognitionResult result)
        {
            try
            {
                _Talk.RandomAck();
                string systemName = result.Semantics["SystemName"].Value.ToString();
                EliteSystem es;

                if (systemName.Equals("__local__"))
                    es = _LocalSystem;
                else
                    es = _Eddb.GetSystem(systemName);

                _Keyboard.Type(es.Name);
            }
            catch (Exception e)
            {
                AerDebug.LogException(e);
            }
        }
        public void TypeCurrentSystem_Handler(RecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.Type(_LocalSystem.Name);
        }
        public void StationDistance_Handler(RecognitionResult result)
        {
            try
            {
                string systemName = result.Semantics["SystemName"].Value.ToString();
                string stationName = result.Semantics["StationName"].Value.ToString();

                EliteSystem es;

                if (systemName.Equals("__local__"))
                    es = _LocalSystem;
                else
                    es = _Eddb.GetSystem(systemName);

                if(es != null)
                {
                    EliteStation est = _Eddb.GetStation(es, stationName);
                    if(est != null)
                    {
                        _Talk.SayStationDistance(est);
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
            catch (Exception e)
            {
                AerDebug.LogException(e);
            }
        }
        public void SayCurrentSystem_Handler(RecognitionResult result)
        {
            if (_LocalSystem != null)
                _Talk.Say(_LocalSystem.Name);
            else
                _Talk.SayUnknownLocation();

        }
        public void SayCurrentVersion_Handler(RecognitionResult result)
        {
            _Talk.Say("1.1");

        }
#endregion
    }
}

