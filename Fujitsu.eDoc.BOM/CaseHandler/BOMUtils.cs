using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fujitsu.eDoc.BOM.CaseHandler
{
    internal class BOMUtils
    {
        internal enum BOMCaseTransferStatusEnum
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
