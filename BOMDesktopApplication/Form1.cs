using Fujitsu.eDoc.BOM.BOMSagsbehandling;
using Fujitsu.eDoc.BOMApplicationDesktopApp.Handler;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Windows.Forms;
using System.Xml.Linq;
using static Fujitsu.eDoc.BOMApplicationDesktopApp.BOMUtils;


namespace Fujitsu.eDoc.BOMApplicationDesktopApp
{
    public partial class Form1 : Form
    {
        ListViewItem listViewItem = new ListViewItem();
        Fujitsu.eDoc.BOMApplicationDesktopApp.Handler.BOMDesktopHandler bOMHandler = new BOMDesktopHandler();

        public Form1()
        {
            InitializeComponent();
        }

        public void CreateBOMCase_Click(object sender, EventArgs e)
        {
            string FuBOmRecnoFromFirstSub = string.Empty;
            List<bool> succeded = new List<bool>();

            bool IsAnyItemsUnChecked = IsAnyUnChecked();

            if (IsAnyItemsUnChecked)
            {
                MessageBox.Show("Please check all items!");
                return;
            }

            else
            {
                ListView.CheckedListViewItemCollection selectedItems = listView1.CheckedItems;
                List<FUBOMSubmission> submissions = new List<FUBOMSubmission>();

                foreach (var item in selectedItems)
                {
                    FUBOMSubmission sub = (FUBOMSubmission)((System.Windows.Forms.ListViewItem)item).Tag;
                    submissions.Add(sub);
                }

                try
                {
                    if (submissions.Any())
                    {
                        for (int i = 0; i < submissions.Count; i++)
                        {
                            //Handle the very first submission
                            if (i == 0)
                            {
                                var firstSubmision = submissions[i];
                                BOM.BOMSagsbehandling.IndsendelseType firstSubmissionFromBOM = GetApplication(firstSubmision);
                                bOMHandler.HandleVeryFirstBOMApplication(firstSubmissionFromBOM, out string FuBomRecno);
                                FuBOmRecnoFromFirstSub = FuBomRecno;

                                //One submission is only missing in eDoc then attach all submissions (range from/to) with the same FuBomRecno
                                if (submissions.Count == 1)
                                {
                                    SetFUBOMRecnoOnAllUnAttached(submissions, FuBOmRecnoFromFirstSub);
                                    listViewItem.ListView.Items.Clear();
                                    MessageBox.Show("The very first submission was created ✅. A FumBOMCase were created along with its first submission and all further submissions were attached to this one.");
                                    break;
                                }
                            }
                            //Attach all further submissions on submission one - Just persist it in DB
                            else
                            {
                                if (!string.IsNullOrEmpty(FuBOmRecnoFromFirstSub))
                                {

                                    BOM.BOMSagsbehandling.IndsendelseType submission = GetApplication(submissions[i]);
                                    //Persist previous submissions that are not persisted in eDOc
                                    bOMHandler.HandleFurtherSubmissions(submission, FuBOmRecnoFromFirstSub);
                                    succeded.Add(true);
                                }
                            }
                        }

                        if (succeded.Any() && succeded.All(IsCompleted => IsCompleted == true))
                        {
                            //Attach all forwarder submissions than the previous submissions with the same FuBomRecno
                            SetFUBOMRecnoOnAllUnAttached(submissions, FuBOmRecnoFromFirstSub);
                            if (listViewItem.ListView != null)
                            {
                                listViewItem.ListView.Items.Clear();
                            }
                            MessageBox.Show("Succesfully created ✅. The BOM Case can be seen in the dialog \"BOM-cases for creation\" in eDoc");
                            BomCaseNumberValLabel.Text = "";
                            SubmittedRangeValLabel.Text = "";
                        }
                    }
                    else
                    {
                        MessageBox.Show("Nothing happens. You need to select items in the list");
                    }
                }
                catch (Exception ex)
                {
                    //ROLLBACK everything
                    bOMHandler.DeleteFuBomCase(submissions.First().caseId.ToString());
                    submissions.ForEach(delegate (FUBOMSubmission sub)
                    {
                        bOMHandler.DeleteSubmission(sub.applicationId.ToString());
                    });
                    bOMHandler.ResetFUBOMRecnoOnAllUnAttachedSubmissions(submissions.First().caseId.ToString());

                    //Logging
                    Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOMApplicationDesktopApp.Handler", "FuBOM",
                                   $"Error on manual creation of the case:\n {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);


                    MessageBox.Show("Everyhting was rollbacked ↩️. Go to the EventViewer and look at the Logname FuBOM.");
                }
            }
        }

