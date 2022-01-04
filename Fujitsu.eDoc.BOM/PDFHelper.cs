using Fujitsu.eDoc.Core;
using System.Xml;

namespace Fujitsu.eDoc.BOM
{
    public enum PDFStatusType
    {
        Pending = 0,
        Success = 1,
        Failed = 2
    }

    public class PDFHelper
    {

        public static string StartConvertFile(string FileRecno)
        {
            string result = FileManagerUtilities.RenderFiles(FileRecno, 0);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            string jobId = doc.SelectSingleNode("RECORDS/RECORD/JobId").InnerText;

            return jobId;
        }

        public static PDFStatusType GetConvertStatus(BOMReplyDocument doc, out string FileFullname)
        {
            if (!string.IsNullOrEmpty(doc.ConversionJobId))
            {
                return GetConvertStatus(doc.ConversionJobId, out FileFullname);
            }

            if (!string.IsNullOrEmpty(doc.FileFullname))
            {
                FileFullname = doc.FileFullname;
                return PDFStatusType.Success;
            }

            FileFullname = "";
            return PDFStatusType.Failed;
        }

        public static PDFStatusType GetConvertStatus(string JobId, out string FileFullname)
        {
            FileFullname = "";
            string result = FileManagerUtilities.GetRenderingStatus(JobId, "", "");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            string status = doc.SelectSingleNode("RECORDS/RECORD/Status").InnerText;
            string failedFilesCount = doc.SelectSingleNode("RECORDS/RECORD/FailedFilesCount").InnerText;
            string filePath = doc.SelectSingleNode("RECORDS/RECORD/FilePath").InnerText;

            if (status == "1" && failedFilesCount == "0")
            {

                string LocalFilePath = Fujitsu.eDoc.Core.FileUploadSupport.InvokeGetTemporaryPath() + filePath;
                Fujitsu.eDoc.Core.TempStorageManager.InvokeDownload(filePath, LocalFilePath);

                FileFullname = LocalFilePath;
                return PDFStatusType.Success;
            }
            if (failedFilesCount != "0")
            {
                return PDFStatusType.Failed;
            }

            return PDFStatusType.Pending;
        }
    }
}
