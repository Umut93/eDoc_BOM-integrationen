using Fujitsu.eDoc.BOM.CaseHandler;
using SI.Portal.BusinessIntegration;
using SI.Portal.BusinessIntegration.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Xml;

namespace Fujitsu.eDoc.BOM.CodeBehind
{
    public class BOMHandler : ICodeBehind
    {
        private static string CHANGED_FROM_TEXT = " ændret";
        private static string DOCUMENTATION_ADDED_TEXT = "Dokumentationskrav tilføjet - {0}, {1}, {2}";

        public object Invoke(ICodeBehindObjectCollection objectCollection, object data)
        {
            ICodeBehindObject codeBehindObject = objectCollection.GetByIndex(0);
            string methodToCall = codeBehindObject.CodeAction;
            string CodeParam = codeBehindObject.CodeParam;

            if (((IEntity)objectCollection.GetByIndex(0).DataObject).Fields.GetByName("ApplicationId").Value.ToString().Contains("[CNV]") && methodToCall == "TransferDataFromBOM")
            {
                methodToCall = "Conversion";
            }

            switch (methodToCall)
            {
                case "UpdateBOMCase":
                    return UpdateBOMCase(objectCollection, data, CodeParam);
                case "GetBOMApplicationFile":
                    return GetBOMApplicationFile(objectCollection, data, CodeParam);
                case "GetBOMCaseMetadata":
                    return GetBOMCaseMetadata(objectCollection, data, CodeParam);
                case "TransferDataFromBOM":
                    return TransferDataFromBOM(objectCollection, data, CodeParam);
                case "UpdateBOMServiceGoal":
                    return UpdateBOMServiceGoal(objectCollection, data, CodeParam);
                case "Conversion":
                    return TransferDataConvertedBOMCaseFromBOM(objectCollection);
            }
            return "";
        }

        private object UpdateBOMCase(ICodeBehindObjectCollection objectCollection, object data, string pCodeParam)
        {
            IEntity FuCase = (IEntity)objectCollection.GetByIndex(0).DataObject;
            IEntity FuBOMUpdateData = (IEntity)objectCollection.GetByIndex(1).DataObject;
            IList FuBOMAdditionalDocumentationList = (IList)objectCollection.GetByIndex(2).DataObject;
            IList FuBOMMainFileList = (IList)objectCollection.GetByIndex(3).DataObject;
            IList FuBOMAttachmentFilesList = (IList)objectCollection.GetByIndex(4).DataObject;
            IEntity FuCurrentUser = (IEntity)objectCollection.GetByIndex(5).DataObject;
            IEntity FuPDFStamperCheckBoxEntity = (IEntity)objectCollection.GetByIndex(6).DataObject;

            BOMCaseUpdateType c = new BOMCaseUpdateType();

            c.Initiator = new eDocUser();
            c.Initiator.ContactRecno = FuCurrentUser.Fields.GetByName("Recno").Value.ToString();
            c.Initiator.Email = FuCurrentUser.Fields.GetByName("Email").Value.ToString();

            c.CaseRecno = FuCase.Fields.GetByName("Recno").Value.ToString();
            c.ToBOMCase = FuCase.Fields.GetByName("ToBOMCase").Value.ToString();
            c.CaseType = FuCase.Fields.GetByName("CaseType").Value.ToString();
            c.CaseNumber = FuCase.Fields.GetByName("CaseNumber").Value.ToString();
            c.CaseTitle = FuCase.Fields.GetByName("CaseTitle").Value.ToString();
            c.InitiativeDuty = FuCase.Fields.GetByName("FUResponsibleAuthority").Value.ToString();

            c.BOMCaseId = FuCase.Fields.GetByName("BOMCaseId").Value.ToString();
            c.Title = FuBOMUpdateData.Fields.GetByName("Title").Value.ToString();
            c.Date = DateTime.Now;

            c.Status = new BOMCaseUpdateStatusType();
            c.Status.SagStatusKode = FuCase.Fields.GetByName("BOMCase.StatusCode").Value.ToString();
            c.Status.SagAndenMyndighedKode = FuCase.Fields.GetByName("BOMCase.OtherAuthorityCode").Value.ToString();
            c.Status.FaseKode = FuCase.Fields.GetByName("BOMCase.PhaseCode").Value.ToString();
            c.Status.InitiativPligtKode = FuCase.Fields.GetByName("BOMCase.InitiativeDuty").Value.ToString();
            c.Status.FristNotifikationProfilKode = FuCase.Fields.GetByName("BOMCase.DeadlineNotificationKode").Value.ToString();
            c.Status.FristDato = DateTime.MinValue;
            c.IsStamped = Int32.Parse(FuPDFStamperCheckBoxEntity.Fields.GetByName("IsChecked").Value.ToString());

