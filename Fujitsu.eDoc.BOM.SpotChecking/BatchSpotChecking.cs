using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Fujitsu.eDoc.BOM.SpotChecking.ProcessEngine
{
    public class BatchSpotChecking
    {
        private const string EMAILRECNO = "300307";

        public static void ProcessSpotChecking()
        {
            //System.Diagnostics.Debugger.Launch();

            int extraction = GetSettingInfo();

            if (extraction != 0)
            {
                Dictionary<int, string> caseRecnos = GetCasesForSpotChecking();

                if (caseRecnos.Count > 0)
                {
                    UpdateCasesForSpotChecking(caseRecnos, extraction);
                }

            }

            else
            {
               Fujitsu.eDoc.Core.Common.SimpleEventLogging(typeof(BatchSpotChecking).FullName, "FuBOM",
               $"Setting Enable SpotChecking is not set", System.Diagnostics.EventLogEntryType.Information);
            }


        }

        private static void UpdateCasesForSpotChecking(Dictionary<int, string> caseRecnos, int extraction)
        {
            for (int i = 0; i < caseRecnos.Count(); i++)
            {
                if (i == 0)
                {
                    continue;
                }

                //When setting is enabled AND modulus returns 0 AND the given activitytype is entitled for stabcontrol = Case will be marked as spot-checked
                else if (extraction != 0 && (i % extraction) == 0 && IsBOMActivityOnCaseEntitled(caseRecnos.ElementAt(i).Value))
                {
                    string xmlQuery = $@"<operation>
                             <UPDATESTATEMENT NAMESPACE='SIRIUS' ENTITY='Case' PRIMARYKEYVALUE='{caseRecnos.ElementAt(i).Key}'>
                                <METAITEM NAME='IsCaseForSpotChecking'>
                                <VALUE>-1</VALUE>
                                </METAITEM>
                             </UPDATESTATEMENT>
                            </operation>";

                    string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
                    Fujitsu.eDoc.Core.EntityLogUtilities.WriteMessage("Case", caseRecnos.ElementAt(i).Key, "Sagen er udtaget til stikprøve.");
                    SendEmailToCaseWorker(caseRecnos.ElementAt(i).Key);
                }

            }

        }

        private static int GetSettingInfo()
        {
            string value = Fujitsu.eDoc.Core.eDocSettingInformation.GetSettingValueFromeDoc("fujitsu", "enableSpotChecking");

            bool isSucceded = Int32.TryParse(value, out int output);

            if (isSucceded)
            {
                return output;
            }

            return 0;
        }

        private static void SendEmailToCaseWorker(int caseRecno)
        {
            string case_name = string.Empty;
            string ct_recno = string.Empty;
            string ct_name = string.Empty;
            string ct_email = string.Empty;
            string cat_casetype = string.Empty;
            string parameters = string.Empty;

            try
            {
                string xmlQuery = $@"<operation>
                              <QUERYDESC NAMESPACE='SIRIUS' ENTITY='Case' SELECTTYPE='DATASET' DATASETFORMAT='XML' TAG='RECORDS' LANGUAGE='DAN'>
                                 <RESULTFIELDS>
                                 <METAITEM TAG='Name'>Name</METAITEM>
                                </RESULTFIELDS>
                                <RELATIONS>
                                <RELATION NAME='OurRef'>
                                <RESULTFIELDS>
                                 <METAITEM TAG='Recno'>Recno</METAITEM>
                                 <METAITEM TAG='OurRef.Name'>Name</METAITEM>
                                 <METAITEM TAG='E-mail'>E-mail</METAITEM>
                                </RESULTFIELDS>
                                </RELATION>
                                <RELATION NAME='CaseType'>
                                <RESULTFIELDS>
                                 <METAITEM TAG='CaseType.Recno'>Recno</METAITEM>
                                </RESULTFIELDS>
                                </RELATION>
                                 </RELATIONS>
                                  <CRITERIA>
                                  <METAITEM NAME='Recno' OPERATOR='='>
                                    <VALUE>{caseRecno}</VALUE>
                                  </METAITEM>
                                </CRITERIA>
                              </QUERYDESC>
                            </operation>";

                string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);
                if (!string.IsNullOrEmpty(result))
                {
                    XDocument doc = XDocument.Parse(result);

                    if (doc.Root.FirstAttribute.Value != "0")
                    {
                        case_name = doc.Element("RECORDS").Element("RECORD").Element("Name").Value.ToString();
                        ct_name = doc.Element("RECORDS").Element("RECORD").Element("OurRef.Name").Value.ToString();
                        ct_recno = doc.Element("RECORDS").Element("RECORD").Element("Recno").Value.ToString();
                        ct_email = doc.Element("RECORDS").Element("RECORD").Element("E-mail").Value.ToString();
                        cat_casetype = doc.Element("RECORDS").Element("RECORD").Element("CaseType.Recno").Value.ToString();

                        parameters = string.Format("To={0};TopLevelEntityTitle={1};TopLevelEntityLinkSubtype={2};TopLevelEntityLinkRecno={3}", ct_email, case_name, cat_casetype, caseRecno.ToString());

                    }
                }

                //ct_recno = "200061";
                Fujitsu.eDoc.Notification.EmailNotificationManager.SendEmail(EMAILRECNO, "DAN", parameters, ct_recno);
            }

            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging(typeof(BatchSpotChecking).FullName, "FuBOM",
                $"Error on sending an email to this case worker: {ct_name} - {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);
            }

        }


        private static Dictionary<int, string> GetCasesForSpotChecking()
        {
            Dictionary<int, string> caseRecnoActivityCodePair = new Dictionary<int, string>();
            try
            {
                string xmlQuery = @"<operation>
                              <QUERYDESC NAMESPACE='SIRIUS' ENTITY='Case' SELECTTYPE='DATASET' DATASETFORMAT='XML' MAXROWS='250' TAG='RECORDS' LANGUAGE='DAN'>
                                <RESULTFIELDS>
                                 <METAITEM TAG='Recno'>Recno</METAITEM>
                                </RESULTFIELDS>
                                 <RELATIONS>
                                <RELATION NAME='ToBOMCase'>
                                <RESULTFIELDS>
                                 <METAITEM TAG='ActivityTypeCode'>ActivityTypeCode</METAITEM>
                                </RESULTFIELDS>
                                </RELATION>
                                 </RELATIONS>
                                <CRITERIA>
                                  <METAITEM NAME='IsCaseForSpotChecking' OPERATOR='IN'>
                                    <VALUE>0</VALUE>
                                  </METAITEM>
                                    <METAITEM NAME='ToCaseStatus' OPERATOR='IN'>
                                    <VALUE>300187</VALUE>
                                  </METAITEM>
                                </CRITERIA>
                                </QUERYDESC>
                            </operation>";

                string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

                if (!string.IsNullOrEmpty(result))
                {
                    XDocument doc = XDocument.Parse(result);

                    foreach (var el in doc.Root.Elements())
                    {
                        int caseRecno = int.Parse(el.Element("Recno").Value.ToString());
                        string activityTypeCode = el.Element("ActivityTypeCode").Value.ToString();
                        caseRecnoActivityCodePair.Add(caseRecno, activityTypeCode);
                    }
                }


            }
            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging(typeof(BatchSpotChecking).FullName, "FuBOM",
                $"Error on fetching cases that should be picked for spot checking. - {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);
            }
            return caseRecnoActivityCodePair;
        }

        private static bool IsBOMActivityOnCaseEntitled(string caseActivityTypeCode)
        {
            bool isBomActivtyEntitled = false;
            try
            {
                string xmlQuery = $@"<operation>
                              <QUERYDESC NAMESPACE='SIRIUS' ENTITY='code table: Fu BOM Activity Type' SELECTTYPE='DATASET' DATASETFORMAT='XML' TAG='RECORDS' LANGUAGE='DAN'>
                                <RESULTFIELDS>
                                 <METAITEM TAG='StabTestControl'>StabTestControl</METAITEM>
                                </RESULTFIELDS>
                                  <CRITERIA>
                                  <METAITEM NAME='Code' OPERATOR='='>
                                    <VALUE>{caseActivityTypeCode}</VALUE>
                                  </METAITEM>
                                </CRITERIA>
                              </QUERYDESC>
                            </operation>";

                string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);
                if (!string.IsNullOrEmpty(result))
                {
                    XDocument doc = XDocument.Parse(result);

                    if (doc.Root.FirstAttribute.Value != "0")
                    {
                        bool isSucceded = int.TryParse(doc.Element("RECORDS").Element("RECORD").Value, out int output);
                        if (isSucceded)
                        {
                            if (output == -1)
                            {
                                isBomActivtyEntitled = true;
                            }
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.BIF.CodeBehind.SpotCheckingOnCase", "FuBOM",
                 $"Error on deciding if this ActivityTypeCode : {caseActivityTypeCode} is entitled for spot checking. -  {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);
            }

            return isBomActivtyEntitled;
        }
    }
}

