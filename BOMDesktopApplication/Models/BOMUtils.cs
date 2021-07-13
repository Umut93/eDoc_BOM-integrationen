namespace Fujitsu.eDoc.BOMApplicationDesktopApp
{
    public class BOMUtils
    {
        public enum BOMCaseTransferStatusEnum
        {
            Pending = 0,
            Processing = 1,
            Completed = 2,
            Failed = 3,
            BOMCaseCreated = 4,
            eDocCaseCreated = 5,
            ApplicationDocumentTransfered = 6,
            ConflictDocumentTransfered = 7,
            AttachmentsTransfered = 8,
            BOMReceivedMessageSent = 9,
            CompletedButInvalidFiles = 10
        }
    }
}
