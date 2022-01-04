﻿using SI.Biz.Core.Events;
using SI.Util.Events;
using System;
using System.Xml;

namespace Fujitsu.eDoc.BOM.EventHandler
{
    [BizEventSetup]
    public class EventHandlerUpdateBOMCaseWorker : IEventSetup
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


            if (configXmlDoc.SelectSingleNode("/config/enable[@handler='Fujitsu.eDoc.Eventhandlers.EventHandlerUpdateBOMCaseWorker']") != null)
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
                    string Recno = args.Operation.Statement.PrimaryKey;
                    string OurRef = GetMetaitemValue(args.Operation.Statement.Items, "OurRef");
                    string OurRefOriginal = GetOriginalValue("OurRef", args.Current);
                    bool IsOurRefChanged = IsMetaitemValueChanged(args, "OurRef");

                    if (IsOurRefChanged)
                    {
                        if (IsBOMCase(args.Operation.Statement.PrimaryKey))
                        {
                            BOMCaseHandler.NotifyBOMOfNewCaseWorker(args.Operation.Statement.PrimaryKey, OurRef);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.EventHandler.EventHandlerUpdateBOMCaseWorker", "FuBOM", string.Format("Error updating caseworker in BOM ({0}):{1}\n{2}", args.Operation.Statement.PrimaryKey, ex.ToString(), args.Operation.Statement.ToString()), System.Diagnostics.EventLogEntryType.Error);
                }
            }
            else
                EventShimFactory.GetEventShim<CaseManagerEventShim>().AfterUpdate -= new SystemEventHandler<MetaOperationEventArgs>(this.Case_AfterUpdate);
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
}
