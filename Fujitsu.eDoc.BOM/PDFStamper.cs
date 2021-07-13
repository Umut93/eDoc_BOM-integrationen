using Fujitsu.eDoc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using TallComponents.PDF.Annotations.Widgets;
using TallComponents.PDF.Colors;
using TallComponents.PDF.Forms.Fields;

namespace Fujitsu.eDoc.BOM
{
    public class PDFStamper
    {
        private static TallComponents.PDF.Document inputDocument;

        public static void StampAttahcments(BOMCaseUpdateType c)
        {
            foreach (BOMReplyDocument attahchment in c.Attachments)
            {
                using (FileStream inputFileStream = File.OpenRead(attahchment.FileFullname))
                {
                    inputDocument = new TallComponents.PDF.Document(inputFileStream);
                    int i = 0;
                    foreach (TallComponents.PDF.Page page in inputDocument.Pages)
                    {
                        var stampText = string.Format(" {0}\n {1}\n {2} {3:dd. MMMM yyyy}\n Der kan ikke måles på tegningerne", c.CaseTitle, c.MainDocument.Title, c.InitiativeDuty, DateTime.Now);

                        TextField textField = new TextField("TextField" + i + inputDocument.ToString());
                        textField.Multiline = true;
                        textField.Value = stampText;
                        inputDocument.Fields.Add(textField);

                        Widget widget = new Widget(page.Width / 3.5, page.Height - 50, 410, 35);
                        widget.HorizontalAlignment = TallComponents.PDF.HorizontalAlignment.Left;
                        widget.FontSize = 0; // auto-size
                        widget.TextColor = RgbColor.Red;
                        widget.BackgroundColor = RgbColor.Transparent;
                        widget.Persistency = WidgetPersistency.Flatten;
                        textField.Widgets.Add(widget);

                        page.Widgets.Add(widget);
                        i++;
                    }
                }

                using (FileStream output = new FileStream(attahchment.FileFullname, FileMode.Create, FileAccess.Write))
                {
                    inputDocument.Write(output);
                }

                if (!attahchment.FileFullname.ToLower().Contains(".pdf"))
                {
                    string pdfExt = Path.ChangeExtension(attahchment.FileFullname, ".pdf");
                    File.Move(attahchment.FileFullname, pdfExt);
                    attahchment.FileFullname = pdfExt;
                }

            }
            CreateNewDocument(c);
        }



        public static void CreateNewDocument(BOMCaseUpdateType c)
        {
            try
            {
                string insertQuery = $@"<operation>
                                          <INSERTSTATEMENT NAMESPACE='SIRIUS' ENTITY='Document'>
                                            <METAITEM NAME='Title'>
                                              <VALUE>Stemplede bilag</VALUE>
                                            </METAITEM>
                                            <METAITEM NAME='ToDocumentArchive'>
                                              <VALUE>2</VALUE>
                                            </METAITEM>
                                            <METAITEM NAME='ToCase'>
                                              <VALUE>{c.CaseRecno}</VALUE>
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
                                              <VALUE>{c.OrgUnitRecno}</VALUE>
                                            </METAITEM>
                                            <METAITEM NAME='OurRef'>
                                              <VALUE>{c.Ct_recno}</VALUE>
                                            </METAITEM>
                                             <METAITEM NAME='ToJournalStatus'>
                                              <VALUE>6</VALUE>
                                            </METAITEM>
                                          </INSERTSTATEMENT>";

                System.Text.StringBuilder files = new System.Text.StringBuilder();
                files.AppendLine("<BATCH>");

                foreach (var attachment in c.Attachments)
                {

                    files.AppendLine($@"
                                      <INSERTSTATEMENT NAMESPACE='SIRIUS' ENTITY='File'>
                                       <METAITEM NAME='url'>
                                         <VALUE>{attachment.FileFullname}</VALUE>
                                       </METAITEM>
                                       <METAITEM NAME='ToRelationType'>
                                        <VALUE>2</VALUE>
                                       </METAITEM>
                                       <METAITEM NAME='Comment'>
                                        <VALUE>{attachment.Title + "_" + "kommunestempel"}</VALUE>
                                       </METAITEM>
                                       </INSERTSTATEMENT>");
                }
                files.AppendLine("</BATCH>");

                string insertXml = insertQuery + files + "</operation>";


                string result = Fujitsu.eDoc.Core.Common.ExecuteSingleAction(insertXml);
            }

            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging(typeof(PDFStamper).FullName, "FuBOM",
                $"Error on creating a new document on case: {c.CaseRecno} - {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);
            }
        }
    }
}

