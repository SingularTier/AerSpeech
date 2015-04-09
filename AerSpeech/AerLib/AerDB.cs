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
    public class EliteStation
    {
        public string Name;
        public int id;
        public EliteSystem System;
        public string MaxPadSize;
        public string DistanceFromStar;
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
        public List<EliteCommodity> Imports;
        public List<EliteCommodity> Exports;

        public EliteStation()
        {
            Imports = new List<EliteCommodity>();
            Exports = new List<EliteCommodity>();
        }
    }
    public class EliteCommodity
    {
        public string Name;
        public int id;
        public int AveragePrice;
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
                _ParseSystems(File.ReadAllText(systemsJson));
                _ParseCommodities(File.ReadAllText(commoditiesJson));
                _ParseStations(File.ReadAllText(stationsJson));
            }
            else
            {
                AerDebug.LogError("Could not find JSON files!");
            }
            Loaded = true;
        }

        private void _ParseSystems(string json)
        {
            AerDebug.Log("Loading Systems...");
            Stopwatch timer = new Stopwatch();
            timer.Start();
            JArray ja = JArray.Parse(json);

            //System.IO.StreamWriter file = new System.IO.StreamWriter(@"output.xml");

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

                    //file.WriteLine(@"<item> " + jo["name"] + " <tag> out.id="+ jo["id"] + " </tag></item>");
                }
                catch (FormatException e)
                {
                    AerDebug.LogError("Malformed/Unexpected System JSON data, " + e.Message);
                }
            
            }

            //file.Close();

            timer.Stop();
        }
        private void _ParseCommodities(string json)
        {
            AerDebug.Log("Loading Commodities...");
            Stopwatch timer = new Stopwatch();
            timer.Start();
            JArray ja = JArray.Parse(json);

            //System.IO.StreamWriter file = new System.IO.StreamWriter(@"output.xml");
            
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
                //file.WriteLine(@"<item> " + jo["name"] + " <tag> out.id="+ jo["id"] + " </tag></item>");
                

            }

            //file.Close();
            timer.Stop();
        }
        private void _ParseStations(string json)
        {
            AerDebug.Log("Loading Stations...");
            Stopwatch timer = new Stopwatch();
            timer.Start();
            JArray ja = JArray.Parse(json);

            foreach (JObject jo in ja)
            {
                try
                {
                    int system_id = int.Parse(jo["system_id"].ToString());
                    EliteSystem es = GetSystem(system_id);
                    if (es == null)
                    {
                        continue;
                    }

                    EliteStation station = new EliteStation();
                    station.Name = jo["name"].ToString();
                    station.id = int.Parse(jo["id"].ToString());
                    station.System = es;
                    station.MaxPadSize = jo["max_landing_pad_size"].ToString();
                    station.DistanceFromStar = jo["distance_to_star"].ToString();
                    station.Faction = jo["faction"].ToString();
                    station.Allegiance = jo["allegiance"].ToString();
                    station.Government = jo["government"].ToString();
                    station.State = jo["state"].ToString();
                    station.StarportType = jo["type"].ToString();
                    station.HasBlackmarket = jo["has_blackmarket"].ToString().Equals("1");
                    station.HasCommodities = jo["has_commodities"].ToString().Equals("1");
                    station.HasRefuel = jo["has_refuel"].ToString().Equals("1");
                    station.HasRepair = jo["has_repair"].ToString().Equals("1");
                    station.HasRearm = jo["has_rearm"].ToString().Equals("1");
                    station.HasOutfitting = jo["has_outfitting"].ToString().Equals("1");
                    station.HasShipyard = jo["has_shipyard"].ToString().Equals("1");

                    foreach (string commodity in jo["export_commodities"])
                    {
                        EliteCommodity ec = GetCommodity(commodity);

                        if (ec != null)
                        {
                            station.Exports.Add(ec);
                        }
                        else
                        {
                            AerDebug.LogError("Found commodity I knew nothing about = " + commodity);
                        }
                    }

                    foreach (string commodity in jo["import_commodities"])
                    {
                        EliteCommodity ec = GetCommodity(commodity);

                        if (ec != null)
                        {
                            station.Imports.Add(ec);
                        }
                        else
                        {
                            AerDebug.LogError("Found commodity I knew nothing about = " + commodity);
                        }
                    }

                    es.Stations.Add(station);
                }
                catch (Exception e)
                {
                    AerDebug.LogError("Malformed/Unexpected Station JSON data, " + e.Message);
                }
            }
        }

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
                //AerDebug.Log("Searching system: " + sys.Name);
                var validStations = from stations in sys.Stations
                                   from commodites in stations.Exports
                                   where commodites.id == commodity_id
                                   select stations;

                stationsWithCommodity.AddRange(validStations);
            }

            double closestDistance = 1000000; //Infinite
            foreach (EliteStation es in stationsWithCommodity)
            {
                double thisDistance = DistanceSqr(es.System, origin);
                if (thisDistance < closestDistance)
                {
                    closest = es;
                    closestDistance = thisDistance;
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
