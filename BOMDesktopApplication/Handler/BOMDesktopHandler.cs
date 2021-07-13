using Fujitsu.eDoc.BOM.BOMSagsbehandling;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using static Fujitsu.eDoc.BOMApplicationDesktopApp.BOMUtils;

namespace Fujitsu.eDoc.BOMApplicationDesktopApp.Handler
{
    public class BOMDesktopHandler
    {
        public static string DEFAULT_SAG_SERVICE_MAAL_KODE = "Default";
        public static string CASE_CONTACT_ROLE_APPLICANT = "50011";
        private BOMDesktopConfigHandler bOMConfigHandler = new BOMDesktopConfigHandler();
        private BOMCase bOMCase = new BOMCase();

        public void HandleVeryFirstBOMApplication(BOM.BOMSagsbehandling.IndsendelseType BOMApplication, out string fuBOMCaseRecno)
        {
            fuBOMCaseRecno = string.Empty;

            BOMConfiguration cfg = bOMConfigHandler.GetBOMConfiguration();

            //Initiate BOMCase as an object and persist the submission in DB
            bOMCase = CreateBOMCase(cfg, BOMApplication);

            ////Create FUBomCase
            RegisterBOMCase(bOMCase);
            UpdateBOMSubmission(bOMCase, BOMCaseTransferStatusEnum.BOMCaseCreated);
            fuBOMCaseRecno = bOMCase.BOMCaseRecno;

        }


        public void HandleFurtherSubmissions(BOM.BOMSagsbehandling.IndsendelseType BOMApplication, string FuBomRecno)
        {
            BOMConfiguration cfg = bOMConfigHandler.GetBOMConfiguration();
            //Initiate BOMCase as an object and persist the submission in DB
            bOMCase = CreateBOMCase(cfg, BOMApplication);
            bOMCase.BOMCaseRecno = FuBomRecno;
            UpdateBOMSubmission(bOMCase, BOMCaseTransferStatusEnum.BOMCaseCreated);

        }

        public void DeleteFuBomCase(string BOMCaseID)
        {
            string xmlQuery = $@"<operation>
                                    <QUERYDESC NAMESPACE='SIRIUS' ENTITY='FuBOMCase' DATASETFORMAT='XML'>
                                      <RESULTFIELDS>
                                        <METAITEM TAG='Recno'>Recno</METAITEM>
                                      </RESULTFIELDS>
                                      <CRITERIA>
                                        <METAITEM NAME='CaseId' OPERATOR='='>
                                          <VALUE>{BOMCaseID}</VALUE>
                                        </METAITEM>
                                      </CRITERIA>
                                    </QUERYDESC>
                                  </operation>";

            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);
            XDocument doc = XDocument.Parse(result);

            if (doc.Root.FirstAttribute.Value != "0")
            {
                xmlQuery = $@"<operation type='metagrammar'><DELETESTATEMENT NAMESPACE='SIRIUS' ENTITY='FuBOMCase' PRIMARYKEYVALUE='{doc.Root.Element("RECORD").Element("Recno").Value}'></DELETESTATEMENT></operation>";
                Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
            }
        }

        public void DeleteSubmission(string appID)
        {
            string xmlQuery = $@"<operation>
                                    <QUERYDESC NAMESPACE='SIRIUS' ENTITY='FuBOMSubmission' DATASETFORMAT='XML'>
                                      <RESULTFIELDS>
                                        <METAITEM TAG='Recno'>Recno</METAITEM>
                                      </RESULTFIELDS>
                                      <CRITERIA>
                                        <METAITEM NAME='ApplicationId' OPERATOR='='>
                                          <VALUE>{appID}</VALUE>
                                        </METAITEM>
                                      </CRITERIA>
                                    </QUERYDESC>
                                  </operation>";

            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);
            XDocument doc = XDocument.Parse(result);

