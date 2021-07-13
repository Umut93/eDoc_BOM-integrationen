using Fujitsu.eDoc.BOM;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace Fujitsu.eDoc.BOMApplicationDesktopApp
{
    public class BOMConfiguration
    {
        private static string CONFIG_TYPE_eDocMetadata = "edocmetadata";
        private static string CONFIG_TYPE_BOMMetadata = "bommetadata";
        private static string CONFIG_TYPE_Casetype = "sagstype";
        private static string CONFIG_TYPE_Estate = "ejendom";
        private static string CONFIG_TYPE_Setup = "setup";
        private static string CONFIG_TYPE_Mail = "mail";

        public static string ESTATE_RELATION_LANDPARCEL = "matrikel";
        public static string ESTATE_RELATION_ESTATE = "ejendom";

        private BOM.BOMKonfigurationV6.KonfigurationDataTransferBasisKonfiguration basisKonf;

        public class BOMConfigurationItem
        {
            public string Type { get; set; }
            public string Description { get; set; }
            public string Key { get; set; }
            public string Value { get; set; }
        }

        public class BOMActivityTypeConfigurationItem
        {
            public string Key { get; set; }
            public string Description { get; set; }
            public string ToResponsibleContact { get; set; }
            public string ToCaseCategory { get; set; }
            public string ToProgressPlan { get; set; }
            public string OrgUnit { get; set; }
            public string OurRef { get; set; }
            public string ToCaseType { get; set; }
        }

        public List<BOMConfigurationItem> Items = new List<BOMConfigurationItem>();
        public List<BOMConfigurationItem> CaseAreas = new List<BOMConfigurationItem>();
        public List<BOMActivityTypeConfigurationItem> ActivityTypes = new List<BOMActivityTypeConfigurationItem>();
        public List<string> FileFormats = new List<string>();
        public bool UseFileFormatControl = false;

        public string GetCaseAreaName(string AreaCode)
        {
            foreach (BOMConfigurationItem item in CaseAreas)
            {
                if (item.Key == AreaCode)
                {
                    return item.Value;
                }
            }
            return AreaCode;
        }

        public string[] GetEnabledCaseAreaNames()
        {
            string enabledNames = "";
            foreach (BOMConfigurationItem item in Items)
            {
                if (item.Type == CONFIG_TYPE_Casetype && item.Value != "0")
                {
                    if (!string.IsNullOrEmpty(enabledNames))
                    {
                        enabledNames += ";";
                    }
                    enabledNames += item.Key;
                }
            }

            string[] names = enabledNames.Split(';');
            return names;
        }

        public string GetCaseTitle()
        {
            string title = GetItemValue(CONFIG_TYPE_eDocMetadata, "Sagstitel", "");
            return title;
        }

        public string GetCaseAccessCode()
        {
            string title = GetItemValue(CONFIG_TYPE_eDocMetadata, "Indsigtsgrad", "");
            return title;
        }

        public string GetCaseAccessGroup()
        {
            string title = GetItemValue(CONFIG_TYPE_eDocMetadata, "Adgangsgruppe", "");
            return title;
        }

        public DateTime GetStartDateTime()
        {
            string sStartDato = GetItemValue(CONFIG_TYPE_Setup, "StartDato", "");
            DateTime StartDato = DateTime.MinValue;
            DateTime.TryParse(sStartDato, out StartDato);
            return StartDato;
        }
        public int GetMaxHandlingTimeInMinutes()
        {
            string sMaxHandlingTimeInMinutes = GetItemValue(CONFIG_TYPE_Setup, "MaxHandlingTimeInMinutes", "");
            int MaxHandlingTimeInMinutes = 60;
            int.TryParse(sMaxHandlingTimeInMinutes, out MaxHandlingTimeInMinutes);
            return MaxHandlingTimeInMinutes;
        }
        public string GetEmailAdresses(string CaseTypeCode)
        {
            string email = GetItemValue(CONFIG_TYPE_Setup, "Email" + CaseTypeCode, "");
            return email;
        }

        public string GetEmailAdressesForCreate(string CaseTypeCode)
        {
            string email = GetItemValue(CONFIG_TYPE_Mail, "Email" + CaseTypeCode, "");
            return email;
        }

        public string GetEstateRelationType()
        {
            string estateRelationType = GetItemValue(CONFIG_TYPE_Estate, "Ejendomstilknytning", ESTATE_RELATION_LANDPARCEL).ToLower();
            return estateRelationType;
        }

        public BOMActivityTypeConfigurationItem GetActivityType(string ActivityTypeKey)
        {
            BOMActivityTypeConfigurationItem item = ActivityTypes.Find(a => a.Key == ActivityTypeKey);
            BOMActivityTypeConfigurationItem itemDefault = ActivityTypes.Find(a => a.Key == "Default");
            if (item == null)
            {
                if (itemDefault == null)
                {
                    return new BOMActivityTypeConfigurationItem();
                }
                item = itemDefault;
            }
            else
            {
                if (string.IsNullOrEmpty(item.ToCaseCategory)) item.ToCaseCategory = itemDefault.ToCaseCategory;
                if (string.IsNullOrEmpty(item.ToProgressPlan)) item.ToProgressPlan = itemDefault.ToProgressPlan;
                if (string.IsNullOrEmpty(item.ToResponsibleContact)) item.ToResponsibleContact = itemDefault.ToResponsibleContact;
                if (string.IsNullOrEmpty(item.ToCaseType)) item.ToCaseType = itemDefault.ToCaseType;
            }

            if (string.IsNullOrEmpty(item.ToResponsibleContact))
            {
                return item;
            }

            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetContactQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMConfiguration", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#RECNO#", item.ToResponsibleContact);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNode n = doc.SelectSingleNode("/RECORDS/RECORD");
            if (n != null)
            {
                string domain = n.SelectSingleNode("Domain").InnerText;
                if (domain == "1")
                {
                    item.OrgUnit = item.ToResponsibleContact;
                }
                else
                {
                    item.OurRef = item.ToResponsibleContact;
                    item.OrgUnit = n.SelectSingleNode("ToEmployer").InnerText;
                }
            }
            return item;
        }

        public string GetReceivedTitle()
        {
            string title = GetItemValue(CONFIG_TYPE_BOMMetadata, "ModtagetTitel", "Ansøgning modtaget");
            return title;
        }
        public string GetReceivedTitle2()
        {
            string title = GetItemValue(CONFIG_TYPE_BOMMetadata, "ModtagetTitel2", "Indsendelse modtaget");
            return title;
        }
        public string GetReceivedStatusCode()
        {
            string status = GetItemValue(CONFIG_TYPE_BOMMetadata, "ModtagetStatus", "Modtaget");
            return status;
        }
        public string GetReceivedPhaseCode()
        {
            string phase = GetItemValue(CONFIG_TYPE_BOMMetadata, "ModtagetFase", "Behandling");
            return phase;
        }
        public string GetReceivedNotificationProfile()
        {
            string notificationprofile = GetItemValue(CONFIG_TYPE_BOMMetadata, "ModtagetNotifikationsProfil", "");
            return notificationprofile;
        }
        public string GetVURendpoint()
        {
            string endpoint = GetItemValue(CONFIG_TYPE_BOMMetadata, "VUREndpoint");
            return endpoint;
        }
        public string GetVURCertificateSerial()
        {
            string serial = GetItemValue(CONFIG_TYPE_BOMMetadata, "VURCertificateSerial");
            return serial;
        }

        private string GetItemValue(string Type, string Key, string DefaultValue = "")
        {
            foreach (BOMConfigurationItem item in Items)
            {
                if (item.Type == Type && item.Key == Key)
                {
                    return item.Value;
                }
            }
            return DefaultValue;
        }


        public int[] GetDeadlines(string code)
        {
            if (basisKonf == null)
            {
                basisKonf = BOMCaller.GetBaseConfiguration();
            }

            int[] deadlines = new int[0];
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferFristNotifikationProfil a in basisKonf.FristNotifikationProfiler)
            {
                if (a.Kode == code)
                {
                    deadlines = a.Frister;
                }
            }

            return deadlines;
        }
    }
}