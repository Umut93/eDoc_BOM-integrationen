using Fujitsu.eDoc.BOM.BOMSagsbehandling;
using Fujitsu.eDoc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace Fujitsu.eDoc.BOM.UpdateServiceMaal.ProcessEngine
{
    public class ProcessServiceMaal
    {
        private List<string> ProcessedServiceGoals = new List<string>();
        private string total;



        /// <summary>
        /// Updatting service goals
        /// </summary>
        public void BatchServiceMaals()
        {
            try
            {
                //Procesing FuBOMCases
                string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetFuBomCases.xml", "Fujitsu.eDoc.BOM.UpdateServiceMaal.XML", Assembly.GetExecutingAssembly());
                string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);
                XDocument xDoc = XDocument.Parse(result);

                total = xDoc.Element("RECORDS").Attribute("RECORDCOUNT").Value;
                IEnumerable<XElement> records = xDoc.Element("RECORDS").Elements();

                Fujitsu.eDoc.BOM.BOMSagsbehandling.AnsoegningId[] ansoegningIds = new BOMSagsbehandling.AnsoegningId[records.Count()];

                if (total != "0")
                {
                    string[] appIDs = records.Elements("ToBomCase.ApplicationId").Select(appId => appId.Value).ToArray();

                    for (int i = 0; i < appIDs.Length; i++)
                    {
                        ansoegningIds[i] = new BOMSagsbehandling.AnsoegningId { Id = appIDs.ElementAt(i) };
                    }
                }

                List<AnsoegningId> list = ansoegningIds.ToList();

                if ((list != null) && (list.Any()))
                {
                    IEnumerable<List<AnsoegningId>> chunkedListOfAnsoegningIds = SplitList(list);

                    BOMSagsbehandling.SagsbehandlingServiceClient sag = BOMCaller.GetSagsbehandlingServiceClient();
                    //Call the service to retrieve the servicegoal for all cases that do not have a servicegoal

                    foreach (List<AnsoegningId> chunk in chunkedListOfAnsoegningIds)
                    {
                        Fujitsu.eDoc.BOM.BOMSagsbehandling.ServiceMaalStatistikkerType serviceMaalStatistikker = sag.LaesServiceMaalStatistik(chunk.ToArray());

                        if ((serviceMaalStatistikker.ServiceMaalStatistik != null) && (serviceMaalStatistikker.ServiceMaalStatistik.Any()))
                        {
                            string updateQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateActiveCasesWithServiceMaal.xml", "Fujitsu.eDoc.BOM.UpdateServiceMaal.XML", Assembly.GetExecutingAssembly());
                            XDocument xDocUpdate = XDocument.Parse(updateQuery);

                            foreach (BOMSagsbehandling.ServiceMaalStatistikType1 type in serviceMaalStatistikker.ServiceMaalStatistik)
                            {
                                bool isSagsbehandlingForbrugtDageSpecified = type.Statistik.SagsbehandlingForbrugtDageSpecified;
                                bool isVisitationForbrugtDageSpecified = type.Statistik.VisitationForbrugtDageSpecified;
                                string serviceMaalDage = type.ServiceMaal.Dage;
                                int caseManageMentSpentDays = type.Statistik.SagsbehandlingForbrugtDage;
                                int visitationForbrugtDate = type.Statistik.VisitationForbrugtDage;

                                bool isServiceTargetProvided = Int32.TryParse(serviceMaalDage, out int serviceMaal);
                                bool IsCaseExempted = !string.IsNullOrEmpty(type.Fritagelsesbegrundelse.Kode);



                                xDocUpdate.Element("operation").Element("UPDATESTATEMENT").Attribute("PRIMARYKEYVALUE").Value = GetFuBomCaseRecno(type.BOMSagID);
                                xDocUpdate.Descendants("METAITEM").Where(x => x.Attribute("NAME").Value == "CaseManagementSpentDaysSpecified").SingleOrDefault().SetElementValue("VALUE", isSagsbehandlingForbrugtDageSpecified == true ? 1 : 0);
                                xDocUpdate.Descendants("METAITEM").Where(x => x.Attribute("NAME").Value == "VisitationSpentDaysSpecified").SingleOrDefault().SetElementValue("VALUE", isVisitationForbrugtDageSpecified == true ? 1 : 0);

                                //Service targets exists
                                if (isServiceTargetProvided)
                                {
                                    xDocUpdate.Descendants("METAITEM").Where(x => x.Attribute("NAME").Value == "ServiceMaalDays").SingleOrDefault().SetElementValue("VALUE", IsCaseExempted ? "-" : (int.Parse(serviceMaalDage) - caseManageMentSpentDays).ToString());
                                    xDocUpdate.Descendants("METAITEM").Where(x => x.Attribute("NAME").Value == "CaseManagementSpentDays").SingleOrDefault().SetElementValue("VALUE", caseManageMentSpentDays);
                                    xDocUpdate.Descendants("METAITEM").Where(x => x.Attribute("NAME").Value == "VisitationSpentDays").SingleOrDefault().SetElementValue("VALUE", visitationForbrugtDate);
                                    xDocUpdate.Descendants("METAITEM").Where(x => x.Attribute("NAME").Value == "ServiceMaalStatus").SingleOrDefault().SetElementValue("VALUE", ServiceGoalStatus.None);
                                    ProcessedServiceGoals.Add(type.BOMSagID);
                                }
                                else
                                {
                                    xDocUpdate.Descendants("METAITEM").Where(x => x.Attribute("NAME").Value == "ServiceMaalDays").SingleOrDefault().SetElementValue("VALUE", serviceMaalDage);
                                    xDocUpdate.Descendants("METAITEM").Where(x => x.Attribute("NAME").Value == "CaseManagementSpentDays").SingleOrDefault().SetElementValue("VALUE", caseManageMentSpentDays);
                                    xDocUpdate.Descendants("METAITEM").Where(x => x.Attribute("NAME").Value == "VisitationSpentDays").SingleOrDefault().SetElementValue("VALUE", visitationForbrugtDate);
                                    xDocUpdate.Descendants("METAITEM").Where(x => x.Attribute("NAME").Value == "ServiceMaalStatus").SingleOrDefault().SetElementValue("VALUE", ServiceGoalStatus.Excluded);
                                }
                                Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xDocUpdate.ToString());
                            }
                        }

                    }
                    LogProcessingCases();
                }

            }
            catch (Exception ex)
            {
                Common.SimpleEventLogging(typeof(ProcessServiceMaal).FullName, "FuBOM", ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
            }

        }

        /// <summary>
        /// Splitting a list into smaller lists of N size of 240.
        /// </summary>
        /// <typeparam name="AnsoegningId"></typeparam>
        /// <param name="ansogeningIDs"></param>
        /// <param name="nSize"></param>
        /// <returns></returns>

        private static IEnumerable<List<AnsoegningId>> SplitList<AnsoegningId>(List<AnsoegningId> ansogeningIDs, int nSize = 240)
        {
            int totalAmountOfansogeningIDs = ansogeningIDs.Count;

            for (int i = 0; i < totalAmountOfansogeningIDs; i += nSize)
            {
                yield return ansogeningIDs.GetRange(i, Math.Min(nSize, totalAmountOfansogeningIDs - i));
            }
        }


        /// <summary>
        /// The total count for cases that have been updated in DB.
        /// </summary>
        /// <param name="serviceMaalStatistikker"></param>
        private void LogProcessingCases()
        {
            if (ProcessedServiceGoals.Any())
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging(typeof(ProcessServiceMaal).FullName, "FuBOM", $"Processed {ProcessedServiceGoals.Count} out of {total} BOM cases for service goals.", System.Diagnostics.EventLogEntryType.Information);
            }
        }


        /// <summary>
        /// Updates always the first occurence. It is searched by the caseID 
        /// </summary>
        /// <param name="caseID"></param>
        /// <returns></returns>
        private string GetFuBomCaseRecno(string caseID)
        {
            string recno = "";
            try
            {
                XmlDocument doc = new XmlDocument();
                string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetFuBomCaseRecno.xml", "Fujitsu.eDoc.BOM.UpdateServiceMaal.XML", Assembly.GetExecutingAssembly());
                doc.LoadXml(xmlQuery);
                doc.SelectSingleNode("//CRITERIA//METAITEM//VALUE").InnerText = caseID;

                string resultQuery = Fujitsu.eDoc.Core.Common.ExecuteQuery(doc.OuterXml);

                XmlDocument resultQueryXML = new XmlDocument();
                resultQueryXML.LoadXml(resultQuery);

                if (resultQueryXML.SelectSingleNode("RECORDS").Attributes["RECORDCOUNT"].Value != "0")
                {
                    recno = resultQueryXML.SelectSingleNode("//RECORD//Recno").InnerText;
                }

            }
            catch (Exception ex)
            {
                Common.SimpleEventLogging(typeof(ProcessServiceMaal).FullName, "FuBOM", $" Error on getting the FuBom caseID: {caseID}. \n {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);
            }
            return recno;
        }

    }
}

