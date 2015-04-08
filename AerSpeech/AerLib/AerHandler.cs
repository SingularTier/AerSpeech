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
            _Eddb = new AerDB();
            _EventRegistry = new Dictionary<string, AerInputHandler>();


            if (File.Exists(systemsJson) && File.Exists(commoditiesJson) && File.Exists(stationsJson))
            {
                _Eddb.ParseSystems(File.ReadAllText(systemsJson));
                _Eddb.ParseCommodities(File.ReadAllText(commoditiesJson));
                _Eddb.ParseStations(File.ReadAllText(stationsJson));
            }
            else
            {
                AerDebug.LogError("Could not find JSON files!");
            }

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
            _EventRegistry.Add("AerSetLocalSystem", AerSetLocalSystem_Handler);
            _EventRegistry.Add("AerDistance", AerDistance_Handler);
            _EventRegistry.Add("AerCapabilities", AerCapabilities_Handler);
            _EventRegistry.Add("AerCreatorInfo", AerCreatorInfo_Handler);
            _EventRegistry.Add("AerIdentity", AerIdentity_Handler);
            _EventRegistry.Add("AerPriceCheck", AerPriceCheck_Handler);
            _EventRegistry.Add("AerFindCommodity", AerFindCommodity_Handler);
            _EventRegistry.Add("CancelSpeech", AerCancelSpeech_Handler);
            _EventRegistry.Add("Instructions", Instruction_Handler);
            _EventRegistry.Add("AerStartListening", AerStartListening_Handler);
            _EventRegistry.Add("AerStopListening", AerStopListening_Handler);
            _EventRegistry.Add("AerTypeLastSpelled", AerTypeLastSpelled_Handler);
            _EventRegistry.Add("AerEraseField", AerEraseField_Handler);
            _EventRegistry.Add("AerTypeDictation", AerTypeDictation_Handler);
            _EventRegistry.Add("AerTypeNato", AerTypeNato_Handler);
            _EventRegistry.Add("AerTypeSystem", AerTypeSystem_Handler);
            _EventRegistry.Add("AerTypeCurrentSystem", AerTypeCurrentSystem_Handler); 
            _EventRegistry.Add("AerSayCurrentSystem", AerSayCurrentSystem_Handler);
            _EventRegistry.Add("AerSayCurrentVersion", AerSayCurrentVersion_Handler); 
            
        }

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
                int idvalid = int.Parse(result.Semantics["IDValid"].Value.ToString());
                int system_id;

                if (idvalid == 1)
                {
                    system_id = int.Parse(result.Semantics["id"].Value.ToString());
                    es = _Eddb.GetSystem(system_id);
                }
                else
                {
                    string systemName = result.Semantics["SystemName"].Value.ToString();
                    Console.WriteLine("SystemName=" + systemName);
                    es = _Eddb.GetSystem(systemName);
                }


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
        public void AerSetLocalSystem_Handler(RecognitionResult result)
        {
            EliteSystem es = null;

            try
            {
                int idvalid = int.Parse(result.Semantics["IDValid"].Value.ToString());
                int system_id;

                if (idvalid == 1)
                {
                    system_id = int.Parse(result.Semantics["id"].Value.ToString());
                    es = _Eddb.GetSystem(system_id);
                }
                else
                {
                    string systemName = result.Semantics["SystemName"].Value.ToString();
                    Console.WriteLine("SystemName=" + systemName);
                    es = _Eddb.GetSystem(systemName);
                }

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
        public void AerDistance_Handler(RecognitionResult result)
        {
            EliteSystem es = null;

            try 
            {
                int idvalid = int.Parse(result.Semantics["IDValid"].Value.ToString());
                int system_id;

                if (idvalid == 1)
                {
                    system_id = int.Parse(result.Semantics["id"].Value.ToString());
                    es = _Eddb.GetSystem(system_id);
                }
                else
                {
                    string systemName = result.Semantics["SystemName"].Value.ToString();
                    Console.WriteLine("SystemName=" + systemName);
                    es = _Eddb.GetSystem(systemName);
                }

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
        public void AerPriceCheck_Handler(RecognitionResult result)
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
        public void AerFindCommodity_Handler(RecognitionResult result)
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
        public void AerCancelSpeech_Handler(RecognitionResult result)
        {
            _Talk.RandomAck();
        }
        public void AerStartListening_Handler(RecognitionResult result)
        {
            _Talk.SayStartListening();
            _Squelched = false;
        }
        public void AerStopListening_Handler(RecognitionResult result)
        {
            _Talk.SayStopListening();
            _Squelched = true;
        }
        public void AerTypeLastSpelled_Handler(RecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.Type(_Talk.LastSpelledWord);
        }
        public void AerEraseField_Handler(RecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.ClearField();
        }
        public void AerTypeDictation_Handler(RecognitionResult result)
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
        public void AerTypeNato_Handler(RecognitionResult result)
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
        public void AerTypeSystem_Handler(RecognitionResult result)
        {
            try
            {
                _Talk.RandomAck();
                int system_id = int.Parse(result.Semantics["Data"].Value.ToString());
                EliteSystem es = _Eddb.GetSystem(system_id);
                _Keyboard.Type(es.Name);
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
        public void AerTypeCurrentSystem_Handler(RecognitionResult result)
        {
            _Talk.RandomAck();
            _Keyboard.Type(_LocalSystem.Name);
        }
        public void AerSayCurrentSystem_Handler(RecognitionResult result)
        {
            if (_LocalSystem != null)
                _Talk.Say(_LocalSystem.Name);
            else
                _Talk.SayUnknownLocation();

        }
        public void AerSayCurrentVersion_Handler(RecognitionResult result)
        {
            _Talk.Say("1.1");

        }
#endregion
    }
}

