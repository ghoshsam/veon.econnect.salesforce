using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Veon.eConnect.Salesforce.utils
{
    public class Safari : BaseOrganization
    {
        public Safari(AppResource appResource) : base(appResource)
        {
        }

        override public XElement GetAccountAddresses(string id)
        {
            XElement xele_Collection = base.GetAccountAddresses(id);

            //Custom address module
            string str_AddressUrl = string.Format("/query?q=SELECT+Id+,+IsDeleted+,+Address_Type__c+,+Name+,+City__c+,+Account__c+,+State_Province__c+,+Zip_Postal_Code__c+,+Country__c+,+Active__c+,+Address_Line_1__c+,+Shipping_Tax_Code__c+,+Address_Line_2__c+from+Address__c+where+Account__c+=+'{0}'", id);

            string str_Response = _appResource.ExtendedAPIFunction(str_AddressUrl);
            XmlDocument xml_Response = new XmlDocument();
            xml_Response.LoadXml(str_Response);
            XElement xele_Response = XElement.Parse(xml_Response.InnerXml);

            foreach (XElement xele_Record in xele_Response.Descendants("records"))
            {
                string address_line = string.Empty;
                if (xele_Record.Descendants("Address_Line_1__c").Any() && xele_Record.Descendants("Address_Line_2__c").Any())
                {
                    address_line = string.Format("{0} {1}", xele_Record.Descendants("Address_Line_1__c").First().Value.ToString(), xele_Record.Descendants("Address_Line_2__c").First().Value.ToString());
                }
                else if (xele_Record.Descendants("Address_Line_1__c").Any())
                {
                    address_line = xele_Record.Descendants("Address_Line_1__c").First().Value.ToString();
                }

                xele_Collection.Add(GetAddressElement(
                    xele_Record.Descendants("Address_Type__c").Any() ? xele_Record.Descendants("Address_Type__c").First().Value.ToString() : "ShipTo",
                    id,
                    address_line,
                    xele_Record.Descendants("City__c").Any() ? xele_Record.Descendants("City__c").First().Value.ToString() : string.Empty,
                    xele_Record.Descendants("State_Province__c").Any() ? xele_Record.Descendants("State_Province__c").First().Value.ToString() : string.Empty,
                    xele_Record.Descendants("Zip_Postal_Code__c").Any() ? xele_Record.Descendants("Zip_Postal_Code__c").First().Value.ToString() : string.Empty,
                    xele_Record.Descendants("Country__c").Any() ? xele_Record.Descendants("Country__c").First().Value.ToString() : string.Empty,
                    string.Format(
                        "Active__c:{0};Shipping_Tax_Code__c:{1};sf_id:{2};sf_is_deleted:{3};name:{4}",
                        xele_Record.Descendants("Active__c").Any() ? xele_Record.Descendants("Active__c").First().Value.ToString() : string.Empty,
                        xele_Record.Descendants("Shipping_Tax_Code__c").Any() ? xele_Record.Descendants("Shipping_Tax_Code__c").First().Value.ToString() : string.Empty,
                        xele_Record.Descendants("Id").Any() ? xele_Record.Descendants("Id").First().Value.ToString() : string.Empty,
                        xele_Record.Descendants("IsDeleted").Any() ? xele_Record.Descendants("IsDeleted").First().Value.ToString() : string.Empty,
                        xele_Record.Descendants("Name").Any() ? xele_Record.Descendants("Name").First().Value.ToString() : string.Empty)));
            }


            return xele_Collection;
        }
    }
}
