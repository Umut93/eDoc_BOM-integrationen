using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Fujitsu.eDoc.BOM;
using System.Xml.Serialization;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;

namespace Fujitsu.eDoc.BOMTest
{
    public partial class Form1 : Form
    {
        private string currentPDFJobId = "";
        private string currentQueueObject = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void buttonHentBeskeder_Click(object sender, EventArgs e)
        {
            string strHighWatermark = textBoxHighWatermark.Text;
            Guid? highWatermark = null;
            if(!string.IsNullOrEmpty(strHighWatermark))
            {
                highWatermark = new Guid(strHighWatermark);
            }

            //BOM.BOMBeskedfordeler.HentBeskederResult result = BOMCaller.GetMessages(new Guid("ce807531-0a58-4762-9bcf-1f13a26d0a6e"));
            BOM.BOMBeskedfordeler.HentBeskederResult result = BOMCaller.GetMessages(highWatermark);
            Guid? newHighWatermark = result.HighWatermark;

            listBoxBeskeder.Items.Clear();
            foreach (BOM.BOMBeskedfordeler.Besked b in result.Beskeder)
            {
                
                string beskedType = "Ukendt";
                if (b is BOM.BOMBeskedfordeler.AnsoegningIndsendtBesked)
                {
                    beskedType = "AnsoegningIndsendt;" + ((BOM.BOMBeskedfordeler.AnsoegningIndsendtBesked)b).AnsoegningId;
                }
                if (b is BOM.BOMBeskedfordeler.FristOverskridelseBesked)
                {
                    beskedType = "FristOverskridelse;" + ((BOM.BOMBeskedfordeler.FristOverskridelseBesked)b).SagId;
                }
                if (b is BOM.BOMBeskedfordeler.SagStatusSkiftBesked)
                {
                    BOM.BOMBeskedfordeler.SagStatusSkiftBesked sb = ((BOM.BOMBeskedfordeler.SagStatusSkiftBesked)b);
                    beskedType = "SagStatusSkift;" + ((BOM.BOMBeskedfordeler.SagStatusSkiftBesked)b).SagId;
                }
                
                string text = string.Format("{0};{1}", b.Tidspunkt.ToString("yyyy-MM-dd HH:mm:ss"), beskedType);
                listBoxBeskeder.Items.Add(text);
            }

            textBoxHighWatermark.Text = newHighWatermark.ToString();
        }

        private void buttonGetApplications_Click(object sender, EventArgs e)
        {
            DateTime fromDate = DateTime.Now.AddDays(-int.Parse(textBoxDays.Text));
            string[] SagOmraader = new string[] { "byg", "miljoe", "kultur" };

            string[] result = BOMCaller.GetApplicationOverview(fromDate, SagOmraader);
            listBoxApplications.Items.Clear();
            for(int i=0; i<result.Length; i++)
            {
                listBoxApplications.Items.Add(result[i]);
            }
        }

