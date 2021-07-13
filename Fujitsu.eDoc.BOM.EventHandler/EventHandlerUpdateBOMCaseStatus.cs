using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SI.Biz.Core;
using SI.Biz.Core.Fluent;
using SI.Biz.Core.Events;
using SI.Linq.Meta;
using Fujitsu.eDoc.Core;
using SI.Util.Events;
using System.Xml;
using System.Reflection;
using Fujitsu.eDoc.BOM.BOMSagsbehandling;
using System.Xml.Linq;

namespace Fujitsu.eDoc.BOM.EventHandler
{
    [BizEventSetup]
    public class EventHandlerUpdateBOMCaseStatus : IEventSetup
    {
        private bool DoEventHandling
        {
            get
            {
                string IsBoMEnabled = SI.Biz.Core.Fluent.Get.SettingsValue("fujitsu", "bomenable");
                return IsBoMEnabled == "-1";
            }
        }

        public void Initialize(string configXml)
        {
            XmlDocument configXmlDoc = new XmlDocument();
            configXmlDoc.LoadXml(configXml);

            XmlNode node = configXmlDoc.SelectSingleNode("/config/enable[@handler='Fujitsu.eDoc.BOM.Eventhandler.EventHandlerUpdateBOMCaseStatus']");

            if (node != null)
            {
                // Case
                EventShimFactory.GetEventShim<CaseManagerEventShim>().AfterUpdate += new SystemEventHandler<MetaOperationEventArgs>(this.Case_AfterUpdate);
            }
        }


