using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AerSpeech
{
    /// <summary>
    /// Defines an Elite System
    /// </summary>
    public class EliteSystem
    {
        public string Name;
        public int id;
        public float x;
        public float y;
        public float z;
        public string Population;
        public string Faction;
        public string Government;
        public string Allegiance;
        public string State;
        public string Security;
        public string PrimaryEconomy;
        public bool PermitRequired;
        public List<EliteStation> Stations;

        public EliteSystem()
        {
            Stations = new List<EliteStation>();
        }
    }
    /// <summary>
    /// Defines an Elite Station
    /// </summary>
    public class EliteStation
    {
        public string Name;
        public int id;
        public EliteSystem System;
        public string MaxPadSize;
        public int DistanceFromStar;
        public string Faction;
        public string Government;
        public string Allegiance;
        public string State;
        public string StarportType;
        public bool HasBlackmarket;
        public bool HasCommodities;
        public bool HasRefuel;
        public bool HasRepair;
        public bool HasRearm;
        public bool HasOutfitting;
        public bool HasShipyard;
        public long UpdatedAt;
        public List<EliteCommodity> Imports;
        public List<EliteCommodity> Exports;
        public List<EliteCommodity> Prohibited;
        public List<EliteListing> Listings;
        public List<string> Economies;

        public EliteStation()
        {
            Imports = new List<EliteCommodity>();
            Exports = new List<EliteCommodity>();
            Prohibited = new List<EliteCommodity>();
            Listings = new List<EliteListing>();
            Economies = new List<string>();
        }
    }

    /// <summary>
    /// Defines an Elite commodity
    /// </summary>
    public class EliteCommodity
    {
        public string Name;
        public int id;
        public int AveragePrice;
    }

    /// <summary>
    /// Defines an Elite Listing
    /// </summary>
    public class EliteListing
    {
        public int id;
        public EliteCommodity Commodity;
        //public EliteStation Station;
        public int StationId; //Because I'm lazy at the moment.
        public int Supply;
        public int Demand;
        public int BuyPrice;
        public int SellPrice;
        public long UpdatedAt;
        public int UpdateCount;
    }

    /// <summary>
    /// Maintains a collection of Data from EDDB (or whereever).
    /// Primary source of in-game data. This encapsulates the rest of Aer from the backend.
    /// Parses JSON files.
    /// </summary>
    /// <remarks>
    /// THIS IS THE MESSIEST CODE IM SORRY ITS WHERE IM WORKING THE MOST.
    /// This used to use SQL, but SQLite wasn't powerful enough to replace LINQ. A full database would be nice, but...
    /// </remarks>
    public class AerDB
    {
        private Dictionary<int, EliteSystem> _SystemRegistry;
        private Dictionary<string, int> _SystemNameRegistry;
        private Dictionary<int, EliteCommodity> _CommodityRegistry;
        private Dictionary<string, int> _CommodityNameRegistry;
        public bool Loaded = false;

        public AerDB(string systemsJson, string stationsJson, string commoditiesJson)
        {
            _SystemRegistry = new Dictionary<int, EliteSystem>();
            _SystemNameRegistry = new Dictionary<string, int>();
            _CommodityRegistry = new Dictionary<int, EliteCommodity>();
            _CommodityNameRegistry = new Dictionary<string, int>();

            if (File.Exists(systemsJson) && File.Exists(commoditiesJson) && File.Exists(stationsJson))
            {
                try
                {
                    _ParseSystems(File.ReadAllText(systemsJson));
                    _ParseCommodities(File.ReadAllText(commoditiesJson));
                    _ParseStations(File.ReadAllText(stationsJson));
                }
                catch (Exception e)
                {
                    AerDebug.LogError("Encountered a problem parsing EDDB json files");
                    AerDebug.LogException(e);
                }
            }
            else
            {
                AerDebug.LogError("Could not find JSON files!");
            }
            Loaded = true;
        }

//This is getting out of hand for the AerDB file, perhaps a static AerJSON should be created
// As a container for all of these darn utility methods
//TODO: Make AerEddb or AerJSON and create an interface between AerDB and it. -SingularTier
//WARNING: THE CODE IN THE JSON PARSING REGION WILL MAKE YOU VOMIT.
#region JSON Parsing
        /// <summary>
        /// Populates the System Registry with data from the eddb json string
        /// </summary>
        /// <param name="json">Json string of EDDB systems.json data</param>
        private void _ParseSystems(string json)
        {
            AerDebug.Log("Loading Systems...");
            Stopwatch timer = new Stopwatch();
            timer.Start();
            JArray ja = JArray.Parse(json);

            foreach (JObject jo in ja)
            {
                try
                {
                    EliteSystem es = new EliteSystem();
                    es.Name = jo["name"].ToString();
                    es.id = int.Parse(jo["id"].ToString());
                    es.x = float.Parse(jo["x"].ToString());
                    es.y = float.Parse(jo["y"].ToString());
                    es.z = float.Parse(jo["z"].ToString());
                    es.Faction = jo["faction"].ToString();
                    es.Population = jo["population"].ToString();
                    es.Government = jo["government"].ToString();
                    es.Allegiance = jo["allegiance"].ToString();
                    es.State = jo["state"].ToString();
                    es.Security = jo["security"].ToString();
                    es.PrimaryEconomy = jo["primary_economy"].ToString();

                    string permit = jo["needs_permit"].ToString();
                    es.PermitRequired = permit.Equals("1");

                    _SystemRegistry.Add(es.id, es);
                    _SystemNameRegistry.Add(es.Name.ToLower(), es.id);
                }
                catch (FormatException e)
                {
                    AerDebug.LogError("Malformed/Unexpected System JSON data, " + e.Message);
                }
            
            }
            timer.Stop();
        }
        /// <summary>
        /// Populates the Commodity Registry with data from the eddb json string
        /// </summary>
        /// <param name="json">Json string of EDDB commodities.json data</param>
        private void _ParseCommodities(string json)
        {
            AerDebug.Log("Loading Commodities...");
            Stopwatch timer = new Stopwatch();
            timer.Start();
            JArray ja = JArray.Parse(json);

            foreach (JObject jo in ja)
            {
                EliteCommodity ec = new EliteCommodity();
                ec.Name = jo["name"].ToString();
                try
                {
                    ec.AveragePrice = int.Parse(jo["average_price"].ToString());
                }
                catch (FormatException e)
                {
                    AerDebug.LogError(@"Error formatting Average Price for "+ ec.Name +", " + e.Message);
                    ec.AveragePrice = -1;
                }

                ec.id = int.Parse(jo["id"].ToString());
                _CommodityRegistry.Add(ec.id, ec);
                _CommodityNameRegistry.Add(ec.Name.ToLower(), ec.id);

            }

            timer.Stop();
        }
        /// <summary>
        /// Populates the Station Data with data from the eddb json string
        /// </summary>
        /// <param name="json">Json string of EDDB stations.json data</param>
        private void _ParseStations(string json)
        {
            JsonTextReader jsonReader = new JsonTextReader(new StringReader(json));
            int arrayDepth = 0;

            while(jsonReader.Read())
            {
                switch(jsonReader.TokenType)
                {
                    case JsonToken.StartArray:
                        arrayDepth++;
                        break;
                    case JsonToken.StartObject:
                        try
                        {
                            EliteStation es = _ParseJsonStation(jsonReader);
                            es.System.Stations.Add(es);
                        }
                        catch (Exception e)
                        {
                            AerDebug.LogError("Encountered a problem parsing stations.");
                            AerDebug.LogException(e);
                        }
                        break;
                    case JsonToken.EndArray:
                        arrayDepth--;
                        break;
                    default:
                        AerDebug.LogError("Unknown JSON TokenType: " + jsonReader.TokenType);
                        break;
                }   
            }

            if(arrayDepth != 0)
            {
                AerDebug.LogError("Malformed JSON parsing - arrayDepth == " + arrayDepth + " at end of parse");
            }
        }


        /// <summary>
        /// Returns an EliteStation from a EDDB Json Object.
        /// JsonReader MUST currently point to the StartObject token for the Station object.
        /// </summary>
        /// <param name="jsonReader">JsonReader populated with the Station Object</param>
        /// <returns>Populated EliteStation data</returns>
        private EliteStation _ParseJsonStation(JsonTextReader jsonReader)
        {
            EliteStation es = new EliteStation();

            if (jsonReader.TokenType != JsonToken.StartObject)
                AerDebug.LogError("Malformed JSON parsing - _ParseJsonStation must be called on a StartObject token");

            while(jsonReader.TokenType != JsonToken.EndObject)
            {
                jsonReader.Read();
                switch(jsonReader.TokenType)
                {
                    case JsonToken.PropertyName:
                        switch(jsonReader.Value.ToString())
                        {
                            case "id":
                                es.id = jsonReader.ReadAsInt32().GetValueOrDefault();
                                break;
                            case "name":
                                es.Name = jsonReader.ReadAsString();
                                break;
                            case "system_id":
                                es.System = GetSystem(jsonReader.ReadAsInt32().GetValueOrDefault());
                                break;
                            case "max_landing_pad_size":
                                es.MaxPadSize = jsonReader.ReadAsString();
                                break;
                            case "distance_to_star":
                                es.DistanceFromStar = jsonReader.ReadAsInt32().GetValueOrDefault();
                                break;
                            case "faction":
                                es.Faction = jsonReader.ReadAsString();
                                break;
                            case "government":
                                es.Government = jsonReader.ReadAsString();
                                break;
                            case "allegiance":
                                es.Allegiance = jsonReader.ReadAsString();
                                break;
                            case "state":
                                es.State = jsonReader.ReadAsString();
                                break;
                            case "type":
                                es.StarportType = jsonReader.ReadAsString();
                                break;
                            case "has_blackmarket":
                                es.HasBlackmarket = (jsonReader.ReadAsInt32().GetValueOrDefault() == 1);
                                break;
                            case "has_commodities":
                                es.HasCommodities = (jsonReader.ReadAsInt32().GetValueOrDefault() == 1);
                                break;
                            case "has_refuel":
                                es.HasRefuel = (jsonReader.ReadAsInt32().GetValueOrDefault() == 1);
                                break;
                            case "has_repear":
                                es.HasRepair = (jsonReader.ReadAsInt32().GetValueOrDefault() == 1);
                                break;
                            case "has_rearm":
                                es.HasRearm = (jsonReader.ReadAsInt32().GetValueOrDefault() == 1);
                                break;
                            case "has_outfitting":
                                es.HasOutfitting = (jsonReader.ReadAsInt32().GetValueOrDefault() == 1);
                                break;
                            case "has_shipyard":
                                es.HasShipyard = (jsonReader.ReadAsInt32().GetValueOrDefault() == 1);
                                break;
                            case "import_commodities":
                                jsonReader.Read();
                                es.Imports = _ParseJsonCommodities(jsonReader);
                                break;
                            case "export_commodities":
                                jsonReader.Read();
                                es.Exports = _ParseJsonCommodities(jsonReader);
                                break;
                            case "prohibited_commodities":
                                jsonReader.Read();
                                es.Prohibited = _ParseJsonCommodities(jsonReader);
                                break;
                            case "economies":
                                jsonReader.Read();
                                es.Economies = _ParseJsonEconomies(jsonReader);
                                break;
                            case "updated_at":
                                es.UpdatedAt = jsonReader.ReadAsInt32().GetValueOrDefault();
                                break;
                            case "listings":
                                jsonReader.Read();
                                es.Listings = _ParseJsonListing(jsonReader);
                                break;
                            default:
                                break;
                        }
                        break;
                    case JsonToken.EndObject:
                        break;
                }
            }

            return es;
            
        }

        /// <summary>
        /// Returns a List of EliteListing objects from a EDDB Json Array.
        /// JsonReader MUST currently point to the StartArray token for the Listing Array.
        /// </summary>
        /// <param name="jsonReader">JsonReader populated with the Listing Array</param>
        /// <returns>List of populated EliteListing Data</returns>
        private List<EliteListing> _ParseJsonListing(JsonTextReader jsonReader)
        {
            List<EliteListing> listings = new List<EliteListing>();
            if (jsonReader.TokenType != JsonToken.StartArray)
            {
                AerDebug.LogError("_ParseJsonListing must be called at the start of an Array");
                return null;
            }
            EliteListing currentListing = null;

            while (jsonReader.TokenType != JsonToken.EndArray)
            {
                jsonReader.Read();
                switch (jsonReader.TokenType)
                {
                    case JsonToken.StartObject:
                        currentListing = new EliteListing();
                        break;
                    case JsonToken.EndObject:
                        listings.Add(currentListing);
                        break;
                    case JsonToken.PropertyName:
                        switch (jsonReader.Value.ToString())
                        {
                            case "id":
                                currentListing.id = jsonReader.ReadAsInt32().GetValueOrDefault();
                                break;
                            case "station_id":
                                currentListing.StationId = jsonReader.ReadAsInt32().GetValueOrDefault();
                                break;
                            case "commodity_id":
                                currentListing.Commodity = GetCommodity(jsonReader.ReadAsInt32().GetValueOrDefault());
                                break;
                            case "supply":
                                currentListing.Supply = jsonReader.ReadAsInt32().GetValueOrDefault();
                                break;
                            case "buy_price":
                                currentListing.BuyPrice = jsonReader.ReadAsInt32().GetValueOrDefault();
                                break;
                            case "sell_price":
                                currentListing.SellPrice = jsonReader.ReadAsInt32().GetValueOrDefault();
                                break;
                            case "demand":
                                currentListing.Demand = jsonReader.ReadAsInt32().GetValueOrDefault();
                                break;
                            case "collected_at":
                                currentListing.UpdatedAt = jsonReader.ReadAsInt32().GetValueOrDefault();
                                break;
                            case "update_count":
                                currentListing.UpdateCount = jsonReader.ReadAsInt32().GetValueOrDefault();
                                break;
                            default:
                                AerDebug.LogError("Unknown JSON property name: " + jsonReader.Value.ToString());
                                break;
                        }
                        break;
                    case JsonToken.EndArray:
                        break;
                    default:
                        AerDebug.LogError("Unknown token type in listing list, " + jsonReader.TokenType);
                        break;
                }
            }
            return listings;
        }
        /// <summary>
        /// Returns a List of EliteCommodity objects from a EDDB Json Array.
        /// JsonReader MUST currently point to the StartArray token for the Commodity Array.
        /// Pulls Commodity Data out of the CommodityRegistry
        /// Used for import/export/prohibited commodity lists
        /// </summary>
        /// <param name="jsonReader">JsonReader populated with the Commodity Array</param>
        /// <returns>List of populated EliteCommodity Data</returns>
        private List<EliteCommodity> _ParseJsonCommodities(JsonTextReader jsonReader)
        {
            List<EliteCommodity> commodities = new List<EliteCommodity>();

            if(jsonReader.TokenType != JsonToken.StartArray)
            {
                AerDebug.LogError("_ParseJsonCommodities must be called at the start of an Array");
                return null;
            }

            while(jsonReader.TokenType != JsonToken.EndArray)
            {
                jsonReader.Read();
                switch(jsonReader.TokenType)
                {
                    case JsonToken.String:
                        commodities.Add(GetCommodity(jsonReader.Value.ToString()));
                        break;
                    case JsonToken.EndArray:
                        break;
                    default:
                        AerDebug.LogError("Unknown token type in commodities list, " + jsonReader.TokenType);
                        break;
                }
            }

            return commodities;
        }

        /// <summary>
        /// Returns a List of strings from a EDDB Json Array.
        /// JsonReader MUST currently point to the StartArray token for the string Array.
        /// Used to pull Economies out of the economy list in station data.
        /// </summary>
        /// <param name="jsonReader">JsonReader populated with the string Array</param>
        /// <returns>List of strings that define the station economies</returns>
        private List<string> _ParseJsonEconomies(JsonTextReader jsonReader)
        {
            List<string> econs = new List<string>();
            if (jsonReader.TokenType != JsonToken.StartArray)
            {
                AerDebug.LogError("_ParseJsonEconomies must be called at the start of an Array");
                return null;
            }

            while (jsonReader.TokenType != JsonToken.EndArray)
            {
                jsonReader.Read();
                switch (jsonReader.TokenType)
                {
                    case JsonToken.String:
                        econs.Add(jsonReader.Value.ToString());
                        break;
                    case JsonToken.EndArray:
                        break;
                    default:
                        AerDebug.LogError("Unknown token type in economies list, " + jsonReader.TokenType);
                        break;
                }
            }
            return econs;
        }

#endregion
#if DEBUG
        //This creates grammar rules out of our data
        public void DBG_CompileGrammars()
        {

            Dictionary<string, bool> stationAdded = new Dictionary<string, bool>();

            System.IO.StreamWriter file;
            AerDebug.Log("Compiling grammars...");
            AerDebug.Log("Compiling grammars for systems...");

            file = new System.IO.StreamWriter(@"systemsGrammar.xml");
            foreach(EliteSystem system in _SystemRegistry.Values)
            {
                file.WriteLine(@"<item> " + stripForXML(system.Name) + " <tag> out.id=" + system.id + "; out.Name=\"" + stripForXML(system.Name) + "\"; </tag></item>");
            }
            file.Close();

            AerDebug.Log("Compiling grammars for commodities...");
            file = new System.IO.StreamWriter(@"commoditiesGrammar.xml");
            foreach (EliteCommodity commodity in _CommodityRegistry.Values)
            {
                file.WriteLine(@"<item> " + stripForXML(commodity.Name) + " <tag> out.id=" + commodity.id + "; </tag></item>");
            }
            file.Close();

            AerDebug.Log("Compiling grammars for stations...");
            file = new System.IO.StreamWriter(@"stationsGrammar.xml");
            foreach (EliteSystem system in _SystemRegistry.Values)
            {
                foreach (EliteStation station in system.Stations)
                {
                    if (!stationAdded.ContainsKey(station.Name))
                    {
                        file.WriteLine(@"<item> " + stripForXML(station.Name) + " <tag> out.Name=\"" + stripForXML(station.Name) + "\";</tag></item>");
                        stationAdded.Add(station.Name, true);
                    }
                }
            }
            file.Close();
        }
        public string stripForXML(string input)
        {
            string output;

            output = Regex.Replace(input, "&", "&amp;");

            return output;
        }
#endif

#region Registry Access Methods
        public EliteSystem GetSystem(int system_id)
        {
            if (_SystemRegistry.ContainsKey(system_id))
                return _SystemRegistry[system_id];
            else
                return null;
        }
        public EliteSystem GetSystem(string name)
        {
            int system_id;

            if (name == null)
                return null;

            if (_SystemNameRegistry.ContainsKey(name.ToLower()))
                system_id = _SystemNameRegistry[name.ToLower()];
            else
                return null;

            return GetSystem(system_id);
        }
        public EliteCommodity GetCommodity(string commodity)
        {
            int commodity_id;
            if (_CommodityNameRegistry.ContainsKey(commodity.ToLower()))
                commodity_id = _CommodityNameRegistry[commodity.ToLower()];
            else
                return null;

            return GetCommodity(commodity_id);
        }
        public EliteCommodity GetCommodity(int commodity_id)
        {
            if (_CommodityRegistry.ContainsKey(commodity_id))
                return _CommodityRegistry[commodity_id];
            else
                return null;
        }
#endregion

#region Utility Methods
        public EliteStation GetStation(EliteSystem es, string stationName)
        {
            List<EliteStation> matchingStations;

            var est = from station in es.Stations
                               where station.Name.Equals(stationName)
                               select station;

            matchingStations = est.ToList<EliteStation>();

            if (matchingStations.Count > 1)
            {
                AerDebug.LogError(@"Found " + matchingStations.Count +" stations with the name '" + stationName + "' in system '" + es.Name + "'");
            }
            if (matchingStations.Count > 0)
                return matchingStations[0];
            else
                return null;
        }
        public int GetPrice(int commodity_id)
        {
            EliteCommodity ec = GetCommodity(commodity_id);
            if(ec == null)
            {
                return -1;
            }
            else
            {
                return ec.AveragePrice;
            }
        }
        public EliteStation FindCommodity(int commodity_id, EliteSystem origin, float distance)
        {
            List<EliteSystem> nearbySystems = GetSystemsAround(origin, distance);

            EliteStation closest = null;
            List<EliteStation> stationsWithCommodity = new List<EliteStation>(); ;
            foreach(EliteSystem sys in nearbySystems)
            {
                var validStations = from stations in sys.Stations
                                   from listing in stations.Listings
                                    where ((listing.Commodity.id == commodity_id) && (listing.Supply > 0) && ((listing.SellPrice > 0)))
                                   select stations;

                stationsWithCommodity.AddRange(validStations);
            }

            double closestDistance = 1000000; //Infinite
            foreach (EliteStation es in stationsWithCommodity)
            {
                double thisDistance = DistanceSqr(es.System, origin);
                if (thisDistance <= closestDistance)
                {
                    //If the two stations are in the same system, choose the one closest to the star
                    if ((closest != null) && (es.System.id == closest.System.id))
                    {
                        if(es.DistanceFromStar < closest.DistanceFromStar)
                        {
                            closest = es;
                            closestDistance = thisDistance;
                        }
                    }
                    else
                    {
                        closest = es;
                        closestDistance = thisDistance;
                    }
                }
            }

            return closest;
        }
        public List<EliteSystem> GetSystemsAround(EliteSystem origin, float distance)
        {
            float distanceSqr = distance * distance;
            var SystemsInRange = from system in _SystemRegistry.Values
                                 where  DistanceSqr(system, origin) < distanceSqr
                                 select system;

            return SystemsInRange.ToList<EliteSystem>();
        }
        public double DistanceSqr(EliteSystem es1, EliteSystem es2)
        {
            float x = es1.x - es2.x;
            float y = es1.y - es2.y;
            float z = es1.z - es2.z;

            return (Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));
        }
#endregion
    }
}
