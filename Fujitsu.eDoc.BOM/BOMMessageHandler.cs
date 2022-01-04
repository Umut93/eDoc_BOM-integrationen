using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace Fujitsu.eDoc.BOM
{
    public class BOMMessageHandler
    {
        public enum BOMMessageStatusEnum
        {
            Stored = 0,
            CaseUpdated = 1,
            UpdateFailed = 2
        }

        public static void HandleNewMessages()
        {
            BOMConfiguration cfg = BOMConfigHandler.GetBOMConfiguration();
            Guid? highWatermark = GetHighWatermark();
            DateTime startDate = cfg.GetStartDateTime();

            //Get the latest HigWaterMark from DB (desc top 1)
            if (highWatermark.HasValue)
            {
                BOM.BOMBeskedfordeler.HentBeskederResult result = BOMCaller.GetMessages(highWatermark);
                Guid? newHighWatermark = result.HighWatermark;

                List<BOM.BOMBeskedfordeler.Besked> sortedlist = new List<BOMBeskedfordeler.Besked>();
                sortedlist.AddRange(result.Beskeder);
                sortedlist.Sort(delegate (BOM.BOMBeskedfordeler.Besked x, BOM.BOMBeskedfordeler.Besked y)
                {
                    return x.Tidspunkt.CompareTo(y.Tidspunkt);
                });

                foreach (BOM.BOMBeskedfordeler.Besked b in sortedlist)
                {
                    if (highWatermark.HasValue || b.Tidspunkt >= startDate)
                    {
                        string recno = SaveMessage(b, newHighWatermark);

                        if (highWatermark.HasValue && b is BOM.BOMBeskedfordeler.SagStatusSkiftBesked)
                        {
                            HandleStatusChange((BOM.BOMBeskedfordeler.SagStatusSkiftBesked)b, recno, cfg);
                        }
                    }
                }
            }
            // If no record found in the DB then a create DB record in fu_bom_message with the latest HighWaterMark(occurs only once)
            else
            {
                Guid? latestHighWaterMark = BOMCaller.GetLatestHighWaterMark();
                if (latestHighWaterMark.HasValue)
                {
                    SaveMessage(new BOMBeskedfordeler.Besked { BehandlendeMyndighedCvr = "Seneste HighWaterMark er indsat, da tabellen var tom.", MyndighedCvr = BOMCaller.GetMunicipalityCVR(), SagId = Guid.Empty, Tidspunkt = DateTime.Now, ModtagerType = BOMBeskedfordeler.ModtagerType.CentralMyndighed }, latestHighWaterMark);
                }
            }
        }


        private static Guid? GetHighWatermark()
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetLatestBOMMessageMetaQuery.xml", "Fujitsu.eDoc.BOM.XML.FuBOMMessage", Assembly.GetExecutingAssembly());
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNode nHighWatermark = doc.SelectSingleNode("/RECORDS/RECORD/HighWatermark");
            if (nHighWatermark != null)
            {
                Guid HighWatermark = new Guid(nHighWatermark.InnerText);
                return HighWatermark;
            }

            return null;
        }

        private static string SaveMessage(BOM.BOMBeskedfordeler.Besked message, Guid? NewHighWatermark)
        {
            string BOMMessageRecno = "";

            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("CreateBOMMessageMetaInsert.xml", "Fujitsu.eDoc.BOM.XML.FuBOMMessage", Assembly.GetExecutingAssembly());
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlQuery);

            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='TreatingMunicipalityCVR']/VALUE").InnerText = message.BehandlendeMyndighedCvr;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='Receivertype']/VALUE").InnerText = message.ModtagerType.ToString();
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='MunicipalityCVR']/VALUE").InnerText = message.MyndighedCvr;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='CaseId']/VALUE").InnerText = message.SagId.ToString();
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='MessageTime']/VALUE").InnerText = message.Tidspunkt.ToString("yyyy-MM-dd HH:mm:ss.fff");

            if (message is BOM.BOMBeskedfordeler.AnsoegningIndsendtBesked)
            {
                doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='MessageType']/VALUE").InnerText = "AnsoegningIndsendtBesked";
                doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ApplicationId']/VALUE").InnerText = ((BOM.BOMBeskedfordeler.AnsoegningIndsendtBesked)message).AnsoegningId.ToString();
            }
            if (message is BOM.BOMBeskedfordeler.FristOverskridelseBesked)
            {
                doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='MessageType']/VALUE").InnerText = "FristOverskridelseBesked";
                doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='DeadlineDate']/VALUE").InnerText = ((BOM.BOMBeskedfordeler.FristOverskridelseBesked)message).FristDato.ToString("yyyy-MM-dd HH:mm:ss.fff");
                doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='DeadlineStatusCode']/VALUE").InnerText = ((BOM.BOMBeskedfordeler.FristOverskridelseBesked)message).StatusKode;
                doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='DeadlineStatusName']/VALUE").InnerText = ((BOM.BOMBeskedfordeler.FristOverskridelseBesked)message).StatusNavn;
            }
            if (message is BOM.BOMBeskedfordeler.SagStatusSkiftBesked)
            {
                doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='MessageType']/VALUE").InnerText = "SagStatusSkiftBesked";
                doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='CaseStatusId']/VALUE").InnerText = ((BOM.BOMBeskedfordeler.SagStatusSkiftBesked)message).SagStatusId.ToString();
            }

            if (NewHighWatermark.HasValue)
            {
                doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='HighWatermark']/VALUE").InnerText = NewHighWatermark.Value.ToString();
            }
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='Status']/VALUE").InnerText = ((int)BOMMessageStatusEnum.Stored).ToString();

            xmlQuery = doc.OuterXml;

            BOMMessageRecno = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
            return BOMMessageRecno;
        }

        private static void HandleStatusChange(BOM.BOMBeskedfordeler.SagStatusSkiftBesked message, string BOMMessageRecno, BOMConfiguration cfg)
        {
            try
            {
                // Get status from BOM
                string SagId = message.SagId.ToString();
                string SagStatusId = message.SagStatusId.ToString();
                BOMSagStatus.SagStatusDetaljeType s = BOM.BOMCaller.GetStatus(SagId, SagStatusId);

                if (string.IsNullOrEmpty(s.FristNotifikationProfilKode))
                {
                    s.FristNotifikationProfilKode = cfg.GetReceivedNotificationProfile();
                }


                // Get BOM Case
                string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMCaseMetaQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMCase", Assembly.GetExecutingAssembly());
                xmlQuery = xmlQuery.Replace("#CaseId#", SagId);
                string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

                XmlDocument docQuery = new XmlDocument();
                docQuery.LoadXml(result);
                XmlNode n = docQuery.SelectSingleNode("/RECORDS/RECORD");
                if (n != null)
                {
                    string recno = n.SelectSingleNode("Recno").InnerText;
                    string ToCase = n.SelectSingleNode("ToCase").InnerText;

                    // Update case metadata
                    string xmlUpdate = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateBOMCaseReplyMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.BOMCase", Assembly.GetExecutingAssembly());
                    XmlDocument docUpdate = new XmlDocument();
                    docUpdate.LoadXml(xmlUpdate);
                    XmlNode nUpdatestatement = docUpdate.SelectSingleNode("/operation/UPDATESTATEMENT");

                    nUpdatestatement.Attributes["PRIMARYKEYVALUE"].Value = recno;

                    nUpdatestatement.SelectSingleNode("METAITEM[@NAME='StatusCode']/VALUE").InnerText = s.StatusKode;
                    nUpdatestatement.SelectSingleNode("METAITEM[@NAME='OtherAuthorityCode']/VALUE").InnerText = s.SagAndenMyndighedKode;
                    nUpdatestatement.SelectSingleNode("METAITEM[@NAME='PhaseCode']/VALUE").InnerText = s.FaseKode;
                    nUpdatestatement.SelectSingleNode("METAITEM[@NAME='InitiativeDuty']/VALUE").InnerText = GetLocalInitiativPligt(s.InitiativPligt);
                    nUpdatestatement.SelectSingleNode("METAITEM[@NAME='DeadlineNotificationKode']/VALUE").InnerText = s.FristNotifikationProfilKode;
                    nUpdatestatement.SelectSingleNode("METAITEM[@NAME='Deadline']/VALUE").InnerText = s.FristDato > DateTime.MinValue ? s.FristDato.ToString() : "";
                    nUpdatestatement.SelectSingleNode("METAITEM[@NAME='LastActivity']/VALUE").InnerText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(docUpdate.OuterXml);


                    // Get after values
                    result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

                    XmlDocument docQueryAfter = new XmlDocument();
                    docQueryAfter.LoadXml(result);
                    XmlNode nAfter = docQueryAfter.SelectSingleNode("/RECORDS/RECORD");
                    if (nAfter != null)
                    {
                        List<ChangeLog> changes = new List<ChangeLog>();
                        CheckFieldChanges(changes, n, nAfter, "StatusCode.Description", "Status", false);
                        CheckFieldChanges(changes, n, nAfter, "OtherAuthorityCode.Description", "Anden myndighed", false);
                        CheckFieldChanges(changes, n, nAfter, "PhaseCode.Description", "Fase", false);
                        CheckFieldChanges(changes, n, nAfter, "InitiativeDuty.Description", "Sagen afventer", false);
                        CheckFieldChanges(changes, n, nAfter, "DeadlineNotificationKode", "Påmindelser", false);
                        CheckFieldChanges(changes, n, nAfter, "Deadline", "Frist", true);

                        EntityLogHelper.LogOnCase(ToCase, changes);
                    }

                    UpdateBOMMessage(BOMMessageRecno, BOMMessageStatusEnum.CaseUpdated);
                }
            }
            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMMessageHandler", "FuBOM",
                                string.Format("HandleStatusChange: Error changing state:\n{0}", ex.ToString()), System.Diagnostics.EventLogEntryType.Error);

                UpdateBOMMessage(BOMMessageRecno, BOMMessageStatusEnum.UpdateFailed);
            }
        }

        private static string GetLocalInitiativPligt(BOM.BOMSagStatus.InitiativPligt1 InitiativPligt)
        {
            switch (InitiativPligt)
            {
                case BOMSagStatus.InitiativPligt1.Ingen:
                    return "Ingen";
                case BOMSagStatus.InitiativPligt1.Ansoeger:
                    return "Ansøger";
                case BOMSagStatus.InitiativPligt1.Myndighed:
                    return "Myndighed";
            }
            return "";
        }

        private static void CheckFieldChanges(List<Fujitsu.eDoc.BOM.ChangeLog> Changes, XmlNode NodeBefore, XmlNode NodeAfter, string FieldName, string PropertyName, bool IsDate)
        {
            string FieldValueOriginal = NodeBefore.SelectSingleNode(FieldName).InnerText;
            string FieldValue = NodeAfter.SelectSingleNode(FieldName).InnerText;

            if (IsDate)
            {
                FieldValue = DateFromString(FieldValue);
                FieldValueOriginal = DateFromString(FieldValueOriginal);
            }

            if (FieldValue != FieldValueOriginal)
            {
                ChangeLog cl = new ChangeLog()
                {
                    LogdataName = PropertyName,
                    LogdataFrom = FieldValueOriginal,
                    LogdataTo = FieldValue
                };
                Changes.Add(cl);
            }
        }

        private static string DateFromString(string DateString)
        {
            DateTime d;
            if (DateTime.TryParse(DateString, out d))
            {
                return d.ToShortDateString();
            }
            return "";
        }

        private static void UpdateBOMMessage(string BOMMessageRecno, BOMMessageStatusEnum NewStatus)
        {
            try
            {
                string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateBOMMessageStatusMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.FuBOMMessage", Assembly.GetExecutingAssembly());
                xmlQuery = xmlQuery.Replace("#Recno#", BOMMessageRecno);
                xmlQuery = xmlQuery.Replace("#Status#", ((int)NewStatus).ToString());

                string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
            }
            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMMessageHandler", "FuBOM",
                                string.Format("UpdateBOMMessage: Error updating status:\n{0}", ex.ToString()), System.Diagnostics.EventLogEntryType.Error);

            }
        }
    }
}