        private bool IsAnyUnChecked()
        {
            bool IsAnyUnChecked = false;

            foreach (ListViewItem item in listView1.Items)
            {
                if (item.Checked != true)
                {
                    IsAnyUnChecked = true;
                    break;
                }
            }
            return IsAnyUnChecked;
        }

        public static void SetFUBOMRecnoOnAllUnAttached(List<FUBOMSubmission> submissions, string FuBOmRecnoFromFirstSub)
        {
            string xmlQuery = $@"<operation>
                                    <QUERYDESC NAMESPACE='SIRIUS' ENTITY='FuBOMSubmission' DATASETFORMAT='XML'>
                                      <RESULTFIELDS>
                                        <METAITEM TAG='Recno'>Recno</METAITEM>
                                      </RESULTFIELDS>
                                      <CRITERIA>
                                        <METAITEM NAME='CaseId' OPERATOR='='>
                                          <VALUE>{submissions[0].caseId}</VALUE>
                                        </METAITEM>
                                        <METAITEM NAME='ToBOMCase' OPERATOR='IS NULL' ANDOR='AND'>
                                          </METAITEM>
                                      </CRITERIA>
                                    </QUERYDESC>
                                  </operation>";

            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);
            XDocument doc = XDocument.Parse(result);

            foreach (var record in doc.Root.Elements())
            {
                xmlQuery = xmlQuery = $@"<operation>
                                <UPDATESTATEMENT NAMESPACE='SIRIUS' ENTITY='FuBOMSubmission' PRIMARYKEYVALUE='{record.Element("Recno").Value}'>
                                  <METAITEM NAME='TOBOMCase'>
                                    <VALUE>{FuBOmRecnoFromFirstSub}</VALUE>
                                  </METAITEM>
                                </UPDATESTATEMENT>
                              </operation>";

                Fujitsu.eDoc.Core.Common.ExecuteSingleAction(xmlQuery);
            }
        }

        private void BOMCaseIDtextBox_TextChanged(object sender, EventArgs e)
        {
            bool isSucceded = Guid.TryParse(BOMCaseIDtextBox.Text, out Guid guid);

            if (isSucceded)
            {
                BOMCaseIDtextBox.BackColor = Color.Green;
            }
            else
            {
                BOMCaseIDtextBox.BackColor = Color.Red;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((ServerURLComboBox.SelectedItem != null && ServerURLComboBox.SelectedItem.ToString() != "<none>") && (CertificatecomboBox.SelectedItem != null && CertificatecomboBox.SelectedItem.ToString() != "<none>"))
            {
                CertificateTestbtn.Enabled = true;
            }
            else
            {
                CertificateTestbtn.Enabled = false;
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DisplayCertificates();

        }

        private void DisplayCertificates()
        {
            var store = new X509Store(StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates;

            CertificatecomboBox.Items.Add("<none>");
            foreach (var certificate in certificates)
            {
                var friendlyName = certificate.FriendlyName;
                if (!string.IsNullOrEmpty(friendlyName)) CertificatecomboBox.Items.Add(friendlyName);
            }
            store.Close();

        }

        private void CertificateTestbtn_Click(object sender, EventArgs e)
        {

            var certName = CertificatecomboBox.SelectedItem.ToString();
            var url = ServerURLComboBox.SelectedItem.ToString();

            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                X509FindType CertificateFindType = X509FindType.FindBySubjectName;
                string CertificateFindValue = certName;

                WSHttpBinding binding = new WSHttpBinding(SecurityMode.Transport);
                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
                binding.MaxReceivedMessageSize = 2147483647;

                EndpointAddress remoteAddress = new EndpointAddress(new Uri("about:blank"));
                if (url.Contains("PROD"))
                {
                    remoteAddress = new EndpointAddress(string.Format("{0}KonfigurationV6.svc", url.Replace("PROD:", "")));
                }
                else
                {
                    remoteAddress = new EndpointAddress(string.Format("{0}KonfigurationV6.svc", url.Replace("TEST:", "")));
                }

                Fujitsu.eDoc.BOM.BOMKonfigurationV6.KonfigurationClient konf = new Fujitsu.eDoc.BOM.BOMKonfigurationV6.KonfigurationClient(binding, remoteAddress);
                konf.ChannelFactory.Credentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, CertificateFindType, CertificateFindValue);
                Fujitsu.eDoc.BOM.BOMKonfigurationV6.KonfigurationDataTransferBasisKonfiguration basisKonf = konf.GetBasisKonfiguration();
                MessageBox.Show("Certifcate is OK");
                BOMCaseIDtextBox.Enabled = true;
                SearchBomCasebtn.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Certifcate is not OK");
                SearchBomCasebtn.Enabled = false;
                BOMCaseIDtextBox.Enabled = false;
                BOMCaseIDtextBox.BackColor = default(Color);
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOMApplicationDesktopApp.Handler", "FuBOM",
                               $"Certificate issue occured:\n {ex}", System.Diagnostics.EventLogEntryType.Error);
            }

        }