        private void listBoxApplications_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxApplications.SelectedItem != null)
            {
                string ansoegningid = listBoxApplications.SelectedItem.ToString();
               FuIndsendelseType fuIndsendelse = BOMCaller.GetApplication(ansoegningid);
                string text = SerializeObject(fuIndsendelse.IndsendelseType);

                textBoxApplication.Text = text;
            }
        }

        private static string SerializeObject(BOM.BOMSagsbehandling.IndsendelseType toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }

        private void buttonCreateCase_Click(object sender, EventArgs e)
        {
            if (listBoxApplications.SelectedItem != null)
            {
                string ansoegningid = listBoxApplications.SelectedItem.ToString();
                FuIndsendelseType fuIndsendelse = BOMCaller.GetApplication(ansoegningid);

                // TEST
                //addAttachedApplicant(indsendelse);

                BOMCaseHandler.HandleBOMApplication(fuIndsendelse);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                BOM.BOMKonfigurationV6.KonfigurationDataTransferBasisKonfiguration basisKonf = BOMCaller.GetBaseConfiguration();

                listBox1.Items.Clear();
                foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferFristNotifikationProfil a in basisKonf.FristNotifikationProfiler)
                {
                    listBox1.Items.Add(a.Kode + " - " + a.Navn);
                }

                listBox2.Items.Clear();
                foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferKonfliktGruppe a in basisKonf.KonfliktGrupper)
                {
                    listBox2.Items.Add(a.Kode + " - " + a.VisningNavn);
                }

                listBox3.Items.Clear();
                foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferKort a in basisKonf.Kort)
                {
                    listBox3.Items.Add(a.Kode + " - " + a.VisningNavn);
                }

                listBox4.Items.Clear();
                foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferSagFase a in basisKonf.SagFaser)
                {
                    listBox4.Items.Add(a.Kode + " - " + a.VisningNavn);
                }

                listBox5.Items.Clear();
                foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferSagOmraade a in basisKonf.SagOmraader)
                {
                    listBox5.Items.Add(a.Kode + " - " + a.VisningNavn);
                }

                listBox6.Items.Clear();
                foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferSagStatusType a in basisKonf.SagStatusTyper)
                {
                    listBox6.Items.Add(a.Kode + " - " + a.VisningNavn);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace);
            }
        }

        private void buttonUpdateLists_Click(object sender, EventArgs e)
        {
            BOMConfigHandler.UpdateDataLists();
        }

        private void buttonGetPending_Click(object sender, EventArgs e)
        {
            BOMConfiguration cfg = BOMConfigHandler.GetBOMConfiguration();
            DateTime d = BOMCaseHandler.GetLatestBOMSubmisstionTime();

            string[] SagOmraader = cfg.GetEnabledCaseAreaNames();

            string[] result = BOMCaller.GetApplicationOverview(d, SagOmraader);
            listBoxApplications.Items.Clear();
            for (int i = 0; i < result.Length; i++)
            {
                bool exists = BOMCaseHandler.BOMSubmisstionExists(result[i]);
                string text = result[i];
                if (exists)
                {
                    text += " - EXISTS";
                }
                listBoxApplications.Items.Add(text);
            }
        }

        private void addAttachedApplicant(BOM.BOMSagsbehandling.IndsendelseType indsendelse)
        {
            indsendelse.BOMSag.TilknyttetAnsoegerListe = new Fujitsu.eDoc.BOM.BOMSagsbehandling.BOMSagTypeTilknyttetAnsoeger[2];
            indsendelse.BOMSag.TilknyttetAnsoegerListe[0] = new Fujitsu.eDoc.BOM.BOMSagsbehandling.BOMSagTypeTilknyttetAnsoeger();
            Fujitsu.eDoc.BOM.BOMSagsbehandling.BOMSagTypeTilknyttetAnsoeger a = indsendelse.BOMSag.TilknyttetAnsoegerListe[0];
            a.NavnTekst = "Peter Hansen";
            a.EmailTekst = "ph@testc.com";
            a.TelefonTekst = "12345678";
            a.AddressPostal = new BOM.BOMSagsbehandling.AddressPostalType();
            a.AddressPostal.StreetName = "Vestergade";
            a.AddressPostal.StreetBuildingIdentifier = "17B";
            a.AddressPostal.FloorIdentifier = "3";
            a.AddressPostal.SuiteIdentifier = "th";
            a.AddressPostal.PostCodeIdentifier = "9000";
            a.AddressPostal.DistrictName = "Aalborg";

            indsendelse.BOMSag.TilknyttetAnsoegerListe[1] = new Fujitsu.eDoc.BOM.BOMSagsbehandling.BOMSagTypeTilknyttetAnsoeger();
            a = indsendelse.BOMSag.TilknyttetAnsoegerListe[1];
            a.NavnTekst = "Jens Nielsen";
            a.EmailTekst = "jn@testc.com";
            a.TelefonTekst = "87654321";
            a.AddressPostal = new BOM.BOMSagsbehandling.AddressPostalType();
            a.AddressPostal.StreetName = "Østergade";
            a.AddressPostal.StreetBuildingIdentifier = "4C";
            a.AddressPostal.PostCodeIdentifier = "9000";
            a.AddressPostal.DistrictName = "Aalborg";
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            BOMCaseUpdateType c = new BOMCaseUpdateType();
            c.BOMCaseId = "e294df65-414c-4f51-9968-007d32740afb";
            c.Title = "Se vedhæftet dokument";
            c.Date = DateTime.Now;
            c.MainDocument = new BOMReplyDocument()
            {
                DocumentIdentifier = Guid.NewGuid().ToString(),
                Title = "Besvarelse",
                DocumentNumber = "1234",
                FileFullname = @"c:\temp\test.pdf",
                FileExtention = "PDF",
                FileMimeType = "application/pdf"
            };
            c.Attachments = new List<BOMReplyDocument>();
            c.Attachments.Add(new BOMReplyDocument()
            {
                DocumentIdentifier = Guid.NewGuid().ToString(),
                Title = "Bilag 1",
                DocumentNumber = "1234-1",
                FileFullname = @"c:\temp\bilag1.pdf",
                FileExtention = "PDF",
                FileMimeType = "application/pdf"
            });
            c.Attachments.Add(new BOMReplyDocument()
            {
                DocumentIdentifier = Guid.NewGuid().ToString(),
                Title = "Bilag 2",
                DocumentNumber = "1234-2",
                FileFullname = @"c:\temp\bilag2.pdf",
                FileExtention = "PDF",
                FileMimeType = "application/pdf"
            });

            BOMCaseHandler.UpdateBOMCase(c);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string FileRecno = "322481|322383";

            currentPDFJobId = PDFHelper.StartConvertFile(FileRecno);
            MessageBox.Show("Sendt til konvertering");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string FileFullname;
            PDFStatusType status = PDFHelper.GetConvertStatus(currentPDFJobId, out FileFullname);
            if (status == PDFStatusType.Success)
            {
                MessageBox.Show(status.ToString() + ": " + FileFullname);
            }
            else
            {
                MessageBox.Show(status.ToString());
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            BOMCaseUpdateType c = new BOMCaseUpdateType();
            c.BOMCaseId = "e294df65-414c-4f51-9968-007d32740afb";
            c.Title = "Test besvarelse";
            c.Date = DateTime.Now;

            c.Status = new BOMCaseUpdateStatusType();
            c.Status.SagStatusKode = "Modtaget";
            c.Status.FaseKode = "Ansoeg";
            c.Status.InitiativPligtKode = "Myndighed";
            c.Status.FristNotifikationProfilKode = "";
            c.Status.FristDato = DateTime.MinValue;

            c.Status.FristDato = DateTime.Now;

            c.Status.StatusText = "HEr er kommentaren";

            /*
            c.DocumentationRequirements = new List<BOMDocumentationRequirementType>();
            c.DocumentationRequirements.Add(new BOMDocumentationRequirementType()
                    {
                        Dokumentationstype = "Affald",
                        Kravstyrke = "Obligatorisk",
                        FaseKode = "Ansoeg"
                    });
            c.DocumentationRequirements.Add(new BOMDocumentationRequirementType()
                    {
                        Dokumentationstype = "Varmeberegning",
                        Kravstyrke = "Frivilligt",
                        FaseKode = "Afslutning"
                    });
            */

            c.MainDocument = new BOMReplyDocument()
            {
                DocumentIdentifier = Guid.NewGuid().ToString(),
                Title = "Besvarelse",
                DocumentNumber = "1234",
                FileRecno = "322481",
                DocumentRevisionRecno = "322383",
                FileExtention = "DOCX",
                FileMimeType = "application/pdf"
            };

            currentQueueObject = BOMQueueHandler.AddToQueue(c);
        }

        //private void button6_Click(object sender, EventArgs e)
        //{
        //    BOMCaseUpdateType cc;
        //    BOMQueueStatusType status = BOMQueueHandler.CheckQueuedStatus(currentQueueObject, out cc);
        //    if (status == BOMQueueStatusType.Success)
        //    {
        //        MessageBox.Show(status.ToString() + ": " + cc.MainDocument.FileFullname);
        //    }
        //    else
        //    {
        //        MessageBox.Show(status.ToString());
        //    }
        //}

        private void button7_Click(object sender, EventArgs e)
        {
            BOMQueueHandler.HandleFuBomQueues();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            string code = "Default";

            int[] deadlines = GetDeadlines(code);
        }

        private int[] GetDeadlines(string code)
        {
            int[] deadlines = new int[0];
            BOM.BOMKonfigurationV6.KonfigurationDataTransferBasisKonfiguration basisKonf = BOMCaller.GetBaseConfiguration();
            foreach (BOM.BOMKonfigurationV6.KonfigurationDataTransferFristNotifikationProfil a in basisKonf.FristNotifikationProfiler)
            {
                if (a.Kode == code)
                {
                    deadlines = a.Frister;
                }
            }

            return deadlines;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            EntityLogHelper.LogChangesTest(null);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Fujitsu.eDoc.BOM.BOMKonfigurationV6.KonfigurationClient konf = GetKonfigurationClient();
            if (konf != null)
            {
                MessageBox.Show("OK");
            }
            else
            {
                MessageBox.Show("konf is null");
            }

            Fujitsu.eDoc.BOM.BOMKonfigurationV6.KonfigurationDataTransferBasisKonfiguration basisKonf = konf.GetBasisKonfiguration();
            if (basisKonf != null)
            {
                MessageBox.Show("OK");
            }
            else
            {
                MessageBox.Show("konf is null");
            }
        }


        private static Fujitsu.eDoc.BOM.BOMKonfigurationV6.KonfigurationClient GetKonfigurationClient()
        {
            X509FindType CertificateFindType = X509FindType.FindBySubjectName;
            string CertificateFindValue = "TU GENEREL FOCES gyldig (funktionscertifikat)";
            MessageBox.Show(CertificateFindValue);

            WSHttpBinding binding = new WSHttpBinding(SecurityMode.Transport);
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.MaxReceivedMessageSize = 2147483647;
            MessageBox.Show(binding.ToString());

            EndpointAddress remoteAddress = new EndpointAddress(string.Format("{0}KonfigurationV6.svc", "https://service-es.bygogmiljoe.dk/"));
            MessageBox.Show(remoteAddress.ToString());

            Fujitsu.eDoc.BOM.BOMKonfigurationV6.KonfigurationClient konf = new Fujitsu.eDoc.BOM.BOMKonfigurationV6.KonfigurationClient(binding, remoteAddress);
            MessageBox.Show("konf created");
            konf.ChannelFactory.Credentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, CertificateFindType, CertificateFindValue);
            MessageBox.Show("certificate set");

            return konf;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            string SagId = "ecf3fa06-68e7-4a26-95e1-a3c1d9d4ab21";
            string SagStatusId = "ee6eb6b5-9de8-4595-b499-47a7f6b00ba4";
            BOM.BOMSagStatus.SagStatusDetaljeType s = BOM.BOMCaller.GetStatus(SagId, SagStatusId);

            //SagStatus.LaesSagStatusRequest request = new SagStatus.LaesSagStatusRequest();
            //request.SagStatusId = "";

            //SagStatus.SagStatusServiceClient client;
            ////client.LaesSagStatus()
        }

        private void button12_Click(object sender, EventArgs e)
        {
            BOMMessageHandler.HandleNewMessages();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            string ansoegningid = textBoxIndsendelsesId.Text;
           FuIndsendelseType fuIndsendelseType = BOMCaller.GetApplication(ansoegningid);
            string text = SerializeObject(fuIndsendelseType.IndsendelseType);

            textBoxApplication.Text = text;
        }

    }
}
