using Fujitsu.eDoc.BOM.BOMSagsbehandling;
using Fujitsu.eDoc.BOM.CaseHandler;
using Fujitsu.eDoc.BOM.Integrations;
using Fujitsu.eDoc.Core;
using Fujitsu.eDoc.Integrations.Datafordeler.VUR;
using SI.Biz.Core;
using SI.Biz.Core.SchemaBasedImport.NoarkXml;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security.Tokens;
using System.Xml;
using static Fujitsu.eDoc.BOM.CaseHandler.BOMUtils;

namespace Fujitsu.eDoc.BOM
{
    public class BOMCaseHandler
    {
        public static string CASE_CONTACT_ROLE_APPLICANT = "50011";
        public static string CASE_CONTACT_ROLE_APPLICANT_NAME = "Ansøger";
        public static string UPDATED_CASEWORKER = "Opdateret sagsbehandler";
        public static string UPDATED_CASESTATUS = "Opdateret sagsstatus";
        public static string DEFAULT_SAG_SERVICE_MAAL_KODE = "Default";

        public static void HandleBOMApplication(FuIndsendelseType BOMApplication)
        {
            BOMConfiguration cfg = BOMConfigHandler.GetBOMConfiguration();
            HandleBOMApplication(cfg, BOMApplication);
        }

