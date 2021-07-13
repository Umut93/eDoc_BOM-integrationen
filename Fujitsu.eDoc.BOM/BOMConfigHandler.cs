using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

namespace Fujitsu.eDoc.BOM
{
    public class BOMConfigHandler
    {
        private static string BOM_DATALIST_FristNotifikationProfil = "FristNotifikationProfil";
        private static string BOM_DATALIST_KonfliktGruppe = "KonfliktGruppe";
        private static string BOM_DATALIST_Kort = "Kort";
        private static string BOM_DATALIST_SagFase = "SagFase";
        private static string BOM_DATALIST_SagOmraade = "SagOmraade";
        private static string BOM_DATALIST_SagStatusType = "SagStatusType";
        private static string BOM_DATALIST_InitiativPligt = "InitiativPligt";
        private static string BOM_DATALIST_SagAndenMyndighed = "SagAndenMyndighed";
        private static string BOM_DATALIST_SagServiceMaal = "SagServiceMaal";

        private static string BOM_DATALIST_AktivitetType = "AktivitetType";
        private static string BOM_DATALIST_Betingelse = "Betingelse";
        private static string BOM_DATALIST_DokumentationType = "DokumentationType";
        private static string BOM_DATALIST_KonfliktType = "KonfliktType";
        private static string BOM_DATALIST_SagType = "SagType";
        private static string BOM_DATALIST_KravStyrkeKodeType = "KravStyrkeKodeType";