            if (!string.IsNullOrEmpty(FuCase.Fields.GetByName("OurRef").Value.ToString()))
            {
                c.Ct_recno = Int32.Parse(FuCase.Fields.GetByName("OurRef").Value.ToString());
            }
            c.Ct_recno = Int32.Parse(c.Initiator.ContactRecno);

            c.OrgUnitRecno = Int32.Parse(FuCase.Fields.GetByName("ToOrgUnit.Recno").Value.ToString());

            if (FuCase.Fields.GetByName("BOMCase.Deadline").Value != null)
            {
                if (!string.IsNullOrEmpty(FuCase.Fields.GetByName("BOMCase.Deadline").Value.ToString()))
                {
                    c.Status.FristDato = DateTime.Parse(FuCase.Fields.GetByName("BOMCase.Deadline").Value.ToString());
                }
            }
            c.Status.StatusText = FuBOMUpdateData.Fields.GetByName("Kommentar").Value.ToString();

            c.Changes = new List<ChangeLog>();
            CheckFieldChanges(c.Changes, FuCase, "BOMCase.StatusDescription", "Status");
            CheckFieldChanges(c.Changes, FuCase, "BOMCase.OtherAuthorityDescription", "Anden myndighed");
            CheckFieldChanges(c.Changes, FuCase, "BOMCase.PhaseDescription", "Fase");
            CheckFieldChanges(c.Changes, FuCase, "BOMCase.InitiativeDuty", "Sagen afventer");
            CheckFieldChanges(c.Changes, FuCase, "BOMCase.DeadlineNotificationKode", "Påmindelser");
            CheckFieldChanges(c.Changes, FuCase, "BOMCase.Deadline", "Frist");

            if (FuBOMAdditionalDocumentationList.Data.Count > 0)
            {
                c.DocumentationRequirements = new List<BOMDocumentationRequirementType>();
                foreach (DataRow dr in FuBOMAdditionalDocumentationList.Data.Table.Rows)
                {
                    c.DocumentationRequirements.Add(new BOMDocumentationRequirementType()
                    {
                        Dokumentationstype = dr["DocumentationTypeCode"].ToString(),
                        Kravstyrke = dr["DocumentationStrengthCode"].ToString(),
                        FaseKode = dr["DocumentationPhaseCode"].ToString()
                    });
                    ChangeLog cl = new ChangeLog()
                    {
                        LogdataName = string.Format(DOCUMENTATION_ADDED_TEXT,
                        dr["DocumentationTypeDescription"].ToString(),
                        dr["DocumentationStrengthDescription"].ToString(),
                        dr["DocumentationPhaseDescription"].ToString())
                    };
                    c.Changes.Add(cl);
                }
            }
            if (FuBOMMainFileList.Data.Count > 0 || FuBOMAttachmentFilesList.Data.Count > 0)
            {
                if (FuBOMMainFileList.Data.Count > 0)
                {
                    DataRow dr = FuBOMMainFileList.Data.Table.Rows[0];
                    c.MainDocument = new BOMReplyDocument()
                    {
                        DocumentIdentifier = Guid.NewGuid().ToString(),
                        Title = dr["DisplayComment"].ToString(),
                        DocumentNumber = dr["DocumentNumber"].ToString(),
                        FileRecno = dr["ToFile"].ToString(),
                        FileVersionRecno = dr["ToFile.ToFileVersion"].ToString(),
                        DocumentRevisionRecno = dr["VersionID"].ToString(),
                        FileExtention = dr["ToFile.ToFileFormat.FileExtension"].ToString(),
                        FileMimeType = dr["ToFile.ToFileFormat.MimeType"].ToString(),
                    };
                }

                if (FuBOMAttachmentFilesList.Data.Count > 0)
                {
                    c.Attachments = new List<BOMReplyDocument>();
                    foreach (DataRow drr in FuBOMAttachmentFilesList.Data.Table.Rows)
                    {
                        c.Attachments.Add(new BOMReplyDocument()
                        {
                            DocumentIdentifier = Guid.NewGuid().ToString(),
                            Title = drr["DisplayComment"].ToString(),
                            DocumentNumber = drr["DocumentNumber"].ToString(),
                            FileRecno = drr["ToFile"].ToString(),
                            FileVersionRecno = drr["ToFile.ToFileVersion"].ToString(),
                            DocumentRevisionRecno = drr["VersionID"].ToString(),
                            FileExtention = drr["ToFile.ToFileFormat.FileExtension"].ToString(),
                            FileMimeType = drr["ToFile.ToFileFormat.MimeType"].ToString()
                        });
                    }
                }
                BOMQueueHandler.AddToQueue(c);
            }
            else
            {
                BOMQueueHandler.SaveQueuedItem(c, BOMQueueStatusType.Pending);
            }

