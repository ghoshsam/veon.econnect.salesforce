using InSync.eConnect.APPSeCONNECT.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Veon.eConnect.Salesforce
{
    public class WebExceptionResponse
    {
        public string ResponseMessage { get; set; }
        public WebExceptionResponse(WebException wex)
        {
            this.ResponseMessage = wex.Message;
            this.ParseException(wex);
        }
        public string ErrorDetails { get; set; } 
        private void ParseException(WebException wex)
        {
            XNamespace nsSys = "http://schemas.xmlsoap.org/soap/envelope/";
            StreamReader reader = new StreamReader(wex.Response.GetResponseStream());

            try
            {
                this.ResponseMessage = reader.ReadToEnd();
                var xResult = XElement.Parse(this.ResponseMessage);

                var descendants = xResult.Descendants("faultstring");
                var element = descendants.First();
                this.ErrorDetails = element.Value;
            }
            catch
            {
            }
        }
    }

    public static class WebExceptionExtension
    {
        public static ReturnMessage<string> ReadResponse(this WebException webex)
        {
            ReturnMessage<string> retValue = new ReturnMessage<string>();
            retValue.AddException(webex);
            var response = webex.Response;
            if (response != null)
            {
                Stream responseStream = null;
                try
                {
                    responseStream = response.GetResponseStream();
                    using (var responseReader = new StreamReader(responseStream))
                    {
                        var xml = responseReader.ReadToEnd();
                        var errordata = XElement.Parse(xml);
                        retValue.Message = errordata.Value;
                    }
                }
                finally
                {
                    if (responseStream != null)
                        responseStream.Dispose();
                }                
            }
            return retValue;
        }
    }
}
