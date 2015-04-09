using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;

namespace AerSpeech
{
    /// <summary>
    /// Connects to wiki and serve queries. 
    /// </summary>
    public class AerWiki
    {

        string wikiURL;

        public AerWiki()
        {
            wikiURL = "http://en.wikipedia.org/w/api.php?action=query&format=xml&prop=extracts&exsentences=2&rvprop=content&rvsection=0&titles=";
        }

        public string Query(string title)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                WebClient webClient = new WebClient();

                StreamReader wikiXml = new StreamReader(webClient.OpenRead(wikiURL + title));

                xmlDoc.Load(new XmlTextReader(wikiXml));

                XmlNode extractNode = xmlDoc.SelectSingleNode("api/query/pages/page/extract");

                if (extractNode != null)
                    return extractNode.InnerText;
                else
                    return "No Results Found";
            }
            catch (Exception e)
            {
                AerDebug.LogError("Problem querying wikipedia, " + e.Message);
                return "Error";
            }
            

        }
    }
}