        private void SearchBomCasebtn_Click(object sender, EventArgs e)
        {
            if (BOMCaseIDtextBox.BackColor == Color.Green)
            {
                var bomCaseID = BOMCaseIDtextBox.Text.Trim();
                List<FUBOMSubmission> submissions = GetFuBOMSubmissions(bomCaseID);

                if (submissions.Any())
                {
                    var IsExisting = submissions.Contains(new FUBOMSubmission { submissionNummer = 1 });

                    // Missing the very first submission in eDoc
                    if (!IsExisting)
                    {
                        int lowestSubmittedNumber = submissions.Min(x => x.submissionNummer);
                        FUBOMSubmission submission = submissions.Find(sub => sub.submissionNummer == lowestSubmittedNumber);
                        var application = GetApplication(submission);

                        if (application != null)
                        {
                            BomCaseNumberValLabel.Text = application.BOMSag.BomNummer;

                            if (submissions.Count == 1)
                            {
                                SubmittedRangeValLabel.Text = $"{lowestSubmittedNumber} (No Range)";
                            }
                            else
                            {
                                SubmittedRangeValLabel.Text = $"{lowestSubmittedNumber} - {submissions.Max(sub => sub.submissionNummer)}";
                            }

                            List<FUBOMSubmission> previousSubmissions = new List<FUBOMSubmission>();

                            foreach (var previousSubmission in application.TidligereIndsendelserListe)
                            {
                                previousSubmissions.Add(new FUBOMSubmission
                                {
                                    applicationId = Guid.Parse(previousSubmission.IndsendelseID),
                                    caseId = Guid.Parse(previousSubmission.BOMSagID),
                                    submissionTime = previousSubmission.IndsendelseDatoTid,
                                    submissionNummer = previousSubmission.IndsendelseLoebenr,
                                    transferStatus = BOMCaseTransferStatusEnum.Pending
                                });
                            }
                            var orderedPreviousSubmissions = previousSubmissions.OrderBy(sub => sub.submissionNummer).ToList();
                            DisplayInFileGrid(orderedPreviousSubmissions);
                        }

                    }
                    else
                    {
                        MessageBox.Show("The very first submission exists in the DB. No recovery needed!");
                    }
                }
                else
                {
                    MessageBox.Show("No result found.");
                }
            }
            else
            {
                MessageBox.Show("Invalid GUID");
            }
        }

        private void DisplayInFileGrid(List<FUBOMSubmission> previousSubmissions)
        {
            if (listView1.Items.Count == 0)
            {
                foreach (FUBOMSubmission item in previousSubmissions)
                {
                    listViewItem = new ListViewItem(item.submissionNummer.ToString());
                    listViewItem.Tag = item;
                    listViewItem.SubItems.Add(item.applicationId.ToString());
                    listViewItem.SubItems.Add(item.caseId.ToString());
                    listViewItem.SubItems.Add(item.submissionTime.ToString());
                    listView1.Items.Add(listViewItem);
                }
            }
            else
            {
                MessageBox.Show("The result is already shown in the listView.");
            }
        }


