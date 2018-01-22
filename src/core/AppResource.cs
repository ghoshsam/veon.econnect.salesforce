using InSync.eConnect.APPSeCONNECT.API;
using InSync.eConnect.APPSeCONNECT.Helpers;
using InSync.eConnect.APPSeCONNECT.Storage;
using InSync.eConnect.APPSeCONNECT.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Linq;
using System.Net;
using System.IO;
using System.Drawing;
using Veon.eConnect.Salesforce.utils;

namespace Veon.eConnect.Salesforce
{
    /// <summary>
    /// Declare all the major resources or functions which could be required during transformation.
    /// </summary>
    public class AppResource : IAppResource
    {

        private ApplicationContext _context;
        private CredentialModel CredentialObject { get; set; }
        const string LAST_DATE = "lastdate";
        private Logger _logger;

        /// <summary>
        /// Default Constructor called by Agent. It will call Initialize to pass Application context
        /// </summary>
        /// <remarks>Do not use this method while creating object of AppResource inside the adapter, as you will find ApplicationContext to null</remarks>
        public AppResource() { }
        /// <summary>
        /// Parameterized constructor to pass the context
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <remarks>Use this overload to create object of AppResource class</remarks>
        public AppResource(ApplicationContext context) // additional constructor to ensure we pass context when creating AppResource object from adapter itself.
        {
            this._context = context;
            this._logger = context.Logger;
        }

        # region IAppResource Implementation
        public void Initialize(ApplicationContext context)
        {
            // first step is to try and get the Credential. If succesful, we store it in object cache, so that every function does not need to get it.
            var credential = context.GetConnectionDetails<CredentialModel>();

            if (credential == null) // this indicates that credentails are already saved in configuration, and we can get its value
                throw new ArgumentNullException("Credential is null");
            this.CredentialObject = credential;

            //We store the context for future use.
            this._context = context;
        }

        # endregion

        //ToDo: Add all your functions here. Here are some of the simple rules on defining an APPResource function. 
        //http://support.appseconnect.com/support/solutions/articles/4000068153-adding-functions-to-appresource-


        /// <summary>
        /// This function is used for getting the value mapping values based on mapping done in Appseconnect admin
        /// </summary>
        /// <param name="mappingType">Particular mapping type based on which value mapping will be fetched</param>
        /// <param name="sourceValue">Source value based on which destination value will be fetched</param>
        /// <returns>Destination value based on mapping</returns>
        public string GetMapping(string mappingType, string sourceValue)
        {
            return this._context.GenericHelpers.GetMapping(sourceValue, mappingType);

        }
        /// <summary>
        /// This function is used to get the last save datetime filter for currently executing touchpoint
        /// </summary>
        /// <returns>returns last save datetime</returns>
        public string CreatedDate()
        {
            string filterdate = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd H:mm:ss-04:00", CultureInfo.InvariantCulture);
            string filterdateget = this._context.GetData(LAST_DATE);
            if (!string.IsNullOrEmpty(filterdateget))
            {
                filterdate = filterdateget.ToString();
            }
            return filterdate.ToString();

        }

        /// <summary>
        /// Get products associated with a order from Salesforce
        /// </summary>
        /// <param name="id">Order id for which product will be fetched</param>
        /// <returns>Products associated with the order</returns>
        public XPathNavigator GetOrderProducts(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+id+,+OrderItemNumber+,+PricebookEntryId+,+Quantity+,+UnitPrice+from+OrderItem+Where+OrderId+=+'", id, "'");
                //_adapter.AppEntity.AppEntityAction = addressurl;

                //_appCore.ExecutePullCommand(getOutput);
                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get orderproducts", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return elemment.CreateNavigator();
        }

        /// <summary>
        /// Get quote line items associated with a quote from Salesforce
        /// </summary>
        /// <param name="id">quote id for which product will be fetched</param>
        /// <returns>Products associated with the quote</returns>
        public XPathNavigator GetQuoteLineitemsSyspex(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+Id+,Product2Id+,+Quantity+,+UOM__c+,+UnitPrice+,+TotalPrice+from+QuoteLineItem+Where+QuoteId+=+'", id, "'");
                //_adapter.AppEntity.AppEntityAction = addressurl;

                //_appCore.ExecutePullCommand(getOutput);
                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get QuoteLineitemsSyspex", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return elemment.CreateNavigator();
        }
        /// <summary>
        /// Get Purchase request line items  associated with a  Purchase request from Salesforce (syspexspecific)
        /// </summary>
        /// <param name="id"> Purchase request id for which product will be fetched</param>
        //  <returns>Products associated with the order</returns>
        public XPathNavigator GetPurchaseRequestitemssyspex(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            XElement OrderItems = new XElement("OrderItems");
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+Id+,+PricebookEntryId+,+Line_Description__c+,+YBID__c+,+Selling_Price_Local__c+,+Free_Text__c+,+Item_Description__c+,+Specification__c+,+Quantity+,+UOM__c+,+UnitPrice+,+Tax_Definition__c+,+ServiceDate+,+Vendor_Code__c+,+Vendor_Name__c+from+OrderItem+Where+OrderId+=+'", id, "'");
                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);
                int i = 0;
                foreach (XElement element in elemment.Elements("records"))
                {
                    XElement OrderItem = new XElement("records");
                    OrderItem.Add(new XElement("Id", element.Descendants("Id").Any() ? element.Elements("Id").FirstOrDefault().Value.ToString() : string.Empty));
                    OrderItem.Add(new XElement("PricebookEntryId", element.Descendants("PricebookEntryId").Any() ? element.Elements("PricebookEntryId").FirstOrDefault().Value.ToString() : string.Empty));
                    OrderItem.Add(new XElement("Item_Description__c", element.Descendants("Item_Description__c").Any() ? element.Elements("Item_Description__c").FirstOrDefault().Value.ToString() : string.Empty));
                    OrderItem.Add(new XElement("Quantity", element.Descendants("Quantity").Any() ? element.Elements("Quantity").FirstOrDefault().Value.ToString() : string.Empty));
                    OrderItem.Add(new XElement("UOM__c", element.Descendants("UOM__c").Any() ? element.Elements("UOM__c").FirstOrDefault().Value.ToString() : string.Empty));
                    OrderItem.Add(new XElement("UnitPrice", element.Descendants("UnitPrice").Any() ? element.Elements("UnitPrice").FirstOrDefault().Value.ToString() : string.Empty));
                    OrderItem.Add(new XElement("Tax_Definition__c", element.Descendants("Tax_Definition__c").Any() ? element.Elements("Tax_Definition__c").FirstOrDefault().Value.ToString() : string.Empty));
                    OrderItem.Add(new XElement("ServiceDate", element.Descendants("ServiceDate").Any() ? element.Elements("ServiceDate").FirstOrDefault().Value.ToString() : string.Empty));
                    OrderItem.Add(new XElement("Vendor_Code__c", element.Descendants("Vendor_Code__c").Any() ? element.Elements("Vendor_Code__c").FirstOrDefault().Value.ToString() : string.Empty));
                    OrderItem.Add(new XElement("Vendor_Name__c", element.Descendants("Vendor_Name__c").Any() ? element.Elements("Vendor_Name__c").FirstOrDefault().Value.ToString() : string.Empty));
                    string productcode = this.GetSyspexProductCode(element.Descendants("PricebookEntryId").Any() ? element.Elements("PricebookEntryId").FirstOrDefault().Value.ToString() : string.Empty);
                    OrderItem.Add(new XElement("Line_Description__c", element.Descendants("Line_Description__c").Any() ? element.Elements("Line_Description__c").FirstOrDefault().Value.ToString() : string.Empty));
                    OrderItem.Add(new XElement("Free_Text__c", element.Descendants("Free_Text__c").Any() ? element.Elements("Free_Text__c").FirstOrDefault().Value.ToString() : string.Empty));
                    OrderItem.Add(new XElement("Selling_Price_Local__c", element.Descendants("Selling_Price_Local__c").Any() ? element.Elements("Selling_Price_Local__c").FirstOrDefault().Value.ToString() : string.Empty));
                    OrderItem.Add(new XElement("Specification__c", element.Descendants("Specification__c").Any() ? element.Elements("Specification__c").FirstOrDefault().Value.ToString() : string.Empty));
                    OrderItem.Add(new XElement("YBID__c", element.Descendants("YBID__c").Any() ? element.Elements("YBID__c").FirstOrDefault().Value.ToString() : string.Empty));
                    OrderItem.Add(new XElement("ProductCode", productcode));
                    OrderItem.Add(new XElement("Linenum", i));
                    OrderItems.Add(OrderItem);
                    i++;
                }

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get orderproducts", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return OrderItems.CreateNavigator();
        }