        public static bool ShouldUpdateDataLists()
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMDataListsLastUpdatedQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMDataLists", Assembly.GetExecutingAssembly());
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNode n = doc.SelectSingleNode("/RECORDS/RECORD/UpdateDate");
            if (n != null)
            {
                if (!string.IsNullOrEmpty(n.InnerText))
                {
                    DateTime d;
                    if (DateTime.TryParse(n.InnerText, out d))
                    {
                        if (d.AddDays(1) > DateTime.Now)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public static void UpdateDataLists()
        {
            // BAsis konfiguration
            BOM.BOMKonfigurationV6.KonfigurationDataTransferBasisKonfiguration basisKonf = BOMCaller.GetBaseConfiguration();

            List<BOMDataListItem> list = new List<BOMDataListItem>();
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferFristNotifikationProfil a in basisKonf.FristNotifikationProfiler)
            {
                list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_FristNotifikationProfil, Code = a.Kode, Description = a.Navn });
            }
            UpdateList(BOM_DATALIST_FristNotifikationProfil, list);

            list = new List<BOMDataListItem>();
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferKonfliktGruppe a in basisKonf.KonfliktGrupper)
            {
                list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_KonfliktGruppe, Code = a.Kode, Description = a.VisningNavn });
            }
            UpdateList(BOM_DATALIST_KonfliktGruppe, list);

            list = new List<BOMDataListItem>();
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferKort a in basisKonf.Kort)
            {
                list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_Kort, Code = a.Kode, Description = a.VisningNavn });
            }
            UpdateList(BOM_DATALIST_Kort, list);

            list = new List<BOMDataListItem>();
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferSagFase a in basisKonf.SagFaser)
            {
                list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_SagFase, Code = a.Kode, Description = a.VisningNavn });
            }
            UpdateList(BOM_DATALIST_SagFase, list);

            list = new List<BOMDataListItem>();
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferSagOmraade a in basisKonf.SagOmraader)
            {
                list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_SagOmraade, Code = a.Kode, Description = a.VisningNavn });
            }
            UpdateList(BOM_DATALIST_SagOmraade, list);

            list = new List<BOMDataListItem>();
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferSagStatusType a in basisKonf.SagStatusTyper)
            {
                list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_SagStatusType, Code = a.Kode, Description = a.VisningNavn });
            }
            UpdateList(BOM_DATALIST_SagStatusType, list);

            list = new List<BOMDataListItem>();
            list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_InitiativPligt, Code = "Myndighed", Description = "Myndighed" });
            list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_InitiativPligt, Code = "Ansøger", Description = "Ansøger" });
            list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_InitiativPligt, Code = "Ingen", Description = "Ingen" });
            UpdateList(BOM_DATALIST_InitiativPligt, list);

            list = new List<BOMDataListItem>();
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferSagAndenMyndighed a in basisKonf.SagAndenMyndighed)
            {
                list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_SagAndenMyndighed, Code = a.Kode, Description = a.VisningNavn });
            }
            UpdateList(BOM_DATALIST_SagAndenMyndighed, list);

            list = new List<BOMDataListItem>();
            list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_SagServiceMaal, Code = BOMCaseHandler.DEFAULT_SAG_SERVICE_MAAL_KODE, Description = "Ikke fritaget" });
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferSagServiceMaal a in basisKonf.SagServiceMaal)
            {
                list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_SagServiceMaal, Code = a.Kode, Description = a.VisningNavn });
            }
            UpdateList(BOM_DATALIST_SagServiceMaal, list);


            // Regel konfiguration
            BOMKonfigurationV6.KonfigurationDataTransferRegelKonfiguration regelKonf = BOMCaller.GetRegelConfiguration();
            list = new List<BOMDataListItem>();
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferAktivitetType a in regelKonf.AktivitetTyper)
            {
                list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_AktivitetType, Code = a.Kode, Description = a.VisningNavn });
            }
            UpdateList(BOM_DATALIST_AktivitetType, list);

            list = new List<BOMDataListItem>();
            list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_InitiativPligt, Code = "Default", Description = "Alle der ikke har specifik opsætning" });
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferAktivitetType a in regelKonf.AktivitetTyper)
            {
                list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_AktivitetType, Code = a.Kode, Description = a.VisningNavn });
            }
            UpdateActivityType(list);

            list = new List<BOMDataListItem>();
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferBetingelse a in regelKonf.Betingelser)
            {
                list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_Betingelse, Code = a.Kode, Description = a.VisningNavn });
            }
            UpdateList(BOM_DATALIST_Betingelse, list);

            list = new List<BOMDataListItem>();
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferDokumentationType a in regelKonf.DokumentationTyper)
            {
                list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_DokumentationType, Code = a.Kode, Description = a.VisningNavn });
            }
            UpdateList(BOM_DATALIST_DokumentationType, list);

            list = new List<BOMDataListItem>();
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferKonfliktType a in regelKonf.KonfliktTyper)
            {
                list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_KonfliktType, Code = a.Kode, Description = a.VisningNavn });
            }
            UpdateList(BOM_DATALIST_KonfliktType, list);

            list = new List<BOMDataListItem>();
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferSagType a in regelKonf.SagTyper)
            {
                list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_SagType, Code = a.Kode, Description = a.VisningNavn });
            }
            UpdateList(BOM_DATALIST_SagType, list);

            list = new List<BOMDataListItem>();
            list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_KravStyrkeKodeType, Code = "Frivilligt", Description = "Kravet er frivilligt" });
            list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_KravStyrkeKodeType, Code = "Obligatorisk", Description = "Kravet er obligatorisk" });
            list.Add(new BOMDataListItem() { ListName = BOM_DATALIST_KravStyrkeKodeType, Code = "IkkeKrav", Description = "Fjern kravet" });
            UpdateList(BOM_DATALIST_KravStyrkeKodeType, list);
        }

        public static BOMConfiguration GetBOMConfiguration()
        {
            BOMConfiguration cfg = new BOMConfiguration();

            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMConfigurationQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMConfiguration", Assembly.GetExecutingAssembly());
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

            xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMDataListsQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMDataLists", Assembly.GetExecutingAssembly());
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

            xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMActivityTypeQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMConfiguration", Assembly.GetExecutingAssembly());
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
            xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetFileFormatQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMConfiguration", Assembly.GetExecutingAssembly());
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
            xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetDocumentArchiveQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMConfiguration", Assembly.GetExecutingAssembly());
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

        private static void UpdateList(string listName, List<BOMDataListItem> list)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMDataListsQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMDataLists", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#ListName#", listName);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNodeList nl = doc.SelectNodes("/RECORDS/RECORD");
            foreach (XmlNode n in nl)
            {
                bool exists = false;
                string recno = n.SelectSingleNode("Recno").InnerText;
                string c = n.SelectSingleNode("Code").InnerText;
                string d = n.SelectSingleNode("Description").InnerText;
                foreach (BOMDataListItem item in list)
                {
                    if (item.Code == c)
                    {
                        item.Recno = recno;
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    list.Add(new BOMDataListItem()
                    {
                        Recno = recno,
                        ListName = listName,
                        Code = c,
                        Description = d,
                        Expired = true
                    });
                }
            }

            foreach (BOMDataListItem item in list)
            {
                if (string.IsNullOrEmpty(item.Recno))
                {
                    // Create
                    xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("CreateBOMDataListItemMetaInsert.xml", "Fujitsu.eDoc.BOM.XML.BOMDataLists", Assembly.GetExecutingAssembly());
                    xmlQuery = xmlQuery.Replace("#ListName#", item.ListName);
                    xmlQuery = xmlQuery.Replace("#Code#", item.Code);
                    xmlQuery = xmlQuery.Replace("#Description#", item.Description);

                    item.Recno = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
                }
                else
                {
                    // Update
                    xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateBOMDataListItemMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.BOMDataLists", Assembly.GetExecutingAssembly());
                    xmlQuery = xmlQuery.Replace("#Recno#", item.Recno);
                    xmlQuery = xmlQuery.Replace("#Description#", item.Description);
                    if (item.Expired)
                    {
                        xmlQuery = xmlQuery.Replace("#ToDate#", DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"));
                    }
                    else
                    {
                        xmlQuery = xmlQuery.Replace("#ToDate#", "");
                    }

                    result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
                }
            }
        }

        private static void UpdateActivityType(List<BOMDataListItem> list)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMActivityTypeQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMActivityType", Assembly.GetExecutingAssembly());
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNodeList nl = doc.SelectNodes("/RECORDS/RECORD");
            foreach (XmlNode n in nl)
            {
                bool exists = false;
                string recno = n.SelectSingleNode("Recno").InnerText;
                string c = n.SelectSingleNode("Code").InnerText;
                string d = n.SelectSingleNode("Description").InnerText;
                foreach (BOMDataListItem item in list)
                {
                    if (item.Code == c)
                    {
                        item.Recno = recno;
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    list.Add(new BOMDataListItem()
                    {
                        Recno = recno,
                        Code = c,
                        Description = d,
                        Expired = true
                    });
                }
            }

            foreach (BOMDataListItem item in list)
            {
                if (string.IsNullOrEmpty(item.Recno))
                {
                    // Create
                    xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("CreateBOMActivityTypeMetaInsert.xml", "Fujitsu.eDoc.BOM.XML.BOMActivityType", Assembly.GetExecutingAssembly());
                    xmlQuery = xmlQuery.Replace("#Code#", item.Code);
                    xmlQuery = xmlQuery.Replace("#Description#", item.Description);

                    item.Recno = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
                }
                else
                {
                    // Update
                    xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateBOMActivityTypeMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.BOMActivityType", Assembly.GetExecutingAssembly());
                    xmlQuery = xmlQuery.Replace("#Recno#", item.Recno);
                    xmlQuery = xmlQuery.Replace("#Description#", item.Description);
                    if (item.Expired)
                    {
                        xmlQuery = xmlQuery.Replace("#ToDate#", DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"));
                    }
                    else
                    {
                        xmlQuery = xmlQuery.Replace("#ToDate#", "");
                    }

                    result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
                }
            }
        }
        public static string GetPhaseDescription(string PhaseCode)
        {
            return GetItemDescription(BOM_DATALIST_SagFase, PhaseCode);
        }

        public static string GetStatusDescription(string StatusCode)
        {
            return GetItemDescription(BOM_DATALIST_SagStatusType, StatusCode);
        }

        public static string GetInitiativeDutyDescription(string InitiativeDutyCode)
        {
            return GetItemDescription(BOM_DATALIST_InitiativPligt, InitiativeDutyCode);
        }

        private static string GetItemDescription(string ListName, string Code)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetItemDescriptionQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMDataLists", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#ListName#", ListName);
            xmlQuery = xmlQuery.Replace("#Code#", Code);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNode n = doc.SelectSingleNode("/RECORDS/RECORD/Description");
            if (n != null)
            {
                return n.InnerText;
            }
            return "";
        }

        private class BOMDataListItem
        {
            public string Recno { get; set; }
            public string ListName { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
            public bool Expired { get; set; }
        }
    }
}