            if (doc.Root.FirstAttribute.Value != "0")
            {
                xmlQuery = $@"<operation type='metagrammar'><DELETESTATEMENT NAMESPACE='SIRIUS' ENTITY='FuBOMSubmission' PRIMARYKEYVALUE='{doc.Root.Element("RECORD").Element("Recno").Value}'></DELETESTATEMENT></operation>";
                Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
            }
        }

        public void ResetFUBOMRecnoOnAllUnAttachedSubmissions(string BomCaseId)
        {
            string xmlQuery = $@"<operation>
                                    <QUERYDESC NAMESPACE='SIRIUS' ENTITY='FuBOMSubmission' DATASETFORMAT='XML'>
                                      <RESULTFIELDS>
                                        <METAITEM TAG='Recno'>Recno</METAITEM>
                                      </RESULTFIELDS>
                                      <CRITERIA>
                                        <METAITEM NAME='CaseId' OPERATOR='='>
                                          <VALUE>{BomCaseId}</VALUE>
                                        </METAITEM>
                                      </CRITERIA>
                                    </QUERYDESC>
                                  </operation>";

            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

            XDocument xDoc = XDocument.Parse(result);

            foreach (var record in xDoc.Root.Elements())
            {
                xmlQuery = $@"<operation>
                              <UPDATESTATEMENT NAMESPACE='SIRIUS' ENTITY='FuBOMSubmission' PRIMARYKEYVALUE='{record.Element("Recno").Value}'>
                                <METAITEM NAME='ToBOMCase'>
                                  <VALUE></VALUE>
                                </METAITEM>
                                </UPDATESTATEMENT>
                            </operation>";
                Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
            }
        }


        private static BOMCase CreateBOMCase(BOMConfiguration cfg, IndsendelseType bOMApplication)
        {
            BOMCase c = new BOMCase();
            c.configuration = cfg;
            RegisterBOMSubmission(bOMApplication, c);
            //
            c.IndsendelseID = bOMApplication.IndsendelseID;
            c.IndsendelseDatoTid = bOMApplication.IndsendelseDatoTid;
            c.BomSubmissionNummer = bOMApplication.IndsendelseLoebenr;
            c.BOMSagID = bOMApplication.BOMSag.BOMSagID;
            c.BomNummer = bOMApplication.BOMSag.BomNummer;
            c.BOMTitle = bOMApplication.IndsendelseTitelTekst;
            c.Title = c.configuration.GetCaseTitle();
            c.IndsenderIdentifikation = bOMApplication.Indsender.Identifikation;

            c.Afsender = bOMApplication.Indsender.NavnTekst;
            // Service goals will be processed by the service goal job
            c.Dage = string.Empty;
            c.SagsbehandlingForbrugtDage = null;
            c.SagsbehandlingForbrugtDageSpecified = false;
            c.VisitationForbrugtDage = null;
            c.VisitationForbrugtDageSpecified = false;
            //

            if (bOMApplication.FuldmagtHaver != null)
            {
                c.FuldmagtHaverIdentifikation = bOMApplication.FuldmagtHaver.Identifikation;
            }
            if (bOMApplication.MyndighedSag != null)
            {
                c.BOMExternCaseUniqueIdentifier = bOMApplication.MyndighedSag.MyndighedSagIdentifikator;
                c.BOMExternCaseBrugerVendtNoegle = bOMApplication.MyndighedSag.BrugervendtNoegle;
            }

            c.SagStatusKode = bOMApplication.BOMSag.SagStatus.SagStatusKode;
            c.InitiativPligt = bOMApplication.BOMSag.SagStatus.InitiativPligt.ToString();
            c.FaseKode = bOMApplication.BOMSag.SagStatus.FaseKode;
            c.FristNotifikationProfilKode = bOMApplication.BOMSag.SagStatus.FristNotifikationProfilKode;
            if (string.IsNullOrEmpty(c.FristNotifikationProfilKode))
            {
                c.FristNotifikationProfilKode = c.configuration.GetReceivedNotificationProfile();
            }
            c.FristDato = bOMApplication.BOMSag.SagStatus.FristDato;
            c.AnsvarligMyndighed = bOMApplication.AnsvarligMyndighed.Myndighed.MyndighedNavn;
            c.AnsvarligMyndighedCVR = bOMApplication.AnsvarligMyndighed.Myndighed.MyndighedCVR;

            // Sagstype
            if (bOMApplication.BOMSag.SagTypeRef != null)
            {
                c.SagsTypeKode = bOMApplication.BOMSag.SagTypeRef.SagTypeKode;
                c.SagsTypeNavn = bOMApplication.BOMSag.SagTypeRef.VisningNavn;
                c.SagsOmraadeKode = bOMApplication.BOMSag.SagTypeRef.SagOmraadeKode;
                c.SagsOmraadeNavn = cfg.GetCaseAreaName(c.SagsOmraadeKode);
            }

            // Aktivitet
            if (bOMApplication.BOMSag.AktivitetListe.Length > 0)
            {
                BOM.BOMSagsbehandling.AktivitetTypeRefType aktivitet = bOMApplication.BOMSag.AktivitetListe[0];
                c.AktivitetTypeKode = aktivitet.AktivitetTypeKode;
                c.AktivitetTypeNavn = aktivitet.VisningNavn;
            }

            // Matrikel og ejendom
            BOM.BOMSagsbehandling.Ejendom[] estates = bOMApplication.BOMSag.SagSteder.Ejendom;
            if (estates != null)
            {
                if (estates.Length > 0)
                {
                    c.Ejendomsnummer = estates[0].RealPropertyStructure.MunicipalRealPropertyIdentifier;
                    c.EjendomKommunenr = estates[0].RealPropertyStructure.MunicipalityCode;
                }
            }
            BOM.BOMSagsbehandling.MATRLandParcelIdentificationStructureType[] parcels = bOMApplication.BOMSag.SagSteder.Matrikel;
            if (parcels != null)
            {
                if (parcels.Length > 0)
                {
                    c.MatrikelEjerlavId = parcels[0].CadastralDistrictIdentifier;
                    c.MatrikelNummer = parcels[0].LandParcelIdentifier;
                }
            }

            if (bOMApplication.DokumentationListe.Items != null)
            {
                foreach (BOM.BOMSagsbehandling.AbstraktDokumentationType ad in bOMApplication.DokumentationListe.Items)
                {
                    if (ad is BOM.BOMSagsbehandling.SagObjektDokumentationType)
                    {
                        BOM.BOMSagsbehandling.SagObjektType[] SagObjektTyper = ((BOM.BOMSagsbehandling.SagObjektDokumentationType)ad).SagObjekt;
                        if (SagObjektTyper != null)
                        {
                            foreach (BOM.BOMSagsbehandling.SagObjektType s in SagObjektTyper)
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

            // KLE
            foreach (BOM.BOMSagsbehandling.KlasseRefType t in bOMApplication.BOMSag.SagTypeRef.KlasseRefListe)
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
            c.Adresse = bOMApplication.BOMSag.SagSteder.VisningNavn;
            if (bOMApplication.BOMSag.SagSteder.Adresse != null)
            {
                foreach (BOM.BOMSagsbehandling.Adresse a in bOMApplication.BOMSag.SagSteder.Adresse)
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
            c.IndsendelsesDokument = GetDocumentInfo(cfg, bOMApplication.IndsendelseDokument);

            // Konflikt Rapport dokument
            c.KonfliktRapportDokument = GetDocumentInfo(cfg, bOMApplication.KonfliktRapportDokument);

            // Bilag
            c.Bilag = new List<BOMDocument>();
            foreach (BOM.BOMSagsbehandling.IndsendelseTypeFilBilag bilag in bOMApplication.FilBilagListe)
            {
                c.Bilag.Add(GetDocumentInfo(cfg, bilag.Dokument));
            }

            // Indsender
            c.Indsender = new Contact(bOMApplication.Indsender);
            c.TilknyttetAnsoeger = new List<Contact>();
            foreach (BOM.BOMSagsbehandling.BOMSagTypeTilknyttetAnsoeger a in bOMApplication.BOMSag.TilknyttetAnsoegerListe)
            {
                c.TilknyttetAnsoeger.Add(new Contact(a));
            }

            // Foerste indsendelse
            c.FoersteIndsendelseDatoTid = c.IndsendelseDatoTid;
            if (bOMApplication.TidligereIndsendelserListe != null)
            {
                foreach (BOM.BOMSagsbehandling.IndsendelseRefType s in bOMApplication.TidligereIndsendelserListe)
                {
                    if (s.IndsendelseLoebenr == 1)
                    {
                        c.FoersteIndsendelseDatoTid = s.IndsendelseDatoTid;
                    }
                }
            }

            c.SagServiceMaalKode = bOMApplication.BOMSag.SagServiceMaalKode;
            if (string.IsNullOrEmpty(c.SagServiceMaalKode))
            {
                c.SagServiceMaalKode = DEFAULT_SAG_SERVICE_MAAL_KODE;
            }

            return c;
        }

        private static void RegisterBOMSubmission(IndsendelseType BOMApplication, BOMCase c)
        {
            string BOMSubmissionRecno = "";
            string IndsendelseID = BOMApplication.IndsendelseID;
            int IndsendelseLoebenr = BOMApplication.IndsendelseLoebenr;
            DateTime IndsendelseDatoTid = BOMApplication.IndsendelseDatoTid;
            string BOMSagID = BOMApplication.BOMSag.BOMSagID;

            // Create new
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("CreateBOMSubmissionMetaInsert.xml", "Fujitsu.eDoc.BOM.XML.BOMSubmission", Assembly.Load("Fujitsu.eDoc.BOM, Version=1.0.0.0, Culture=neutral, PublicKeyToken=402811e591a0c620"));
            XmlDocument doc = new XmlDocument();
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

        private static BOMDocument GetDocumentInfo(BOMConfiguration configuration, BOM.BOMSagsbehandling.DokumentType doc)
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



        private static void RegisterBOMCase(BOMCase c)
        {
            UpdateBOMSubmission(c, BOMCaseTransferStatusEnum.Processing);

            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("CreateBOMCaseMetaInsert.xml", "Fujitsu.eDoc.BOM.XML.BOMCase", Assembly.Load("Fujitsu.eDoc.BOM, Version=1.0.0.0, Culture=neutral, PublicKeyToken=402811e591a0c620"));
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlQuery);

            XmlNode n1 = doc.SelectSingleNode("/operation");
            XmlNode n2 = doc.SelectSingleNode("/operation/INSERTSTATEMENT");
            XmlNode n3 = doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='Address']");
            XmlNode n4 = doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='Address']/VALUE");

            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='Address']/VALUE").InnerText = c.Adresse;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ApplicantIdentification']/VALUE").InnerText = c.IndsenderIdentifikation;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ApplicationId']/VALUE").InnerText = c.IndsendelseID + "[CNV]";
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
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ApplicantsXML']/VALUE").InnerText = Fujitsu.eDoc.BOMApplicationDesktopApp.Serialization.Serialize<List<Contact>>(c.TilknyttetAnsoeger);
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ServiceGoalCode']/VALUE").InnerText = c.SagServiceMaalKode;

            //new fields
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='ServiceMaalDays']/VALUE").InnerText = c.Dage;
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='CaseManagementSpentDays']/VALUE").InnerText = c.SagsbehandlingForbrugtDage.ToString();
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='CaseManagementSpentDaysSpecified']/VALUE").InnerText = c.SagsbehandlingForbrugtDageSpecified == true ? 1.ToString() : 0.ToString();
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='VisitationSpentDays']/VALUE").InnerText = c.VisitationForbrugtDage.ToString();
            doc.SelectSingleNode("/operation/INSERTSTATEMENT/METAITEM[@NAME='VisitationSpentDaysSpecified']/VALUE").InnerText = c.VisitationForbrugtDageSpecified == true ? 1.ToString() : 0.ToString();


            xmlQuery = doc.OuterXml;

            c.BOMCaseRecno = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
        }




        private static void UpdateBOMSubmission(BOMCase c, BOMCaseTransferStatusEnum newStatus)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("UpdateBOMSubmissionMetaUpdate.xml", "Fujitsu.eDoc.BOM.XML.BOMSubmission", Assembly.Load("Fujitsu.eDoc.BOM, Version=1.0.0.0, Culture=neutral, PublicKeyToken=402811e591a0c620"));
            xmlQuery = xmlQuery.Replace("#Recno#", c.BOMSubmissionRecno);
            xmlQuery = xmlQuery.Replace("#TransferStatus#", ((int)newStatus).ToString());
            xmlQuery = xmlQuery.Replace("#ToBOMCase#", c.BOMCaseRecno);
            xmlQuery = xmlQuery.Replace("#SubmissionNummer#", c.BomSubmissionNummer.ToString());

            string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
            c.BOMSubmissionTransferStatus = newStatus;
        }
    }

}