        public string GetSyspexProductCode(string PricebookEntryId)
        {
            if (string.IsNullOrEmpty(PricebookEntryId))
            {
                return string.Empty;
            }
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string product = string.Concat("/query?q=SELECT+ProductCode+from+PricebookEntry+Where+Id+=+'", PricebookEntryId, "'");
                string responseData = ExtendedAPIFunction(product);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);
                foreach (XElement element in elemment.Elements("records"))
                {
                    string productcode = element.Descendants("ProductCode").Any() ? element.Elements("ProductCode").FirstOrDefault().Value.ToString() : string.Empty;
                    if (!string.IsNullOrEmpty(productcode))
                    {
                        return productcode;
                    }
                }

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to Product Code", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return string.Empty;
        }

        /// <summary>
        /// Get contact persons associated with a account from Salesforce
        /// </summary>
        /// <param name="id">Account id for which contact person will be fetched</param>
        /// <returns>contact persons associated with the Account</returns>
        ///   <author>seema@veon.in</author>
        ///    <date>19 Aug 2015 </date>
        public XPathNavigator GetContactPersons(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+AccountId+,+Id+,+Title+,+FirstName+,+LastName+,+Email+,+Fax+,+Phone+,+MailingStreet+,+MailingCity+,+MailingPostalCode+,+MailingState+,+OtherStreet+,+OtherCity+,+OtherState+,+OtherPostalCode+from+contact+Where+AccountId+=+'", id, "'");


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get contactpersons", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return elemment.CreateNavigator();
        }
        /// <summary>
        /// Get contact persons associated with a account from Salesforce
        /// </summary>
        /// <param name="id">Account id for which contact person will be fetched</param>
        /// <returns>contact persons associated with the Account</returns>
        ///   <author>seema@veon.in</author>
        ///    <date>19 Aug 2015 </date>
        public XPathNavigator GetContactPersonsTotus(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+AccountId+,+Id+,+Title+,+FirstName+,+LastName+,+Email+,+Fax+,+Phone+,+MailingStreet+,+MailingCity+,+MailingPostalCode+,+MailingState+from+contact+Where+AccountId+=+'", id, "'");


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get contactpersons", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return elemment.CreateNavigator();
        }
        /// <summary>
        /// Get contact persons associated with a account from Salesforce
        /// </summary>
        /// <param name="id">Account id for which contact person will be fetched</param>
        /// <returns>contact persons associated with the Account</returns>
        ///   <author>seema@veon.in</author>
        ///    <date>07/06/2016 </date>
        public XPathNavigator GetContactPersonsSyspex(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+AccountId+,+Id+,+Title+,+Position__c+,+FirstName+,+LastName+,+Email+,+Fax+,+Phone+,+Contact_ID__c+,+Gender__c+,+Customer_Code__c+,+MobilePhone+,+Description+,+Department+,+Birthdate+from+contact+Where+AccountId+=+'", id, "'");


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get contactpersons", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return elemment.CreateNavigator();
        }

        /// <summary>
        /// Get contact persons associated with an account from Salesforce
        /// </summary>
        /// <param name="id">Account id for which contact person will be fetched</param>
        /// <returns>contact persons associated with the Account</returns>
        ///   <author>roja.r@veon.in</author>
        ///    <date>27/03/2017 </date>
        public XPathNavigator GetContactPersonsEligo(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+AccountId+,+Id+,+Title+,+Name+,+Position__c+,+Salutation+,+FirstName+,+LastName+,+Email+,+Fax+,+Phone+,+MobilePhone+,+OtherPhone+,+MiddleName+from+contact+Where+AccountId+=+'", id, "'");


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get contactpersons", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return elemment.CreateNavigator();
        }

        /// <summary>
        /// Get contact persons associated with an account from Salesforce
        /// </summary>
        /// <param name="id">Account id for which contact person will be fetched</param>
        /// <returns>contact persons associated with the Account</returns>
        ///   <author>roja.r@veon.in</author>
        ///    <date>16/06/2017 </date>
        public XPathNavigator GetContactPersonsFonkel(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+Id+,+Nombre_Contacto__c+,+Salutation+,+Clave_Contacto__c+,+FirstName+,+LastName+,+Phone+,+Fax+,+MobilePhone+,+HomePhone+,+Title+,+Email+,+Department+,+Area__c+,+Colonia_de_correo__c+,+MailingStreet+,+MailingCity+,+MailingState+,+MailingPostalCode+,+MailingCountry+,+Birthdate+,+SAP_Contact_Code__c+from+contact+Where+AccountId+=+'", id, "'");


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get contactpersons", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return elemment.CreateNavigator();
        }
        /// <summary>
        /// Get contact persons associated with a account from Salesforce
        /// </summary>
        /// <param name="id">Account id for which contact person will be fetched</param>
        /// <returns>contact persons associated with the Account</returns>
        ///   <author>seema@veon.in</author>
        ///    <date>27/07/2016 </date>
        public XPathNavigator GetContactPersonsAfirmon(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+AccountId+,+Id+,+Title+,+Contact_ID__c+,+FirstName+,+LastName+,+Email+,+Fax+,+Phone+,+Position__c+,+MobilePhone+,+Active__c+from+contact+Where+AccountId+=+'", id, "'");

                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get contactpersons", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return elemment.CreateNavigator();
        }

        /// <summary>
        /// Get contact persons associated with a account from Salesforce
        /// </summary>
        /// <param name="id">Account id for which contact person will be fetched</param>
        /// <returns>contact persons associated with the Account</returns>
        ///   <author>seema@veon.in</author>
        ///    <date>27/07/2016 </date>
        public XPathNavigator GetContactPersonsMarco(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+AccountId+,+Id+,+Title+,+FirstName+,+LastName+,+Email+,+Suffix+,+Fax+,+Phone+,+Position__c+,+OwnerId+from+contact+Where+AccountId+=+'", id, "'");

                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get contactpersons", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return elemment.CreateNavigator();
        }

        /// <summary>
        /// Get Addresses associated with a account from Salesforce
        /// </summary>
        /// <param name="id">Account id for which Address will be fetched</param>
        /// <returns>Addresses associated with the Account</returns>
        ///   <author>seema@veon.in</author>
        ///    <date>02/06/2016 </date>
        public XPathNavigator GetMultipleAddressesSyspex(string id, string BilltoRecType, string ShiptoRecType)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            XElement Addresses = new XElement("Addresses");
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+RecordTypeId+,+Name+,+Id+,+Address_Line_2__c+,+Account__c+,+Postal_Code_bill_to__c+,+Delivery_Tel_Fax__c+,+Delivery_Attn__c+,+Country__c+,+Address_Line_1__c+,+City__c+,+State_Province__c+from+Address__c+Where+Account__c+=+'", id, "'");


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);

                /*   if (elemment.Elements("records").Count() == 0)
                   {
                       XElement Address = new XElement("Address");
                       Address.Add(new XElement("type", "bo_BillTo"));
                    	
                       Address.Add(new XElement("Street", ""));
                       Address.Add(new XElement("City", ""));
                       Address.Add(new XElement("State", ""));
                       Address.Add(new XElement("ZipCode", ""));
                       Address.Add(new XElement("Country", ""));
                       Address.Add(new XElement("Addressname", ""));
                       Address.Add(new XElement("Add_ID", ""));
                       Addresses.Add(Address);

                       XElement Address1 = new XElement("Address");
                       Address1.Add(new XElement("type", "bo_ShipTo"));
                       Address1.Add(new XElement("Street", ""));
                       Address1.Add(new XElement("City", ""));
                       Address1.Add(new XElement("State", ""));
                       Address1.Add(new XElement("ZipCode", ""));
                       Address1.Add(new XElement("Country", ""));
                       Address.Add(new XElement("Addressname", ""));
                       Address.Add(new XElement("Add_ID", ""));
                       Addresses.Add(Address1);
                   }  */

                foreach (XElement element in elemment.Elements("records"))
                {

                    XElement Address = new XElement("Address");

                    string type = element.Descendants("RecordTypeId").Any() ? element.Elements("RecordTypeId").FirstOrDefault().Value.ToString() : string.Empty;
                    if (!string.IsNullOrEmpty(type))
                    {
                        if (type == BilltoRecType)
                        {
                            Address.Add(new XElement("type", "bo_BillTo"));
                        }
                        else if (type == ShiptoRecType)
                        {
                            Address.Add(new XElement("type", "bo_ShipTo"));
                        }
                    }
                    Address.Add(new XElement("Street", element.Descendants("Address_Line_1__c").Any() ? element.Elements("Address_Line_1__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("City", element.Descendants("City__c").Any() ? element.Elements("City__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("State", element.Descendants("State_Province__c").Any() ? element.Elements("State_Province__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("ZipCode", element.Descendants("Postal_Code_bill_to__c").Any() ? element.Elements("Postal_Code_bill_to__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("U_BillAttn", element.Descendants("Delivery_Attn__c").Any() ? element.Elements("Delivery_Attn__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("U_TelFax", element.Descendants("Delivery_Tel_Fax__c").Any() ? element.Elements("Delivery_Tel_Fax__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Country", element.Descendants("Country__c").Any() ? element.Elements("Country__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Addressname", element.Descendants("Name").Any() ? element.Elements("Name").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Address_Line_2__c", element.Descendants("Address_Line_2__c").Any() ? element.Elements("Address_Line_2__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Add_ID", element.Descendants("Id").Any() ? element.Elements("Id").FirstOrDefault().Value.ToString() : string.Empty));
                    Addresses.Add(Address);
                }

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get Addresses", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return Addresses.CreateNavigator();
        }

        /// <summary>
        /// Get owner Id
        /// </summary>
        /// <param name="id">salesperson name</param>
        /// <returns>owner Id</returns>
        ///   <author>seema@veon.in</author>
        ///    <date>07/06/2016 </date>
        public string GetUserSyspex(string salesperson)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+Sales_Employee__c+,+Id+,+Company__c+,+Email+,+Name+from+user");
                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);
                foreach (XElement element in elemment.Elements("records"))
                {
                    string spname = element.Descendants("Sales_Employee__c").Any() ? element.Elements("Sales_Employee__c").FirstOrDefault().Value.ToString() : string.Empty;
                    if (!string.IsNullOrEmpty(spname))
                    {
                        if (spname == salesperson)
                        {
                            return element.Descendants("Id").Any() ? element.Elements("Id").FirstOrDefault().Value.ToString() : string.Empty;
                        }
                    }
                }

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get Users", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return "00561000001zWYDAA2";
        }


        /// <summary>
        /// Get owner Id for Marco
        /// </summary>
        /// <param name="id">salesperson id</param>
        /// <returns>owner Id</returns>
        ///   <author>seema@veon.in</author>
        ///    <date>09/08/2016 </date>
        public string GetUserMarco(string salesperson)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+SLP__c+,+Id+,+Name+,+LastName+,+Username+from+user");
                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);
                foreach (XElement element in elemment.Elements("records"))
                {
                    string spname = element.Descendants("SLP__c").Any() ? element.Elements("SLP__c").FirstOrDefault().Value.ToString() : string.Empty;
                    if (!string.IsNullOrEmpty(spname))
                    {
                        if (spname == salesperson)
                        {
                            return element.Descendants("Id").Any() ? element.Elements("Id").FirstOrDefault().Value.ToString() : string.Empty;
                        }
                    }
                }

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get Users", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return "00536000002PFRDAA4";
        }


        /// <summary>
        /// Get slpcode  for Marco
        /// </summary>
        /// <param name="id">owner id</param>
        /// <returns>slpcode</returns>
        ///   <author>seema@veon.in</author>
        ///    <date>09/08/2016 </date>
        public string GetSlpcodeMarco(string ownerid)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+SLP__c+,+Id+,+Name+,+LastName+,+Username+from+user+Where+id+=+'", ownerid, "'");
                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);
                foreach (XElement element in elemment.Elements("records"))
                {
                    string slpcode = element.Descendants("SLP__c").Any() ? element.Elements("SLP__c").FirstOrDefault().Value.ToString() : string.Empty;
                    if (!string.IsNullOrEmpty(slpcode))
                    {
                        return slpcode;
                    }
                }
            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get slpcode", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return "47";
        }

        /// <summary>
        /// Get Customer_Auto_Number__c associated with a account from Salesforce
        /// </summary>
        /// <param name="id">Account id for which Customer_Auto_Number__c will be fetched</param>
        /// <returns>Customer_Auto_Number__c associated with the Account</returns>
        ///   <author>seema@veon.in</author>
        ///    <date>01 Mar 2016 </date>
        public XPathNavigator GetAccountAutonumber(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string addressurl = string.Concat("/query?q=SELECT+Customer_AutoNumber__c+from+Account+Where+id+=+'", id, "'");


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get AccountAutonumber", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return elemment.CreateNavigator();
        }

        /// <summary>
        ///To get All Accounts which is updated and whose Contact Persons are updated or created since Yesterday
        /// </summary>
        /// <returns>updated Account and account who has updated contact person and newly created contact person</returns>
        ///  <author>seema@veon.in</author>
        /// <date>29 Sept 2015 </date>
        public XPathNavigator getUpdatedAccounts()
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            XElement elemment1 = null;
            XElement Accounts = new XElement("Accounts");
            try
            {
                //For pull updated Accounts
                /*
                string addressurl = string.Concat("/query?q=SELECT+Id+,SAP_Customer_Number__c+,+RecordTypeId+,+Name+,+Email__c+,+Customer_Group__c+,+BillingStreet+,+BillingCity+,+BillingState+,+BillingPostalCode+,+BillingCountry+,+ShippingStreet+,+ShippingCity+,+ShippingState+,+ShippingPostalCode+,+ShippingCountry+,+Phone+,+Fax+from+Account+Where+LastModifiedDate+>+YESTERDAY");
                */

                // totus

                string addressurl = string.Concat("/query?q=SELECT+Id+,SAP_Customer_Number__c+,+Name+,+Email__c+,+BillingStreet+,+BillingCity+,+BillingState+,+BillingPostalCode+,+BillingCountry+,+ShippingStreet+,+ShippingCity+,+ShippingState+,+ShippingPostalCode+,+ShippingCountry+,+Phone+from+Account+Where+LastModifiedDate+>+YESTERDAY");

                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);


                //_adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //  soapEnvelopeSession.LoadXml(_getOutput);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);
                Accounts.Add(elemment);

                //For pull updated Accounts whose contact person is updated and contact persons is created
                /*
                addressurl = string.Concat("/query?q=SELECT+Id+,SAP_Customer_Number__c+,+Name+,+Email__c+,+RecordTypeId+,+Customer_Group__c+,+BillingStreet+,+BillingCity+,+BillingState+,+BillingPostalCode+,+BillingCountry+,+ShippingStreet+,+ShippingCity+,+ShippingState+,+ShippingPostalCode+,+ShippingCountry+,+Phone+,+Fax+from+Account+Where+Id+IN+(+SELECT+Contact.accountId+FROM+Contact+WHERE+contact.CreatedDate+>+YESTERDAY+OR+LastModifiedDate+>+YESTERDAY+)");
                */
                //totus

                addressurl = string.Concat("/query?q=SELECT+Id+,SAP_Customer_Number__c+,+Name+,+Email__c+,+BillingStreet+,+BillingCity+,+BillingState+,+BillingPostalCode+,+BillingCountry+,+ShippingStreet+,+ShippingCity+,+ShippingState+,+ShippingPostalCode+,+ShippingCountry+,+Phone+from+Account+Where+Id+IN+(+SELECT+Contact.accountId+FROM+Contact+WHERE+contact.CreatedDate+>+YESTERDAY+OR+LastModifiedDate+>+YESTERDAY+)");


                // _adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //   soapEnvelopeSession.LoadXml(_getOutput);

                responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);

                str = soapEnvelopeSession.InnerXml;
                elemment1 = XElement.Parse(str);
                Accounts.Add(elemment1);



            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get updatedaccount", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return Accounts.CreateNavigator();
        }


        /// <summary>
        /// Syspex
        ///To get All Accounts which is updated and whose Contact Persons are updated or created since Yesterday
        /// </summary>
        /// <returns>updated Account and account who has updated contact person and newly created contact person</returns>
        ///  <author>seema@veon.in</author>
        /// <date>28 june 2016 </date>
        public XPathNavigator getUpdatedAccountsSyspex(string company)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            XElement elemment1 = null;
            XElement elemment2 = null;
            XElement Accounts = new XElement("Accounts");
            try
            {
                //For pull updated Accounts

                string addressurl = string.Concat("/query?q=SELECT+Id+,Type+,+RecordTypeId+,+Name+,+Website+,+Description+,+CurrencyIsoCode+,+OwnerId+,+Company__c+,+Email__c+,+Account_Number__c+,+Commitment_Limit__c+,+Credit_Limit__c+,+Account_Balance__c+,+Customer_AutoNumber__c+,+Customer_Reporting_Type__c+,+Industry+,+Remarks_Transfer_of_Account__c+,+Payment_Terms__c+,+ROC_Number__c+,+Tax_Status__c+,+Tax_Reference__c+,+Sales_Employee__c+,+Phone+,+Fax+from+Account+Where+LastModifiedDate+>+YESTERDAY+and+Syncflag__c+=+'Y'+and+Company__c+=+'", company, "'");


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);


                //_adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //  soapEnvelopeSession.LoadXml(_getOutput);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);
                Accounts.Add(elemment);

                //For pull updated Accounts whose contact person is updated and contact persons is created

                addressurl = string.Concat("/query?q=SELECT+Id+,Type+,+RecordTypeId+,+Name+,+Website+,+Description+,+CurrencyIsoCode+,+OwnerId+,+Company__c+,+Email__c+,+Account_Number__c+,+Commitment_Limit__c+,+Credit_Limit__c+,+Account_Balance__c+,+Customer_AutoNumber__c+,+Customer_Reporting_Type__c+,+Industry+,+Remarks_Transfer_of_Account__c+,+Payment_Terms__c+,+ROC_Number__c+,+Tax_Status__c+,+Tax_Reference__c+,+Sales_Employee__c+,+Phone+,+Fax+from+Account+Where+Id+IN+(+SELECT+Contact.accountId+FROM+Contact+WHERE+contact.CreatedDate+>+YESTERDAY+OR+LastModifiedDate+>+YESTERDAY+)+and+Syncflag__c+=+'Y'+and+Company__c+=+'", company, "'");

                // _adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //   soapEnvelopeSession.LoadXml(_getOutput);

                responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);

                str = soapEnvelopeSession.InnerXml;
                elemment1 = XElement.Parse(str);
                Accounts.Add(elemment1);

                //For pull updated Accounts whose address is updated and address is created

                addressurl = string.Concat("/query?q=SELECT+Id+,Type+,+RecordTypeId+,+Name+,+Website+,+Description+,+CurrencyIsoCode+,+OwnerId+,+Company__c+,+Email__c+,+Account_Number__c+,+Commitment_Limit__c+,+Credit_Limit__c+,+Account_Balance__c+,+Customer_AutoNumber__c+,+Customer_Reporting_Type__c+,+Industry+,+Remarks_Transfer_of_Account__c+,+Payment_Terms__c+,+ROC_Number__c+,+Tax_Status__c+,+Tax_Reference__c+,+Sales_Employee__c+,+Phone+,+Fax+from+Account+Where+Id+IN+(+SELECT+Address__c.Account__c+FROM+Address__c+WHERE+Address__c.CreatedDate+>+YESTERDAY+OR+LastModifiedDate+>+YESTERDAY+)+and+Syncflag__c+=+'Y'+and+Company__c+=+'", company, "'");

                // _adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //   soapEnvelopeSession.LoadXml(_getOutput);

                responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);

                str = soapEnvelopeSession.InnerXml;
                elemment2 = XElement.Parse(str);
                Accounts.Add(elemment2);



            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get updatedaccount", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return Accounts.CreateNavigator();
        }


        /// <summary>
        /// Syspex
        ///To get service call number from service call for creating service report in sap
        /// </summary>
        /// <returns>service call number</returns>
        ///  <author>seema@veon.in</author>
        /// <date>22 sept 2016 </date>
        public XPathNavigator getservicecallnoSyspex(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            XElement Accounts = new XElement("Accounts");
            try
            {
                //For pull updated Accounts

                string addressurl = string.Concat("/query?q=SELECT+Name+from+Service_Call__c+where+Id+=+'", id, "'");


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);


                //_adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //  soapEnvelopeSession.LoadXml(_getOutput);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);
                Accounts.Add(elemment);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get service call number", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return Accounts.CreateNavigator();
        }

        /// <summary>
        /// Syspex
        ///To get Account number (cardcode)from Account for creating quotation in sap
        /// </summary>
        /// <returns>service call number</returns>
        ///  <author>seema@veon.in</author>
        /// <date>22 sept 2016 </date>
        public XPathNavigator getAccountNoSyspex(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            XElement Accounts = new XElement("Accounts");
            try
            {
                //For pull updated Accounts

                string addressurl = string.Concat("/query?q=SELECT+Account_Number__c+from+Account+where+Id+=+'", id, "'");


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);


                //_adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //  soapEnvelopeSession.LoadXml(_getOutput);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);
                Accounts.Add(elemment);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get account number", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return Accounts.CreateNavigator();
        }

        /// <summary>
        /// Syspex
        ///To get address fields from address for creating service report in sap
        /// </summary>
        /// <returns>address fields </returns>
        ///  <author>seema@veon.in</author>
        /// <date>22 sept 2016 </date>
        public string getaddressfieldsSyspex(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement element = null;
            try
            {
                //For pull updated Accounts

                string addressurl = string.Concat("/query?q=SELECT+Address_Line_1__c+,+City__c+,+State_Province__c+,+Country__c+,+Postal_Code_bill_to__c+from+Address__c+where+Id+=+'", id, "'");


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                element = XElement.Parse(str);

                string addr_line = element.Descendants("Address_Line_1__c").Any() ? element.Descendants("Address_Line_1__c").FirstOrDefault().Value.ToString() : string.Empty;
                string city = element.Descendants("City__c").Any() ? element.Descendants("City__c").FirstOrDefault().Value.ToString() : string.Empty;
                string state_prov = element.Descendants("State_Province__c").Any() ? element.Descendants("State_Province__c").FirstOrDefault().Value.ToString() : string.Empty;
                string countr = element.Descendants("Country__c").Any() ? element.Descendants("Country__c").FirstOrDefault().Value.ToString() : string.Empty;
                string postal = element.Descendants("Postal_Code_bill_to__c").Any() ? element.Descendants("Postal_Code_bill_to__c").FirstOrDefault().Value.ToString() : string.Empty;

                return string.Concat(addr_line, " ", city, " ", state_prov, " ", countr, " ", postal);
            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get address fields", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return string.Empty;

        }
        /// <summary>
        /// Marco
        ///To get All Accounts which is updated and whose Contact Persons are updated or created since Yesterday
        /// </summary>
        /// <returns>updated Account and account who has updated contact person and newly created contact person</returns>
        ///  <author>seema@veon.in</author>
        /// <date>6 sept 2016 </date>
        public XPathNavigator getUpdatedAccountsMarco()
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            XElement elemment1 = null;
            XElement elemment2 = null;
            XElement Accounts = new XElement("Accounts");
            try
            {
                //For pull updated Accounts

                string addressurl = "/query?q=SELECT+SAP_Customer_Number__c+,+Customer_Addresses_Billing__c+,+Customer_Addresses_Shipping__c+,+Email__c+,+Name+,+Query_Group1__c+,+Query_Group2__c+,+Query_Group3__c+,+Query_Group4__c+,+Query_Group5__c+,+Query_Group6__c+,+Query_Group7__c+,+Query_Group8__c+,+Query_Group9__c+,+Query_Group10__c+,+Query_Group11__c+,+Query_Group12__c+,+Query_Group13__c+,+Query_Group14__c+,+Query_Group15__c+,+Query_Group16__c+,+Query_Group17__c+,+Query_Group18__c+,+Query_Group19__c+,+Query_Group20__c+,+Payment_Terms__c+,+Industry+,+Territory__c+,+PriceBook__c+,+BillingStreet+,+BillingCity+,+BillingState+,+BillingPostalCode+,+BillingCountry+,+ShippingStreet+,+ShippingCity+,+ShippingState+,+ShippingPostalCode+,+ShippingCountry+,+Phone+,+OwnerId+,+Id+,+Type+,+Ignore_Flag__c+,+Foreign_Name__c+,+Credit__c+,+Slow_Pay_Hold__c+,+Fax+from+Account+Where+Syncflag__c+=+'Y'+and+LastModifiedById+!=+'00536000002PFRDAA4'+LIMIT+5";


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);


                //_adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //  soapEnvelopeSession.LoadXml(_getOutput);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);
                Accounts.Add(elemment);

                //For pull updated Accounts whose contact person is updated and contact persons is created

                addressurl = "/query?q=SELECT+SAP_Customer_Number__c+,+Customer_Addresses_Billing__c+,+Customer_Addresses_Shipping__c+,+Email__c+,+Name+,+Query_Group1__c+,+Query_Group2__c+,+Query_Group3__c+,+Query_Group4__c+,+Query_Group5__c+,+Query_Group6__c+,+Query_Group7__c+,+Query_Group8__c+,+Query_Group9__c+,+Query_Group10__c+,+Query_Group11__c+,+Query_Group12__c+,+Query_Group13__c+,+Query_Group14__c+,+Query_Group15__c+,+Query_Group16__c+,+Query_Group17__c+,+Query_Group18__c+,+Query_Group19__c+,+Query_Group20__c+,+Payment_Terms__c+,+Industry+,+Territory__c+,+PriceBook__c+,+BillingStreet+,+BillingCity+,+BillingState+,+BillingPostalCode+,+BillingCountry+,+ShippingStreet+,+ShippingCity+,+ShippingState+,+ShippingPostalCode+,+ShippingCountry+,+Phone+,+OwnerId+,+Id+,+Type+,+Ignore_Flag__c+,+Fax+from+Account+Where+Id+IN+(+SELECT+Contact.AccountId+FROM+contact+WHERE+Contact.LastModifiedById+!=+'00536000002PFRDAA4'+and+Contact.Updated_After_last_sync__c+=+True+)+and+Syncflag__c+=+'Y'+LIMIT+5";

                // _adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //   soapEnvelopeSession.LoadXml(_getOutput);

                responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);

                str = soapEnvelopeSession.InnerXml;
                elemment1 = XElement.Parse(str);
                Accounts.Add(elemment1);

                //For pull updated Accounts whose address is updated and address is created

                addressurl = "/query?q=SELECT+SAP_Customer_Number__c+,+Customer_Addresses_Billing__c+,+Customer_Addresses_Shipping__c+,+Email__c+,+Name+,+Query_Group1__c+,+Query_Group2__c+,+Query_Group3__c+,+Query_Group4__c+,+Query_Group5__c+,+Query_Group6__c+,+Query_Group7__c+,+Query_Group8__c+,+Query_Group9__c+,+Query_Group10__c+,+Query_Group11__c+,+Query_Group12__c+,+Query_Group13__c+,+Query_Group14__c+,+Query_Group15__c+,+Query_Group16__c+,+Query_Group17__c+,+Query_Group18__c+,+Query_Group19__c+,+Query_Group20__c+,+Payment_Terms__c+,+Industry+,+Territory__c+,+PriceBook__c+,+BillingStreet+,+BillingCity+,+BillingState+,+BillingPostalCode+,+BillingCountry+,+ShippingStreet+,+ShippingCity+,+ShippingState+,+ShippingPostalCode+,+ShippingCountry+,+Phone+,+OwnerId+,+Id+,+Type+,+Ignore_Flag__c+,+Fax+from+Account+Where+Id+IN+(+SELECT+Address__c.Account__c+FROM+Address__c+WHERE+Address__c.LastModifiedById+!=+'00536000002PFRDAA4'+and+Address__c.changed_after_last_sync__c+=+True+)+and+Syncflag__c+=+'Y'+LIMIT+5";

                // _adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //   soapEnvelopeSession.LoadXml(_getOutput);

                responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);

                str = soapEnvelopeSession.InnerXml;
                elemment2 = XElement.Parse(str);
                Accounts.Add(elemment2);



            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get updatedaccount", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return Accounts.CreateNavigator();
        }


        /// <summary>
        /// Afirmon
        ///To get All Accounts which are updated and whose Contact Persons are updated or created since Yesterday
        /// </summary>
        /// <returns>updated Account and account who has updated contact person and newly created contact person</returns>
        ///  <author>roja.r@veon.in</author>
        /// <date>6 January 2017 </date>

        public XPathNavigator getUpdatedAccountsTechnologySupply()
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            XElement elemment1 = null;
           
            XElement Accounts = new XElement("Accounts");
            try
            {
                //To pull updated Accounts

                string addressurl = "/query?q=SELECT+SAP_Customer_Number__c+,+Email__c+,+Name+,+BillingStreet+,+BillingCity+,+BillingState+,+BillingPostalCode+,+BillingCountry+,+ShippingStreet+,+ShippingCity+,+ShippingState+,+ShippingPostalCode+,+ShippingCountry+,+Phone+,+OwnerId+,+Id+,+Type+from+Account+Where+Syncflag__c+=+'Y'+and+LastModifiedDate+>=+Yesterday+LIMIT+3";

                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);


                //_adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //  soapEnvelopeSession.LoadXml(_getOutput);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);
                Accounts.Add(elemment);

                //To pull updated Accounts whose contact person is updated and contact persons is created

                addressurl = "/query?q=SELECT+SAP_Customer_Number__c+,+Email__c+,+Name+,+BillingStreet+,+BillingCity+,+BillingState+,+BillingPostalCode+,+BillingCountry+,+ShippingStreet+,+ShippingCity+,+ShippingState+,+ShippingPostalCode+,+ShippingCountry+,+Phone+,Id+from+Account+Where+Id+IN+(+SELECT+Contact.AccountId+FROM+contact+WHERE+Contact.LastModifiedDate+>=+Yesterday+)+and+Syncflag__c+=+'Y'+LIMIT+3";


                responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);

                str = soapEnvelopeSession.InnerXml;
                elemment1 = XElement.Parse(str);
                Accounts.Add(elemment1);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get updatedaccount", e.Message, ErrorTypes.None, Severitys.Low);
            }
            return Accounts.CreateNavigator();
        }



        /// <summary>
        /// Afirmon
        ///To get All Accounts which is updated and whose Contact Persons are updated or created since Yesterday
        /// </summary>
        /// <returns>updated Account and account who has updated contact person and newly created contact person</returns>
        ///  <author>seema@veon.in</author>
        /// <date>28 june 2016 </date>
        public XPathNavigator getUpdatedAccountsAfirmon(string rectype)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            XElement elemment1 = null;
            XElement elemment2 = null;
            XElement Accounts = new XElement("Accounts");
            try
            {
                //For pull updated Accounts

                string addressurl = string.Concat("/query?q=SELECT+Id+,SAP_Customer_Number__c+,+Bronze__c+,+SILVER__c+,+GOLD__c+,+PLATINUM__c+,+DIAMOND__c+,+DEALER_I__c+,+DEALER_II__c+,+DEALER_III__c+,+DEALER_IV__c+,+DEALER_V__c+,+DISTRIBUTOR_I__c+,+DISTRIBUTOR_II__c+,+DISTRIBUTOR_III__c+,+DISTRIBUTOR_IV__c+,+DISTRIBUTOR_V__c+,+SAPPHIRE__c+,+RUBY__c+,+EMERALD__c+,+SOUTH_AMERICA_LEVEL_1__c+,+SOUTH_AMERICA_LEVEL_2__c+,+SOUTH_AMERICA_LEVEL_3__c+,+JEWELRY_LEVEL_I__c+,+JEWELRY_LEVEL_II__c+,+JEWELRY_LEVEL_III__c+,+FOOD_SERVICE__c+,+MEDICAL_CONSUMER__c+,+SERVICE_MEDICAL__c+,+SERVICE_WEIGHING__c+,+MEDICAL_PROFESSIONAL__c+,+WEIGHING__c+,+Payment_Terms__c+,+Name+,+SAP_Code__c+,+Ship_Payment_Account__c+,+Market__c+,+Currency__c+,+Phone_2__c+,+Website+,+Shipping_Type__c+,+UPS_Account__c+,+FedEx_Account__c+,+DHL_Account__c+,+Channel_Code__c+,+Shipping_Location__c+,+Sales_Employee__c+,+Territory__c+,+Ship_Payment_Type__c+,+Default_Warehouse__c+,+Email__c+,+Price_List__c+,+Phone+,+Fax+from+Account+Where+LastModifiedDate+>+YESTERDAY+and+Syncflag__c+=+'Y'+and+RecordTypeId+=+'", rectype, "'");



                // testc sap recordtype id = 012R00000005E2VIAU


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);


                //_adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //  soapEnvelopeSession.LoadXml(_getOutput);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);
                Accounts.Add(elemment);

                //For pull updated Accounts whose contact person is updated and contact persons is created

                addressurl = string.Concat("/query?q=SELECT+Id+,SAP_Customer_Number__c+,+Bronze__c+,+SILVER__c+,+GOLD__c+,+PLATINUM__c+,+DIAMOND__c+,+DEALER_I__c+,+DEALER_II__c+,+DEALER_III__c+,+DEALER_IV__c+,+DEALER_V__c+,+DISTRIBUTOR_I__c+,+DISTRIBUTOR_II__c+,+DISTRIBUTOR_III__c+,+DISTRIBUTOR_IV__c+,+DISTRIBUTOR_V__c+,+SAPPHIRE__c+,+RUBY__c+,+EMERALD__c+,+SOUTH_AMERICA_LEVEL_1__c+,+SOUTH_AMERICA_LEVEL_2__c+,+SOUTH_AMERICA_LEVEL_3__c+,+JEWELRY_LEVEL_I__c+,+JEWELRY_LEVEL_II__c+,+JEWELRY_LEVEL_III__c+,+FOOD_SERVICE__c+,+MEDICAL_CONSUMER__c+,+SERVICE_MEDICAL__c+,+SERVICE_WEIGHING__c+,+MEDICAL_PROFESSIONAL__c+,+WEIGHING__c+,+Payment_Terms__c+,+Name+,+SAP_Code__c+,+Ship_Payment_Account__c+,+Market__c+,+Currency__c+,+Phone_2__c+,+Website+,+Shipping_Type__c+,+UPS_Account__c+,+FedEx_Account__c+,+DHL_Account__c+,+Channel_Code__c+,+Shipping_Location__c+,+Sales_Employee__c+,+Territory__c+,+Ship_Payment_Type__c+,+Default_Warehouse__c+,+Email__c+,+Price_List__c+,+Phone+,+Fax+from+Account+Where+Id+IN+(+SELECT+Contact.accountId+FROM+Contact+WHERE+contact.CreatedDate+>+YESTERDAY+OR+LastModifiedDate+>+YESTERDAY+)and+Syncflag__c+=+'Y'+and+RecordTypeId+=+'", rectype, "'");

                // _adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //   soapEnvelopeSession.LoadXml(_getOutput);

                responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);

                str = soapEnvelopeSession.InnerXml;
                elemment1 = XElement.Parse(str);
                Accounts.Add(elemment1);

                //For pull updated Accounts whose Address is updated and Address is created

                addressurl = string.Concat("/query?q=SELECT+Id+,SAP_Customer_Number__c+,+Bronze__c+,+SILVER__c+,+GOLD__c+,+PLATINUM__c+,+DIAMOND__c+,+DEALER_I__c+,+DEALER_II__c+,+DEALER_III__c+,+DEALER_IV__c+,+DEALER_V__c+,+DISTRIBUTOR_I__c+,+DISTRIBUTOR_II__c+,+DISTRIBUTOR_III__c+,+DISTRIBUTOR_IV__c+,+DISTRIBUTOR_V__c+,+SAPPHIRE__c+,+RUBY__c+,+EMERALD__c+,+SOUTH_AMERICA_LEVEL_1__c+,+SOUTH_AMERICA_LEVEL_2__c+,+SOUTH_AMERICA_LEVEL_3__c+,+JEWELRY_LEVEL_I__c+,+JEWELRY_LEVEL_II__c+,+JEWELRY_LEVEL_III__c+,+FOOD_SERVICE__c+,+MEDICAL_CONSUMER__c+,+SERVICE_MEDICAL__c+,+SERVICE_WEIGHING__c+,+MEDICAL_PROFESSIONAL__c+,+WEIGHING__c+,+Payment_Terms__c+,+Name+,+SAP_Code__c+,+Ship_Payment_Account__c+,+Market__c+,+Currency__c+,+Phone_2__c+,+Website+,+Shipping_Type__c+,+UPS_Account__c+,+FedEx_Account__c+,+DHL_Account__c+,+Channel_Code__c+,+Shipping_Location__c+,+Sales_Employee__c+,+Territory__c+,+Ship_Payment_Type__c+,+Default_Warehouse__c+,+Email__c+,+Price_List__c+,+Phone+,+Fax+from+Account+Where+Id+IN+(+SELECT+Address__c.Account__c+FROM+Address__c+WHERE+Address__c.CreatedDate+>+YESTERDAY+OR+LastModifiedDate+>+YESTERDAY+)and+Syncflag__c+=+'Y'+and+RecordTypeId+=+'", rectype, "'");

                // _adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //   soapEnvelopeSession.LoadXml(_getOutput);

                responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);

                str = soapEnvelopeSession.InnerXml;
                elemment2 = XElement.Parse(str);
                Accounts.Add(elemment2);



            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get updatedaccount", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return Accounts.CreateNavigator();
        }


        /// <summary>
        /// Syspex
        ///To get All Products which is updated since Yesterday
        /// </summary>
        /// <returns>updated Products</returns>
        ///  <author>seema@veon.in</author>
        /// <date>30 june 2016 </date>
        public XPathNavigator getUpdatedProductsSyspex(string company)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            XElement Accounts = new XElement("Product");
            try
            {
                //For pull updated Accounts

                string addressurl = string.Concat("/query?q=SELECT+Id+,Remarks__c+,+Lead_time__c+,+ProductCode+,+Property_Name__c+,+Name+,+Product_Status__c+,+Brand_Origin__c+,+Inventory_Item__c+,+Item_Family__c+,+Company__c+,+Item_Manage_by__c+,+YBID__c+,+Preferred_Vendor__c+,+Purchasing_UOM__c+,+Item_Per_Purchase_Unit__c+,+Length_Sales__c+,+Width_Sales__c+,+Weight_in_Kg__c+,+Height_Sales__c+,+Volume_Unit_Sales__c+,+Volume_Unit__c+,+Length__c+,+Width__c+,+Height__c+,+Weight_Sales__c+,+Item_Per_Sales_Unit__c+,+Minimum_Order_Qty__c+,+Order_Multiple__c+,+Item_Group__c+,+Sales_UOM__c+from+Product2+Where+LastModifiedDate+>+YESTERDAY+and+Syncflag__c+=+'Y'+and+Company__c+=+'", company, "'");


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);


                //_adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //  soapEnvelopeSession.LoadXml(_getOutput);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);
                Accounts.Add(elemment);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get updatedproduct", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return Accounts.CreateNavigator();
        }

        /// <summary>
        /// Syspex
        ///To get All Products which are updated since Yesterday
        /// </summary>
        /// <returns>updated Products</returns>
        ///  <author>roja.r@veon.in</author>
        /// <date>6 January 2017 </date>
        /// 

        public XPathNavigator getUpdatedProductsTechnologySupply()
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            XElement Accounts = new XElement("Product");
            try
            {
                //For pull updated Accounts

                string addressurl = string.Concat("/query?q=SELECT+Id+,+ProductCode+,+Name+,+Description+,+SAP_Product_Number__c+from+Product2+Where+LastModifiedDate+>=+YESTERDAY+and+Syncflag__c+=+'Y'");


                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);


                //_adapter.AppEntity.AppEntityAction = addressurl;
                // _appCore.ExecutePullCommand(getOutput);
                //  soapEnvelopeSession.LoadXml(_getOutput);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);
                Accounts.Add(elemment);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get updatedproduct", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return Accounts.CreateNavigator();
        }
        

        /// <summary>
        /// This function is used for getting prices associated with a product
        /// </summary>
        /// <param name="id">Id of the product for which price will be fetched</param>
        /// <returns>product prices</returns>
       
        
        public XPathNavigator GetProductPrices(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {
                string queryString = string.Concat("/query?q=SELECT+Pricebookentry.Pricebook2Id+,+Pricebookentry.Id+,+Pricebookentry.Product2Id+,+Pricebookentry.UnitPrice+from+Pricebookentry+where+Pricebookentry.Product2Id+=+'", id, "'");

                string responseData = ExtendedAPIFunction(queryString);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get productprices", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return elemment.CreateNavigator();
        }

        /// <summary>
        /// This fuvtion is written for for performing any API call based on the parameters pass
        /// </summary>
        /// <param name="queryString">Query string for callin Salesforce API</param>
        /// <returns>Data after calling Salesforce Api</returns>
        public XPathNavigator FetchRelatedData(string queryString)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elemment = null;
            try
            {


                //_adapter.AppEntity.AppEntityAction = addressurl;
                //_appCore.ExecutePullCommand(getOutput);
                string responseData = ExtendedAPIFunction(queryString);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get fetchrelateddata", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return elemment.CreateNavigator();
        }

        /// <summary>
        /// Get addresses associated with a account from the salesforce
        /// Customize return structure specific to the customer
        /// Developed with respect to SAP - SF integration
        /// </summary>
        /// <param name="AccountId"></param>
        /// <param name="OrganizationKey"></param>
        /// <returns></returns>
        /// <author>aejaz@veon</author>
        /// <date>04 Oct 2017</date>
        public XPathNavigator GetAccountAddresses(string AccountId, string OrganizationKey)
        {
            BaseOrganization org;
            switch (OrganizationKey) {
                case "Safari":
                    org = new Safari(this);
                    break;
                default:
                    org = new BaseOrganization(this);
                    break;
            }

            return org.GetAccountAddresses(AccountId).CreateNavigator();
        }


        /// <summary>
        /// Get Addresses associated with a account from Salesforce
        /// </summary>
        /// <param name="id">Account id for which contact person will be fetched</param>
        /// <returns>contact persons associated with the Account</returns>
        ///   <author>seema@veon.in</author>
        ///   <date>19 Aug 2015 </date>
        public XPathNavigator GetAccountAddresses(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elements = null;
            XElement Addresses = new XElement("Addresses");

            try
            {
                string addressurl = string.Concat("/query?q=SELECT+BillingStreet+,+BillingCountry+,+BillingCity+,+BillingState+,+BillingPostalCode+,+ShippingStreet+,+ShippingCity+,+ShippingState+,+ShippingCountry+,+ShippingPostalCode+from+Account+Where+Id+=+'", id, "'");

                //_adapter.AppEntity.AppEntityAction = addressurl;
                //_appCore.ExecutePullCommand(getOutput);

                //soapEnvelopeSession.LoadXml(_getOutput);
                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elements = XElement.Parse(str);

                if (elements.Elements("records").Count() == 0)
                {
                    XElement Address = new XElement("Address");
                    Address.Add(new XElement("type", "bo_BillTo"));
                    /*		elemment.Element("records").Element("BillingStreet").Value.ToString()	*/
                    Address.Add(new XElement("Street", ""));
                    Address.Add(new XElement("City", ""));
                    Address.Add(new XElement("State", ""));
                    Address.Add(new XElement("PostalCode", ""));
                    Address.Add(new XElement("Country", ""));
                    Addresses.Add(Address);

                    XElement Address1 = new XElement("Address");
                    Address1.Add(new XElement("type", "bo_ShipTo"));
                    Address1.Add(new XElement("Street", ""));
                    Address1.Add(new XElement("City", ""));
                    Address1.Add(new XElement("State", ""));
                    Address1.Add(new XElement("PostalCode", ""));
                    Address1.Add(new XElement("Country", ""));
                    Addresses.Add(Address1);
                }

                foreach (XElement element in elements.Elements("records"))
                {
                    var obj = element.Elements().FirstOrDefault();

                    XElement Address = new XElement("Address");
                    Address.Add(new XElement("type", "bo_BillTo"));
                    /*		elemment.Element("records").Element("BillingStreet").Value.ToString()	*/
                    Address.Add(new XElement("Street", element.Descendants("BillingStreet").Any() ? element.Elements("BillingStreet").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("City", element.Descendants("BillingCity").Any() ? element.Elements("BillingCity").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("State", element.Descendants("BillingState").Any() ? element.Elements("BillingState").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("PostalCode", element.Descendants("BillingPostalCode").Any() ? element.Elements("BillingPostalCode").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Country", element.Descendants("BillingCountry").Any() ? element.Elements("BillingCountry").FirstOrDefault().Value.ToString() : string.Empty));
                    Addresses.Add(Address);

                    XElement Address1 = new XElement("Address");
                    Address1.Add(new XElement("type", "bo_ShipTo"));
                    Address1.Add(new XElement("Street", element.Descendants("ShippingStreet").Any() ? element.Elements("ShippingStreet").FirstOrDefault().Value.ToString() : string.Empty));
                    Address1.Add(new XElement("City", element.Descendants("ShippingCity").Any() ? element.Elements("ShippingCity").FirstOrDefault().Value.ToString() : string.Empty));
                    Address1.Add(new XElement("State", element.Descendants("ShippingState").Any() ? element.Elements("ShippingState").FirstOrDefault().Value.ToString() : string.Empty));
                    Address1.Add(new XElement("PostalCode", element.Descendants("ShippingPostalCode").Any() ? element.Elements("ShippingPostalCode").FirstOrDefault().Value.ToString() : string.Empty));
                    Address1.Add(new XElement("Country", element.Descendants("ShippingCountry").Any() ? element.Elements("ShippingCountry").FirstOrDefault().Value.ToString() : string.Empty));
                    Addresses.Add(Address1);

                }

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get accountaddresses", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return Addresses.CreateNavigator();
        }

        /// <summary>
        /// Get Addresses associated with a account from Salesforce
        /// </summary>
        /// <param name="id">Account id for which contact person will be fetched</param>
        /// <returns>Address associated with the Account</returns>
        ///   <author>roja.r@veon.in</author>
        ///   <date>16 JUN 2017 </date>
        public XPathNavigator GetAccountAddressesFonkel(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elements = null;
            XElement Addresses = new XElement("Addresses");

            try
            {
                string addressurl = string.Concat("/query?q=SELECT+Id+,+Socio_de_negocio__c+,+Colonia__c+,+Name+,+Calle_y_numero__c+,+Ciudad__c+,+Delegacion_Municipio__c+,+Estado__c+,+Codigo_Postal__c+,+Pais__c+,+Tipo__c+from+Ubicacion__c+Where+Socio_de_negocio__c+=+'", id, "'");

                //_adapter.AppEntity.AppEntityAction = addressurl;
                //_appCore.ExecutePullCommand(getOutput);

                //soapEnvelopeSession.LoadXml(_getOutput);
                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elements = XElement.Parse(str);


                foreach (XElement element in elements.Elements("records"))
                {
                    var obj = element.Elements().FirstOrDefault();

                    XElement Address = new XElement("Address");

                    string type = element.Descendants("Address_Type__c").Any() ? element.Elements("Address_Type__c").FirstOrDefault().Value.ToString() : string.Empty;
                    if (!string.IsNullOrEmpty(type))
                    {
                        if (type == "B")
                        {
                            Address.Add(new XElement("type", "bo_BillTo"));
                        }
                        else if (type == "S")
                        {
                            Address.Add(new XElement("type", "bo_ShipTo"));
                        }
                    }


                    Address.Add(new XElement("Name", element.Descendants("Name").Any() ? element.Elements("Name").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Id", element.Descendants("Id").Any() ? element.Elements("Id").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Ciudad__c", element.Descendants("Ciudad__c").Any() ? element.Elements("Ciudad__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Socio_de_negocio__c", element.Descendants("Socio_de_negocio__c").Any() ? element.Elements("Socio_de_negocio__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Calle_y_numero__c", element.Descendants("Calle_y_numero__c").Any() ? element.Elements("Calle_y_numero__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Colonia__c", element.Descendants("Colonia__c").Any() ? element.Elements("Colonia__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Delegacion_Municipio__c", element.Descendants("Delegacion_Municipio__c").Any() ? element.Elements("Delegacion_Municipio__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Estado__c", element.Descendants("Estado__c").Any() ? element.Elements("Estado__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Codigo_Postal__c", element.Descendants("Codigo_Postal__c").Any() ? element.Elements("Codigo_Postal__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Pais__c", element.Descendants("Pais__c").Any() ? element.Elements("Pais__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Tipo__c", element.Descendants("Tipo__c").Any() ? element.Elements("Tipo__c").FirstOrDefault().Value.ToString() : string.Empty));

                    Addresses.Add(Address);

                }

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get accountaddresses", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return Addresses.CreateNavigator();
        }


        /// <summary>
        /// Get Addresses associated with a account from Salesforce afirmon
        /// </summary>
        /// <param name="id">Account id for which contact person will be fetched</param>
        /// <returns>contact persons associated with the Account</returns>
        ///   <author>seema@veon.in</author>
        ///   <date>27 july 2016 </date>
        public XPathNavigator GetAccountAddressesAfirmon(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elements = null;
            XElement Addresses = new XElement("Addresses");

            try
            {
                string addressurl = string.Concat("/query?q=SELECT+Name+,+Account_Type__c+,+Id+,+Account__c+,+Street_PO_Box__c+,+City__c+,+State__c+,+Country__c+,+Zip_Code__c+,+Block__c+,+Street_No__c+,+Building_Floor_Room__c+,+UPS_Account__c+,+FedEx_Account__c+,+DHL_Account__c+,+Tax_Code__c+,+Tax_Office__c+,+EDI_Location_Number__c+,+EDI_Shipping_Account__c+,+County__c+from+Address__c+Where+Account__c+=+'", id, "'");

                //_adapter.AppEntity.AppEntityAction = addressurl;
                //_appCore.ExecutePullCommand(getOutput);

                //soapEnvelopeSession.LoadXml(_getOutput);
                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elements = XElement.Parse(str);


                foreach (XElement element in elements.Elements("records"))
                {
                    var obj = element.Elements().FirstOrDefault();

                    XElement Address = new XElement("Address");

                    string type = element.Descendants("Account_Type__c").Any() ? element.Elements("Account_Type__c").FirstOrDefault().Value.ToString() : string.Empty;
                    if (!string.IsNullOrEmpty(type))
                    {
                        if (type == "Bill-To")
                        {
                            Address.Add(new XElement("type", "bo_BillTo"));
                        }
                        else if (type == "Ship-To")
                        {
                            Address.Add(new XElement("type", "bo_ShipTo"));
                        }
                    }


                    Address.Add(new XElement("Name", element.Descendants("Name").Any() ? element.Elements("Name").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Id", element.Descendants("Id").Any() ? element.Elements("Id").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Account__c", element.Descendants("Account__c").Any() ? element.Elements("Account__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Street_PO_Box__c", element.Descendants("Street_PO_Box__c").Any() ? element.Elements("Street_PO_Box__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("City__c", element.Descendants("City__c").Any() ? element.Elements("City__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("State__c", element.Descendants("State__c").Any() ? element.Elements("State__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Country__c", element.Descendants("Country__c").Any() ? element.Elements("Country__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Zip_Code__c", element.Descendants("Zip_Code__c").Any() ? element.Elements("Zip_Code__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Street_No__c", element.Descendants("Street_No__c").Any() ? element.Elements("Street_No__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Block__c", element.Descendants("Block__c").Any() ? element.Elements("Block__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Building_Floor_Room__c", element.Descendants("Building_Floor_Room__c").Any() ? element.Elements("Building_Floor_Room__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("UPS_Account__c", element.Descendants("UPS_Account__c").Any() ? element.Elements("UPS_Account__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("FedEx_Account__c", element.Descendants("FedEx_Account__c").Any() ? element.Elements("FedEx_Account__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("DHL_Account__c", element.Descendants("DHL_Account__c").Any() ? element.Elements("DHL_Account__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Tax_Code__c", element.Descendants("Tax_Code__c").Any() ? element.Elements("Tax_Code__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Tax_Office__c", element.Descendants("Tax_Office__c").Any() ? element.Elements("Tax_Office__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("EDI_Location_Number__c", element.Descendants("EDI_Location_Number__c").Any() ? element.Elements("EDI_Location_Number__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("EDI_Shipping_Account__c", element.Descendants("EDI_Shipping_Account__c").Any() ? element.Elements("EDI_Shipping_Account__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("County__c", element.Descendants("County__c").Any() ? element.Elements("County__c").FirstOrDefault().Value.ToString() : string.Empty));


                    Addresses.Add(Address);



                }

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get accountaddresses", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return Addresses.CreateNavigator();
        }



        /// <summary>
        /// Get Addresses associated with a account from Salesforce marco
        /// </summary>
        /// <param name="id">Account id for which contact person will be fetched</param>
        /// <returns>contact persons associated with the Account</returns>
        ///   <author>seema@veon.in</author>
        ///   <date>1th sept 2016 </date>
        public XPathNavigator GetAccountAddressesMarco(string id)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();
            XElement elements = null;
            XElement Addresses = new XElement("Addresses");

            try
            {
                string addressurl = string.Concat("/query?q=SELECT+Name+,+Address_Type__c+,+Id+,+Street__c+,+Phone__c+,+Email__c+,+Account__c+,+City__c+,+State__c+,+Country__c+,+Zip__c+,+Default__c+from+Address__c+Where+Account__c+=+'", id, "'");

                //_adapter.AppEntity.AppEntityAction = addressurl;
                //_appCore.ExecutePullCommand(getOutput);

                //soapEnvelopeSession.LoadXml(_getOutput);
                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elements = XElement.Parse(str);


                foreach (XElement element in elements.Elements("records"))
                {
                    var obj = element.Elements().FirstOrDefault();

                    XElement Address = new XElement("Address");

                    string type = element.Descendants("Address_Type__c").Any() ? element.Elements("Address_Type__c").FirstOrDefault().Value.ToString() : string.Empty;
                    if (!string.IsNullOrEmpty(type))
                    {
                        if (type == "Billing")
                        {
                            Address.Add(new XElement("type", "bo_BillTo"));
                        }
                        else if (type == "Shipping")
                        {
                            Address.Add(new XElement("type", "bo_ShipTo"));
                        }
                    }


                    Address.Add(new XElement("Name", element.Descendants("Name").Any() ? element.Elements("Name").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Id", element.Descendants("Id").Any() ? element.Elements("Id").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Account__c", element.Descendants("Account__c").Any() ? element.Elements("Account__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Street__c", element.Descendants("Street__c").Any() ? element.Elements("Street__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("City__c", element.Descendants("City__c").Any() ? element.Elements("City__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("State__c", element.Descendants("State__c").Any() ? element.Elements("State__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Country__c", element.Descendants("Country__c").Any() ? element.Elements("Country__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Phone__c", element.Descendants("Phone__c").Any() ? element.Elements("Phone__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Email__c", element.Descendants("Email__c").Any() ? element.Elements("Email__c").FirstOrDefault().Value.ToString() : string.Empty));
                    Address.Add(new XElement("Zip__c", element.Descendants("Zip__c").Any() ? element.Elements("Zip__c").FirstOrDefault().Value.ToString() : string.Empty));

                    Addresses.Add(Address);

                }

            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get accountaddresses", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return Addresses.CreateNavigator();
        }

        /// <summary>
        /// Syspex
        ///To get All update special price since Yesterday from salesforce and get existing special prices from SAP for same cardcode and itemcode and merge them and update to SAP 
        /// </summary>
        /// <returns>Merge All update special price since Yesterday from salesforce and get existing special prices from SAP </returns>
        ///  <author>seema@veon.in</author>
        /// <date>5 Aug 2016 </date>
        public XPathNavigator getUpdatedspecialpriceSyspex(XPathNavigator spp2, string bpid, string itemid, string company)
        {
            XmlDocument soapEnvelopeSession = new XmlDocument();

            XElement elemment = null;
            XElement B1data = XElement.Parse(spp2.OuterXml);
            XElement SPrice = new XElement("SpecialPrices");
            XNamespace nsr = "http://www.sap.com/SBO/DIS";
            try
            {
                //For pull special price from sf

                string addressurl = string.Concat("/query?q=SELECT+Id+,+CurrencyIsoCode+,+Customer__c+,+Unit_Price_Per_Qty__c+,+MOQ__c+,+Price__c+,+Product__c+from+Customer_Price__c+Where+LastModifiedDate+>+YESTERDAY+and+Customer__c+=+'", bpid, "'+and+Product__c+=+'", itemid, "'+and+Company__c+=+'", company, "'");



                string responseData = ExtendedAPIFunction(addressurl);
                soapEnvelopeSession.LoadXml(responseData);
                string str = soapEnvelopeSession.InnerXml;
                elemment = XElement.Parse(str);

                foreach (XElement element in elemment.Elements("records"))
                {
                    XElement records = new XElement("records");
                    records.Add(new XElement("Id", element.Descendants("Id").Any() ? element.Elements("Id").FirstOrDefault().Value.ToString() : string.Empty));
                    records.Add(new XElement("CurrencyIsoCode", element.Descendants("CurrencyIsoCode").Any() ? element.Elements("CurrencyIsoCode").FirstOrDefault().Value.ToString() : string.Empty));
                    records.Add(new XElement("Customer__c", element.Descendants("Customer__c").Any() ? element.Elements("Customer__c").FirstOrDefault().Value.ToString() : string.Empty));
                    records.Add(new XElement("MOQ__c", element.Descendants("MOQ__c").Any() ? element.Elements("MOQ__c").FirstOrDefault().Value.ToString() : string.Empty));
                    records.Add(new XElement("Price__c", element.Descendants("Price__c").Any() ? element.Elements("Price__c").FirstOrDefault().Value.ToString() : string.Empty));
                    records.Add(new XElement("Product__c", element.Descendants("Product__c").Any() ? element.Elements("Product__c").FirstOrDefault().Value.ToString() : string.Empty));
                    SPrice.Add(records);
                }

                //For pull special price from sap b1

                foreach (XElement element in B1data.Descendants(nsr + "row"))
                {
                    XElement records1 = new XElement("records");
                    records1.Add(new XElement("Id", element.Descendants(nsr + "U_custprice").Any() ? element.Elements(nsr + "U_custprice").FirstOrDefault().Value.ToString() : string.Empty));

                    records1.Add(new XElement("CurrencyIsoCode", element.Descendants(nsr + "Currency").Any() ? element.Elements(nsr + "Currency").FirstOrDefault().Value.ToString() : string.Empty));
                    records1.Add(new XElement("Customer__c", element.Descendants(nsr + "CardCode").Any() ? element.Elements(nsr + "CardCode").FirstOrDefault().Value.ToString() + "$" : string.Empty));
                    records1.Add(new XElement("MOQ__c", element.Descendants(nsr + "Amount").Any() ? element.Elements(nsr + "Amount").FirstOrDefault().Value.ToString() : string.Empty));
                    records1.Add(new XElement("Price__c", element.Descendants(nsr + "Price").Any() ? element.Elements(nsr + "Price").FirstOrDefault().Value.ToString() : string.Empty));
                    records1.Add(new XElement("Product__c", element.Descendants(nsr + "ItemCode").Any() ? element.Elements(nsr + "ItemCode").FirstOrDefault().Value.ToString() + "$" : string.Empty));
                    SPrice.Add(records1);
                }
            }
            catch (Exception e)
            {
                this._logger.ErrorLog("failed to get updatedspecialprice", e.Message, ErrorTypes.None, Severitys.Low);
            }

            return SPrice.CreateNavigator();
        }


        /// <summary>
        /// Formating date
        /// </summary>
        /// <param name="dateime"></param>
        /// <returns></returns>
        public string DateFormat(string dateime)
        {
            DateTime date = Convert.ToDateTime(dateime);
            return date.ToString("yyyyMMdd");
        }

        /// <summary>
        /// Get date difference between two dates
        /// </summary>
        /// <param name="dateime"></param>
        /// <returns></returns>
        public int DateDifference(string date1, string date2)
        {
            DateTime sdate = Convert.ToDateTime(date1);
            DateTime edate = Convert.ToDateTime(date2);
            return (sdate - edate).Days;
          
        }


        private SQLFactory _factory;
        public SQLFactory Factory
        {
            get
            {
                this._factory = this._factory ?? new SQLFactory();
                return this._factory;
            }
        }

        public string GetSalesforceIdReference(string databaseKey, string sourceId)
        {
            string connectionId = this._context.CurrentTouchpoint.Task.OrgAppSyncId;
            string sql = string.Format("Select DestinationId from [{0}_{1}] where SourceId = '{2}'", databaseKey, connectionId, sourceId);

            var tResult = this.Factory.ExecuteScalarAsync(sql);

            var data = tResult.Result;

            if (data != null)
            {
                return data.ToString();
            }

            return String.Empty;
        }

        /// <summary>
        /// Get Destination Date Format
        /// </summary>
        /// <param name="date">Source Date</param>
        /// <returns>Destination DateFormat</returns>
        public string GetDateFormat(string date)
        {
            var changeDateFormat = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);
            return changeDateFormat.ToString("yyyy-MM-dd");
        }

       

        /// <summary>
        /// This function is used for getting salesforce session id
        /// </summary>
        /// <returns>Salesforce session</returns>
        private string ReturnSalesforceSession()
        {

            Adapter obj = new Adapter();

            string logindata = obj.LoginState(this.CredentialObject);
            JObject obj1 = JObject.Parse(logindata);
            string token = (string)obj1["access_token"];
            return token;
        }
        /// <summary>
        /// This function is used for calling Salesforce API 
        /// </summary>
        /// <param name="actionandparams">parameters for calling Saleforce API</param>
        /// <returns>Data returned after calling Saleforce API</returns>
        public string ExtendedAPIFunction(string actionandparams)
        {

            string session = ReturnSalesforceSession();
            Adapter obj = new Adapter();

            string logindata = obj.LoginState(this.CredentialObject);
            JObject obj1 = JObject.Parse(logindata);
            string Serviceurl = (string)obj1["instance_url"];

            string returnData = string.Empty;
            HttpClient queryClient = new HttpClient();
            if (actionandparams.Substring(actionandparams.Length - 1, 1) == "?")
            {
                actionandparams = actionandparams.Remove(actionandparams.Length - 1);
            }
            string restQuery = Serviceurl + "/services/data/v33.0" + actionandparams;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, restQuery);
            request.Headers.Add("Authorization", "Bearer " + session);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            HttpResponseMessage response = queryClient.SendAsync(request).Result;
            returnData = response.Content.ReadAsStringAsync().Result;

            return returnData;
        }

        /// <summary>
        /// Prepare Key value pair for Pricebook ID of SAP and Salesforce
        /// </summary>
        /// <param name="input">1:ASSD000000001~2:ASSD000000002~3:ASSD000000003</param>
        /// <param name="pairDelimiter">:</param>
        /// <param name="stringDelimiter">~</param>
        /// <returns></returns>
        /// <author>Aejaz@veon</author>
        /// <date>09 Sep 2016</date>
        public XPathNavigator GetPricebookIdPair(string input, string pairDelimiter, string stringDelimiter)
        {
            XElement PricebookIdPair = new XElement("PricebookIdPair");
            try
            {
                string[] lst_IDPair = input.Split(stringDelimiter.ToCharArray());

                foreach (string str_IDPair in lst_IDPair)
                {
                    XElement item = new XElement("item");
                    string[] lst_ID = str_IDPair.Split(pairDelimiter.ToCharArray());
                    item.Add(new XElement("key", (lst_ID.Length > 0) ? lst_ID[0] : string.Empty));
                    item.Add(new XElement("value", (lst_ID.Length > 1) ? lst_ID[1] : string.Empty));
                    item.Add(new XElement("udf", (lst_ID.Length > 2) ? lst_ID[2] : string.Empty));
                    PricebookIdPair.Add(item);
                }
            }
            catch (Exception ex)
            {
                this._logger.ErrorLog(ex.Message, ErrorTypes.Sync, Severitys.Medium);
            }

            return PricebookIdPair.CreateNavigator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <author>aejaz@veon.in</author>
        /// <date>12Sep2016</date>
        public XPathNavigator ExecuteSFQuery(string query)
        {
            //this._logger.InfoLog("Start DateTime:", DateTime.Now.ToString());
            string returnData = "<root></root>";
            try
            {
                string session = ReturnSalesforceSession();
                Adapter obj = new Adapter();

                string logindata = obj.LoginState(this.CredentialObject);
                JObject obj1 = JObject.Parse(logindata);
                string Serviceurl = (string)obj1["instance_url"];

                HttpClient queryClient = new HttpClient();
                string restQuery = Serviceurl + query;
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, restQuery);
                request.Headers.Add("Authorization", "Bearer " + session);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                HttpResponseMessage response = queryClient.SendAsync(request).Result;
                returnData = response.Content.ReadAsStringAsync().Result;
            }
            catch
            {
                this._logger.InfoLog("SalesForce Query Failed.", string.Format("Query - '{0}'", query));
            }

            //this._logger.InfoLog("End DateTime:", DateTime.Now.ToString());
            return XElement.Parse(returnData).CreateNavigator();
        }

        /// <summary>
        /// Converts Image information  to base64 string
        /// </summary>
        /// <param name="imgPath"></param>
        /// <returns>base64 value</returns>
        /// <author>aejaz@veon</author>
        /// <date>12Sep2016</date>
        public string Base64StringImage(string imgpath)
        {
            try
            {
                string base64String = string.Empty;
                using (WebClient client = new WebClient())
                {

                    byte[] imageData = client.DownloadData(@imgpath);
                    using (MemoryStream ms = new MemoryStream(imageData))
                    {

                        var image = System.Drawing.Image.FromStream(ms);
                        byte[] imageBytes = ms.ToArray();
                        base64String = Convert.ToBase64String(imageBytes);
                    }
                }
                return base64String;
            }
            catch (Exception ex)
            {
                this._logger.ErrorLog(string.Format("Image file path {0} not found\n\t Exception : {1}", imgpath, ex.Message), ex, ErrorTypes.None, Severitys.Low);

                return string.Empty;
            }

        }

        /// <summary>
        /// base64 encrypt file path
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public string Base64String(string filepath)
        {
            try
            {
                byte[] filedata = File.ReadAllBytes(@filepath);
                return Convert.ToBase64String(filedata);
            }
            catch (Exception ex)
            {
                this._logger.ErrorLog(string.Format("file path {0} not found\n\t Exception : {1}", filepath, ex.Message), ex, ErrorTypes.None, Severitys.Low);
                return string.Empty;
            }
        }
        /// <summary>
        /// FOr generic pricebook update
        /// </summary>
        /// <param name="data">whole XML</param>
        /// <param name="itemData">itemcode</param>
        /// <returns>XML</returns>
        public XPathNavigator DataReFormat(XPathNavigator data, string itemData)
        {
            var result = new XElement("Data");
            var itemDataCollection = XElement.Parse(data.OuterXml).Elements("Result").Where(c => c.Element("SourceKey").Value.Contains(itemData));
            result.Add(new XElement("ItemCode", itemData));

            foreach (var dataList in itemDataCollection)
            {
                var g = dataList.Element("SourceKey").Value;
                result.Add(new XElement(g.Substring(g.IndexOf('^') + 1), dataList.Element("id").Value));
            }

            return result.CreateNavigator();
        }

        /// <summary>
        /// Parse and Get diffrent parts details of the date
        /// </summary>
        /// <param name="sf_Date">date of format 'yyyy-MM-ddTHH:mm:ss.fffK'</param>
        /// <returns></returns>
        public XPathNavigator DateDetails(string sf_Date, string format)
        {
            XElement xele_date = new XElement("Date");

            DateTime date = DateTime.ParseExact(sf_Date, "yyyy-MM-ddTHH:mm:ss.fffK", CultureInfo.InvariantCulture);
            xele_date.Add(new XElement("year", date.Year));
            xele_date.Add(new XElement("month", date.Month));
            xele_date.Add(new XElement("date", date.Day));
            xele_date.Add(new XElement("hour", date.Hour));
            xele_date.Add(new XElement("minute", date.Minute));
            xele_date.Add(new XElement("second", date.Second));
            xele_date.Add(new XElement("millisecond", date.Millisecond));

            XElement xele_FormatDate = new XElement("format");
            xele_FormatDate.Add(new XElement("format1", date.ToString("yyyy-MM-dd")));
            xele_FormatDate.Add(new XElement("format2", date.ToString("yyyy/MM/dd")));
            xele_FormatDate.Add(new XElement("format3", date.ToString("dd/MM/yyyy")));
            if (!string.IsNullOrEmpty(format))
            {
                xele_FormatDate.Add(new XElement("req", date.ToString(format)));
            }

            xele_date.Add(xele_FormatDate);

            return xele_date.CreateNavigator();
        }

        /// <summary>
        /// Merge two Complex object collection variable
        /// </summary>
        /// <param name="env1">name of the first variable</param>
        /// <param name="env2">name of the second variable</param>
        /// <returns>Complex object collection</returns>

        public XPathNavigator Merge(XPathNavigator env1, XPathNavigator env2)
        {
            XElement xele_Env1 = XElement.Parse(env1.OuterXml);
            xele_Env1.Add(XElement.Parse(env2.OuterXml).Elements());
            return xele_Env1.CreateNavigator();
        }

        /// <summary>
        /// Move file from one location to another
        /// </summary>
        /// <param name="filename">name of the file</param>
        /// <param name="fromdir">Source directory path without file name</param>
        /// <param name="todir">Destination directory path without file name</param>
        /// <returns></returns>
        public Boolean Move(string filename, string fromdir, string todir)
        {
            try
            {
                string pathSource;
                string pathDestination;

                if (fromdir.Contains(':'))
                {
                    pathSource = string.Format(@"{0}\{1}", fromdir.Trim('\\'), filename);
                }
                else
                {
                    pathSource = string.Format(@"\\{0}\{1}", fromdir.Trim('\\'), filename);
                }

                if (todir.Contains(':'))
                {
                    pathDestination = string.Format(@"{0}\{1}", todir.Trim('\\'), filename);
                }
                else
                {
                    pathDestination = string.Format(@"\\{0}\{1}", todir.Trim('\\'), filename);
                }
                

                if (File.Exists(pathDestination))
                {
                    _logger.InfoLog("Destination File already exists", string.Format("Destination File already exists '{0}'", pathDestination));
                    File.Delete(pathDestination);
                }
                    
                if (!File.Exists(pathSource))
                {
                    _logger.ErrorLog("File not exists", string.Format("File not exists for path '{0}'", pathSource));
                    return false;
                }
                else
                {
                    File.Move(pathSource, pathDestination);
                }

            }
            catch (Exception exc)
            {
                _logger.ErrorLog(exc);
                return false;
            }

            return true;
        }

    }
}