            return "";
        }

        private object UpdateBOMServiceGoal(ICodeBehindObjectCollection objectCollection, object data, string pCodeParam)
        {
            IEntity FuCase = (IEntity)objectCollection.GetByIndex(0).DataObject;

            BOMCaseUpdateType c = new BOMCaseUpdateType();
            c.CaseRecno = FuCase.Fields.GetByName("Recno").Value.ToString();
            c.ToBOMCase = FuCase.Fields.GetByName("ToBOMCase").Value.ToString();
            c.CaseType = FuCase.Fields.GetByName("CaseType").Value.ToString();
            c.CaseNumber = FuCase.Fields.GetByName("CaseNumber").Value.ToString();
            c.CaseTitle = FuCase.Fields.GetByName("CaseTitle").Value.ToString();

            c.BOMCaseId = FuCase.Fields.GetByName("BOMCaseId").Value.ToString();
            c.SagServiceMaalKode = FuCase.Fields.GetByName("BOMCase.ServiceGoalCode").Value.ToString();
            c.VisitationForbrugtDageSpecified = int.Parse(FuCase.Fields.GetByName("VisitationSpentDaysSpecified").Value.ToString()) == 1 ? true : false;
            c.SagsbehandlingForbrugtDageSpecified = int.Parse(FuCase.Fields.GetByName("CaseManagementSpentDaysSpecified").Value.ToString()) == 1 ? true : false;

            c.Changes = new List<ChangeLog>();
            CheckFieldChanges(c.Changes, FuCase, "BOMCase.ServiceGoalCodeDescription", "Servicemål");

            BOMCaseHandler.UpdateServiceGoal(c);
            BOMQueueHandler.SaveQueuedItem(c, BOMQueueStatusType.Success);

            return "";
        }

        private object GetBOMApplicationFile(ICodeBehindObjectCollection objectCollection, object data, string pCodeParam)
        {
            IEntity FuScript = (IEntity)objectCollection.GetByIndex(0).DataObject;

            string ApplicationId = FuScript.Fields.GetByName("ApplicationId").Value.ToString();
            string LocalFilePath = BOMCaseHandler.DownloadApplicationFile(ApplicationId);
            LocalFilePath = LocalFilePath.Replace("file:", "").Replace("/", @"\");

            string fileGuid = Fujitsu.eDoc.Core.TempStorageManager.InvokeSave(LocalFilePath, new TimeSpan(1, 0, 0));

            FuScript.Fields.GetByName("FileGuid").Value = fileGuid;

            return "";
        }

        private object GetBOMCaseMetadata(ICodeBehindObjectCollection objectCollection, object data, string pCodeParam)
        {
            IEntity FuBOMCase = (IEntity)objectCollection.GetByIndex(0).DataObject;
            IEntity CaseEntityData = (IEntity)objectCollection.GetByIndex(1).DataObject;
            IEntity OurRef_InternalTempEntity = (IEntity)objectCollection.GetByIndex(2).DataObject;
            IList CaseContactListData = (IList)objectCollection.GetByIndex(3).DataObject;

            CaseEntityData.Fields.GetByName("Description").Value = FuBOMCase.Fields.GetByName("Title").Value.ToString();

            string BOMActivityType = FuBOMCase.Fields.GetByName("ActivityTypeCode").Value.ToString();

