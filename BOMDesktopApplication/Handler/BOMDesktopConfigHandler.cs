using System.Reflection;
using System.Xml;

namespace Fujitsu.eDoc.BOMApplicationDesktopApp
{
    public class BOMDesktopConfigHandler
    {
        public BOMConfiguration GetBOMConfiguration()
        {
            BOMConfiguration cfg = new BOMConfiguration();

            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMConfigurationQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMConfiguration", Assembly.Load("Fujitsu.eDoc.BOM, Version=1.0.0.0, Culture=neutral, PublicKeyToken=402811e591a0c620"));
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNodeList nl = doc.SelectNodes("/RECORDS/RECORD");
            foreach (XmlNode n in nl)
            {
                BOMConfiguration.BOMConfigurationItem item = new BOMConfiguration.BOMConfigurationItem()
                {
                    Type = n.SelectSingleNode("Type").InnerText.ToLower(),
                    Description = n.SelectSingleNode("Description").InnerText,
                    Key = n.SelectSingleNode("Key").InnerText,
                    Value = n.SelectSingleNode("Value").InnerText
                };
                cfg.Items.Add(item);
            }

            xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMDataListsQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMDataLists", Assembly.Load("Fujitsu.eDoc.BOM, Version=1.0.0.0, Culture=neutral, PublicKeyToken=402811e591a0c620"));
            xmlQuery = xmlQuery.Replace("#ListName#", "SagOmraade");
            result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            doc = new XmlDocument();
            doc.LoadXml(result);
            nl = doc.SelectNodes("/RECORDS/RECORD");
            foreach (XmlNode n in nl)
            {
                BOMConfiguration.BOMConfigurationItem item = new BOMConfiguration.BOMConfigurationItem()
                {
                    Key = n.SelectSingleNode("Code").InnerText,
                    Value = n.SelectSingleNode("Description").InnerText
                };
                cfg.CaseAreas.Add(item);
            }

            xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMActivityTypeQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMConfiguration", Assembly.Load("Fujitsu.eDoc.BOM, Version=1.0.0.0, Culture=neutral, PublicKeyToken=402811e591a0c620"));
            result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            doc = new XmlDocument();
            doc.LoadXml(result);
            nl = doc.SelectNodes("/RECORDS/RECORD");
            foreach (XmlNode n in nl)
            {
                BOMConfiguration.BOMActivityTypeConfigurationItem item = new BOMConfiguration.BOMActivityTypeConfigurationItem()
                {
                    Key = n.SelectSingleNode("Code").InnerText,
                    Description = n.SelectSingleNode("Description").InnerText,
                    ToResponsibleContact = n.SelectSingleNode("ToResponsibleContact").InnerText,
                    ToCaseCategory = n.SelectSingleNode("ToCaseCategory").InnerText,
                    ToProgressPlan = n.SelectSingleNode("ToProgressPlan").InnerText,
                    ToCaseType = n.SelectSingleNode("ToCaseType").InnerText
                };
                cfg.ActivityTypes.Add(item);
            }


            // Get list of valid file formats
            xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetFileFormatQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMConfiguration", Assembly.Load("Fujitsu.eDoc.BOM, Version=1.0.0.0, Culture=neutral, PublicKeyToken=402811e591a0c620"));
            result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            doc = new XmlDocument();
            doc.LoadXml(result);
            nl = doc.SelectNodes("/RECORDS/RECORD");
            foreach (XmlNode n in nl)
            {
                string fileExtension = n.SelectSingleNode("FileExtension").InnerText;
                if (!string.IsNullOrEmpty(fileExtension) && !cfg.FileFormats.Contains(fileExtension))
                {
                    cfg.FileFormats.Add(fileExtension.ToLower());
                }
            }

            // Check if document archive uses FileFormatControl
            xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetDocumentArchiveQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMConfiguration", Assembly.Load("Fujitsu.eDoc.BOM, Version=1.0.0.0, Culture=neutral, PublicKeyToken=402811e591a0c620"));
            result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            doc = new XmlDocument();
            doc.LoadXml(result);
            nl = doc.SelectNodes("/RECORDS/RECORD");
            foreach (XmlNode n in nl)
            {
                string fileExtension = n.SelectSingleNode("FileFormatControl").InnerText;
                cfg.UseFileFormatControl = (fileExtension == "-1");
            }

            return cfg;
        }

    }
}