        public BOM.BOMSagsbehandling.IndsendelseType GetApplication(FUBOMSubmission submission)
        {
            X509FindType CertificateFindType = X509FindType.FindBySubjectName;
            string CertificateFindValue = (string)CertificatecomboBox.SelectedItem;

            BOM.BOMSagsbehandling.SagsbehandlingServiceClient sag = GetSagsbehandlingServiceClient();
            sag.ChannelFactory.Credentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, CertificateFindType, CertificateFindValue);

            BOM.BOMSagsbehandling.IndsendelseType indsendelse = sag.LaesAnsoegning(submission.applicationId.ToString(), out ServiceMaalStatistikType ServiceMaalStatistik);
            return indsendelse;
        }

        public BOM.BOMSagsbehandling.SagsbehandlingServiceClient GetSagsbehandlingServiceClient()
        {
            X509FindType CertificateFindType = X509FindType.FindBySubjectName;
            string CertificateFindValue = (string)CertificatecomboBox.SelectedItem;

            var url = ServerURLComboBox.SelectedItem.ToString();

            if (url.Contains("PROD"))
            {
                url = url.Replace("PROD:", "");
            }
            else
            {
                url = url.Replace("TEST:", "");
            }

            WSHttpBinding binding = new WSHttpBinding(SecurityMode.Transport);
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.MaxReceivedMessageSize = 2147483647;

            EndpointAddress remoteAddress = new EndpointAddress(string.Format("{0}SagsbehandlingV5.svc", url));

            BOM.BOMSagsbehandling.SagsbehandlingServiceClient sag = new BOM.BOMSagsbehandling.SagsbehandlingServiceClient(binding, remoteAddress);
            sag.ChannelFactory.Credentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, CertificateFindType, CertificateFindValue);

            return sag;
        }


        private static List<FUBOMSubmission> GetFuBOMSubmissions(string bomCaseID)
        {
            string xmlQuery = Fujitsu.eDoc.Core.Common.GetResourceXml("GetBOMSubmissions.xml", "Fujitsu.eDoc.BOMApplicationDesktopApp.XML", Assembly.GetExecutingAssembly());
            xmlQuery = xmlQuery.Replace("#CaseId#", bomCaseID);
            string result = Fujitsu.eDoc.Core.Common.ExecuteQuery(xmlQuery);
            var xDoc = XDocument.Parse(result);
            List<FUBOMSubmission> submissions = new List<FUBOMSubmission>();
            foreach (var record in xDoc.Root.Elements())
            {
                var recno = record.Element("Recno").Value;
                var submissionTime = DateTime.Parse(record.Element("SubmissionTime").Value);
                var submissionNummer = int.Parse(record.Element("SubmissionNummer").Value);
                var applicationId = Guid.Parse(record.Element("ApplicationId").Value);
                var caseId = Guid.Parse(record.Element("CaseId").Value);
                var toBOMCase = record.Element("ToBOMCase").Value;
                BOMCaseTransferStatusEnum transferStatus = (BOMCaseTransferStatusEnum)Enum.Parse(typeof(BOMCaseTransferStatusEnum), record.Element("TransferStatus").Value);
                var insertDate = DateTime.Parse(record.Element("InsertDate").Value);
                var errorMessage = record.Element("ErrorMessage").Value;
                FUBOMSubmission fUBOMSubmission = new FUBOMSubmission(recno, submissionTime, submissionNummer, applicationId, caseId, toBOMCase, transferStatus, insertDate, errorMessage);
                submissions.Add(fUBOMSubmission);
            }

            return submissions;
        }

        private void ClerarListBtn_Click(object sender, EventArgs e)
        {

            if (listViewItem.ListView != null)
            {
                listViewItem.ListView.Items.Clear();
                BomCaseNumberValLabel.Text = string.Empty;
                SubmittedRangeValLabel.Text = string.Empty;

            };
        }

        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Checked & e.Item.Index == 0)
                MessageBox.Show("The very first submission creates a FUBOMCase along with its first submission and all further submissions will be attached to this one");
        }

        private void ServerURLComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ServerURLComboBox.SelectedItem.ToString() == "<none>")
            {
                CertificatecomboBox.Enabled = false;
                MessageBox.Show("Please select a right one");
            }
            else
            {
                CertificatecomboBox.Enabled = true;
            }
        }
    }
}
