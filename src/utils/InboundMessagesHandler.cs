using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Veon.eConnect.Salesforce.utils
{
    /// <summary>
    /// Class is needed to handle Salesforce Outbound Messages
    /// which are to be considered in the fairest terms as inbound message for the adapter
    /// </summary>
    public class InboundMessagesHandler
    {
        public string GetEntityType(XElement package)
        {
            string str_Return = string.Empty;
            if (package.Descendants("sObject").Count() > 0)
                str_Return = package.Descendants("sObject").FirstOrDefault().Attribute("type").Value.ToString();
            else if (package.Elements("records").Count() > 0)
                str_Return = package.Element("records").Attribute("type").Value.ToString();
            return str_Return;
        }

        public XElement ProcessPackage(string content)
        {
            XElement xele_Content = RemoveAllNamespaces(XElement.Parse(content));
            XElement xele_Output = ChangeDataStructure(xele_Content);
            return xele_Output;
        }

        private XElement ChangeDataStructure(XElement xele_Content)
        {
            XElement xele_Result = new XElement("NotificationResult");
            xele_Result.Add(new XElement("Id", xele_Content.Descendants("Notification").FirstOrDefault().Element("Id").Value));
            foreach (XElement xele_SObject in xele_Content.Descendants("sObject"))
            {
                XElement xele_Record = new XElement("records");
                xele_Record.Add(xele_SObject.Attribute("type"));
                xele_Record.Add(xele_SObject.Elements());
                xele_Result.Add(xele_Record);
            }
            return xele_Result;
        }

        private XElement RemoveAllNamespaces(XElement xele_Content)
        {
            XElement xele_Return = new XElement(xele_Content.Name.LocalName);

            foreach (XAttribute x_Attribute in xele_Content.Attributes())
            {
                xele_Return.Add(new XAttribute(x_Attribute.Name.LocalName, x_Attribute.Value.Split(':').Last()));
            }

            if (xele_Content.HasElements) { xele_Return.Add(xele_Content.Elements().Select(e => RemoveAllNamespaces(e))); }
            else { xele_Return.Value = xele_Content.Value; }
            return xele_Return;
        }
    }
}
