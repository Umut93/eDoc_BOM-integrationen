using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Fujitsu.eDoc.BOM
{
    public enum BOMQueueStatusType
    {
        None = 300000,
        Pending = 300001,
        PendingPDFConversion = 300002,
        FinishConverting = 300003,
        Success = 300004,
        Failed = 300005,
        ConversionFailed = 300006
    }

    public static class BOMQueueHandler
    {
        public static string AddToQueue(BOMCaseUpdateType c)
        {            
            // Send to conversion

            if (c.MainDocument != null)
            {
                MakePDF(c.MainDocument);
            }

            if (c.Attachments != null)
            {
                foreach (BOMReplyDocument att in c.Attachments)
                {
                    MakePDF(att);
                }
            }

            return SaveQueuedItem(c, BOMQueueStatusType.PendingPDFConversion);
        }

        public static string SaveQueuedItem(BOMCaseUpdateType c, BOMQueueStatusType Status)
        {
            // Serialize object
            string s = Serialization.Serialize<BOMCaseUpdateType>(c);

            // Store in database
            string xmlStmt = Fujitsu.eDoc.Core.Common.GetResourceXml("CreateBOMQueueMetaInsert.xml", "Fujitsu.eDoc.BOM.XML.BOMQueue", Assembly.GetExecutingAssembly());
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlStmt);

            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ToCase']/VALUE").InnerText = c.CaseRecno;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='MetaXML']/VALUE").InnerText = s;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='Status']/VALUE").InnerText = ((int)Status).ToString();
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='Title']/VALUE").InnerText = c.Title;
            xmlStmt = doc.OuterXml;

            Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlStmt);

            return s;
        }
        
        private static void MakePDF(BOMReplyDocument doc)
        {            
            System.Security.Principal.WindowsImpersonationContext wic = System.Security.Principal.WindowsIdentity.Impersonate(IntPtr.Zero);
            try
            {
                if (doc.FileExtention.ToLower() == "pdf")
                {
                    doc.FileFullname = GetOriginalFile(doc.FileRecno, doc.FileExtention);
                }
                else
                {
                    string file = string.Format("{0}|{1}", doc.FileRecno, doc.DocumentRevisionRecno);
                    doc.ConversionJobId = PDFHelper.StartConvertFile(file);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                wic.Undo();
            }
        }

        private static string GetOriginalFile(string FileRecno, string FileExtension)
        {
            string filename = string.Format("{0}{1}.{2}", Fujitsu.eDoc.Core.FileUploadSupport.InvokeGetTemporaryPath(), Guid.NewGuid().ToString(), FileExtension);
            Fujitsu.eDoc.Core.FileManagerUtilities.RetrieveActiveFileFromArchive(FileRecno, filename);

            return filename;
        }


        public static void HandleFuBomQueues()
        {            
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetPendingBOMQueueListMetaQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMQueue", Assembly.GetExecutingAssembly());
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNodeList nItems = doc.SelectNodes("/RECORDS/RECORD");

            var bomQueues = new ConcurrentBag<FuBomQueue>();

            foreach (XmlNode n in nItems)
            {
                var Recno = n.SelectSingleNode("Recno").InnerText;
                var MetaXML = n.SelectSingleNode("MetaXML").InnerText;
                var CreatedDate = n.SelectSingleNode("CreatedDate").InnerText;
                var bOMCaseUpdateType = Serialization.Deserialize<BOMCaseUpdateType>(MetaXML);

                FuBomQueue fubomQueue = new FuBomQueue(Recno, MetaXML, bOMCaseUpdateType.CaseRecno, CreatedDate, bOMCaseUpdateType, BOMQueueStatusType.None);
                bomQueues.Add(fubomQueue);
            };

            IList<FuBomQueue> fuBomQueueStatuses = GetFuBomQueueStatusParallel(bomQueues);

            List<IGrouping<string, FuBomQueue>> queues = fuBomQueueStatuses.OrderBy(x => x.CreatedDate).GroupBy(ca => ca.CaseRecno).ToList();

            foreach (var groupByCase in queues)
            {
                foreach (var item in groupByCase)
                {
                    try
                    {
                        item.BOMQueueStatusType = BOMQueueHandler.CheckQueuedStatus(item.MetaXML, out item.BOMCaseUpdateType);

                        if (item.BOMQueueStatusType == BOMQueueStatusType.Pending)
                        {
                            break;
                        }

                        else if (item.BOMQueueStatusType == BOMQueueStatusType.FinishConverting)
                        {                                                        
                            BOMCaseHandler.UpdateBOMCase(item.BOMCaseUpdateType);
                            UpdateQueueItem(item.Recno, Serialization.Serialize<BOMCaseUpdateType>(item.BOMCaseUpdateType), BOMQueueStatusType.Success);
                        }

                        else if (item.BOMQueueStatusType == BOMQueueStatusType.ConversionFailed || item.BOMQueueStatusType == BOMQueueStatusType.Failed)
                        {
                            UpdateQueueItem(item.Recno, Serialization.Serialize<BOMCaseUpdateType>(item.BOMCaseUpdateType), item.BOMQueueStatusType, $"{item.BOMQueueStatusType.ToString()}");

                            if (item.BOMQueueStatusType == BOMQueueStatusType.ConversionFailed)
                            {
                                EmailHelper.SendFailedNotification(item.BOMCaseUpdateType, "Konvertering til PDF fejlede");
                            }
                            else
                            {
                                EmailHelper.SendFailedNotification(item.BOMCaseUpdateType, "Fejl ved behandling af filer");
                            }

                            foreach (var pendingQueues in groupByCase)
                            {
                                if (pendingQueues.BOMQueueStatusType == BOMQueueStatusType.Pending)
                                {
                                    pendingQueues.BOMQueueStatusType = BOMQueueStatusType.Failed;
                                    UpdateQueueItem(pendingQueues.Recno, Serialization.Serialize<BOMCaseUpdateType>(pendingQueues.BOMCaseUpdateType), pendingQueues.BOMQueueStatusType, $"Previous queue failed {item.Recno} and all further pendings will be faulted as well.");
                                    EmailHelper.SendFailedNotification(item.BOMCaseUpdateType, "Fejl ved behandling af filer");
                                }
                            }
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdateQueueItem(item.Recno, item.MetaXML, BOMQueueStatusType.Failed, ex.Message);
                        Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMQueueHandler", "FuBOM",
                            string.Format("Error handling queued item (Recno={0}):\n{1}\nMetaXML:\n{2}", item.Recno, ex.ToString(), item.MetaXML), System.Diagnostics.EventLogEntryType.Error);
                        EmailHelper.SendFailedNotification(item.BOMCaseUpdateType, ex.Message);

                        foreach (var pendingQueues in groupByCase)
                        {
                            if (pendingQueues.BOMQueueStatusType == BOMQueueStatusType.Pending)
                            {
                                pendingQueues.BOMQueueStatusType = BOMQueueStatusType.Failed;
                                UpdateQueueItem(pendingQueues.Recno, Serialization.Serialize<BOMCaseUpdateType>(pendingQueues.BOMCaseUpdateType), pendingQueues.BOMQueueStatusType, $"Previous queue failed {item.Recno} and all further pendings will be faulted as well.");
                                EmailHelper.SendFailedNotification(item.BOMCaseUpdateType, "Fejl ved behandling af filer");
                            }
                        }
                        break;
                    }
                }
            }
        }


        public static IList<FuBomQueue> GetFuBomQueueStatusParallel(ConcurrentBag<FuBomQueue> bomQueues)
        {
            var fuBOMQueues = new ConcurrentBag<FuBomQueue>();

            Parallel.ForEach(bomQueues, bomQueue =>
            {
                try
                {
                    // Old, may be used again if this test don't work
                    //bomQueue.BOMQueueStatusType = BOMQueueHandler.CheckQueuedStatus(bomQueue.MetaXML, out cc);
                    fuBOMQueues.Add(bomQueue);
                }
                catch (Exception ex)
                {
                    Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMQueueHandler", "FuBOM",
                       $"Failed getting a status for the FuBomQueue recno: {bomQueue.Recno}  {ex.ToString()}\n", System.Diagnostics.EventLogEntryType.Error);
                }
            });

            return fuBOMQueues.ToList();
        }

        private static void UpdateQueueItem(string Recno, string MetaXML, BOMQueueStatusType status, string ErrorMsg = null)
        {
            string xmlStmt = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateBOMQueueMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.BOMQueue", Assembly.GetExecutingAssembly());
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlStmt);

            doc.SelectSingleNode("/operation/UPDATESTATEMENT").Attributes["PRIMARYKEYVALUE"].Value = Recno;
            doc.SelectSingleNode("/operation/UPDATESTATEMENT/METAITEM[@NAME='MetaXML']/VALUE").InnerText = MetaXML;
            doc.SelectSingleNode("/operation/UPDATESTATEMENT/METAITEM[@NAME='Status']/VALUE").InnerText = ((int)status).ToString();
            doc.SelectSingleNode("/operation/UPDATESTATEMENT/METAITEM[@NAME='ErrorMsg']/VALUE").InnerText = ErrorMsg;
            xmlStmt = doc.OuterXml;

            Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlStmt);
        }

        public static BOMQueueStatusType CheckQueuedStatus(string SerializedBOMCaseUpdate, out BOMCaseUpdateType cc)
        {
            BOMQueueStatusType status = BOMQueueStatusType.FinishConverting;

            cc = Serialization.Deserialize<BOMCaseUpdateType>(SerializedBOMCaseUpdate);

            string FileFullname = string.Empty;

            if (cc.MainDocument != null)
            {
                PDFStatusType pdfStatus = PDFHelper.GetConvertStatus(cc.MainDocument, out FileFullname);
                switch (pdfStatus)
                {
                    case PDFStatusType.Pending:
                        status = BOMQueueStatusType.Pending;
                        break;
                    case PDFStatusType.Success:
                        cc.MainDocument.FileFullname = FileFullname;
                        cc.MainDocument.FileExtention = "PDF";
                        cc.MainDocument.FileMimeType = "application/pdf";
                        break;
                    case PDFStatusType.Failed:
                        status = BOMQueueStatusType.ConversionFailed;
                        break;
                }
            }

            if (cc.Attachments != null)
            {
                foreach (BOMReplyDocument att in cc.Attachments)
                {
                    PDFStatusType pdfStatus = PDFHelper.GetConvertStatus(att, out FileFullname);
                    switch (pdfStatus)
                    {
                        case PDFStatusType.Pending:
                            status = BOMQueueStatusType.Pending;
                            break;
                        case PDFStatusType.Success:
                            att.FileFullname = FileFullname;
                            att.FileExtention = "PDF";
                            att.FileMimeType = "application/pdf";
                            break;
                        case PDFStatusType.Failed:
                            status = BOMQueueStatusType.ConversionFailed;
                            break;
                    }
                }
            }
            return status;
        }
    }
}
