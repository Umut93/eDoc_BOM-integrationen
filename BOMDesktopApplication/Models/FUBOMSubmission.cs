using System;
using static Fujitsu.eDoc.BOMApplicationDesktopApp.BOMUtils;

namespace Fujitsu.eDoc.BOMApplicationDesktopApp
{
    public class FUBOMSubmission : IEquatable<FUBOMSubmission>
    {
        public string recno { get; set; }
        public DateTime submissionTime { get; set; }
        public int submissionNummer { get; set; }
        public Guid applicationId { get; set; }
        public Guid caseId { get; set; }
        public string toBOMCase { get; set; }
        public BOMCaseTransferStatusEnum transferStatus { get; set; }
        public DateTime insertDate { get; set; }
        public string errorMessage { get; set; }

        public FUBOMSubmission()
        {

        }

        public FUBOMSubmission(string recno, DateTime submissionTime, int submissionNummer, Guid applicationId, Guid caseId, string toBOMCase, BOMCaseTransferStatusEnum transferStatus, DateTime insertDate, string errorMessage)
        {
            this.recno = recno;
            this.submissionTime = submissionTime;
            this.submissionNummer = submissionNummer;
            this.applicationId = applicationId;
            this.caseId = caseId;
            this.toBOMCase = toBOMCase;
            this.transferStatus = transferStatus;
            this.insertDate = insertDate;
            this.errorMessage = errorMessage;
        }


        public bool Equals(FUBOMSubmission other)
        {
            if (this.submissionNummer == other.submissionNummer)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}