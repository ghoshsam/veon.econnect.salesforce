using InSync.eConnect.APPSeCONNECT.API;
using InSync.eConnect.APPSeCONNECT.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.IO;
using InSync.eConnect.APPSeCONNECT.Helpers;
using InSync.eConnect.APPSeCONNECT.Object;

namespace Veon.eConnect.Salesforce
{
    /// <summary>
    ///     Represents the main class for accessing an adapter. Pulls and/or Pushes data from one application.
    /// </summary>
    public class Adapter : IAdapter, IRealTimeProcessor
    {

        private AppResource _resource = null;
        private ApplicationContext _context = null;
        private bool _IsConnected;
        internal static string _Session = string.Empty;
        internal static string _Serviceurl = string.Empty;
        private CredentialModel _credential;
        public Adapter()
        {

        }

        #region IAdapter Implementation

        public void Initialize(ApplicationContext context)
        {
            if (context == null)
                throw new ArgumentNullException("Application context is null send by Agent. Retry again");
            this._credential = context.GetConnectionDetails<CredentialModel>();
            if (this._credential == null) // this indicates that credentails are already saved in configuration, and we can get its value
                this._credential = new CredentialModel();

            this._resource = new AppResource(context);
            this._context = context;
            _context.RealtimeProcessingStarted += Context_RealtimeProcessingStarted;
        }

        private void Context_RealtimeProcessingStarted(IRealTimeProcessor processor)
        {
            utils.InboundMessagesHandler IM_Handler = new utils.InboundMessagesHandler();
            XElement xele_Package = IM_Handler.ProcessPackage(processor.Request.Content);
            if (_context.Settings != null)
                _context.Settings.RawResponse = xele_Package.ToString();
            string str_PackageKey = IM_Handler.GetEntityType(xele_Package);
            if (!string.IsNullOrEmpty(str_PackageKey))
            {
                foreach (var rt_Touchpoint in processor.Request.Touchpoints)
                {
                    if (rt_Touchpoint.SourceAppEntity.AppEntityName == str_PackageKey)
                    {
                        rt_Touchpoint.IsSelected = true;
                    }
                }
            }
        }

        public IAppResource Resource
        {
            get { return this._resource; }
        }

        public ReturnMessage<bool> ValidateProcess(ExecutionSettings settings)
        {
            var retResult = new ReturnMessage<bool>(false, "Execution unsuccesful");
            //Todo : Validate the process  and return true if the process is valid.
            return retResult;
        }

        public ReturnMessage<string> Execute(ExecutionSettings settings)
        {
            var retResult = new ReturnMessage<string>(string.Empty, "Execution unsuccesful");
            switch (settings.ExecutionType)
            {
                case OperationType.GET:
                    retResult = ExecuteGetOperation(settings);
                    break;
                case OperationType.POST:
                    retResult = this.ExecutePostOperation(settings);
                    break;

            }
            return retResult;
        }

        #endregion

        # region Implementation

        private ReturnMessage<string> ExecutePostOperation(ExecutionSettings settings)
        {
            var returnMessage = new ReturnMessage<string>();
            var logger = this._context.ApplicationUtility.Logger;

            IEnumerable<XElement> Requests = XElement.Parse(settings.TransformedResponse).Elements();
            logger.StatusLog("PUSH operation started", string.Format("Trying to push {0} {1} entities to SalesForce", Requests.Count(), settings.EntityName));

            var outputs = new XElement("Envelope");
            foreach (XElement element in Requests)
            {
                try
                {
                    var returnMsg = InteractPush(element.ToString(), settings.ActionName);
                    string key_primary = !string.IsNullOrEmpty(settings.PrimaryKeyField) && element.Element(settings.PrimaryKeyField) != null ? element.Element(settings.PrimaryKeyField).Value : String.Empty;
                    string key = key_primary;
                    if (element.Element("SourceKey") != null)
                        key = element.Element("SourceKey").Value.Split('^')[0].Split('$')[0];
                    EntityRow entity = settings.EntityData.FirstOrDefault(e => e.SourceCode == key);
                    if (entity == null)
                    {
                        entity = new EntityRow();
                    }

                    if (returnMsg.Value != null)
                    {
                        outputs.Add(returnMsg.Value);
                        if (returnMsg.Status)
                        {
                            entity.DestinationCode = !string.IsNullOrEmpty(key) ? key : "SUCCESS";
                            entity.State = EntityStatus.SUCCESS;
                            entity.Message = "Pushed Successfully";
                        }
                        else
                        {
                            //entity.DestinationCode = !string.IsNullOrEmpty(key_primary) ? key_primary : "FAILED";
                            entity.State = EntityStatus.ERROR;
                            entity.Message = returnMsg.Value.Descendants("errorCode").FirstOrDefault().Value;
                            logger.ErrorLog(string.Format("{0} : {1}", key, entity.Message), returnMsg.Value.Descendants("message").FirstOrDefault().Value, ErrorTypes.Sync, Severitys.None);
                        }
                    }
                    else
                    {
                        //entity.DestinationCode = !string.IsNullOrEmpty(key_primary) ? key_primary : "UNPROCESSED";
                        entity.State = EntityStatus.UNPROCESSED;
                        entity.Message = "No Response";
                        logger.ErrorLog(string.Format("{0} : {1}", key, entity.Message), "No Response Received from SalesForce; please check for SourceKey");
                    }
                }
                catch (Exception ex)
                {
                    logger.ErrorLog("Request package failed", ex, ErrorTypes.Application, Severitys.High);
                }

            }

            returnMessage.SetSuccess("Data pushed successfully", string.Concat(outputs));
            logger.StatusLog("PUSH operation completed", string.Format("Received {0} {1} response entities from SalesForce", outputs.Elements().Count(), settings.EntityName));

            return returnMessage;
        }

