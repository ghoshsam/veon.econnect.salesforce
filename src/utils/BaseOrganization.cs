using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Veon.eConnect.Salesforce.utils
{
    public class BaseOrganization
    {
        protected AppResource _appResource;
        public BaseOrganization(AppResource appResource)
        {
            _appResource = appResource;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual XElement GetAccountAddresses(string id)
        {
            string str_AddressUrl = string.Format("/query?q=SELECT+BillingStreet+,+BillingCountry+,+BillingCity+,+BillingState+,+BillingPostalCode+,+ShippingStreet+,+ShippingCity+,+ShippingState+,+ShippingCountry+,+ShippingPostalCode+from+Account+Where+Id+=+'{0}'", id);

            string str_Response = _appResource.ExtendedAPIFunction(str_AddressUrl);
            XmlDocument xml_Response = new XmlDocument();
            xml_Response.LoadXml(str_Response);
            XElement xele_Response = XElement.Parse(xml_Response.InnerXml);

            XElement xele_Addresses = new XElement("items");
            XElement xele_Record = xele_Response.Elements("records").FirstOrDefault();
            if(xele_Record == null)
            {
                return xele_Addresses;
            }

            xele_Addresses.Add(GetAddressElement(
                "bo_BillTo",
                id,
                xele_Record.Descendants("BillingStreet").Any() ? xele_Record.Descendants("BillingStreet").First().Value.ToString() : string.Empty,
                xele_Record.Descendants("BillingCity").Any() ? xele_Record.Descendants("BillingCity").First().Value.ToString() : string.Empty,
                xele_Record.Descendants("BillingState").Any() ? xele_Record.Descendants("BillingState").First().Value.ToString() : string.Empty,
                xele_Record.Descendants("BillingPostalCode").Any() ? xele_Record.Descendants("BillingPostalCode").First().Value.ToString() : string.Empty,
                xele_Record.Descendants("BillingCountry").Any() ? xele_Record.Descendants("BillingCountry").First().Value.ToString() : string.Empty,
                string.Empty));

            xele_Addresses.Add(GetAddressElement(
                "bo_ShipTo",
                id,
                xele_Record.Descendants("ShippingStreet").Any() ? xele_Record.Descendants("ShippingStreet").First().Value.ToString() : string.Empty,
                xele_Record.Descendants("ShippingCity").Any() ? xele_Record.Descendants("ShippingCity").First().Value.ToString() : string.Empty,
                xele_Record.Descendants("ShippingState").Any() ? xele_Record.Descendants("ShippingState").First().Value.ToString() : string.Empty,
                xele_Record.Descendants("ShippingPostalCode").Any() ? xele_Record.Descendants("ShippingPostalCode").First().Value.ToString() : string.Empty,
                xele_Record.Descendants("ShippingCountry").Any() ? xele_Record.Descendants("ShippingCountry").First().Value.ToString() : string.Empty,
                string.Empty));

            return xele_Addresses;
        }

        /// <summary>
        /// generate xelement item for the address
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="AccountId"></param>
        /// <param name="Street"></param>
        /// <param name="City"></param>
        /// <param name="State"></param>
        /// <param name="ZIP"></param>
        /// <param name="Country"></param>
        /// <param name="Extra"></param>
        /// <returns></returns>
        public XElement GetAddressElement(string Type, string AccountId, string Street, string City, string State, string ZIP, string Country, string Extra)
        {
            XElement xele_Item = new XElement("item");

            xele_Item.Add(new XElement("type", Type));
            xele_Item.Add(new XElement("sf_account", AccountId));
            xele_Item.Add(new XElement("street", Street));
            xele_Item.Add(new XElement("city", City));
            xele_Item.Add(new XElement("state", State));
            xele_Item.Add(new XElement("country", Country));
            xele_Item.Add(new XElement("postal_code", ZIP));

            List<KeyValuePair<string, string>> kvp_extra = Split(Extra);
            foreach(KeyValuePair<string,string> kvp_pair in kvp_extra)
            {
                xele_Item.Add(new XElement(kvp_pair.Key, kvp_pair.Value));
            }

            return xele_Item;
        }

        /// <summary>
        /// split the string based upon the ';' and ':'
        /// </summary>
        /// <param name="extra">key1:value1;key2:value2</param>
        /// <returns></returns>
        private List<KeyValuePair<string, string>> Split(string extra)
        {
            List<KeyValuePair<string, string>> lst_return = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrEmpty(extra))
                return lst_return;

            string[] arr_pair = extra.Split(';');
            foreach(string pair in arr_pair)
            {
                if (string.IsNullOrEmpty(pair))
                    continue;
                string[] keyValue = pair.Split(':');
                lst_return.Add(new KeyValuePair<string, string>(keyValue[0], keyValue.Count()<2?string.Empty:keyValue[1]));
            }

            return lst_return;
        }
    }
}