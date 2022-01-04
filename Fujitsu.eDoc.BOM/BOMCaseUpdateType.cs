using System;
using System.Collections.Generic;

namespace Fujitsu.eDoc.BOM
{
    public class BOMCaseUpdateType
    {
        public int IsStamped { get; set; }
        public int Ct_recno { get; set; }
        public eDocUser Initiator { get; set; }
        public string CaseRecno { get; set; }
        public string ToBOMCase { get; set; }
        public string CaseType { get; set; }
        public string CaseNumber { get; set; }
        public string CaseTitle { get; set; }
        public string BOMCaseId { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public BOMCaseUpdateStatusType Status { get; set; }
        public List<BOMDocumentationRequirementType> DocumentationRequirements { get; set; }
        public BOMReplyDocument MainDocument { get; set; }
        public List<BOMReplyDocument> Attachments { get; set; }
        public List<ChangeLog> Changes { get; set; }
        public string SagServiceMaalKode { get; set; }
        public string InitiativeDuty { get; set; }
        public int OrgUnitRecno { get; set; }
        public bool VisitationForbrugtDageSpecified { get; set; }
        public bool SagsbehandlingForbrugtDageSpecified { get; set; }
    }

    public class BOMCaseUpdateStatusType
    {
        public string SagStatusKode { get; set; }
        public string InitiativPligtKode { get; set; }
        public string FaseKode { get; set; }
        public string FristNotifikationProfilKode { get; set; }
        public DateTime FristDato { get; set; }
        public string StatusText { get; set; }
        public string SagAndenMyndighedKode { get; set; }
    }

    public class BOMDocumentationRequirementType
    {
        public string Dokumentationstype { get; set; }
        public string Kravstyrke { get; set; }
        public string FaseKode { get; set; }
    }

    public class BOMReplyDocument
    {
        public string DocumentIdentifier { get; set; }
        public string Title { get; set; }
        public string DocumentNumber { get; set; }
        public string FileRecno { get; set; }
        public string FileVersionRecno { get; set; }
        public string DocumentRevisionRecno { get; set; }
        public string FileFullname { get; set; }
        public string FileExtention { get; set; }
        public string FileMimeType { get; set; }
        public string ConversionJobId { get; set; }
        public string Url { get; set; }
    }

    public class eDocUser
    {
        public string ContactRecno { get; set; }
        public string Email { get; set; }
    }

    public class ChangeLog
    {
        public string LogdataName { get; set; }
        public string LogdataFrom { get; set; }
        public string LogdataTo { get; set; }
    }
}
