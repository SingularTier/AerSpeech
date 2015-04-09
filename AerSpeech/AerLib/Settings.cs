using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace AerSpeech
{
    public static class Settings
    {
        private static string SettingsFile = Application.UserAppDataPath + "\\AERSettings.xml";
        public static Dictionary<string, string> Keys;
       
        static Settings()
        {
            Keys = new Dictionary<string, string>();
            ReloadSettings();
        }

        public static void ReloadSettings()
        {
            if(!File.Exists(SettingsFile))
            {
                _SaveData(); //This will create a blank file
            }

            StreamReader settingsFile;
            XmlDocument xmlDoc = new XmlDocument();

            settingsFile = new StreamReader(SettingsFile);
            xmlDoc.Load(new XmlTextReader(settingsFile));

            XmlNode settingsRoot = xmlDoc.SelectSingleNode("settings");
            XmlNodeList settings = settingsRoot.ChildNodes;

            foreach (XmlNode entry in settings)
            {
                string name = entry.Name;
                string data = entry.InnerText;
                Keys.Add(name, data);
            }

            settingsFile.Close();
        }

        public static bool Store(string name, string data)
        {
            if(!Keys.ContainsKey(name))
            {
                Keys.Add(name, data);
            }
            else
            {
                Keys[name] = data;
            }

            _SaveData(); //Writes the data to disk
            return true;
        }

        public static string Load(string name)
        {
            if (Keys.ContainsKey(name))
                return Keys[name];
            else
                return null;
        }

        private static void _SaveData()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode settingsNode = xmlDoc.CreateNode(XmlNodeType.Element, "settings", "");

            foreach(KeyValuePair<string, string> kvp in Keys)
            {
                XmlNode keyNode = xmlDoc.CreateNode(XmlNodeType.Element, kvp.Key, "");
                keyNode.InnerText = kvp.Value;
                settingsNode.AppendChild(keyNode);
            }

            xmlDoc.AppendChild(settingsNode);
            xmlDoc.Save(SettingsFile);
        }

    }
}
