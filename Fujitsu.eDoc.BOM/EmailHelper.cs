using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Fujitsu.eDoc.BOM
{
    public class EmailHelper
    {
        private static string EMAIL_MESSAGE_BOM = "300077";

        public static void SendFailedNotification(BOMCaseUpdateType c, string ErrorMessage)
        {
            if (c.Initiator != null && !string.IsNullOrEmpty(c.Initiator.Email))
            {
                string WebUrl = Fujitsu.eDoc.Core.Url.GetCommonSiteNameUrl();
                string parameterstring = string.Format("WebAppUrl={0};To={1};Subtype={2};Recno={3};CaseNumber={4};CaseTitle={5};ErrorMessage={6}", 
                    WebUrl, c.Initiator.Email, c.CaseType, c.CaseRecno, c.CaseNumber, c.CaseTitle, ErrorMessage);

                Fujitsu.eDoc.Core.Mail.SendMail(EMAIL_MESSAGE_BOM, parameterstring);
            }
        }
    }
}
