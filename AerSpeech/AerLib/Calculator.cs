using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AerSpeech
{
    public class Calculator
    {

        private WebClient _dataDownloader;

        public Calculator()
        {
            _dataDownloader = new WebClient();
        }

        public string CalculationResult(string query)
        {
            string result = null;

            try
            {
                result = _dataDownloader.DownloadString("https://www.calcatraz.com/calculator/api?c=" + WebUtility.UrlEncode(query)).Trim();
                if (result == "answer")
                {
                    result = null;
                }
            }
            catch (Exception e)
            {
                AerDebug.LogException(e);
            }
            
            return result;

        }
    }
}
