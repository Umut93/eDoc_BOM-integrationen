using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace Fujitsu.eDoc.BOM
{
    public class EntityLogHelper
    {
        public static string LOGMESSAGE_INFO_BOM_CHANGES = "BOM_CASE_UPDATED";
        public static string LOGMESSAGE_INFO_BOM_FILE_SENT = "BOM_FILE_SENT";
        public static int LOGMESSAGE_RECNO_BOM_CHANGES = 300061;
        public static int LOGMESSAGE_RECNO_BOM_FILE_SENT = 300062;

        public static void LogChangesTest(BOMCaseUpdateType c)
        {
            ChangeLog cl1 = new ChangeLog()
            {
                LogdataName = "Sagsfase",
                LogdataFrom = "Myndighedens behandling",
                LogdataTo = "Afventer start"
            };
            ChangeLog cl2 = new ChangeLog()
            {
                LogdataName = "Sagen afventer",
                LogdataFrom = "Myndigheden",
                LogdataTo = "Ansøger"
            };
            ChangeLog cl3 = new ChangeLog()
            {
                LogdataName = "Dokumentationskrav tilføjet - Tinglyste dokumenter, Krav fjernet, Afventer start",
                LogdataFrom = "",
                LogdataTo = ""
            };

            LogOnCase("327559", LOGMESSAGE_RECNO_BOM_CHANGES, LOGMESSAGE_INFO_BOM_CHANGES, new List<ChangeLog>() { cl1, cl2, cl3 });
        }

        public static void LogChanges(BOMCaseUpdateType c)
        {
            if (c.Changes != null)
            {
                if (c.Changes.Count > 0)
                {
                    LogOnCase(c.CaseRecno, LOGMESSAGE_RECNO_BOM_CHANGES, LOGMESSAGE_INFO_BOM_CHANGES, c.Changes);
                }
            }

            if(c.MainDocument != null)
            {
                LogOnFile(c.MainDocument.FileRecno, LOGMESSAGE_RECNO_BOM_FILE_SENT, LOGMESSAGE_INFO_BOM_FILE_SENT);
            }
            if (c.Attachments != null)
            {
                foreach(BOMReplyDocument att in c.Attachments)
                {
                    LogOnFile(att.FileRecno, LOGMESSAGE_RECNO_BOM_FILE_SENT, LOGMESSAGE_INFO_BOM_FILE_SENT);
                }
            }
        }
        
        public static void LogOnCase(string caseRecno, List<ChangeLog> logs)
        {
            if (logs != null)
            {
                if (logs.Count > 0)
                {
                    LogOnCase(caseRecno, LOGMESSAGE_RECNO_BOM_CHANGES, LOGMESSAGE_INFO_BOM_CHANGES, logs);
                }
            }
        }

        private static void LogOnCase(string caseRecno, int ToEntityLogMessages, string Infotype, List<ChangeLog> logs)
        {
            string doclogstmt = string.Format(@"<operation>
                              <UPDATESTATEMENT NAMESPACE='SIRIUS' ENTITY='Case' PRIMARYKEYVALUE='{0}'>
                              </UPDATESTATEMENT>
                              <BATCH>
                                  <INSERTSTATEMENT NAMESPACE='SIRIUS' ENTITY='Entitylog'>
                                      <METAITEM NAME='Recno'>
                                          <VALUE></VALUE>
                                      </METAITEM>
                                      <METAITEM NAME='Name'>
                                          <VALUE>case</VALUE>
                                      </METAITEM>
                                      <METAITEM NAME='Key'>
                                          <VALUE>{0}</VALUE>
                                      </METAITEM>
                                      <METAITEM NAME='Type'>
                                          <VALUE>4</VALUE>
                                      </METAITEM>
                                      <METAITEM NAME='ToEntityLogMessages'>
                                          <VALUE>{1}</VALUE>
                                      </METAITEM>
                                      <METAITEM NAME='Infotype'>
                                          <VALUE>{2}</VALUE>
                                      </METAITEM>
                                      <METAITEM NAME='LogdataName'>
                                          <VALUE></VALUE>
                                      </METAITEM>
                                      <METAITEM NAME='LogdataFrom'>
                                          <VALUE></VALUE>
                                      </METAITEM>
                                      <METAITEM NAME='LogdataTo'>
                                          <VALUE></VALUE>
                                      </METAITEM>
                                  </INSERTSTATEMENT>
                              </BATCH>
                            </operation>", caseRecno, ToEntityLogMessages, Infotype);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(doclogstmt);
            XmlNode nBatch = doc.SelectSingleNode("operation/BATCH");
            XmlNode nEntitylog = doc.SelectSingleNode("operation/BATCH/INSERTSTATEMENT");
            foreach (ChangeLog l in logs)
            {
                nEntitylog.SelectSingleNode("METAITEM[@NAME='LogdataName']/VALUE").InnerText = l.LogdataName;
                nEntitylog.SelectSingleNode("METAITEM[@NAME='LogdataFrom']/VALUE").InnerText = l.LogdataFrom;
                nEntitylog.SelectSingleNode("METAITEM[@NAME='LogdataTo']/VALUE").InnerText = l.LogdataTo;
                if (nEntitylog.ParentNode == null)
                {
                    nBatch.AppendChild(nEntitylog);
                }
                nEntitylog = nEntitylog.Clone();
            }
            doclogstmt = doc.OuterXml;

            Fujitsu.eDoc.Core.Common.ExecuteSingleAction(doclogstmt);
        }

        private static void LogOnFile(string fileRecno, int ToEntityLogMessages, string Infotype)
        {
            string doclogstmt = string.Format(@"<operation>
                              <UPDATESTATEMENT NAMESPACE='SIRIUS' ENTITY='File' PRIMARYKEYVALUE='{0}'>
                              </UPDATESTATEMENT>
                              <BATCH>
                                  <INSERTSTATEMENT NAMESPACE='SIRIUS' ENTITY='Entitylog'>
                                      <METAITEM NAME='Recno'>
                                          <VALUE></VALUE>
                                      </METAITEM>
                                      <METAITEM NAME='Name'>
                                          <VALUE>file</VALUE>
                                      </METAITEM>
                                      <METAITEM NAME='Key'>
                                          <VALUE>{0}</VALUE>
                                      </METAITEM>
                                      <METAITEM NAME='Type'>
                                          <VALUE>4</VALUE>
                                      </METAITEM>
                                      <METAITEM NAME='ToEntityLogMessages'>
                                          <VALUE>{1}</VALUE>
                                      </METAITEM>
                                      <METAITEM NAME='Infotype'>
                                          <VALUE>{2}</VALUE>
                                      </METAITEM>
                                  </INSERTSTATEMENT>
                              </BATCH>
                            </operation>", fileRecno, ToEntityLogMessages, Infotype);

            Fujitsu.eDoc.Core.Common.ExecuteSingleAction(doclogstmt);
        }
    }
}
