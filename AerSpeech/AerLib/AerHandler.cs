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
    /// Represents the data parsed out of the Grammar file by the handler
    /// </summary>
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
    /// Default Handler for incoming RecognitionResults. Populates AerRecognitionResult and
    /// hands the data off to the personality. Advanced tactics for massaging grammar/data before
    /// handing it off should be done here. This include homophone parsing, confidence heuristics/state,
    /// Last/Previous substitions, dictation corrections, etc.
    /// </summary>
    /// <remarks>
    /// TODO: Remove some dependency Personality or make it an interface. -SingularTier
    /// </remarks>
    public class AerHandler
    {
        private AerDB _Data;
        private Personality _Personality;

        public AerHandler(AerDB data, Personality personality)
        {
            _Data = data;
            _Personality = personality;
        }

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
                    output.Commodity = _Data.GetCommodity(commodityId);
                    numberOfSemantics++;
                }

                if (input.Semantics.ContainsKey("SystemName") && input.Semantics["SystemName"] != null)
                {
                    string systemName = input.Semantics["SystemName"].Value.ToString();

                    if (systemName.Equals("__last__"))
                    {
                        output.System = _Personality.LastSystem; 
                    }

                    else
                    {
                        if (systemName.Equals("__local__"))
                            output.System = _Personality.LocalSystem;
                        else
                            output.System = _Data.GetSystem(systemName);
                    }

                    _Personality.LastSystem = output.System;  //TODO: Remove this somehow

                    numberOfSemantics++;
                }

                if (input.Semantics.ContainsKey("FromSystem") && input.Semantics["FromSystem"] != null)
                {
                    string systemName = input.Semantics["FromSystem"].Value.ToString();

                    if (systemName.Equals("__last__"))
                    {
                        output.FromSystem = _Personality.LastSystem;
                    }
                    else
                    {
                        if (systemName.Equals("__local__"))
                            output.FromSystem = _Personality.LocalSystem;
                        else
                            output.FromSystem = _Data.GetSystem(systemName);
                    }
                    numberOfSemantics++;
                }

                if (input.Semantics.ContainsKey("ToSystem") && input.Semantics["ToSystem"] != null)
                {
                    string systemName = input.Semantics["ToSystem"].Value.ToString();

                    if (systemName.Equals("__last__"))
                    {
                        output.ToSystem = _Personality.LastSystem;
                    }
                    else
                    {
                        if (systemName.Equals("__local__"))
                            output.ToSystem = _Personality.LocalSystem;
                        else
                            output.ToSystem = _Data.GetSystem(systemName);
                    }
                    numberOfSemantics++;
                }

                if (input.Semantics.ContainsKey("StationName") && input.Semantics["StationName"] != null)
                {
                    string station = input.Semantics["StationName"].Value.ToString();

                    if(station.Equals("__last__")){
                        output.Station = _Personality.LastStation;
                    }

                    if (output.System != null)
                    {
                        output.Station = _Data.GetStation(output.System, station);

                        _Personality.LastStation = output.Station; //TODO: Remove this somehow
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
        public void InputHandler(RecognitionResult result)
        {
            AerRecognitionResult input = BuildAerRecognition(result);
            _Personality.RecognizedInput(input);
        }

#region Debug
#if DEBUG
        public void DBG_CompileGrammars()
        {
            _Eddb.DBG_CompileGrammars();
        }
#endif
#endregion



    }
}

