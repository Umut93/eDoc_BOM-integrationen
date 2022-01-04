using System;
using System.Collections.Generic;
using static Fujitsu.eDoc.BOMApplicationDesktopApp.BOMUtils;

namespace Fujitsu.eDoc.BOMApplicationDesktopApp
{
    internal class BOMCase
    {
        private string titlemask;
        public string Afsender { get; set; }

        public BOMConfiguration configuration { get; set; }
        public string IndsendelseID { get; set; }
        public string BOMSagID { get; set; }
        public string BomNummer { get; set; }
        public DateTime IndsendelseDatoTid { get; set; }
        public DateTime FoersteIndsendelseDatoTid { get; set; }
        public int BomSubmissionNummer { get; set; }
        public string BOMExternCaseUniqueIdentifier { get; set; }
        public string BOMExternCaseBrugerVendtNoegle { get; set; }


        public string AktivitetTypeKode { get; set; }
        public string AktivitetTypeNavn { get; set; }
        public string SagsTypeKode { get; set; }
        public string SagsTypeNavn { get; set; }
        public string SagsOmraadeKode { get; set; }
        public string SagsOmraadeNavn { get; set; }
        public string BOMTitle { get; set; }
        public string SagServiceMaalKode { get; set; }
        public string ToCaseType { get; set; }

        //Extra fields - Servicemål
        public string Dage { get; set; }
        public int? SagsbehandlingForbrugtDage { get; set; }
        public bool? SagsbehandlingForbrugtDageSpecified { get; set; }
        public int? VisitationForbrugtDage { get; set; }
        public bool? VisitationForbrugtDageSpecified { get; set; }

        public string Title
        {
            get
            {
                return MakeTitle(titlemask);
            }
            set
            {
                titlemask = value;
            }
        }
        public string IndsenderIdentifikation { get; set; }
        public string FuldmagtHaverIdentifikation { get; set; }

        public string SagStatusKode { get; set; }
        public string InitiativPligt { get; set; }
        public string FaseKode { get; set; }
        public string FristNotifikationProfilKode { get; set; }  // TODO
        public DateTime FristDato { get; set; }
        public string AnsvarligMyndighed { get; set; }
        public string AnsvarligMyndighedCVR { get; set; }

        public string MatrikelEjerlavId { get; set; }
        public string MatrikelNummer { get; set; }
        public string Ejendomsnummer { get; set; }  // TODO
        public string EjendomKommunenr { get; set; }  // TODO
        public string KLEmnenr { get; set; }
        public string KLFacet { get; set; }
        public string KLKassation { get; set; }
        public string Adresse { get; set; }

        public string StreetName { get; set; }
        public string StreetBuildingIdentifier { get; set; }
        public string PostCodeIdentifier { get; set; }
        public string DistrictName { get; set; }

        public string MunicipalityCode { get; set; }

        public BOMDocument IndsendelsesDokument { get; set; }
        public BOMDocument KonfliktRapportDokument { get; set; }
        public List<BOMDocument> Bilag { get; set; }

        public string EstateRecno { get; set; }
        public string SubArchiveRecno { get; set; }
        public string DiscardCodeRecno { get; set; }
        public string OrgUnitRecno { get; set; }
        public string OurRefRecno { get; set; }
        public string ToCaseCategory { get; set; }
        public string ToProgressPlan { get; set; }
        public string AccessCode { get; set; }
        public string AccessGroup { get; set; }
        public string CaseRecno { get; set; }
        public string BOMSubmissionRecno { get; set; }
        public DateTime BOMSubmissionCreated { get; set; }
        public BOMCaseTransferStatusEnum BOMSubmissionTransferStatus { get; set; }
        public string BOMSubmissionErrMsg { get; set; }
        public string BOMCaseRecno { get; set; }
        public string CaseUniqueIdentifier { get; set; }
        public string CaseNumber { get; set; }
        public string CaseWorkerName { get; set; }
        public string CaseWorkerEmail { get; set; }
        public string CaseWorkerPhone { get; set; }

        public string ApplicantRecno { get; set; }

        public Contact Indsender { get; set; }
        public List<Contact> TilknyttetAnsoeger { get; set; }

        public string eDocPhaseCode { get; set; }
        public string eDocPhaseCodeDescription { get; set; }
        public string eDocStatusCode { get; set; }
        public string eDocStatusCodeDescription { get; set; }
        public string eDocInitiativeDutyCode { get; set; }
        public string eDocInitiativeDutyDescription { get; set; }
        public int edocBFENumber { get; set; }

        private string MakeTitle(string titleMask)
        {
            titleMask = titleMask.Replace("[TITLE]", BOMTitle);
            titleMask = titleMask.Replace("[AKTIVITETSTYPEKODE]", AktivitetTypeKode);
            titleMask = titleMask.Replace("[AKTIVITETSTYPENAVN]", AktivitetTypeNavn);
            titleMask = titleMask.Replace("[VEJNAVN]", StreetName);
            titleMask = titleMask.Replace("[HUSNUMMER]", StreetBuildingIdentifier);
            titleMask = titleMask.Replace("[POSTNUMMER]", PostCodeIdentifier);
            titleMask = titleMask.Replace("[POSTDISTRIKT]", DistrictName);

            if (titleMask.Length > 254)
            {
                titleMask = titleMask.Substring(0, 254);
            }
            return titleMask;
        }

        public bool CanCreateEDocCase()
        {
            return !string.IsNullOrEmpty(MatrikelEjerlavId) && !string.IsNullOrEmpty(MatrikelNummer) && !string.IsNullOrEmpty(MunicipalityCode) && !string.IsNullOrEmpty(ToCaseType);
        }
    }
}