            BOMConfiguration cfg = BOMConfigHandler.GetBOMConfiguration();
            BOMConfiguration.BOMActivityTypeConfigurationItem activityType = cfg.GetActivityType(BOMActivityType);
            if (!string.IsNullOrEmpty(activityType.OrgUnit))
            {
                string orgunitName = GetContactName(activityType.OrgUnit);
                CaseEntityData.Fields.GetByName("ToOrgUnit.Recno").Value = activityType.OrgUnit;
                CaseEntityData.Fields.GetByName("ToOrgUnit.SubType").Value = "1";
                CaseEntityData.Fields.GetByName("ToOrgUnit.SearchName").Value = orgunitName;
                OurRef_InternalTempEntity.Fields.GetByName("OrgUnit.Recno").Value = activityType.OrgUnit;
                OurRef_InternalTempEntity.Fields.GetByName("Key").Value = orgunitName;
                OurRef_InternalTempEntity.Fields.GetByName("OrgUnit.SearchName").Value = orgunitName;
                OurRef_InternalTempEntity.Fields.GetByName("OurRef.Recno").Value = activityType.OrgUnit;
                OurRef_InternalTempEntity.Fields.GetByName("OurRef.SearchName").Value = orgunitName;
            }
            if (!string.IsNullOrEmpty(activityType.OurRef))
            {
                string ourrefName = GetContactName(activityType.OurRef);
                CaseEntityData.Fields.GetByName("OurRef.Recno").Value = activityType.OurRef;
                CaseEntityData.Fields.GetByName("OurRef.SubType").Value = "2";
                CaseEntityData.Fields.GetByName("OurRef.SearchName").Value = ourrefName;
                OurRef_InternalTempEntity.Fields.GetByName("OurRef.Recno").Value = activityType.OurRef;
                OurRef_InternalTempEntity.Fields.GetByName("Key").Value = ourrefName;
                OurRef_InternalTempEntity.Fields.GetByName("OurRef.SearchName").Value = ourrefName;
            }
            CaseEntityData.Fields.GetByName("ToCaseCategory").Value = activityType.ToCaseCategory;
            CaseEntityData.Fields.GetByName("SelectedLapseRecno").Value = activityType.ToProgressPlan;

            string ApplicantsXML = FuBOMCase.Fields.GetByName("ApplicantsXML").Value.ToString();
            if (!string.IsNullOrEmpty(ApplicantsXML))
            {
                List<Contact> applicants = Fujitsu.eDoc.BOM.Serialization.Deserialize<List<Contact>>(ApplicantsXML);
                foreach (Contact applicant in applicants)
                {
                    DataRow dr = CaseContactListData.Data.Table.NewRow();
                    dr["Name"] = applicant.NavnTekst;
                    dr["Email"] = applicant.EmailTekst;
                    dr["Phone"] = applicant.TelefonTekst;
                    dr["Address"] = applicant.Address;
                    dr["ZipCode"] = applicant.PostCodeIdentifier;
                    dr["ZipPlace"] = applicant.DistrictName;
                    dr["Country"] = applicant.CountryIdentificationCode;
                    dr["Role.Recno"] = BOMCaseHandler.CASE_CONTACT_ROLE_APPLICANT;
                    dr["Role.Code"] = BOMCaseHandler.CASE_CONTACT_ROLE_APPLICANT_NAME;
                    dr["ToDomain"] = "0";
                    CaseContactListData.Data.Table.Rows.Add(dr);
                }
            }
            return "";

        }

