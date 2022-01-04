using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using SI.Portal.BusinessIntegration;
using SI.Portal.BusinessIntegration.Data;
using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace Fujitsu.eDoc.BOM.CodeBehind
{
    public class AttachmentListGenerator : ICodeBehind
    {
        public object Invoke(ICodeBehindObjectCollection objectCollection, object data)
        {
            string fileRecno = string.Empty;


            if (objectCollection != null && objectCollection.Count == 2)
            {
                IEntity FuCase = (IEntity)objectCollection.GetByIndex(0).DataObject;

                IList FuBOMAttachmentFilesList = (IList)objectCollection.GetByIndex(1).DataObject;

                string xDoc = CreateXMLForAttachmentList(FuCase, FuBOMAttachmentFilesList);

                string htmloutput = Transform(XDocument.Parse(xDoc));

                byte[] fileStream = GenerateWordDocFromHTMLContent(htmloutput);

                CreateNewDocument(FuCase, fileStream, out string fileName);

                if (!string.IsNullOrEmpty(fileName))
                {
                    fileRecno = GetFileRecno(fileName);
                }
            }

            return fileRecno;
        }

        private string GetFileRecno(string fileName)
        {
            string fileRecno = string.Empty;
            try
            {
                string xmlQuery = $@"<operation>
                                    <QUERYDESC NAMESPACE='SIRIUS' ENTITY='File' DATASETFORMAT='XML'>
                                    <RESULTFIELDS>
                                        <METAITEM TAG='Recno'>Recno</METAITEM>
                                    </RESULTFIELDS>
                                            <CRITERIA>
                                                <METAITEM NAME='OriginalFilename' OPERATOR='='>
                                                    <VALUE>{fileName}</VALUE>
                                                </METAITEM>
                                            </CRITERIA>
                                        </QUERYDESC>
                                        </operation>";

                string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);

                if (!string.IsNullOrEmpty(result))
                {
                    XDocument doc = XDocument.Parse(result);
                    if (doc.Root.FirstAttribute.Value != "0")
                    {
                        fileRecno = doc.Root.Element("RECORD").Element("Recno").Value;
                    }
                }

            }
            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging(typeof(AttachmentListGenerator).FullName, "FuBOM",
                $"Error on getting fileRecno on this fileName: {fileName} - {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);

            }
            return fileRecno;
        }

        private string CreateXMLForAttachmentList(IEntity fuCase, IList fuBOMAttachmentFilesList)
        {
            XDocument doc = new XDocument();
            doc.Add(new XElement("Case"));

            var caseNumber = fuCase.Fields.GetByName("CaseNumber").Value.ToString();
            var caseTitle = fuCase.Fields.GetByName("CaseTitle").Value.ToString();
            var caseTypeDesc = fuCase.Fields.GetByName("CaseTypeDesc").Value.ToString();
            var ourRef = fuCase.Fields.GetByName("OurRefName").Value.ToString();
            var orgunitName = fuCase.Fields.GetByName("ToOrgUnit.SearchName").Value.ToString();

            XElement xcaseNumberEl = new XElement("CaseNumber");
            xcaseNumberEl.SetValue(caseNumber);
            XElement xCaseTitleEl = new XElement("CaseTitle");
            xCaseTitleEl.SetValue(caseTitle);
            XElement xCaseTypeDescEl = new XElement("CaseTypeDesc");
            xCaseTypeDescEl.SetValue(caseTypeDesc);
            XElement xOurRefEl = new XElement("OurRef");
            xOurRefEl.SetValue(ourRef);
            XElement xOrgunitNameEl = new XElement("OrgUniName");
            xOrgunitNameEl.SetValue(orgunitName);

            doc.Root.Add(xcaseNumberEl);
            doc.Root.Add(xCaseTitleEl);
            doc.Root.Add(xCaseTypeDescEl);
            doc.Root.Add(xOurRefEl);
            doc.Root.Add(xOrgunitNameEl);

            XElement xAttachmentList = new XElement("AttachmentList");


            foreach (DataRow drr in fuBOMAttachmentFilesList.Data.Table.Rows)
            {
                XElement xAttachMent = new XElement("Attachment");

                XElement xDocTitle = new XElement("Title");
                XElement xDocNumber = new XElement("DocumentNumber");

                xDocTitle.SetValue(drr["DisplayComment"].ToString());
                xDocNumber.SetValue(drr["DocumentNumber"].ToString());

                xAttachMent.Add(xDocNumber);
                xAttachMent.Add(xDocTitle);

                xAttachmentList.Add(xAttachMent);
            }

            doc.Root.Add(xAttachmentList);

            return doc.ToString();

        }

        private XmlDocument GetDefaultXSLT()
        {
            XmlDocument xdDocument = new XmlDocument();
            string resourcePath = "Fujitsu.eDoc.BOM.CodeBehind.XSLT";

            // Get request xml from file
            string xmlString = Fujitsu.eDoc.Core.Common.GetResourceXml("Default.xslt", resourcePath, Assembly.GetAssembly(this.GetType()));

            xdDocument.LoadXml(xmlString);
            return xdDocument;
        }

        private string Transform(XDocument XML)
        {
            return Transform(XML, GetDefaultXSLT());
        }

        private string Transform(XDocument XML, XmlDocument XSLT)
        {
            XslCompiledTransform XslCompTrans = new XslCompiledTransform();
            XslCompTrans.Load(XSLT, new XsltSettings(false, true), new XmlUrlResolver()); //Load the layout xslt

            System.IO.StringWriter StrWriter = new System.IO.StringWriter();
            XslCompTrans.Transform(XmlReader.Create(new StringReader(XML.ToString())), null, StrWriter); //Do the layout transformation
            return StrWriter.ToString();
        }


        public byte[] GenerateWordDocFromHTMLContent(string html)
        {
            byte[] filestream = new byte[] { };
            using (MemoryStream mem = new MemoryStream())
            {
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    Body body = new Body();

                    string altChunkId = "AltChunkId1";

                    //Import data as html content using Altchunk
                    AlternativeFormatImportPart chunk = mainPart.AddAlternativeFormatImportPart(AlternativeFormatImportPartType.Xhtml, altChunkId);

                    using (Stream chunkStream = chunk.GetStream(FileMode.Create, FileAccess.Write))
                    using (StreamWriter stringStream = new StreamWriter(chunkStream, Encoding.UTF8)) //Encoding.UTF8 is important to remove special characters
                        stringStream.Write(html);

                    AltChunk altChunk = new AltChunk();
                    altChunk.Id = altChunkId;

                    body.AppendChild(altChunk);
                    mainPart.Document.Append(body);
                    mainPart.Document.Save();
                    wordDoc.Close();
                }
                filestream = mem.ToArray();
                mem.Close();
            }
            return filestream;
        }



        public void CreateNewDocument(IEntity caseEntity, byte[] fileStream, out string fileName)
        {
            fileName = string.Empty;

            string filepath = string.Format("{0}{1}.{2}", Fujitsu.eDoc.Core.FileUploadSupport.InvokeGetTemporaryPath(), Guid.NewGuid().ToString(), "docx");
            File.WriteAllBytes(filepath, fileStream);

            try
            {
                string insertQuery = $@"<operation>
                                          <INSERTSTATEMENT NAMESPACE='SIRIUS' ENTITY='Document'>
                                            <METAITEM NAME='Title'>
                                              <VALUE>Bilag</VALUE>
                                            </METAITEM>
                                            <METAITEM NAME='ToDocumentArchive'>
                                              <VALUE>2</VALUE>
                                            </METAITEM>
                                            <METAITEM NAME='ToCase'>
                                              <VALUE>{caseEntity.Fields.GetByName("Recno").Value.ToString()}</VALUE>
                                            </METAITEM>
                                            <METAITEM NAME='ToDocumentCategory'>
                                              <VALUE>111</VALUE>
                                            </METAITEM>
                                            <METAITEM NAME='DocumentDate'>
                                              <VALUE>{DateTime.Now.GetDateTimeFormats().GetValue(21).ToString()}</VALUE>
                                            </METAITEM>
                                            <METAITEM NAME='JournalDate'>
                                              <VALUE>{DateTime.Now.GetDateTimeFormats().GetValue(21).ToString()}</VALUE>
                                            </METAITEM>
                                            <METAITEM NAME='ToOrgUnit'>
                                              <VALUE>3</VALUE>
                                            </METAITEM>
                                            <METAITEM NAME='OurRef'>
                                              <VALUE>{caseEntity.Fields.GetByName("OurRef").Value.ToString()}</VALUE>
                                            </METAITEM>
                                             <METAITEM NAME='ToJournalStatus'>
                                              <VALUE>6</VALUE>
                                            </METAITEM>
                                          </INSERTSTATEMENT>
                                          <BATCH>
                                            <INSERTSTATEMENT NAMESPACE='SIRIUS' ENTITY='File'>
                                              <METAITEM NAME='url'>
                                                <VALUE>{filepath}</VALUE>
                                              </METAITEM>
                                              <METAITEM NAME='ToRelationType'>
                                                <VALUE>2</VALUE>
                                              </METAITEM>
                                              <METAITEM NAME='Comment'>
                                                <VALUE>Bilagsliste</VALUE>
                                              </METAITEM>
                                            </INSERTSTATEMENT>
                                             </BATCH>
                                            </operation>";


                string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(insertQuery);
                fileName = Path.GetFileName(filepath);
            }
            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging(typeof(AttachmentListGenerator).FullName, "FuBOM",
                $"Error on creating a new document on case: {caseEntity.Fields.GetByName("Recno").Value.ToString()} - {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);
            }

        }


    }
}
