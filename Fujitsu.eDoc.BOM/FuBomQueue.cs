using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fujitsu.eDoc.BOM
{
    public class FuBomQueue
    {
        public string Recno;
        public string MetaXML;
        public string CaseRecno;
        public string CreatedDate;
        public BOMCaseUpdateType BOMCaseUpdateType;
        public BOMQueueStatusType BOMQueueStatusType;

        public FuBomQueue()
        {

        }
        public FuBomQueue(string recno, string metaXML, string caseRecno, string createdDate, BOMCaseUpdateType bOMCaseUpdateType, BOMQueueStatusType bOMQueueStatusType)
        {
            Recno = recno;
            MetaXML = metaXML;
            CaseRecno = caseRecno;
            CreatedDate = createdDate;
            BOMCaseUpdateType = bOMCaseUpdateType;
            BOMQueueStatusType = bOMQueueStatusType;
        }
    }
}