        /// <summary>
        /// This function is used for adding and updating data in Salesforce
        /// </summary>
        /// <param name="xmldata">Data to be passed for the uploading entity</param>
        /// <param name="actionandparams">Action parameters of the request</param>
        /// <returns>Response of the request and the status as an key value pair</returns>
        private ReturnMessage<XElement> InteractPush(string xmldata, string actionandparams)
        {
            string responsedata = string.Empty;
            var logger = this._context.ApplicationUtility.Logger;
            this.Connect();
            var returnMessage = new ReturnMessage<XElement>();
            try
            {
                var elements = XElement.Parse(xmldata);
                var urlparam = elements.Descendants("UploadURL").FirstOrDefault();
                var sourceidentifier = elements.Descendants("SourceKey").FirstOrDefault();
                var DBKey = elements.Descendants("DatabaseKey").FirstOrDefault();

                elements.Element("UploadURL").Remove();
                if (DBKey != null)
                {
                    elements.Element("DatabaseKey").Remove();
                }

                if (sourceidentifier != null)
                {
                    elements.Element("SourceKey").Remove();
                }

                xmldata = elements.ToString();
                HttpClient createClient = new HttpClient();

                HttpContent content = new StringContent(xmldata, Encoding.UTF8, "application/xml");
                string uri = _Serviceurl + urlparam.Value;
                HttpRequestMessage postrequest = null;
                if (actionandparams == "PATCH")
                {
                    var method = new HttpMethod("PATCH");
                    postrequest = new HttpRequestMessage(method, uri);
                }
                else
                {
                    postrequest = new HttpRequestMessage(HttpMethod.Post, uri);
                }


                //postrequest.Headers.Add("Authorization", "Bearer " + _connectionProperty.OuthToken);
                postrequest.Headers.Add("Authorization", "Bearer " + _Session);
                postrequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                postrequest.Content = content;
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)Enum.Parse(typeof(SecurityProtocolType), this._credential.Protocol);
                HttpResponseMessage postresponse = createClient.SendAsync(postrequest).Result;
                responsedata = postresponse.Content.ReadAsStringAsync().Result;
                if (!string.IsNullOrEmpty(responsedata) && sourceidentifier != null)
                {
                    if (responsedata.Contains("error") ||
                        responsedata.Contains("Error") ||
                        responsedata.Contains("Fail") ||
                        responsedata.Contains("fail") ||
                        responsedata.Contains("fault") ||
                        responsedata.Contains("Fault") ||
                        responsedata.Contains("errorCode")
                        )
                    {
                        XElement ErrorElement = XElement.Parse(responsedata);
                        ErrorElement.Add(new XElement("SourceKey", sourceidentifier.Value));
                        returnMessage.SetError("Fail to push", ErrorElement);
                    }
                    else
                    {
                        if (actionandparams == "PATCH")
                        {
                            //  var patchResponseData = new XElement("Result", new XElement("SourceKey", sourceidentifier.Value));
                            XElement patchResponseData = XElement.Parse(responsedata);
                            patchResponseData.Add(new XElement("SourceKey", sourceidentifier.Value));
                            responsedata = patchResponseData.ToString();
                            returnMessage.SetSuccess("Patch successfull", patchResponseData);
                        }
                        else
                        {
                            if (sourceidentifier != null)
                            {
                                XElement newelement = XElement.Parse(responsedata);
                                newelement.Add(new XElement("SourceKey", sourceidentifier.Value));
                                responsedata = newelement.ToString();
                                returnMessage.SetSuccess("Post successfull", newelement);
                            }
                        }
                    }


                    //returnMessage.Message = responsedata;
                    //returnMessage.Value = true;
                    //return returnMessage;

                }
                else
                {
                    if (actionandparams == "PATCH" && sourceidentifier != null)
                    {
                        var patchResponseData = new XElement("Result", new XElement("SourceKey", sourceidentifier.Value));

                        responsedata = patchResponseData.ToString();
                        returnMessage.SetSuccess("Update successfull", patchResponseData);

                    }
                }
            }
            catch (Exception e)
            {
                logger.ErrorLog(e.Message, ErrorTypes.Sync, Severitys.High);
                returnMessage.SetError(e.Message);
            }
            return returnMessage;


        }
        private ReturnMessage<string> ExecuteGetOperation(ExecutionSettings settings)
        {
            var returnMessage = new ReturnMessage<string>();
            var returnAPIData = new ReturnMessage<string>();
            var logger = this._context.Logger;
            logger.StatusLog("Trying to pull data from application", "Inside Adapter GET");

            string responseData = string.Empty;
            string requestData = string.Empty;

            try
            {

                returnAPIData = InteractPull(settings);

                if (string.IsNullOrEmpty(returnAPIData.Value) == true)
                {
                    returnMessage.SetError(returnAPIData.Message);
                }
                else
                {

                    returnMessage.SetSuccess("Data fetched successfully from Salesforce", returnAPIData.Value);
                    AssignPrimaryKeys(settings, returnMessage, this._context);
                }


            }
            catch (WebException ex)
            {
                var actualResponse = ex.ReadResponse();
                string errorMessage = string.Format("Exception Caught to pull: {0}. Error Message: {1}", settings.EntityName, actualResponse.Message);

                logger.ErrorLog(errorMessage, ex, ErrorTypes.Sync, Severitys.High);
                returnMessage.SetError(actualResponse.Message);
            }
            catch (Exception ex)
            {
                returnMessage.AddException(ex);
            }

            return returnMessage;
        }