        private static bool BOMCaseExists(BOMCase c)
        {
            string bOMSagID = c.BOMSagID;
            bool isBomCaseExisting = false;
            //Søgniong i BOMCAse med BOMNummer

            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("CheckIfBOMsagExist.xml", "Fujitsu.eDoc.BOM.XML.FuBOMValidation", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#CaseId#", bOMSagID);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNode n = doc.SelectSingleNode("/RECORDS/RECORD/Recno");
            if (n == null)
            {
                isBomCaseExisting = true;
            }

            if (!isBomCaseExisting)
            {
                string BOMCaseRecno = n.InnerText;
                c.BOMCaseRecno = BOMCaseRecno;
                string BOMSubmissionRecno = c.BOMSubmissionRecno;
                //Update bomsubmission set BOMCaserecno = BOMCaseRecno
                // string uptstmt = "<oprer<tion><UPDATESTATEMENT NAME='FuBOMSubmission' PRIMARYKEYVALUE='BOMSubmissionRecno'><METAIEM NAME='FuBOMCase'>";
                xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateBOMCaseNumber.xml", "Fujitsu.eDoc.BOM.XML.FuBOMValidation", Assembly.GetExecutingAssembly());
                xmlQuery = xmlQuery.Replace("#BOMSubmissionRecno#", BOMSubmissionRecno).Replace("#FuBOMCaseRecno#", BOMCaseRecno);
                Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
            }

            return isBomCaseExisting;
        }

        public static void HandleBOMApplication(BOMConfiguration configuration, FuIndsendelseType BOMApplication)
        {
            BOMCase c = null;
            try
            {
                c = GetBOMCaseData(configuration, BOMApplication);

                if (HandlingHasExpired(configuration, c))
                {
                    return;
                }
                bool eDocCasePreviouslyCreated = FindExistingCase(c);

                if (!eDocCasePreviouslyCreated)
                {
                    if (IsOldBOMCase(c))
                    {
                        UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.Completed);
                        return;
                    }

                    if (BOMCaseExists(c) == true)
                    {
                        if (c.BOMSubmissionTransferStatus != BOMCaseTransferStatusEnum.BOMCaseCreated)
                        {
                            RegisterBOMCase(c);
                            UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.BOMCaseCreated);
                        }
                    }

                    BOMConfiguration.BOMActivityTypeConfigurationItem activityType = c.configuration.GetActivityType(c.AktivitetTypeKode);
                    c.OrgUnitRecno = activityType.OrgUnit;
                    c.OurRefRecno = activityType.OurRef;
                    c.ToCaseCategory = activityType.ToCaseCategory;
                    c.ToProgressPlan = activityType.ToProgressPlan;
                    c.ToCaseType = activityType.ToCaseType;
                    c.AccessCode = c.configuration.GetCaseAccessCode();
                    c.AccessGroup = c.configuration.GetCaseAccessGroup();

                    if (c.CanCreateEDocCase())
                    {
                        GetEstate(c);
                        if (!string.IsNullOrEmpty(c.EstateRecno))
                        {
                            c.SubArchiveRecno = GetSubArchiveRecno(c.KLEmnenr.Substring(0, 2));
                            c.DiscardCodeRecno = GetDiscardRecno(c.KLKassation);
                            CreateEdocCase(c);

                            UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.eDocCaseCreated);
                        }
                        else
                        {
                            UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.Completed);
                            return;
                        }
                    }
                    else
                    {
                        SendEmailWhenBomSagsStayInListForCreation(configuration, c);
                        UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.Completed);
                        return;
                    }
                }
                else
                {
                    if (c.BOMSubmissionTransferStatus == BOMCaseTransferStatusEnum.Pending || c.BOMSubmissionTransferStatus == BOMCaseTransferStatusEnum.Processing)
                    {
                        UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.eDocCaseCreated);
                    }
                }

                if (c.BOMSubmissionTransferStatus == BOMCaseTransferStatusEnum.eDocCaseCreated)
                {
                    try
                    {
                        CreateDocumentOnCase(c, c.IndsendelsesDokument, c.KonfliktRapportDokument, c.Bilag);
                    }
                    catch (Exception ex)
                    {

                        Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMCaseHandler", "FuBOM",
                                  $"Error on adding a document for the case recno {c.CaseRecno}:\n {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);
                    }


                }

                if (c.BOMSubmissionTransferStatus == BOMCaseTransferStatusEnum.AttachmentsTransfered)
                {
                    if (!string.IsNullOrEmpty(c.BOMExternCaseBrugerVendtNoegle))
                    {
                        SendBOMCaseReceivedMessageForExisting(c);
                    }
                    else
                    {
                        SendBOMCaseReceivedMessage(c);
                    }
                    UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.BOMReceivedMessageSent);
                }

                if (c.BOMSubmissionTransferStatus == BOMCaseTransferStatusEnum.BOMReceivedMessageSent)
                {
                    UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.Completed);
                }

                if (c.BOMSubmissionTransferStatus == BOMCaseTransferStatusEnum.Completed)
                {
                    foreach (BOMDocument bilag in c.Bilag)
                    {
                        if (!bilag.IsFileTypeValid)
                        {
                            SendEmailWithInfoAboutInvalidAttachments(configuration, c);
                            UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.CompletedButInvalidFiles);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RegisterBOMSubmissionError(c, ex);
                throw ex;
            }
        }

        private static void CreateMessageHistoryOnCase(string titelTekst, DateTime brevDato, string fileURL, string afsender, string caseRecno)
        {
            try
            {
                string xmlQuery = $@"<operation>
                <INSERTSTATEMENT NAMESPACE='SIRIUS' ENTITY='FuBOMMessageHistory'>
                    <METAITEM NAME='Correspondence'>
                    <VALUE>{titelTekst}</VALUE>
                    </METAITEM>
                    <METAITEM NAME='Date'>
                    <VALUE>{brevDato.ToString("yyyy-MM-dd HH:mm:ss.fff")}</VALUE>
                   </METAITEM>
                  <METAITEM NAME='FileURL'>
                    <VALUE>{fileURL}</VALUE>
                  </METAITEM>
                  <METAITEM NAME='Sender'>
                    <VALUE>{afsender}</VALUE>
                  </METAITEM>
                  <METAITEM NAME='ToBomCase'>
                    <VALUE>{caseRecno}</VALUE>
                  </METAITEM>
                </INSERTSTATEMENT>
              </operation>";

                string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
            }

            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMCaseHandler", "FuBOM",
                   $"Error on inserting on table FuBOMMessageHistory for the case recno {caseRecno}:\n {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);

            }
        }


        private static int EnsureBFENumber(BOMCase c, BOMConfiguration configuration)
        {
            try
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMCaseHandler", "FuBOM", $"EnsureBFENumber start", System.Diagnostics.EventLogEntryType.Information);
                if (c.edocBFENumber > 0)
                {
                    return c.edocBFENumber;
                }

                if (string.IsNullOrEmpty(c.BOMCaseRecno))
                {
                    throw new Exception($"BOMCaseRecno is empty or null for BomSubmissionNummer: {c.BomSubmissionNummer}");
                }

                int bfeNumber = BFENumberHandler.GetBFENumber(c, configuration);
                if (bfeNumber <= 0)
                {
                    throw new Exception($"BFENmber is {bfeNumber} with Ejendomsnummer: {c.EjendomKommunenr} and kommunenummer: {c.EjendomKommunenr}");
                }

                AddBFENumberToBOMCase(c.BOMCaseRecno, bfeNumber);

                return bfeNumber;
            }
            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMCaseHandler", "FuBOM",
                    $"Error ensuring BFENumber on BOM Case: {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);

                return 0;
            }
        }

        private static void AddBFENumberToBOMCase(string bomCaseRecno, int bfeNumber)
        {
            string xmlUpdate = Fujitsu.eDoc.Core.Common.GetResourceXml("AddBFENumberToBOMCase.xml", "Fujitsu.eDoc.BOM.XML.BOMCase", Assembly.GetExecutingAssembly());
            xmlUpdate = xmlUpdate.Replace("#BOMCaseRecno#", bomCaseRecno).Replace("#BFENumber#", bfeNumber.ToString());
            Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlUpdate);

            Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMCaseHandler", "FuBOM", $"EnsureBFENumber: BFEnumber added to bomCaseRecno: {bomCaseRecno}, BFENumber: {bfeNumber}", System.Diagnostics.EventLogEntryType.Information);
        }

        private static void GetEstate(BOMCase c)
        {
            try
            {
                if (c.configuration.GetEstateRelationType() == BOMConfiguration.ESTATE_RELATION_LANDPARCEL)
                {
                    c.EstateRecno = Fujitsu.ExternRegister.Estate.GetOrCreateLandParcelFromExtern("", c.Ejendomsnummer, c.MatrikelNummer, c.MunicipalityCode, c.MatrikelEjerlavId, "", "");
                }
                else
                {
                    c.EstateRecno = Fujitsu.ExternRegister.Estate.GetOrCreateEstateFromLandParcelFromExtern("", c.Ejendomsnummer, c.MatrikelNummer, c.MunicipalityCode, c.MatrikelEjerlavId, "", "");
                }
            }
            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMCaseHandler", "FuBOM",
                    string.Format("Error looking up estate ({0}, {1}, {2}):\n{3}", c.MatrikelNummer, c.MunicipalityCode, c.MatrikelEjerlavId, ex.ToString()), System.Diagnostics.EventLogEntryType.Error);

                c.EstateRecno = string.Empty;
            }
        }

        private static void SendEmailWithInfoAboutInvalidAttachments(BOMConfiguration configuration, BOMCase c)
        {
            try
            {
                string email = configuration.GetEmailAdresses(c.SagsOmraadeKode);
                bool sendEmail = !string.IsNullOrEmpty(email);
                if (sendEmail)
                {
                    string DocumentList = "";

                    foreach (BOMDocument bilag in c.Bilag)
                    {
                        if (!bilag.IsFileTypeValid)
                        {
                            DocumentList = string.Format("{0}{1}<br />", DocumentList, bilag.BrugervendtNoegleTekst);
                        }
                    }

                    string EmailMessageRecno = "300122";
                    string parameters = string.Format("To={0};BOMCaseNumber={1};eDocCaseNumber={2};DocumentList={3}", email, c.BomNummer, c.CaseNumber, DocumentList);

                    Fujitsu.eDoc.Notification.EmailNotificationManager.InvokeSendEmail(EmailMessageRecno.ToString(), "DAN", parameters, "", "");

                }
            }
            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMCaseHandler.SendEmailWithInfoAboutInvalidAttachments", "FuBOM",
                    string.Format("Error sending email With Info About Invalid Attachments:\n{0}", ex.ToString()), System.Diagnostics.EventLogEntryType.Error);
            }
        }

        private static void SendEmailWhenBomSagsStayInListForCreation(BOMConfiguration configuration, BOMCase c)
        {
            try
            {
                string email = configuration.GetEmailAdressesForCreate(c.SagsOmraadeKode);
                bool sendEmail = !string.IsNullOrEmpty(email);
                if (sendEmail)
                {
                    string EmailMessageRecno = "300244";
                    string WebAppUrl = Fujitsu.eDoc.Core.Url.GeteDocWebsiteUrl();
                    string parameters = string.Format("To={0};BOMCaseNumber={1};WebAppUrl={2}", email, c.BomNummer, WebAppUrl);
                    if (email != string.Empty)
                    {
                        Fujitsu.eDoc.Notification.EmailNotificationManager.InvokeSendEmail(EmailMessageRecno.ToString(), "DAN", parameters, "", "");
                    }
                }
            }
            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMCaseHandler.SendEmailWhenBomSagStaysInListForCreation", "FuBOM",
                    string.Format("Error Sending email When Bom Sager Stay In List For Creation:\n{0}", ex.ToString()), System.Diagnostics.EventLogEntryType.Error);
            }
        }

        private static bool HandlingHasExpired(BOMConfiguration configuration, BOMCase c)
        {
            bool hasExpired = c.BOMSubmissionCreated.AddMinutes(configuration.GetMaxHandlingTimeInMinutes()) < DateTime.Now;

            if (hasExpired)
            {
                BOMCaseTransferStatusEnum failedStep = c.BOMSubmissionTransferStatus;
                UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.Failed);

                // Send email
                try
                {
                    string email = configuration.GetEmailAdresses(c.SagsOmraadeKode);
                    bool sendEmail = !string.IsNullOrEmpty(email) && c.BOMSubmissionCreated.AddDays(1) > DateTime.Now;
                    if (sendEmail)
                    {
                        string FailedStep = "";
                        switch (failedStep)
                        {
                            case BOMCaseTransferStatusEnum.Pending:
                            case BOMCaseTransferStatusEnum.Processing:
                                FailedStep = "BOM indsendelsen kunne ikke registreres i eDoc";
                                break;
                            case BOMCaseTransferStatusEnum.BOMCaseCreated:
                                FailedStep = "Der kan ikke oprettes en sag i eDoc";
                                break;
                            case BOMCaseTransferStatusEnum.eDocCaseCreated:
                                FailedStep = "Ansøgnings PDF kan ikke overføres til eDoc";
                                break;
                            case BOMCaseTransferStatusEnum.ApplicationDocumentTransfered:
                                FailedStep = "Konfliksrapport PDF kan ikke overføres til eDoc";
                                break;
                            case BOMCaseTransferStatusEnum.ConflictDocumentTransfered:
                                FailedStep = "Bilag kan ikke overføres til eDoc";
                                break;
                            case BOMCaseTransferStatusEnum.AttachmentsTransfered:
                                FailedStep = "Der kan ikke sendes kvitterring til BOM";
                                break;
                            case BOMCaseTransferStatusEnum.BOMReceivedMessageSent:
                                FailedStep = "Indsendelsen kan ikke færdig behandles";
                                break;
                        }
                        string EmailMessageRecno = "300091";
                        string parameters = string.Format("To={0};BOMCaseNumber={1};FailedStep={2};ErrorMessage={3}", email, c.BomNummer, FailedStep, c.BOMSubmissionErrMsg);

                        Fujitsu.eDoc.Notification.EmailNotificationManager.InvokeSendEmail(EmailMessageRecno.ToString(), "DAN", parameters, "", "");


                    }
                }
                catch (Exception ex)
                {
                    Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMCaseHandler", "FuBOM",
                        string.Format("Error sending email on failure:\n{0}", ex.ToString()), System.Diagnostics.EventLogEntryType.Error);
                }
            }

            return hasExpired;
        }


        private static bool IsOldBOMCase(BOMCase c)
        {
            // BOM case already has a external case number
            if (!string.IsNullOrEmpty(c.BOMExternCaseUniqueIdentifier) || !string.IsNullOrEmpty(c.BOMExternCaseBrugerVendtNoegle))
            {
                return true;
            }

            if (c.FoersteIndsendelseDatoTid < c.configuration.GetStartDateTime())
            {
                return true;
            }
            return false;
        }

        public static void TransferDataForBOMApplication(string BOMApplicationId, string BOMCaseRecno, string eDocCaseRecno, bool isConvertedBOMcase)
        {
            FuIndsendelseType BOMApplication = BOMCaller.GetApplication(BOMApplicationId);
            BOMConfiguration cfg = BOMConfigHandler.GetBOMConfiguration();
            BOMCase c = GetBOMCaseData(cfg, BOMApplication);

            c.CaseRecno = eDocCaseRecno;
            c.BOMCaseRecno = BOMCaseRecno;
            PostCreateEdocCase(c);

            try
            {
                CreateDocumentOnCase(c, c.IndsendelsesDokument, c.KonfliktRapportDokument, c.Bilag);
            }
            catch (Exception ex)
            {

                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMCaseHandler", "FuBOM", ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
            }

            //Dont make an reply to a converion case
            if (isConvertedBOMcase != true)
            {
                SendBOMCaseReceivedMessage(c);
            }

            UpdateBOMCaseTransferStatus(c, BOMCaseTransferStatusEnum.Completed);



        }



        public static void UpdateBOMCase(BOMCaseUpdateType c)
        {
            BOMSagsbehandling.BesvarelseType reply = new BOMSagsbehandling.BesvarelseType();
            reply.BesvarelseID = Guid.NewGuid().ToString();
            reply.BOMSagID = c.BOMCaseId;
            reply.TitelTekst = c.Title;
            reply.BrevDato = c.Date;

            reply.Afsender = new BOMSagsbehandling.BesvarelseTypeAfsender();
            reply.Afsender.MyndighedCvrNummer = GetMunicipalityCVR();

            if (c.Status != null)
            {
                reply.SagStatusOpdatering = new BOMSagsbehandling.BesvarelseTypeSagStatusOpdatering();
                reply.SagStatusOpdatering.SagStatus = new BOMSagsbehandling.SagStatusType();
                reply.SagStatusOpdatering.SagStatus.StatusTidspunkt = DateTime.Now;
                reply.SagStatusOpdatering.SagStatus.SagStatusKode = c.Status.SagStatusKode;
                reply.SagStatusOpdatering.SagStatus.SagAndenMyndighedKode = string.IsNullOrEmpty(c.Status.SagAndenMyndighedKode) ? null : c.Status.SagAndenMyndighedKode;
                switch (c.Status.InitiativPligtKode)
                {
                    case "Myndighed":
                        reply.SagStatusOpdatering.SagStatus.InitiativPligt = BOMSagsbehandling.SagStatusTypeInitiativPligt.Myndighed;
                        break;
                    case "Ansøger":
                        reply.SagStatusOpdatering.SagStatus.InitiativPligt = BOMSagsbehandling.SagStatusTypeInitiativPligt.Ansoeger;
                        break;
                    case "Ingen":
                        reply.SagStatusOpdatering.SagStatus.InitiativPligt = BOMSagsbehandling.SagStatusTypeInitiativPligt.Ingen;
                        break;
                    default:
                        reply.SagStatusOpdatering.SagStatus.InitiativPligt = BOMSagsbehandling.SagStatusTypeInitiativPligt.Ingen;
                        break;
                }

                reply.SagStatusOpdatering.SagStatus.FaseKode = c.Status.FaseKode;
                if (!string.IsNullOrEmpty(c.Status.FristNotifikationProfilKode))
                {
                    reply.SagStatusOpdatering.SagStatus.FristNotifikationProfilKode = c.Status.FristNotifikationProfilKode;
                }
                else
                {
                    reply.SagStatusOpdatering.SagStatus.FristNotifikationProfilKode = null;
                }
                reply.SagStatusOpdatering.SagStatus.StatusTekst = c.Status.StatusText;

                if (c.Status.FristDato > DateTime.MinValue)
                {
                    reply.SagStatusOpdatering.SagStatus.FristDato = c.Status.FristDato;
                    reply.SagStatusOpdatering.SagStatus.FristDatoSpecified = true;
                }
            }

            if (c.DocumentationRequirements != null && c.DocumentationRequirements.Any())
            {
                reply.DokumentationKravOpdatering = new BOMSagsbehandling.DokumentationKravOpdateringType[c.DocumentationRequirements.Count];
                for (int i = 0; i < c.DocumentationRequirements.Count; i++)
                {
                    reply.DokumentationKravOpdatering[i] = new BOMSagsbehandling.DokumentationKravOpdateringType();
                    reply.DokumentationKravOpdatering[i].DokumentationTypeKode = c.DocumentationRequirements[i].Dokumentationstype;
                    switch (c.DocumentationRequirements[i].Kravstyrke)
                    {
                        case "Frivilligt":
                            reply.DokumentationKravOpdatering[i].KravStyrkeKode = BOMSagsbehandling.KravStyrkeKodeType.Frivilligt;
                            break;
                        case "Obligatorisk":
                            reply.DokumentationKravOpdatering[i].KravStyrkeKode = BOMSagsbehandling.KravStyrkeKodeType.Obligatorisk;
                            break;
                        case "IkkeKrav":
                        default:
                            reply.DokumentationKravOpdatering[i].KravStyrkeKode = BOMSagsbehandling.KravStyrkeKodeType.IkkeKrav;
                            break;
                    }
                    reply.DokumentationKravOpdatering[i].FaseKode = c.DocumentationRequirements[i].FaseKode;
                }
            }

            if ((c.MainDocument != null) || (c.Attachments != null && c.Attachments.Any()))
            {
                reply.Skrivelse = new BOMSagsbehandling.BesvarelseTypeSkrivelse();

                if (c.MainDocument != null)
                {
                    reply.Skrivelse.HovedDokument = PrepareReplyDocument(c.BOMCaseId, c.MainDocument);
                }

                if (c.Attachments != null && c.Attachments.Count > 0)
                {
                    if (c.IsStamped == -1)
                    {
                        PDFStamper.StampAttahcments(c);
                    }

                    reply.Skrivelse.UnderDokument = new BOMSagsbehandling.DokumentType[c.Attachments.Count];
                    for (int i = 0; i < c.Attachments.Count; i++)
                    {
                        reply.Skrivelse.UnderDokument[i] = PrepareReplyDocument(c.BOMCaseId, c.Attachments[i]);
                    }
                }
            }

            BOMCaller.Reply(reply);
            string url = BOMCaller.GetBOMServer() + "ansoegningbesvarelse/" + reply.BesvarelseID;
            url = url.Replace("service", "dokument");
            CreateMessageHistoryOnCase(reply.TitelTekst, reply.BrevDato, url, c.InitiativeDuty, c.CaseRecno);

            EntityLogHelper.LogChanges(c);
            UpdateEdocCase(c);
        }

        public static void UpdateServiceGoal(BOMCaseUpdateType c)
        {
            BOMSagsbehandling.OpdaterType opdater = new BOMSagsbehandling.OpdaterType();
            opdater.BOMSagID = c.BOMCaseId;
            opdater.SagServiceMaalKode = c.SagServiceMaalKode == DEFAULT_SAG_SERVICE_MAAL_KODE ? "" : c.SagServiceMaalKode;

            BOMCaller.UpdateServiceGoal(opdater);

            EntityLogHelper.LogChanges(c);
            UpdateEdocCaseWithServiceGoal(c);
            UpdateServiceGoalStatus(c);
        }

        private static void UpdateServiceGoalStatus(BOMCaseUpdateType c)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateBOMServiceGoalStatus.xml", "Fujitsu.eDoc.BOM.XML.BOMCase", Assembly.GetExecutingAssembly());
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlQuery);
            XmlNode nUpdatestatement = doc.SelectSingleNode("/operation/UPDATESTATEMENT");
            nUpdatestatement.Attributes["PRIMARYKEYVALUE"].Value = c.ToBOMCase;

            //Any excempts
            if (c.SagServiceMaalKode != DEFAULT_SAG_SERVICE_MAAL_KODE)
            {
                nUpdatestatement.SelectSingleNode("METAITEM[@NAME='ServiceMaalDays']/VALUE").InnerText = "-";
                nUpdatestatement.SelectSingleNode("METAITEM[@NAME='ServiceMaalStatus']/VALUE").InnerText = ServiceGoalStatus.Exempted;
            }
            else
            {
                nUpdatestatement.SelectSingleNode("METAITEM[@NAME='ServiceMaalDays']/VALUE").InnerText = "";
                nUpdatestatement.SelectSingleNode("METAITEM[@NAME='ServiceMaalStatus']/VALUE").InnerText = ServiceGoalStatus.None;
            }

            //Waiting for statics
            if (!c.SagsbehandlingForbrugtDageSpecified && !c.VisitationForbrugtDageSpecified && c.SagServiceMaalKode == DEFAULT_SAG_SERVICE_MAAL_KODE)
            {
                nUpdatestatement.SelectSingleNode("METAITEM[@NAME='ServiceMaalStatus']/VALUE").InnerText = ServiceGoalStatus.PendingCalculation;
            }

            Fujitsu.eDoc.Core.Common.ExecuteSingleAction(doc.OuterXml);
        }

        public static BOMSagsbehandling.DokumentType PrepareReplyDocument(string BOMCaseId, BOMReplyDocument doc)
        {
            doc.Url = UploadReplyFile(BOMCaseId, doc.DocumentIdentifier, doc.FileFullname);

            BOMSagsbehandling.DokumentType d = new BOMSagsbehandling.DokumentType()
            {
                DokumentEgenskaber = new BOMSagsbehandling.DokumentEgenskaberType()
                {
                    BeskrivelseTekst = doc.Title,
                    BrevDato = DateTime.Now,
                    BrugervendtNoegleTekst = doc.DocumentNumber,
                    TitelTekst = doc.Title
                },
                DokumentID = doc.DocumentIdentifier,
                VariantListe = new BOMSagsbehandling.VariantType[1] {
                    new BOMSagsbehandling.VariantType() {
                        VariantTekst = doc.FileExtention,
                        Del = new BOMSagsbehandling.DelType[1] {
                            new BOMSagsbehandling.DelType() {
                                DelTekst = "1",
                                IndeksIdentifikator = "1",
                                MimeTypeTekst = doc.FileMimeType,
                                IndholdTekst = doc.Url
                            }
                        }
                    }
                }
            };

            return d;
        }

        public static DateTime GetLatestBOMSubmisstionTime()
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetLatestBOMSubmissionMetaQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMSubmission", Assembly.GetExecutingAssembly());
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNode nSubmissionTime = doc.SelectSingleNode("/RECORDS/RECORD/SubmissionTime");
            if (nSubmissionTime != null)
            {
                DateTime SubmissionTime = DateTime.Parse(nSubmissionTime.InnerText);
                return SubmissionTime;
            }

            return DateTime.MinValue;
        }

        public static bool BOMSubmisstionExists(string IndsendelseID)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMSubmissionMetaQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMSubmission", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#ApplicationId#", IndsendelseID);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNodeList nl = doc.SelectNodes("/RECORDS/RECORD");
            return (nl.Count > 0);
        }
        public static bool BOMSubmisstionHandled(string IndsendelseID)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMSubmissionHandledMetaQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMSubmission", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#ApplicationId#", IndsendelseID);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNodeList nl = doc.SelectNodes("/RECORDS/RECORD");
            return (nl.Count > 0);
        }
        public static string[] BOMSubmisstionInterrupted()
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMSubmissionInterruptedMetaQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMSubmission", Assembly.GetExecutingAssembly());
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNodeList nl = doc.SelectNodes("/RECORDS/RECORD");

            string[] interrupted = new string[nl.Count];
            for (int i = 0; i < nl.Count; i++)
            {
                XmlNode n = nl[i];
                interrupted[i] = n.SelectSingleNode("ApplicationId").InnerText;
            }
            return interrupted;
        }
        private static BOMCase GetBOMCaseData(BOMConfiguration configuration, FuIndsendelseType BOMApplication)
        {
            BOMCase c = new BOMCase();
            c.configuration = configuration;
            RegisterBOMSubmission(BOMApplication.IndsendelseType, c);


            // 
            c.IndsendelseID = BOMApplication.IndsendelseType.IndsendelseID;
            c.IndsendelseDatoTid = BOMApplication.IndsendelseType.IndsendelseDatoTid;
            c.BomSubmissionNummer = BOMApplication.IndsendelseType.IndsendelseLoebenr;
            c.BOMSagID = BOMApplication.IndsendelseType.BOMSag.BOMSagID;
            c.BomNummer = BOMApplication.IndsendelseType.BOMSag.BomNummer;
            c.BOMTitle = BOMApplication.IndsendelseType.IndsendelseTitelTekst;
            c.Title = c.configuration.GetCaseTitle();
            c.IndsenderIdentifikation = BOMApplication.IndsendelseType.Indsender.Identifikation;

            c.Afsender = BOMApplication.IndsendelseType.Indsender.NavnTekst;
            c.Dage = BOMApplication.ServiceMaalStatistikType?.ServiceMaal?.Dage;
            c.SagsbehandlingForbrugtDage = BOMApplication.ServiceMaalStatistikType?.Statistik?.SagsbehandlingForbrugtDage;
            c.SagsbehandlingForbrugtDageSpecified = BOMApplication.ServiceMaalStatistikType?.Statistik?.SagsbehandlingForbrugtDageSpecified;
            c.VisitationForbrugtDage = BOMApplication.ServiceMaalStatistikType?.Statistik?.VisitationForbrugtDage;
            c.VisitationForbrugtDageSpecified = BOMApplication.ServiceMaalStatistikType?.Statistik?.VisitationForbrugtDageSpecified;

            if (BOMApplication.IndsendelseType.FuldmagtHaver != null)
            {
                c.FuldmagtHaverIdentifikation = BOMApplication.IndsendelseType.FuldmagtHaver.Identifikation;
            }
            if (BOMApplication.IndsendelseType.MyndighedSag != null)
            {
                c.BOMExternCaseUniqueIdentifier = BOMApplication.IndsendelseType.MyndighedSag.MyndighedSagIdentifikator;
                c.BOMExternCaseBrugerVendtNoegle = BOMApplication.IndsendelseType.MyndighedSag.BrugervendtNoegle;
            }

            c.SagStatusKode = BOMApplication.IndsendelseType.BOMSag.SagStatus.SagStatusKode;
            c.InitiativPligt = BOMApplication.IndsendelseType.BOMSag.SagStatus.InitiativPligt.ToString();
            c.FaseKode = BOMApplication.IndsendelseType.BOMSag.SagStatus.FaseKode;
            c.FristNotifikationProfilKode = BOMApplication.IndsendelseType.BOMSag.SagStatus.FristNotifikationProfilKode;
            if (string.IsNullOrEmpty(c.FristNotifikationProfilKode))
            {
                c.FristNotifikationProfilKode = c.configuration.GetReceivedNotificationProfile();
            }
            c.FristDato = BOMApplication.IndsendelseType.BOMSag.SagStatus.FristDato;
            c.AnsvarligMyndighed = BOMApplication.IndsendelseType.AnsvarligMyndighed.Myndighed.MyndighedNavn;
            c.AnsvarligMyndighedCVR = BOMApplication.IndsendelseType.AnsvarligMyndighed.Myndighed.MyndighedCVR;

            // Sagstype
            if (BOMApplication.IndsendelseType.BOMSag.SagTypeRef != null)
            {
                c.SagsTypeKode = BOMApplication.IndsendelseType.BOMSag.SagTypeRef.SagTypeKode;
                c.SagsTypeNavn = BOMApplication.IndsendelseType.BOMSag.SagTypeRef.VisningNavn;
                c.SagsOmraadeKode = BOMApplication.IndsendelseType.BOMSag.SagTypeRef.SagOmraadeKode;
                c.SagsOmraadeNavn = configuration.GetCaseAreaName(c.SagsOmraadeKode);
            }

            // Aktivitet
            if (BOMApplication.IndsendelseType.BOMSag.AktivitetListe.Length > 0)
            {
                BOM.BOMSagsbehandling.AktivitetTypeRefType aktivitet = BOMApplication.IndsendelseType.BOMSag.AktivitetListe[0];
                c.AktivitetTypeKode = aktivitet.AktivitetTypeKode;
                c.AktivitetTypeNavn = aktivitet.VisningNavn;
            }

            // Matrikel og ejendom
            BOM.BOMSagsbehandling.Ejendom[] estates = BOMApplication.IndsendelseType.BOMSag.SagSteder.Ejendom;
            if (estates != null)
            {
                if (estates.Length > 0)
                {
                    c.Ejendomsnummer = estates[0].RealPropertyStructure.MunicipalRealPropertyIdentifier;
                    c.EjendomKommunenr = estates[0].RealPropertyStructure.MunicipalityCode;
                }
            }
            BOM.BOMSagsbehandling.MATRLandParcelIdentificationStructureType[] parcels = BOMApplication.IndsendelseType.BOMSag.SagSteder.Matrikel;
            if (parcels != null)
            {
                if (parcels.Length > 0)
                {
                    c.MatrikelEjerlavId = parcels[0].CadastralDistrictIdentifier;
                    c.MatrikelNummer = parcels[0].LandParcelIdentifier;
                }
            }

            if (BOMApplication.IndsendelseType.DokumentationListe.Items != null)
            {
                foreach (BOMSagsbehandling.AbstraktDokumentationType ad in BOMApplication.IndsendelseType.DokumentationListe.Items)
                {
                    if (ad is BOMSagsbehandling.SagObjektDokumentationType)
                    {
                        BOMSagsbehandling.SagObjektType[] SagObjektTyper = ((BOMSagsbehandling.SagObjektDokumentationType)ad).SagObjekt;
                        if (SagObjektTyper != null)
                        {
                            foreach (BOMSagsbehandling.SagObjektType s in SagObjektTyper)
                            {
                                if (s.Item is BOM.BOMSagsbehandling.MatrikelType)
                                {
                                    BOM.BOMSagsbehandling.MatrikelType parcel = (BOM.BOMSagsbehandling.MatrikelType)s.Item;
                                    c.MatrikelEjerlavId = parcel.MATRLandParcelIdentificationStructure.CadastralDistrictIdentifier;
                                    c.MatrikelNummer = parcel.MATRLandParcelIdentificationStructure.LandParcelIdentifier;
                                }
                                if (s.Item is BOM.BOMSagsbehandling.Ejendom)
                                {
                                    BOM.BOMSagsbehandling.Ejendom ejendom = (BOM.BOMSagsbehandling.Ejendom)s.Item;
                                    c.Ejendomsnummer = ejendom.RealPropertyStructure.MunicipalRealPropertyIdentifier;
                                    c.EjendomKommunenr = ejendom.RealPropertyStructure.MunicipalityCode;
                                }
                            }
                        }
                    }
                }
            }

            if (BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe.Length == 0)
            {
                // Add a test KLE number
                if (CheckSetting("AddKLEIfMissing"))
                {
                    BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe = new BOMSagsbehandling.KlasseRefType[3];

                    BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe[0] = new BOMSagsbehandling.KlasseRefType();
                    BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe[0].KlassifikationSystemKode = "KLnr";
                    BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe[0].KlassifikationFacetKode = "KLEmne";
                    BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe[0].KlasseKode = "00.01.00";

                    BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe[1] = new BOMSagsbehandling.KlasseRefType();
                    BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe[1].KlassifikationSystemKode = "KLnr";
                    BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe[1].KlassifikationFacetKode = "KLHandling";
                    BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe[1].KlasseKode = "A00";

                    BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe[2] = new BOMSagsbehandling.KlasseRefType();
                    BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe[2].KlassifikationSystemKode = "KLnr";
                    BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe[2].KlassifikationFacetKode = "KLKassation";
                    BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe[2].KlasseKode = "B";
                }
            }

            // KLE
            foreach (BOMSagsbehandling.KlasseRefType t in BOMApplication.IndsendelseType.BOMSag.SagTypeRef.KlasseRefListe)
            {
                if (t.KlassifikationSystemKode == "KLnr")
                {
                    switch (t.KlassifikationFacetKode)
                    {
                        case "KLEmne":
                            c.KLEmnenr = t.KlasseKode;
                            break;
                        case "KLHandling":
                            c.KLFacet = t.KlasseKode;
                            break;
                        case "KLKassation":
                            c.KLKassation = t.KlasseKode;
                            break;
                    }
                }
            }

            // Adresse
            c.Adresse = BOMApplication.IndsendelseType.BOMSag.SagSteder.VisningNavn;
            if (BOMApplication.IndsendelseType.BOMSag.SagSteder.Adresse != null)
            {
                foreach (BOMSagsbehandling.Adresse a in BOMApplication.IndsendelseType.BOMSag.SagSteder.Adresse)
                {
                    if (a.AddressPostal != null)
                    {
                        c.StreetName = a.AddressPostal.StreetName;
                        c.StreetBuildingIdentifier = a.AddressPostal.StreetBuildingIdentifier;
                        c.PostCodeIdentifier = a.AddressPostal.PostCodeIdentifier;
                        c.DistrictName = a.AddressPostal.DistrictName;
                    }
                    if (a.AddressSpecific != null)
                    {
                        c.MunicipalityCode = a.AddressSpecific.AddressAccess.MunicipalityCode;
                    }
                    else
                    {
                        c.MunicipalityCode = c.EjendomKommunenr;
                    }
                }
            }
            // Indsendelses dokument
            c.IndsendelsesDokument = GetDocumentInfo(configuration, BOMApplication.IndsendelseType.IndsendelseDokument);

            // Konflikt Rapport dokument
            c.KonfliktRapportDokument = GetDocumentInfo(configuration, BOMApplication.IndsendelseType.KonfliktRapportDokument);

            // Bilag
            c.Bilag = new List<BOMDocument>();
            foreach (BOMSagsbehandling.IndsendelseTypeFilBilag bilag in BOMApplication.IndsendelseType.FilBilagListe)
            {
                c.Bilag.Add(GetDocumentInfo(configuration, bilag.Dokument));
            }

            // Indsender
            c.Indsender = new Contact(BOMApplication.IndsendelseType.Indsender);
            c.TilknyttetAnsoeger = new List<Contact>();
            foreach (BOMSagsbehandling.BOMSagTypeTilknyttetAnsoeger a in BOMApplication.IndsendelseType.BOMSag.TilknyttetAnsoegerListe)
            {
                c.TilknyttetAnsoeger.Add(new Contact(a));
            }

            // Foerste indsendelse
            c.FoersteIndsendelseDatoTid = c.IndsendelseDatoTid;
            if (BOMApplication.IndsendelseType.TidligereIndsendelserListe != null)
            {
                foreach (BOMSagsbehandling.IndsendelseRefType s in BOMApplication.IndsendelseType.TidligereIndsendelserListe)
                {
                    if (s.IndsendelseLoebenr == 1)
                    {
                        c.FoersteIndsendelseDatoTid = s.IndsendelseDatoTid;
                    }
                }
            }

            c.SagServiceMaalKode = BOMApplication.IndsendelseType.BOMSag.SagServiceMaalKode;
            if (string.IsNullOrEmpty(c.SagServiceMaalKode))
            {
                c.SagServiceMaalKode = DEFAULT_SAG_SERVICE_MAAL_KODE;
            }

            return c;
        }

        private static BOMDocument GetDocumentInfo(BOMConfiguration configuration, BOMSagsbehandling.DokumentType doc)
        {
            if (doc == null)
            {
                return null;
            }

            BOMDocument d = new BOMDocument();
            d.DokumentID = doc.DokumentID;
            d.BrugervendtNoegleTekst = doc.DokumentEgenskaber.BrugervendtNoegleTekst;
            d.BeskrivelseTekst = doc.DokumentEgenskaber.BeskrivelseTekst;
            d.BrevDato = doc.DokumentEgenskaber.BrevDato;
            d.TitelTekst = doc.DokumentEgenskaber.TitelTekst;
            d.IndholdTekst = doc.VariantListe[0].Del[0].IndholdTekst;
            d.MimeTypeTekst = doc.VariantListe[0].Del[0].MimeTypeTekst;
            if (configuration != null)
            {
                d.IsFileTypeValid = configuration.FileFormats.Contains(d.FileType) || !configuration.UseFileFormatControl;
            }
            return d;
        }

        private static void SendBOMCaseReceivedMessage(BOMCase c)
        {
            BOMSagsbehandling.BesvarelseType reply = new BOMSagsbehandling.BesvarelseType();
            reply.BesvarelseID = Guid.NewGuid().ToString();
            reply.BOMSagID = c.BOMSagID;
            reply.TitelTekst = c.configuration.GetReceivedTitle();
            reply.BrevDato = DateTime.Now;

            reply.Afsender = new BOMSagsbehandling.BesvarelseTypeAfsender();
            reply.Afsender.MyndighedCvrNummer = GetMunicipalityCVR();
            if (!string.IsNullOrEmpty(c.CaseWorkerName))
            {
                reply.Afsender.Sagsbehandler = new BOMSagsbehandling.SagsbehandlerType();
                reply.Afsender.Sagsbehandler.NavnTekst = c.CaseWorkerName;
                reply.Afsender.Sagsbehandler.EmailTekst = c.CaseWorkerEmail;
                reply.Afsender.Sagsbehandler.TelefonTekst = c.CaseWorkerPhone;

                reply.SagsbehandlerOpdatering = new BOMSagsbehandling.BesvarelseTypeSagsbehandlerOpdatering();
                reply.SagsbehandlerOpdatering.Sagsbehandler = new BOMSagsbehandling.SagsbehandlerType();
                reply.SagsbehandlerOpdatering.Sagsbehandler.NavnTekst = c.CaseWorkerName;
                reply.SagsbehandlerOpdatering.Sagsbehandler.EmailTekst = c.CaseWorkerEmail;
                reply.SagsbehandlerOpdatering.Sagsbehandler.TelefonTekst = c.CaseWorkerPhone;
            }

            reply.MyndighedSagOpdatering = new BOMSagsbehandling.BesvarelseTypeMyndighedSagOpdatering();
            reply.MyndighedSagOpdatering.MyndighedSag = new BOMSagsbehandling.MyndighedSagType();
            reply.MyndighedSagOpdatering.MyndighedSag.MyndighedSagIdentifikator = c.CaseUniqueIdentifier;
            reply.MyndighedSagOpdatering.MyndighedSag.BrugervendtNoegle = c.CaseNumber;

            reply.SagStatusOpdatering = new BOMSagsbehandling.BesvarelseTypeSagStatusOpdatering();
            reply.SagStatusOpdatering.SagStatus = new BOMSagsbehandling.SagStatusType();
            reply.SagStatusOpdatering.SagStatus.StatusTidspunkt = DateTime.Now;
            reply.SagStatusOpdatering.SagStatus.SagStatusKode = c.configuration.GetReceivedStatusCode();
            reply.SagStatusOpdatering.SagStatus.InitiativPligt = BOMSagsbehandling.SagStatusTypeInitiativPligt.Myndighed;
            reply.SagStatusOpdatering.SagStatus.FaseKode = c.configuration.GetReceivedPhaseCode();

            string NotificationProfile = c.configuration.GetReceivedNotificationProfile();
            if (!string.IsNullOrEmpty(NotificationProfile))
            {
                reply.SagStatusOpdatering.SagStatus.FristNotifikationProfilKode = NotificationProfile;
            }

            BOMCaller.Reply(reply);
            string url = BOMCaller.GetBOMServer() + "ansoegningbesvarelse/" + reply.BesvarelseID;
            url = url.Replace("service", "dokument");
            CreateMessageHistoryOnCase(c.BOMTitle, c.BOMSubmissionCreated, c.IndsendelsesDokument.IndholdTekst, c.Afsender, c.CaseRecno);
            CreateMessageHistoryOnCase(reply.TitelTekst, reply.BrevDato, url, c.AnsvarligMyndighed, c.CaseRecno);

            UpdateReplyBOMCase(c,
                reply.SagStatusOpdatering.SagStatus.FaseKode,
                reply.SagStatusOpdatering.SagStatus.SagStatusKode,
                "Myndighed",
                reply.SagStatusOpdatering.SagStatus.FristNotifikationProfilKode);

            if (!string.IsNullOrEmpty(NotificationProfile) && c.FristNotifikationProfilKode != NotificationProfile)
            {
                c.FristNotifikationProfilKode = NotificationProfile;
                // KBL - VSTS 30855: Erindringer skal ikke oprettes
                //CreateRemindersOnCase(c);
            }
        }

        private static void SendBOMCaseReceivedMessageForExisting(BOMCase c)
        {
            BOMSagsbehandling.BesvarelseType reply = new BOMSagsbehandling.BesvarelseType();
            reply.BesvarelseID = Guid.NewGuid().ToString();
            reply.BOMSagID = c.BOMSagID;
            reply.TitelTekst = c.configuration.GetReceivedTitle2();
            reply.BrevDato = DateTime.Now;

            reply.Afsender = new BOMSagsbehandling.BesvarelseTypeAfsender();
            reply.Afsender.MyndighedCvrNummer = GetMunicipalityCVR();
            if (!string.IsNullOrEmpty(c.CaseWorkerName))
            {
                reply.Afsender.Sagsbehandler = new BOMSagsbehandling.SagsbehandlerType();
                reply.Afsender.Sagsbehandler.NavnTekst = c.CaseWorkerName;
                reply.Afsender.Sagsbehandler.EmailTekst = c.CaseWorkerEmail;
                reply.Afsender.Sagsbehandler.TelefonTekst = c.CaseWorkerPhone;

                reply.SagsbehandlerOpdatering = new BOMSagsbehandling.BesvarelseTypeSagsbehandlerOpdatering();
                reply.SagsbehandlerOpdatering.Sagsbehandler = new BOMSagsbehandling.SagsbehandlerType();
                reply.SagsbehandlerOpdatering.Sagsbehandler.NavnTekst = c.CaseWorkerName;
                reply.SagsbehandlerOpdatering.Sagsbehandler.EmailTekst = c.CaseWorkerEmail;
                reply.SagsbehandlerOpdatering.Sagsbehandler.TelefonTekst = c.CaseWorkerPhone;
            }
            BOMCaller.Reply(reply);
            string url = BOMCaller.GetBOMServer() + "ansoegningbesvarelse/" + reply.BesvarelseID;
            url = url.Replace("service", "dokument");
            CreateMessageHistoryOnCase(c.BOMTitle, c.BOMSubmissionCreated, c.IndsendelsesDokument.IndholdTekst, c.Afsender, c.CaseRecno);
            CreateMessageHistoryOnCase(reply.TitelTekst, reply.BrevDato, url, c.AnsvarligMyndighed, c.CaseRecno);


            UpdateReplyBOMCase(c,
                c.FaseKode,
                c.SagStatusKode,
                c.InitiativPligt,
                c.FristNotifikationProfilKode);

            List<ChangeLog> Changes = new List<ChangeLog>();
            if (c.eDocPhaseCode != c.FaseKode)
            {
                Changes.Add(new ChangeLog()
                {
                    LogdataName = "Fase",
                    LogdataFrom = c.eDocPhaseCodeDescription,
                    LogdataTo = BOMConfigHandler.GetPhaseDescription(c.FaseKode)
                });
            }
            if (c.eDocStatusCode != c.SagStatusKode)
            {
                Changes.Add(new ChangeLog()
                {
                    LogdataName = "Status",
                    LogdataFrom = c.eDocStatusCodeDescription,
                    LogdataTo = BOMConfigHandler.GetStatusDescription(c.SagStatusKode)
                });
            }
            if (c.eDocInitiativeDutyCode != c.InitiativPligt)
            {
                Changes.Add(new ChangeLog()
                {
                    LogdataName = "Sagen afventer",
                    LogdataFrom = c.eDocInitiativeDutyDescription,
                    LogdataTo = BOMConfigHandler.GetInitiativeDutyDescription(c.InitiativPligt)
                });
            }
            EntityLogHelper.LogOnCase(c.CaseRecno, Changes);
        }

        private static string GetBOMCaseRecno(string caseRecno)
        {
            // Get BOMCase recno
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMCaseRecnoQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMCase", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#TO_CASE#", caseRecno);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);

            return doc.SelectSingleNode("/RECORDS/RECORD/Recno").InnerText;
        }

        public static List<string> GetBOMStatus(string caseStatusRecno)
        {
            List<string> fuBOmStatusMap = new List<string>();
            // Get Case metadata
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMCaseStatusMapQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMCase", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#TO_CASE_STATUS#", caseStatusRecno);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);

            if (doc.SelectSingleNode("/RECORDS/@RECORDCOUNT").Value != "0")
            {
                string BOMStatusCode = doc.SelectSingleNode("/RECORDS/RECORD/BOMCaseStatusType.Code").InnerText;
                string BOMInitiativPligtCode = doc.SelectSingleNode("/RECORDS/RECORD/BOMInitiativPligt.Code").InnerText;
                string BOMPhaseCode = doc.SelectSingleNode("/RECORDS/RECORD/BOMPhaseCode.Code").InnerText;

                if (!string.IsNullOrEmpty(BOMStatusCode) & !string.IsNullOrEmpty(BOMInitiativPligtCode) & !string.IsNullOrEmpty(BOMPhaseCode))
                {
                    fuBOmStatusMap.Add(BOMStatusCode);
                    fuBOmStatusMap.Add(BOMInitiativPligtCode);
                    fuBOmStatusMap.Add(BOMPhaseCode);
                }

            }
            return fuBOmStatusMap;

        }

        public static void NotifyBOMOfNewCaseState(string eDocCaseRecno, List<string> BOMStatusCodes)
        {
            // Get Case metadata

            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetCaseQuery.xml", "Fujitsu.eDoc.BOM.XML.Case", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#RECNO#", eDocCaseRecno);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);

            string caseRecno = doc.SelectSingleNode("/RECORDS/RECORD/Recno").InnerText;
            string CaseNumber = doc.SelectSingleNode("/RECORDS/RECORD/Name").InnerText;
            string CaseWorkerName = doc.SelectSingleNode("/RECORDS/RECORD/SearchName").InnerText;
            string CaseWorkerEmail = doc.SelectSingleNode("/RECORDS/RECORD/E-mail").InnerText;
            string CaseWorkerPhone = doc.SelectSingleNode("/RECORDS/RECORD/Telephone").InnerText;
            string BOMCaseID = doc.SelectSingleNode("/RECORDS/RECORD/BOMCaseID").InnerText;
            string PhaseCode = doc.SelectSingleNode("/RECORDS/RECORD/PhaseCode").InnerText;
            string responsibleAuthority = doc.SelectSingleNode("/RECORDS/RECORD/ResponsibleAuthority").InnerText;

            BOMSagsbehandling.BesvarelseType reply = new BOMSagsbehandling.BesvarelseType();
            reply.BesvarelseID = Guid.NewGuid().ToString();
            reply.BOMSagID = BOMCaseID;
            reply.TitelTekst = UPDATED_CASESTATUS;
            reply.BrevDato = DateTime.Now;

            reply.Afsender = new BOMSagsbehandling.BesvarelseTypeAfsender();
            reply.Afsender.MyndighedCvrNummer = GetMunicipalityCVR();

            reply.SagStatusOpdatering = new BOMSagsbehandling.BesvarelseTypeSagStatusOpdatering();
            reply.SagStatusOpdatering.SagStatus = new BOMSagsbehandling.SagStatusType();
            reply.SagStatusOpdatering.SagStatus.StatusTidspunkt = DateTime.Now;
            reply.SagStatusOpdatering.SagStatus.SagStatusKode = BOMStatusCodes.First();

            if (BOMStatusCodes.Contains("Ansøger"))
            {
                reply.SagStatusOpdatering.SagStatus.InitiativPligt = SagStatusTypeInitiativPligt.Ansoeger;
            }
            else
            {
                reply.SagStatusOpdatering.SagStatus.InitiativPligt = (SagStatusTypeInitiativPligt)Enum.Parse(typeof(SagStatusTypeInitiativPligt), BOMStatusCodes.ElementAt(1));
            }

            reply.SagStatusOpdatering.SagStatus.FaseKode = BOMStatusCodes.ElementAt(2);

            BOMConfiguration bOMconfiguration = BOMConfigHandler.GetBOMConfiguration();

            string NotificationProfile = bOMconfiguration.GetReceivedNotificationProfile();
            if (!string.IsNullOrEmpty(NotificationProfile))
            {
                reply.SagStatusOpdatering.SagStatus.FristNotifikationProfilKode = NotificationProfile;
            }

            BOMCaller.Reply(reply);

            string url = BOMCaller.GetBOMServer() + "ansoegningbesvarelse/" + reply.BesvarelseID;
            url = url.Replace("service", "dokument");
            CreateMessageHistoryOnCase(reply.TitelTekst, reply.BrevDato, url, responsibleAuthority, caseRecno);

        }


        public static void NotifyBOMOfNewCaseWorker(string eDocCaseRecno, string CaseWorkerRecno)
        {
            // Get Case metadata
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetCaseQuery.xml", "Fujitsu.eDoc.BOM.XML.Case", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#RECNO#", eDocCaseRecno);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);

            string CaseNumber = doc.SelectSingleNode("/RECORDS/RECORD/Name").InnerText;
            string CaseWorkerName = doc.SelectSingleNode("/RECORDS/RECORD/SearchName").InnerText;
            string CaseWorkerEmail = doc.SelectSingleNode("/RECORDS/RECORD/E-mail").InnerText;
            string CaseWorkerPhone = doc.SelectSingleNode("/RECORDS/RECORD/Telephone").InnerText;
            string BOMCaseID = doc.SelectSingleNode("/RECORDS/RECORD/BOMCaseID").InnerText;

            BOMSagsbehandling.BesvarelseType reply = new BOMSagsbehandling.BesvarelseType();
            reply.BesvarelseID = Guid.NewGuid().ToString();
            reply.BOMSagID = BOMCaseID;
            reply.TitelTekst = UPDATED_CASEWORKER;
            reply.BrevDato = DateTime.Now;

            reply.Afsender = new BOMSagsbehandling.BesvarelseTypeAfsender();
            reply.Afsender.MyndighedCvrNummer = GetMunicipalityCVR();
            if (!string.IsNullOrEmpty(CaseWorkerName))
            {
                reply.Afsender.Sagsbehandler = new BOMSagsbehandling.SagsbehandlerType();
                reply.Afsender.Sagsbehandler.NavnTekst = CaseWorkerName;
                reply.Afsender.Sagsbehandler.EmailTekst = CaseWorkerEmail;
                reply.Afsender.Sagsbehandler.TelefonTekst = CaseWorkerPhone;

                reply.SagsbehandlerOpdatering = new BOMSagsbehandling.BesvarelseTypeSagsbehandlerOpdatering();
                reply.SagsbehandlerOpdatering.Sagsbehandler = new BOMSagsbehandling.SagsbehandlerType();
                reply.SagsbehandlerOpdatering.Sagsbehandler.NavnTekst = CaseWorkerName;
                reply.SagsbehandlerOpdatering.Sagsbehandler.EmailTekst = CaseWorkerEmail;
                reply.SagsbehandlerOpdatering.Sagsbehandler.TelefonTekst = CaseWorkerPhone;
            }

            BOMCaller.Reply(reply);

        }

        private static string GetSubArchiveRecno(string SubArchiveCode)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetSubArchiveQuery.xml", "Fujitsu.eDoc.BOM.XML.Case", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#SUBARCHIVECODE#", SubArchiveCode);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            string subArchiveRecno = doc.SelectSingleNode("/RECORDS/RECORD/Recno").InnerXml;

            return subArchiveRecno;
        }

        private static string GetDiscardRecno(string DiscardCode)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetDiscardQuery.xml", "Fujitsu.eDoc.BOM.XML.Case", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#DISCARDCODE#", DiscardCode);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            string DiscardCodeRecno = doc.SelectSingleNode("/RECORDS/RECORD/Recno").InnerXml;

            return DiscardCodeRecno;
        }

        private static void RegisterBOMCase(BOMCase c)
        {
            UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.Processing);

            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("CreateBOMCaseMetaInsert.xml", "Fujitsu.eDoc.BOM.XML.BOMCase", Assembly.GetExecutingAssembly());
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlQuery);

            XmlNode n1 = doc.SelectSingleNode("/operation");
            XmlNode n2 = doc.SelectSingleNode("/operation/INSERTSTATEMENT");
            XmlNode n3 = doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='Address']");
            XmlNode n4 = doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='Address']/VALUE");

            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='Address']/VALUE").InnerText = c.Adresse;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ApplicantIdentification']/VALUE").InnerText = c.IndsenderIdentifikation;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ApplicationId']/VALUE").InnerText = c.IndsendelseID;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='CadastralDistrictIdentifier']/VALUE").InnerText = c.MatrikelEjerlavId;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='CaseId']/VALUE").InnerText = c.BOMSagID;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='CaseNumber']/VALUE").InnerText = c.BomNummer;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ClassificationDiscard']/VALUE").InnerText = c.KLKassation;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ClassificationFacet']/VALUE").InnerText = c.KLFacet;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ClassificationNumber']/VALUE").InnerText = c.KLEmnenr;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='Deadline']/VALUE").InnerText = c.FristDato == DateTime.MinValue ? "" : c.FristDato.ToString("yyyy-MM-dd");
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='InitiativeDuty']/VALUE").InnerText = c.InitiativPligt;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='LandParcelIdentifier']/VALUE").InnerText = c.MatrikelNummer;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='LastActivity']/VALUE").InnerText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='MandataryIdentification']/VALUE").InnerText = c.FuldmagtHaverIdentifikation;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='MunicipalityCode']/VALUE").InnerText = c.MunicipalityCode;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='PhaseCode']/VALUE").InnerText = c.FaseKode;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ResponsibleAuthority']/VALUE").InnerText = c.AnsvarligMyndighed;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ResponsibleAuthorityCVR']/VALUE").InnerText = c.AnsvarligMyndighedCVR;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='StatusCode']/VALUE").InnerText = c.SagStatusKode;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='Title']/VALUE").InnerText = c.BOMTitle;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='AreaCode']/VALUE").InnerText = c.SagsOmraadeKode;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='AreaName']/VALUE").InnerText = c.SagsOmraadeNavn;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='CaseTypeCode']/VALUE").InnerText = c.SagsTypeKode;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='CaseTypeName']/VALUE").InnerText = c.SagsTypeNavn;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='SubmissionTime']/VALUE").InnerText = c.IndsendelseDatoTid.ToString("yyyy-MM-dd HH:mm:ss");
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='DeadlineNotificationKode']/VALUE").InnerText = c.FristNotifikationProfilKode;

            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='EstateIdentifier']/VALUE").InnerText = c.Ejendomsnummer;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='EstateMunicipalityCode']/VALUE").InnerText = c.EjendomKommunenr;

            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='TransferStatus']/VALUE").InnerText = ((int)BOMCaseTransferStatusEnum.Pending).ToString();

            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ActivityTypeCode']/VALUE").InnerText = c.AktivitetTypeKode;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ApplicantsXML']/VALUE").InnerText = Serialization.Serialize<List<Contact>>(c.TilknyttetAnsoeger);
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ServiceGoalCode']/VALUE").InnerText = c.SagServiceMaalKode;

            //Knowledge Cube provides statics after 24 hours
            c.ServiceMaalStatus = ServiceGoalStatus.PendingCalculation;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ServiceMaalStatus']/VALUE").InnerText = c.ServiceMaalStatus;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ServiceMaalDays']/VALUE").InnerText = c.Dage;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='CaseManagementSpentDays']/VALUE").InnerText = c.SagsbehandlingForbrugtDage.ToString();
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='CaseManagementSpentDaysSpecified']/VALUE").InnerText = c.SagsbehandlingForbrugtDageSpecified == true ? 1.ToString() : 0.ToString();
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='VisitationSpentDays']/VALUE").InnerText = c.VisitationForbrugtDage.ToString();
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='VisitationSpentDaysSpecified']/VALUE").InnerText = c.VisitationForbrugtDageSpecified == true ? 1.ToString() : 0.ToString();


            xmlQuery = doc.OuterXml;

            c.BOMCaseRecno = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
        }

        private static void CreateEdocCase(BOMCase c)
        {
            UpdateBOMCaseTransferStatus(c, BOMCaseTransferStatusEnum.Processing);

            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("CreateCaseMetaInsert.xml", "Fujitsu.eDoc.BOM.XML.Case", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#TITLE#", c.Title);
            xmlQuery = xmlQuery.Replace("#ORGUNIT#", c.OrgUnitRecno);
            xmlQuery = xmlQuery.Replace("#OURREF#", c.OurRefRecno);
            xmlQuery = xmlQuery.Replace("#ESTATERECNO#", c.EstateRecno);
            xmlQuery = xmlQuery.Replace("#SUBARCHIVERECNO#", c.SubArchiveRecno);
            xmlQuery = xmlQuery.Replace("#CLASSCODE1#", c.KLEmnenr.Substring(0, 5));
            xmlQuery = xmlQuery.Replace("#CLASSCODE2#", c.KLEmnenr);
            xmlQuery = xmlQuery.Replace("#CLASSCODE3#", c.KLFacet);
            xmlQuery = xmlQuery.Replace("#DISCARDCODERECNO#", c.DiscardCodeRecno);
            xmlQuery = xmlQuery.Replace("#ACCESSCODE#", c.AccessCode);
            xmlQuery = xmlQuery.Replace("#ACCESSGROUP#", c.AccessGroup);
            xmlQuery = xmlQuery.Replace("#ToCaseCategory#", c.ToCaseCategory);
            xmlQuery = xmlQuery.Replace("#ToProgressPlan#", c.ToProgressPlan);
            xmlQuery = xmlQuery.Replace("#CaseType#", c.ToCaseType);
            xmlQuery = xmlQuery.Replace("#FuBomRecno#", c.BOMCaseRecno);


            XmlDocument docQuery = new XmlDocument();
            docQuery.LoadXml(xmlQuery);
            XmlNode nBatch = docQuery.SelectSingleNode("/operation/BATCH");
            XmlNode nCaseContact = docQuery.SelectSingleNode("/operation/BATCH/INSERTSTATEMENT[@ENTITY='CaseContact']");
            nBatch.RemoveChild(nCaseContact);

            // Add Tilknyttetansoegere
            if (c.TilknyttetAnsoeger.Count > 0)
            {
                foreach (Contact con in c.TilknyttetAnsoeger)
                {
                    nCaseContact = nCaseContact.Clone();
                    nCaseContact.SelectSingleNode("METAITEM[@NAME='Name']/VALUE").InnerText = con.NavnTekst;
                    nCaseContact.SelectSingleNode("METAITEM[@NAME='Email']/VALUE").InnerText = con.EmailTekst;
                    nCaseContact.SelectSingleNode("METAITEM[@NAME='Phone']/VALUE").InnerText = con.TelefonTekst;
                    nCaseContact.SelectSingleNode("METAITEM[@NAME='Address']/VALUE").InnerText = con.Address;
                    nCaseContact.SelectSingleNode("METAITEM[@NAME='ZipCode']/VALUE").InnerText = con.PostCodeIdentifier;
                    nCaseContact.SelectSingleNode("METAITEM[@NAME='ZipPlace']/VALUE").InnerText = con.DistrictName;
                    nCaseContact.SelectSingleNode("METAITEM[@NAME='Country']/VALUE").InnerText = con.CountryIdentificationCode;
                    nCaseContact.SelectSingleNode("METAITEM[@NAME='ToRole']/VALUE").InnerText = CASE_CONTACT_ROLE_APPLICANT;
                    nBatch.AppendChild(nCaseContact);
                }
            }
            else
            {
                nCaseContact.SelectSingleNode("METAITEM[@NAME='Name']/VALUE").InnerText = c.Indsender.NavnTekst;
                nCaseContact.SelectSingleNode("METAITEM[@NAME='Email']/VALUE").InnerText = c.Indsender.EmailTekst;
                nCaseContact.SelectSingleNode("METAITEM[@NAME='Phone']/VALUE").InnerText = c.Indsender.TelefonTekst;
                nCaseContact.SelectSingleNode("METAITEM[@NAME='Address']/VALUE").InnerText = c.Indsender.Address;
                nCaseContact.SelectSingleNode("METAITEM[@NAME='ZipCode']/VALUE").InnerText = c.Indsender.PostCodeIdentifier;
                nCaseContact.SelectSingleNode("METAITEM[@NAME='ZipPlace']/VALUE").InnerText = c.Indsender.DistrictName;
                nCaseContact.SelectSingleNode("METAITEM[@NAME='Country']/VALUE").InnerText = c.Indsender.CountryIdentificationCode;
                nCaseContact.SelectSingleNode("METAITEM[@NAME='ToRole']/VALUE").InnerText = CASE_CONTACT_ROLE_APPLICANT;
            }
            xmlQuery = docQuery.OuterXml;


            // Do create in eDoc
            c.CaseRecno = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);

            PostCreateEdocCase(c);

            //// Update BOMCase
            //xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateBOMCaseMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.BOMCase", Assembly.GetExecutingAssembly());
            //xmlQuery = xmlQuery.Replace("#Recno#", c.BOMCaseRecno);
            //xmlQuery = xmlQuery.Replace("#TransferStatus#", ((int)BOMCaseTransferStatusEnum.Processing).ToString());
            //xmlQuery = xmlQuery.Replace("#ToCase#", c.CaseRecno);

            //string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);


            //// Get Case metadata
            //xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetCaseQuery.xml", "Fujitsu.eDoc.BOM.XML.Case", Assembly.GetExecutingAssembly());
            //xmlQuery = xmlQuery.Replace("#RECNO#", c.CaseRecno);
            //result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            //XmlDocument doc = new XmlDocument();
            //doc.LoadXml(result);
            //c.CaseUniqueIdentifier = doc.SelectSingleNode("/RECORDS/RECORD/UniqueIdentity").InnerText;
            //c.CaseNumber = doc.SelectSingleNode("/RECORDS/RECORD/Name").InnerText;
            //c.CaseWorkerName = doc.SelectSingleNode("/RECORDS/RECORD/SearchName").InnerText;
            //c.CaseWorkerEmail = doc.SelectSingleNode("/RECORDS/RECORD/E-mail").InnerText;
            //c.CaseWorkerPhone = doc.SelectSingleNode("/RECORDS/RECORD/Telephone").InnerText;
        }

        private static void PostCreateEdocCase(BOMCase c)
        {
            // Update BOMCase
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateBOMCaseMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.BOMCase", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#Recno#", c.BOMCaseRecno);
            xmlQuery = xmlQuery.Replace("#TransferStatus#", ((int)BOMCaseTransferStatusEnum.Processing).ToString());
            xmlQuery = xmlQuery.Replace("#ToCase#", c.CaseRecno);

            string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);


            // Get Case metadata
            xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetCaseQuery.xml", "Fujitsu.eDoc.BOM.XML.Case", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#RECNO#", c.CaseRecno);
            result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            c.CaseUniqueIdentifier = doc.SelectSingleNode("/RECORDS/RECORD/UniqueIdentity").InnerText;
            c.CaseNumber = doc.SelectSingleNode("/RECORDS/RECORD/Name").InnerText;
            c.CaseWorkerName = doc.SelectSingleNode("/RECORDS/RECORD/SearchName").InnerText;
            c.CaseWorkerEmail = doc.SelectSingleNode("/RECORDS/RECORD/E-mail").InnerText;
            c.CaseWorkerPhone = doc.SelectSingleNode("/RECORDS/RECORD/Telephone").InnerText;
        }

        private static void UpdateEdocCase(BOMCaseUpdateType c)
        {
            // Update case metadata
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateBOMCaseReplyMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.BOMCase", Assembly.GetExecutingAssembly());
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlQuery);
            XmlNode nUpdatestatement = doc.SelectSingleNode("/operation/UPDATESTATEMENT");

            nUpdatestatement.Attributes["PRIMARYKEYVALUE"].Value = c.ToBOMCase;

            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='StatusCode']/VALUE").InnerText = c.Status.SagStatusKode;
            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='OtherAuthorityCode']/VALUE").InnerText = c.Status.SagAndenMyndighedKode;
            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='PhaseCode']/VALUE").InnerText = c.Status.FaseKode;
            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='InitiativeDuty']/VALUE").InnerText = c.Status.InitiativPligtKode;
            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='DeadlineNotificationKode']/VALUE").InnerText = c.Status.FristNotifikationProfilKode;
            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='Deadline']/VALUE").InnerText = c.Status.FristDato > DateTime.MinValue ? c.Status.FristDato.ToString() : "";
            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='LastActivity']/VALUE").InnerText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(doc.OuterXml);

            // Update files
            if (c.MainDocument != null)
            {
                UpdateFileVersionSentToBOM(c.MainDocument);
            }
            if (c.Attachments != null)
            {
                foreach (BOMReplyDocument att in c.Attachments)
                {
                    UpdateFileVersionSentToBOM(att);
                }
            }
        }

        private static void UpdateEdocCaseWithServiceGoal(BOMCaseUpdateType c)
        {
            // Update case metadata
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateBOMCaseServiceGoalMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.BOMCase", Assembly.GetExecutingAssembly());
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlQuery);
            XmlNode nUpdatestatement = doc.SelectSingleNode("/operation/UPDATESTATEMENT");

            nUpdatestatement.Attributes["PRIMARYKEYVALUE"].Value = c.ToBOMCase;

            nUpdatestatement.SelectSingleNode("METAITEM[@NAME='ServiceGoalCode']/VALUE").InnerText = c.SagServiceMaalKode;

            string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(doc.OuterXml);
        }

        private static void UpdateFileVersionSentToBOM(BOMReplyDocument doc)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateFileVersionMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.FileVersion", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#Recno#", doc.FileVersionRecno);
            xmlQuery = xmlQuery.Replace("#FuSentToBOMDate#", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
        }

        private static void UpdateBOMCaseTransferStatus(BOMCase c, BOMCaseTransferStatusEnum newStatus)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateBOMCaseTransferStatusMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.BOMCase", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#Recno#", c.BOMCaseRecno);
            xmlQuery = xmlQuery.Replace("#TransferStatus#", ((int)newStatus).ToString());

            string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
        }

        private static bool FindExistingCase(BOMCase c)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetCaseFromBOMCaseNumberQuery.xml", "Fujitsu.eDoc.BOM.XML.Case", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#CaseId#", c.BOMSagID);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            if (string.IsNullOrEmpty(result))
            {
                return false;
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            if (doc.SelectNodes("/RECORDS/RECORD").Count > 0)
            {
                c.CaseRecno = doc.SelectSingleNode("/RECORDS/RECORD/Recno").InnerText;
                c.CaseUniqueIdentifier = doc.SelectSingleNode("/RECORDS/RECORD/UniqueIdentity").InnerText;
                c.CaseNumber = doc.SelectSingleNode("/RECORDS/RECORD/Name").InnerText;
                c.CaseWorkerName = doc.SelectSingleNode("/RECORDS/RECORD/SearchName").InnerText;
                c.CaseWorkerEmail = doc.SelectSingleNode("/RECORDS/RECORD/E-mail").InnerText;
                c.CaseWorkerPhone = doc.SelectSingleNode("/RECORDS/RECORD/Telephone").InnerText;
                c.BOMCaseRecno = doc.SelectSingleNode("/RECORDS/RECORD/BOMCaseRecno").InnerText;
                c.OrgUnitRecno = doc.SelectSingleNode("/RECORDS/RECORD/ToOrgUnit").InnerText;
                c.OurRefRecno = doc.SelectSingleNode("/RECORDS/RECORD/OurRef").InnerText;

                c.eDocPhaseCode = doc.SelectSingleNode("/RECORDS/RECORD/PhaseCode").InnerText;
                c.eDocPhaseCodeDescription = doc.SelectSingleNode("/RECORDS/RECORD/PhaseCodeDescription").InnerText;
                c.eDocStatusCode = doc.SelectSingleNode("/RECORDS/RECORD/StatusCode").InnerText;
                c.eDocStatusCodeDescription = doc.SelectSingleNode("/RECORDS/RECORD/StatusCodeDescription").InnerText;
                c.eDocInitiativeDutyCode = doc.SelectSingleNode("/RECORDS/RECORD/InitiativeDutyCode").InnerText;
                c.eDocInitiativeDutyDescription = doc.SelectSingleNode("/RECORDS/RECORD/InitiativeDutyDescription").InnerText;

                if (doc.SelectSingleNode("//RECORDS/RECORD/BFEnumber").Attributes.Count < 1)
                {
                    c.edocBFENumber = Convert.ToInt32(doc.SelectSingleNode("/RECORDS/RECORD/BFEnumber").InnerText);
                }

                return true;
            }
            return false;
        }

        private static void CreateDocumentOnCase(BOMCase c, BOMDocument doc, BOMDocument konfliktRapportDokument, List<BOMDocument> bilag)
        {
            if (doc == null & konfliktRapportDokument == null)
            {
                return;
            }

            if (!doc.IsFileTypeValid & !konfliktRapportDokument.IsFileTypeValid)
            {
                return;
            }

            //START
            //Application.pdf + KonfliktRapport.pdf
            Dictionary<string, BOMDocument> ansoegKonfliktPDFPairs = new Dictionary<string, BOMDocument>
            {
                { doc.BrugervendtNoegleTekst, doc },
                { konfliktRapportDokument.BrugervendtNoegleTekst, konfliktRapportDokument }
            };

            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("CreateDocumentMetaInsert.xml", "Fujitsu.eDoc.BOM.XML.Document", Assembly.GetExecutingAssembly());
            XmlDocument docQuery = new XmlDocument();
            docQuery.LoadXml(xmlQuery);

            docQuery.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='Title']/VALUE").InnerText = "Indsendelse " + c.BomSubmissionNummer;
            docQuery.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ToCase']/VALUE").InnerText = c.CaseRecno;
            docQuery.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='IsBomDocumentRead']/VALUE").InnerText = "0";
            docQuery.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ToOrgUnit']/VALUE").InnerText = c.OrgUnitRecno;
            docQuery.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='OurRef']/VALUE").InnerText = c.OurRefRecno;
            docQuery.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='BomSubmissionNummer']/VALUE").InnerText = c.BomSubmissionNummer.ToString();
            docQuery.SelectSingleNode("/operation/BATCH/INSERTSTATEMENT[@ENTITY='ActivityContact']/METAITEM[@NAME='Name']/VALUE").InnerText = c.Indsender.NavnTekst;
            docQuery.SelectSingleNode("/operation/BATCH/INSERTSTATEMENT[@ENTITY='ActivityContact']/METAITEM[@NAME='Email']/VALUE").InnerText = c.Indsender.EmailTekst;
            docQuery.SelectSingleNode("/operation/BATCH/INSERTSTATEMENT[@ENTITY='ActivityContact']/METAITEM[@NAME='Phone']/VALUE").InnerText = c.Indsender.TelefonTekst;
            docQuery.SelectSingleNode("/operation/BATCH/INSERTSTATEMENT[@ENTITY='ActivityContact']/METAITEM[@NAME='Address']/VALUE").InnerText = c.Indsender.Address;
            docQuery.SelectSingleNode("/operation/BATCH/INSERTSTATEMENT[@ENTITY='ActivityContact']/METAITEM[@NAME='ZipCode']/VALUE").InnerText = c.Indsender.PostCodeIdentifier;
            docQuery.SelectSingleNode("/operation/BATCH/INSERTSTATEMENT[@ENTITY='ActivityContact']/METAITEM[@NAME='ZipPlace']/VALUE").InnerText = c.Indsender.DistrictName;
            docQuery.SelectSingleNode("/operation/BATCH/INSERTSTATEMENT[@ENTITY='ActivityContact']/METAITEM[@NAME='Country']/VALUE").InnerText = c.Indsender.CountryIdentificationCode;
            docQuery.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='DocumentDate']/VALUE").InnerText = doc.BrevDato.ToString("yyyy-MM-dd");

            XmlNode insertNode = docQuery.SelectSingleNode("/operation/BATCH/INSERTSTATEMENT[@ENTITY='File']");
            XmlNode batchNode = docQuery.SelectSingleNode("/operation/BATCH");
            List<string> localFilePaths = new List<string>();

            foreach (KeyValuePair<string, BOMDocument> entry in ansoegKonfliktPDFPairs)
            {
                Uri documentURI = new Uri(entry.Value.IndholdTekst);
                string LocalFilePath = Fujitsu.eDoc.Core.FileUploadSupport.InvokeGetTemporaryPath() + (entry.Value.TitelTekst);
                localFilePaths.Add(LocalFilePath);
                BOMCaller.DownloadFile(documentURI, LocalFilePath);


                insertNode.SelectSingleNode("METAITEM[@NAME='url']/VALUE").InnerText = LocalFilePath;
                insertNode.SelectSingleNode("METAITEM[@NAME='Comment']/VALUE").InnerText = entry.Value.TitelTekst;

                if (insertNode.ParentNode == null)
                {
                    insertNode.SelectSingleNode("METAITEM[@NAME='ToRelationType']/VALUE").InnerText = "2";
                    batchNode.AppendChild(insertNode);
                }
                insertNode = insertNode.Clone();

            }

            xmlQuery = docQuery.OuterXml;
            //END


            List<BOMDocument> validAttachments = bilag.Where(appendix => appendix.IsFileTypeValid == true).Select(attachment => attachment).ToList();

            //Attachments on the application
            if (validAttachments.Any())
            {
                List<string> localFilepaths = new List<string>();

                XmlElement metaItem = docQuery.CreateElement("METAITEM");
                metaItem.SetAttribute("NAME", "AttachmentID");

                XmlElement elemValue = docQuery.CreateElement("VALUE");
                metaItem.AppendChild(elemValue);

                insertNode.AppendChild(metaItem);

                foreach (BOMDocument b in validAttachments)
                {
                    if (!(IsDocumentExisting(b.DokumentID)))
                    {
                        Uri documentURI = new Uri(b.IndholdTekst);
                        string LocalFilePath = Fujitsu.eDoc.Core.FileUploadSupport.InvokeGetTemporaryPath() + b.TitelTekst;
                        BOMCaller.DownloadFile(documentURI, LocalFilePath);

                        insertNode.SelectSingleNode("//METAITEM[@NAME='url']/VALUE").InnerText = LocalFilePath;
                        insertNode.SelectSingleNode("//METAITEM[@NAME='Comment']/VALUE").InnerText = b.TitelTekst;
                        insertNode.SelectSingleNode("METAITEM[@NAME='ToRelationType']/VALUE").InnerText = "2";
                        insertNode.SelectSingleNode("METAITEM[@NAME='AttachmentID']/VALUE").InnerText = b.DokumentID;
                        batchNode.AppendChild(insertNode);
                        insertNode = insertNode.Clone();

                        localFilepaths.Add(LocalFilePath);
                    }
                }
                xmlQuery = docQuery.OuterXml;
                doc.DocumentRecno = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
                UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.AttachmentsTransfered);

                try
                {
                    localFilepaths.ForEach(y => System.IO.File.Delete(y));
                }
                catch (Exception ex)
                {
                    Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMCaseHandler", "FuBOM", ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
                }
            }

            else
            {
                doc.DocumentRecno = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);

                UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.ApplicationDocumentTransfered);
                UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.ConflictDocumentTransfered);
                UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.AttachmentsTransfered);

                try
                {
                    localFilePaths.ForEach(x => System.IO.File.Delete(x));

                }
                catch (Exception ex)
                {

                    Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMCaseHandler", "FuBOM", ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
                }
            }

        }


        public static bool IsDocumentExisting(string docID)
        {
            XmlDocument xml = new XmlDocument();
            bool isDocExisting = false;
            try
            {
                string xmlQuery = string.Format(@"
                           <operation>
                            <QUERYDESC NAMESPACE='SIRIUS' ENTITY='File' DATASETFORMAT='XML' TAG='RECORDS' MAXROWS='0'>
                                <RESULTFIELDS>
                                  <METAITEM TAG='AttachmentID'>AttachmentID</METAITEM>
                                </RESULTFIELDS>
                             <CRITERIA>
                              <METAITEM NAME='AttachmentID' OPERATOR='='>
                                <VALUE>{0}</VALUE>
                              </METAITEM>
                           </CRITERIA>
                         </QUERYDESC>
                         </operation>", docID);

                string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);


                if (!string.IsNullOrEmpty(result))
                {
                    xml.LoadXml(result);

                    if (xml.FirstChild.Attributes[0].Value != "0")
                    {
                        isDocExisting = true;
                    }
                }

            }

            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMCaseHandler", "FuBOM",
                   $"Error occured when searching a document ID {docID} :\n {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);
            }

            return isDocExisting;

        }

        public static string DownloadApplicationFile(string ApplicationId)
        {
            FuIndsendelseType indsendelse = BOMCaller.GetApplication(ApplicationId);
            BOMDocument doc = GetDocumentInfo(null, indsendelse.IndsendelseType.IndsendelseDokument);

            Uri documentURI = new Uri(doc.IndholdTekst);
            string LocalFilePath = Fujitsu.eDoc.Core.FileUploadSupport.InvokeGetTemporaryPath() + ApplicationId + " - " + doc.TitelTekst;

            if (!System.IO.File.Exists(LocalFilePath))
            {
                BOMCaller.DownloadFile(documentURI, LocalFilePath);
            }

            return LocalFilePath;
        }

        public static string UploadReplyFile(string BOMCaseId, string DokId, string localFilePath)
        {
            string baseUrl = "https://dokument-es.bygogmiljoe.dk";
            string BOMServer = BOMCaller.GetBOMServer();
            if (BOMServer.Contains("-es"))
            {
                baseUrl = "https://dokument-es.bygogmiljoe.dk";
            }
            else if (BOMServer.Contains("-bomet"))
            {
                baseUrl = "https://dokumentweb-bomet.knowledgecube.net";
            }
            else
            {
                baseUrl = "https://dokument.bygogmiljoe.dk";
            }
            string url = string.Format("{0}/besvarelsesdokumenter/myndigheder/{1}/sager/{2}/{3}", baseUrl, GetMunicipalityCVR(), BOMCaseId, DokId);

            Uri documentURI = new Uri(url);
            if (System.IO.File.Exists(localFilePath) == false)
            {
                throw new ArgumentException($"localFilePath does not exist: '{localFilePath}'{Environment.NewLine}url to use: '{url}'");
            }

            BOMCaller.UploadFile(documentURI, localFilePath);

            return url;
        }

        // Configuration
        private static string GetMunicipalityCVR()
        {
            //string cvr = "29189846";
            string cvr = Fujitsu.eDoc.Core.eDocSettingInformation.GetSettingValueFromeDoc("fujitsu", "municipalitycvr");
            return cvr;
        }


        private static void RegisterBOMSubmission(BOM.BOMSagsbehandling.IndsendelseType BOMApplication, BOMCase c)
        {
            string BOMSubmissionRecno = "";
            string IndsendelseID = BOMApplication.IndsendelseID;
            int IndsendelseLoebenr = BOMApplication.IndsendelseLoebenr;
            DateTime IndsendelseDatoTid = BOMApplication.IndsendelseDatoTid;
            string BOMSagID = BOMApplication.BOMSag.BOMSagID;

            // Chech if exsists
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMSubmissionMetaQuery.xml", "Fujitsu.eDoc.BOM.XML.BOMSubmission", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#ApplicationId#", IndsendelseID);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            XmlNodeList nl = doc.SelectNodes("/RECORDS/RECORD");
            if (nl.Count > 0)
            {
                BOMSubmissionRecno = nl[0].SelectSingleNode("Recno").InnerText;
                string strBOMSubmissionTransferStatus = nl[0].SelectSingleNode("TransferStatus").InnerText;
                int BOMSubmissionTransferStatus = (int)BOMCaseTransferStatusEnum.Pending;

                c.BOMSubmissionRecno = BOMSubmissionRecno;
                if (int.TryParse(strBOMSubmissionTransferStatus, out BOMSubmissionTransferStatus))
                {
                    c.BOMSubmissionTransferStatus = (BOMCaseTransferStatusEnum)BOMSubmissionTransferStatus;
                }

                if (!string.IsNullOrEmpty(nl[0].SelectSingleNode("ToBOMCase").InnerText))
                {
                    c.BOMCaseRecno = nl[0].SelectSingleNode("ToBOMCase").InnerText;
                }

                c.BOMSubmissionCreated = DateTime.Now;
                if (!string.IsNullOrEmpty(nl[0].SelectSingleNode("InsertDate").InnerText))
                {
                    string sCreated = nl[0].SelectSingleNode("InsertDate").InnerText;
                    DateTime Created;
                    if (DateTime.TryParse(sCreated, out Created))
                    {
                        c.BOMSubmissionCreated = Created;
                    }
                }

                c.BOMSubmissionErrMsg = nl[0].SelectSingleNode("ErrorMessage").InnerText;
                return;
            }

            // Create new
            xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("CreateBOMSubmissionMetaInsert.xml", "Fujitsu.eDoc.BOM.XML.BOMSubmission", Assembly.GetExecutingAssembly());
            doc = new XmlDocument();
            doc.LoadXml(xmlQuery);

            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='SubmissionTime']/VALUE").InnerText = IndsendelseDatoTid.ToString("yyyy-MM-dd HH:mm:ss");
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ApplicationId']/VALUE").InnerText = IndsendelseID;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='CaseId']/VALUE").InnerText = BOMSagID;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='TransferStatus']/VALUE").InnerText = ((int)BOMCaseTransferStatusEnum.Pending).ToString();
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='SubmissionNummer']/VALUE").InnerText = IndsendelseLoebenr.ToString();
            xmlQuery = doc.OuterXml;

            BOMSubmissionRecno = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
            c.BOMSubmissionRecno = BOMSubmissionRecno;
            c.BOMSubmissionCreated = DateTime.Now;
            c.BOMSubmissionTransferStatus = BOMCaseTransferStatusEnum.Pending;
        }

        private static void UpdateBOMSubmission(BOMCase c, BOMCaseTransferStatusEnum newStatus)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateBOMSubmissionMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.BOMSubmission", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#Recno#", c.BOMSubmissionRecno);
            xmlQuery = xmlQuery.Replace("#TransferStatus#", ((int)newStatus).ToString());
            xmlQuery = xmlQuery.Replace("#ToBOMCase#", c.BOMCaseRecno);
            xmlQuery = xmlQuery.Replace("#SubmissionNummer#", c.BomSubmissionNummer.ToString());

            string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
            c.BOMSubmissionTransferStatus = newStatus;
        }

        private static void RegisterBOMSubmissionError(BOMCase c, Exception ex)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("RegisterBOMSubmissionErrorMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.BOMSubmission", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#Recno#", c.BOMSubmissionRecno);
            xmlQuery = xmlQuery.Replace("#ErrorMessage#", ex.ToString());

            string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
        }


        private static void CreateRemindersOnCase(BOMCase c)
        {
            // KBL - VSTS 30855: Erindringer skal ikke oprettes
            //int[] deadlines = c.configuration.GetDeadlines(c.FristNotifikationProfilKode);
            //if (deadlines.Length > 0)
            //{
            //    for (int i = 0; i < deadlines.Length; i++)
            //    {
            //        int days = deadlines[i];
            //        string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("CreateReminderActivityMetaInsert.xml", "Fujitsu.eDoc.BOM.XML.Activity", Assembly.GetExecutingAssembly());
            //        xmlQuery = xmlQuery.Replace("#DAYS#", days.ToString());
            //        xmlQuery = xmlQuery.Replace("#StartDate#", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            //        xmlQuery = xmlQuery.Replace("#EndDate#", DateTime.Now.AddDays(days).ToString());
            //        xmlQuery = xmlQuery.Replace("#OurRef#", c.OurRefRecno);
            //        xmlQuery = xmlQuery.Replace("#ToCase#", c.CaseRecno);
            //        xmlQuery = xmlQuery.Replace("#ToOrgUnit#", c.OrgUnitRecno);
            //        xmlQuery = xmlQuery.Replace("#ConnectedEntityDescription#", c.Title);

            //        Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
            //    }
            //}
        }


        private static void UpdateReplyBOMCase(BOMCase c, string PhaseCode, string StatusCode, string InitiativeDuty, string DeadlineNotificationKode)
        {
            // Update BOMCase
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateReplyBOMCaseMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.BOMCase", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#Recno#", c.BOMCaseRecno);
            xmlQuery = xmlQuery.Replace("#PhaseCode#", PhaseCode);
            xmlQuery = xmlQuery.Replace("#StatusCode#", StatusCode);
            xmlQuery = xmlQuery.Replace("#InitiativeDuty#", InitiativeDuty);
            xmlQuery = xmlQuery.Replace("#DeadlineNotificationKode#", DeadlineNotificationKode);

            string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
        }

        private static bool CheckSetting(string Key)
        {
            string val = System.Configuration.ConfigurationManager.AppSettings[Key];
            if (!string.IsNullOrEmpty(val))
            {
                if (val.ToLower() == "false")
                {
                    return false;
                }
            }
            return true;
        }

    }
}