        private void Case_AfterUpdate(MetaOperationEventArgs args)
        {
            if (DoEventHandling)
            {
                try
                {
                    string ca_recno = args.Operation.Statement.PrimaryKey;
                    string caseStatusRecno = GetMetaitemValue(args.Operation.Statement.Items, "ToCaseStatus"); // ToCaseStatus 

                    //string bomCaseRecno = GetBomCaseRecno(Recno);

                    bool IsOurRefChanged = IsMetaitemValueChanged(args, "ToCaseStatus");

                    if (IsOurRefChanged)
                    {
                        if (IsBOMCase(args.Operation.Statement.PrimaryKey))
                        {
                            List<string> fuBOMStatusMap = BOMCaseHandler.GetBOMStatus(caseStatusRecno);
                            FuBomCase fuBomCase = GetBomCase(ca_recno);

                            //FuBomStatusMap needs to be configured with 3 codes before being queued
                            if (fuBOMStatusMap.Any() & fuBomCase != null)
                            {
                                BOMConfiguration bomConfiguration = BOMConfigHandler.GetBOMConfiguration();
                                string notificationProfile = bomConfiguration.GetReceivedNotificationProfile();

                                BOMCaseUpdateType caseUpdateType = CreateBomCaseUpdateType(ca_recno, fuBOMStatusMap, fuBomCase, notificationProfile);
                                //Add to bom_queue
                                BOMQueueHandler.SaveQueuedItem(caseUpdateType, BOMQueueStatusType.Pending);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.EventHandler.EventHandlerUpdateBOMCaseStatus", "FuBOM", string.Format("Error updating case Status in BOM ({0}):{1}\n{2}", args.Operation.Statement.PrimaryKey, ex.ToString(), args.Operation.Statement.ToString()), System.Diagnostics.EventLogEntryType.Error);
                }
            }
        }

        private static BOMCaseUpdateType CreateBomCaseUpdateType(string ca_recno, List<string> fuBOMStatusMap, FuBomCase fuBomCase, string notificationProfile)
        {
            return new BOMCaseUpdateType()
            {
                CaseRecno = ca_recno,
                BOMCaseId = fuBomCase.Caseid,
                Title = "Opdateret status",
                Status = new BOMCaseUpdateStatusType
                {
                    SagStatusKode = fuBOMStatusMap.First(),
                    InitiativPligtKode = fuBOMStatusMap.ElementAt(1).ToString(),
                    FaseKode = fuBOMStatusMap.ElementAt(2),
                    FristNotifikationProfilKode = notificationProfile,
                },
                InitiativeDuty = fuBomCase.ResponsibleAuthority,
                ToBOMCase = fuBomCase.Recno,
                Date = DateTime.Now,
                Changes = new List<ChangeLog>
                                    {
                                        new ChangeLog {
                                            LogdataName ="Status ændret",
                                            LogdataFrom=fuBomCase.StatusCode,
                                            LogdataTo =fuBOMStatusMap.First()

                                        },
                                        new ChangeLog {
                                            LogdataName ="Sagen afventer ændret",
                                            LogdataFrom=fuBomCase.InitiativeDuty,
                                            LogdataTo =fuBOMStatusMap.ElementAt(1)

                                        },
                                        new ChangeLog {
                                            LogdataName ="Fase ændret",
                                            LogdataFrom=fuBomCase.PhaseCode,
                                            LogdataTo =fuBOMStatusMap.ElementAt(2)

                                        }
                                    }

            };
        }

        private bool IsBOMCase(string Recno)
        {
            string xmlQuery = string.Format(@"<operation>
                                  <QUERYDESC NAMESPACE='SIRIUS' ENTITY='Case' DATASETFORMAT='XML'>
                                    <RELATIONS>
                                      <RELATION NAME='ToBOMCase'>
                                        <RESULTFIELDS>
                                          <METAITEM TAG='BOMCaseRecno'>Recno</METAITEM>
                                        </RESULTFIELDS>
                                      </RELATION>
                                    </RELATIONS>
                                    <CRITERIA>
                                      <METAITEM NAME='Recno' OPERATOR='='>
                                        <VALUE>{0}</VALUE>
                                      </METAITEM>
                                    </CRITERIA>
                                  </QUERYDESC>
                                </operation>", Recno);

            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            if (doc.SelectNodes("/RECORDS/RECORD/BOMCaseRecno").Count > 0)
            {
                if (!string.IsNullOrEmpty(doc.SelectSingleNode("/RECORDS/RECORD/BOMCaseRecno").InnerText))
                {
                    return true;
                }
            }
            return false;
        }

        private FuBomCase GetBomCase(string ca_Recno)
        {
            FuBomCase bomCase = null;

            try
            {
                string xmlQuery = string.Format(@"<operation>
                                  <QUERYDESC NAMESPACE='SIRIUS' ENTITY='Case' DATASETFORMAT='XML'>
                                    <RELATIONS>
                                      <RELATION NAME='ToBOMCase' JOIN='EQUAL'>
                                        <RESULTFIELDS>
                                          <METAITEM TAG='Recno'>Recno</METAITEM>
                                          <METAITEM TAG='CaseId'>CaseId</METAITEM>
                                          <METAITEM TAG='ResponsibleAuthority'>ResponsibleAuthority</METAITEM>
                                          <METAITEM TAG='StatusCode'>StatusCode</METAITEM>
                                          <METAITEM TAG='InitiativeDuty'>InitiativeDuty</METAITEM>
                                          <METAITEM TAG='PhaseCode'>PhaseCode</METAITEM>
                                        </RESULTFIELDS>
                                      </RELATION>
                                    </RELATIONS>
                                    <CRITERIA>
                                      <METAITEM NAME='Recno' OPERATOR='='>
                                        <VALUE>{0}</VALUE>
                                      </METAITEM>
                                    </CRITERIA>
                                  </QUERYDESC>
                                </operation>", ca_Recno);

                string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);
                XDocument doc = XDocument.Parse(result);

                if (doc.Root.FirstAttribute.Value != "0")
                {
                    bomCase = new FuBomCase();
                    foreach (var metaitem in doc.Root.Elements())
                    {
                        bomCase.Recno = metaitem.Element("Recno").Value;
                        bomCase.Caseid = metaitem.Element("CaseId").Value;
                        bomCase.ResponsibleAuthority = metaitem.Element("ResponsibleAuthority").Value;
                        bomCase.StatusCode = metaitem.Element("StatusCode").Value;
                        bomCase.InitiativeDuty = metaitem.Element("InitiativeDuty").Value;
                        bomCase.PhaseCode = metaitem.Element("PhaseCode").Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.SimpleEventLogging(typeof(EventHandlerUpdateBOMCaseStatus).FullName, "FuBOM", $"Could not get a FuBomCase by using ca_recno: {ca_Recno}\n {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);
            }
            return bomCase;
        }


        private string GetOriginalValue(string elementId, SI.Biz.Core.CurrentValues currentValues)
        {
            string elementValue = string.Empty;
            if (currentValues.Exists(elementId))
                elementValue = currentValues[elementId];
            return elementValue;
        }
        private string GetMetaitemValue(SI.Biz.Core.MetaExecution.MetaItemList items, string itemName)
        {
            string elementValue = string.Empty;
            if (items.Exists(new SI.Linq.Meta.MetaItemName(itemName)))
                elementValue = ((SI.Biz.Core.MetaExecution.MetaItem)items[new SI.Linq.Meta.MetaItemName(itemName)]).Value;
            return elementValue;
        }
        private bool IsMetaitemValueChanged(MetaOperationEventArgs args, string itemName)
        {
            if (args.Operation.Statement.Items.Exists(new SI.Linq.Meta.MetaItemName(itemName)))
            {
                string elementValue = ((SI.Biz.Core.MetaExecution.MetaItem)args.Operation.Statement.Items[new SI.Linq.Meta.MetaItemName(itemName)]).Value;
                string originalValue = GetOriginalValue(itemName, args.Current);
                if (elementValue != originalValue)
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal class FuBomCase
    {
        public string Recno { get; set; }
        public string Caseid { get; set; }
        public string ResponsibleAuthority { get; set; }
        public string StatusCode { get; set; }
        public string InitiativeDuty { get; set; }
        public string PhaseCode { get; set; }

    }
}