        private ReturnMessage<string> InteractPull(ExecutionSettings settings)
        {
            HttpClient queryClient = new HttpClient();
            this.Connect();
            var commandProcessor = settings.GetCommandProcessor(Protocol.REST);
            var requestData = commandProcessor.PrepareCommand();
            var paramValue = settings.CurrentActionFilter.ActionParameters[0].Values;
            var actionandparams = settings.CurrentActionFilter.ActionName + "/?q=" + paramValue[0].Value;
            string returndata = string.Empty;
            var logger = this._context.ApplicationUtility.Logger;
            var retMessage = new ReturnMessage<string>();
            if (settings.CalledFrom != CallerType.ReSync)
            {

                try
                {

                    if (actionandparams.Substring(actionandparams.Length - 1, 1) == "?")
                    {
                        actionandparams = actionandparams.Remove(actionandparams.Length - 1);
                    }
                    string restQuery = _Serviceurl + "/services/data/v33.0" + actionandparams;
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, restQuery);
                    request.Headers.Add("Authorization", "Bearer " + _Session);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)Enum.Parse(typeof(SecurityProtocolType), this._credential.Protocol);
                    HttpResponseMessage response = queryClient.SendAsync(request).Result;
                    returndata = response.Content.ReadAsStringAsync().Result;


                    retMessage.SetSuccess("Data Pulled successfully", returndata);
                }
                catch (Exception e)
                {
                    logger.ErrorLog(e.Message, ErrorTypes.Sync, Severitys.High);
                    retMessage.SetError(e.Message);

                }
            }
            else
            {
                if (settings.EntityData.Count > 0)
                {

                    foreach (var entity in settings.EntityData)
                    {
                        string tempactionParams = actionandparams.Replace("$", entity.SourceCode);
                        try
                        {

                            //if (actionandparams.Substring(actionandparams.Length - 1, 1) == "?")
                            //{
                            //    actionandparams = actionandparams.Remove(actionandparams.Length - 1);
                            //}
                            string restQuery = _Serviceurl + "/services/data/v33.0" + tempactionParams;
                            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, restQuery);
                            request.Headers.Add("Authorization", "Bearer " + _Session);
                            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                            ServicePointManager.SecurityProtocol = (SecurityProtocolType)Enum.Parse(typeof(SecurityProtocolType), this._credential.Protocol);
                            HttpResponseMessage response = queryClient.SendAsync(request).Result;
                            returndata = response.Content.ReadAsStringAsync().Result;
                            retMessage.SetSuccess(entity.SourceCode + " pulled successfully", returndata);
                        }
                        catch (Exception e)
                        {
                            logger.ErrorLog(e.Message, ErrorTypes.Sync, Severitys.High);
                            retMessage.SetError(e.Message);

                        }
                    }
                }
            }
            return retMessage;
        }

        //private CredentialModel GetCredential()
        //{
        //    CredentialModel model = null;
        //    var credential = _context.GetConnectionDetails<CredentialModel>();

        //    if (credential != null) // this indicates that credentails are already saved in configuration, and we can get its value
        //        model = credential;

        //    return model;
        //}

        # endregion


        public string LoginState(CredentialModel credential)
        {

            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string loginPassword = credential.Password + credential.Token;
            string responseString = string.Empty;

            HttpClient authClient = new HttpClient();
            string token = string.Empty;
            try
            {


                HttpContent content = new FormUrlEncodedContent(new Dictionary<string, string>
                                                                  {
                                                                     {"grant_type","password"},
                                                                     {"client_id",credential.ConsumerKey},
                                                                     {"client_secret",credential.ConsumerSecret},
                                                                     {"username",credential.UserName},
                                                                     {"password",loginPassword}
                                                                   }
                                                                );
                HttpResponseMessage message = authClient.PostAsync("https://login.salesforce.com/services/oauth2/token?", content).Result;
                responseString = message.Content.ReadAsStringAsync().Result;

            }
            catch
            {
                // this._context.Logger.ErrorLog(ex.ToString());
            }

            if (!string.IsNullOrEmpty(responseString))
            {
                JObject obj = JObject.Parse(responseString);
                token = (string)obj["access_token"];
            }



            if (string.IsNullOrEmpty(token))
            {
                var dictionary = new Dictionary<string, string>
                                                                  {
                                                                     {"grant_type","password"},
                                                                     {"client_id",credential.ConsumerKey},
                                                                     {"client_secret",credential.ConsumerSecret},
                                                                     {"username",credential.UserName},
                                                                     {"password",loginPassword}
                                                                   };
                //HttpContent content1 = new FormUrlEncodedContent(dictionary);
                string url = string.Format("https://test.salesforce.com/services/oauth2/token{0}", this.GetContent(dictionary));
                HttpResponseMessage message = authClient.PostAsync(url, new StringContent(string.Empty)).Result;
                responseString = message.Content.ReadAsStringAsync().Result;
            }
            return responseString;

        }

        private string GetContent(Dictionary<string, string> contents)
        {
            StringBuilder builder = new StringBuilder();
            string delimeter = "?";
            foreach (KeyValuePair<string, string> content in contents)
            {
                builder.AppendFormat("{0}{1}={2}", delimeter, content.Key, content.Value);
                delimeter = "&";
            }
            return builder.ToString();
        }


        //    if (string.IsNullOrEmpty(token))
        //    {
        //        HttpContent content1 = new FormUrlEncodedContent(new Dictionary<string, string>
        //                                                          {
        //                                                             {"grant_type","password"},
        //                                                             {"client_id",credential.ConsumerKey},
        //                                                             {"client_secret",credential.ConsumerSecret},
        //                                                             {"username",credential.UserName},
        //                                                             {"password",loginPassword}
        //                                                           }
        //                                                          );
        //        string url = string.Format("https://test.salesforce.com/services/oauth2/token?", this.GetContent(content1).Result);
        //        HttpResponseMessage message = authClient.PostAsync(url, new StringContent(string.Empty)).Result;
        //        responseString = message.Content.ReadAsStringAsync().Result;
        //    }
        //    return responseString;

        //}

        //private async Task<string> GetContent(HttpContent content1)
        //{
        //    return await content1.ReadAsStringAsync();
        //}



        /// <summary>
        /// To Get the Connection
        /// </summary>
        public void Connect()
        {
            if (!this.IsConnected)
            {
                //This is for authentication to the application

                string logindata = LoginState(this._credential);
                JObject obj = JObject.Parse(logindata);
                _Session = (string)obj["access_token"];
                _Serviceurl = (string)obj["instance_url"];
                IsConnected = true;

            }
        }

        /// <summary>
        /// To Disconnect This is not require for this adaptor
        /// </summary>
        /// <returns></returns>
        public bool Disconnect()
        {
            _Session = string.Empty;
            return true;
        }

        /// <summary>
        /// Global status for the authenticated session status
        /// </summary>
        public bool IsConnected
        {
            get { return _IsConnected; }
            set { _IsConnected = value; }
        }

        public RealtimeContext Request { get; set; }



        /// <summary>
        /// Add the entity keys for the resync bucket
        /// Edited on 26 Sep 2017
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="returnAPIData"></param>
        /// <param name="logger"></param>
        private static void AssignPrimaryKeys(ExecutionSettings settings, ReturnMessage<string> returnAPIData, ApplicationContext logger)
        {
            /*
            var xmlElement = XElement.Parse(returnAPIData.Value);
            var items = xmlElement.Descendants("records");

            if (!string.IsNullOrEmpty(settings.PrimaryKeyField))
            {
                foreach (var element in items)
                {
                    var ele = element.Descendants("SAP_Customer_Number__c").Any() ? element.Elements("SAP_Customer_Number__c").FirstOrDefault().Value.ToString() : string.Empty;
                    if (!string.IsNullOrEmpty(ele.ToString()))
                    {

                        var sourceCode = element.Element(settings.PrimaryKeyField).Value;

                        settings.AddSourceEntity(sourceCode);

                    }
                    else
                    {
                        logger.ApplicationUtility.Logger.ErrorLog("SAP_Customer_Number__c is not found", ErrorTypes.Sync, Severitys.High);

                        //logger.ErrorLog(, ErrorTypes.Sync, Severitys.High);


                    }
                }

            }
            */

            if (string.IsNullOrEmpty(settings.PrimaryKeyField))
            {
                logger.ApplicationUtility.Logger.InfoLog("Primary Key not assigned", string.Format("Primary Key not assigned for {0} entity in Salesforce", settings.EntityName));
            }
            else
            {
                XElement xele_QueryData = XElement.Parse(returnAPIData.Value);
                foreach (XElement xele_Record in xele_QueryData.Descendants("records"))
                {
                    string str_PrimaryKeyValue = xele_Record.Descendants(settings.PrimaryKeyField).Any() ? xele_Record.Descendants(settings.PrimaryKeyField).FirstOrDefault().Value.ToString() : string.Empty;
                    if (string.IsNullOrEmpty(str_PrimaryKeyValue))
                    {
                        logger.ApplicationUtility.Logger.ErrorLog("Primary Key Value not available", string.Format("Value for '{0}' in '{1}' not available", settings.PrimaryKeyField, settings.EntityName, ErrorTypes.Sync, Severitys.Medium));
                    }
                    else
                    {
                        settings.AddSourceEntity(str_PrimaryKeyValue);
                    }
                }
            }
        }

        public ReturnMessage<bool> ValidateRequest(RealtimeExecutionSettings settings)
        {
            var returnMsg = new ReturnMessage<bool>();
            //ToDo : Check whether the request is coming from actual recepient. 

            returnMsg.SetSuccess("Request Validation");
            return returnMsg;
        }

        public ReturnMessage<string> Execute(RealtimeExecutionSettings settings)
        {
            utils.InboundMessagesHandler IM_Handler = new utils.InboundMessagesHandler();
            XElement xele_Package = IM_Handler.ProcessPackage(Request.Content);
            if (_context.Settings != null)
                _context.Settings.RawResponse = xele_Package.ToString();
            string str_PackageKey = IM_Handler.GetEntityType(xele_Package);
            if (!string.IsNullOrEmpty(str_PackageKey))
            {
                foreach (var rt_Touchpoint in settings.RealtimeTouchpoints)
                {
                    if (rt_Touchpoint.SourceAppEntity.AppEntityName == str_PackageKey)
                    {
                        rt_Touchpoint.IsSelected = true;
                    }
                }
            }
            var returnMsg = new ReturnMessage<string>();
            returnMsg.SetSuccess("Realtime touchpoints are ready to execute.", xele_Package.ToString());
            return returnMsg;
        }
    }
}