        private string GetContactName(string Recno)
        {
            string xmlQuery = string.Format(@"<operation>
                                  <QUERYDESC NAMESPACE='SIRIUS' ENTITY='Contact' DATASETFORMAT='XML'>
                                    <RESULTFIELDS>
                                      <METAITEM TAG='SearchName'>SearchName</METAITEM>
                                    </RESULTFIELDS>
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
            if (doc.SelectNodes("/RECORDS/RECORD/SearchName").Count > 0)
            {
                return doc.SelectSingleNode("/RECORDS/RECORD/SearchName").InnerText;
            }
            return "";
        }

        private object TransferDataFromBOM(ICodeBehindObjectCollection objectCollection, object data, string pCodeParam)
        {
            IEntity FuBOMCase = (IEntity)objectCollection.GetByIndex(0).DataObject;
            IEntity CaseEntityData = (IEntity)objectCollection.GetByIndex(1).DataObject;
            string BOMCaseRecno = FuBOMCase.Key;
            string BOMApplicaitonId = String.Empty;
            // Search submissions with BOM CAseRecno oder by insertdate

            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("SelectAllExistingCasesInSubmission.xml", "Fujitsu.eDoc.BOM.CodeBehind.XML", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#BOMCaseNumber#", BOMCaseRecno);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNodeList nl = doc.SelectNodes("/RECORDS/RECORD");
            string eDocCaseRecno = CaseEntityData.Key;

            foreach (XmlNode nd in nl)
            {

                BOMApplicaitonId = nd.SelectSingleNode("ApplicationId").InnerText;
                BOMCaseHandler.TransferDataForBOMApplication(BOMApplicaitonId, BOMCaseRecno, eDocCaseRecno, false);

            }

            return "";
        }

        private object TransferDataConvertedBOMCaseFromBOM(ICodeBehindObjectCollection objectCollection)
        {
            IEntity FuBOMCase = (IEntity)objectCollection.GetByIndex(0).DataObject;
            IEntity CaseEntityData = (IEntity)objectCollection.GetByIndex(1).DataObject;
            string BOMCaseRecno = FuBOMCase.Key;
            string BOMApplicaitonId = String.Empty;

            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("SelectAllExistingConvertedCasesInSubmission.xml", "Fujitsu.eDoc.BOM.CodeBehind.XML", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#BOMCaseNumber#", BOMCaseRecno);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNodeList nl = doc.SelectNodes("/RECORDS/RECORD");
            string eDocCaseRecno = CaseEntityData.Key;

            foreach (XmlNode nd in nl)
            {
                BOMApplicaitonId = nd.SelectSingleNode("ApplicationId").InnerText;
                BOMCaseHandler.TransferDataForBOMApplication(BOMApplicaitonId, BOMCaseRecno, eDocCaseRecno, true);
            }



            //Update the case to the latest status 
            xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateConvertedBOMCase.xml", "Fujitsu.eDoc.BOM.CodeBehind.XML", Assembly.GetExecutingAssembly());
            doc = new XmlDocument();
            doc.LoadXml(xmlQuery);
            XmlNode nUpdatestatement = doc.SelectSingleNode("/operation/UPDATESTATEMENT");
            nUpdatestatement.Attributes["PRIMARYKEYVALUE"].Value = BOMCaseRecno;

            int recordCount = nl.Count;

            string lastSubmittedApplicationid = nl.Item(recordCount - 1).SelectSingleNode("ApplicationId").InnerText;

            FuIndsendelseType BOMApplication = BOMCaller.GetApplication(lastSubmittedApplicationid);

            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='ApplicationId']/VALUE").InnerText = FuBOMCase.Fields.GetByName("ApplicationId").Value.ToString().Replace("[CNV]", "");
            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='StatusCode']/VALUE").InnerText = BOMApplication.IndsendelseType.BOMSag.SagStatus.SagStatusKode;
            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='OtherAuthorityCode']/VALUE").InnerText = BOMApplication.IndsendelseType.BOMSag.SagStatus.SagAndenMyndighedKode;
            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='PhaseCode']/VALUE").InnerText = BOMApplication.IndsendelseType.BOMSag.SagStatus.FaseKode;
            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='InitiativeDuty']/VALUE").InnerText = BOMApplication.IndsendelseType.BOMSag.SagStatus.InitiativPligt.ToString();
            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='DeadlineNotificationKode']/VALUE").InnerText = BOMApplication.IndsendelseType.BOMSag.SagStatus.FristNotifikationProfilKode;
            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='Deadline']/VALUE").InnerText = BOMApplication.IndsendelseType.BOMSag.SagStatus.FristDato > DateTime.MinValue ? BOMApplication.IndsendelseType.BOMSag.SagStatus.FristDato.ToString() : "";
            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='LastActivity']/VALUE").InnerText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            Fujitsu.eDoc.Core.Common.ExecuteSingleAction(doc.OuterXml);

            return "";
        }



        private void CheckFieldChanges(List<Fujitsu.eDoc.BOM.ChangeLog> Changes, IEntity Entity, string FieldName, string PropertyName)
        {
            string fieldValue = "";
            if (Entity.Fields.GetByName(FieldName).Value != null)
            {
                fieldValue = Entity.Fields.GetByName(FieldName).Value.ToString();
            }

            string fieldValueOriginal = Entity.Fields.GetByName(FieldName + ".Original").Value.ToString();
            if (Entity.Fields.GetByName(FieldName + ".Original").Value != null)
            {
                fieldValueOriginal = Entity.Fields.GetByName(FieldName + ".Original").Value.ToString();
            }


            if (Entity.Fields.GetByName(FieldName).DataType.Name == "DateTime")
            {
                fieldValue = DateFromString(fieldValue);
                fieldValueOriginal = DateFromString(fieldValueOriginal);
            }

            if (fieldValue != fieldValueOriginal)
            {
                ChangeLog cl = new ChangeLog()
                {
                    LogdataName = PropertyName + CHANGED_FROM_TEXT,
                    LogdataFrom = fieldValueOriginal,
                    LogdataTo = fieldValue
                };
                Changes.Add(cl);
            }
        }

        private string DateFromString(string DateString)
        {
            DateTime d;
            if (DateTime.TryParse(DateString, out d))
            {
                return d.ToShortDateString();
            }
            return "";
        }
    }
}
