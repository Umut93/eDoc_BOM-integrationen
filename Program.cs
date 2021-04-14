using Fujitsu.eDoc.BOM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Fujitsu.eDoc.ServiceMaalTest
{
    class Program
    {
        static void Main(string[] args)
        {
            BOM.BOMSagsbehandling.SagsbehandlingServiceClient sag = GetSagsbehandlingServiceClientTest();

            string[] appIds = sag.LaesAnsoegningOversigt(new DateTime(2015, 1, 1), DateTime.Now, "29189447", new string[] { "byg", "kultur", "miljoe", "virkmiljoe" }, new string[] { });
            StringBuilder sb = new StringBuilder();

            try
            {
                using (StreamWriter writer = new StreamWriter(@"C:\Users\denumk\Desktop\Test.txt", false, Encoding.UTF8, 65536))
                {
                    foreach (var appID in appIds)
                    {
                        FuIndsendelseType app = GetApplicationTest(appID);

                        if (app.IndsendelseType.IndsendelseLoebenr == 1)
                        {
                            sb.AppendFormat("--------START---------{0}", Environment.NewLine);
                            sb.AppendFormat("BOMSagID: {0}{1}", app.IndsendelseType.BOMSag.BOMSagID, Environment.NewLine);
                            sb.AppendFormat("BomNummer: {0}{1}", app.IndsendelseType.BOMSag.BomNummer, Environment.NewLine);
                            sb.AppendFormat("IndsendelseLoebenr: {0}{1}", app.IndsendelseType.IndsendelseLoebenr, Environment.NewLine);
                            sb.AppendFormat("IndsendelseID: {0}{1}", app.IndsendelseType.IndsendelseID, Environment.NewLine);
                            sb.AppendFormat("IndsendelseDatoTid: {0}{1}", app.IndsendelseType.IndsendelseDatoTid, Environment.NewLine);
                            if (app.ServiceMaalStatistikType.ServiceMaal != null)
                            {
                                sb.AppendFormat("--------Servicemål START---------{0}", Environment.NewLine);
                                sb.AppendFormat("Fritagelsesbegrundelse VisningsNavn: {0}{1}", app.ServiceMaalStatistikType.Fritagelsesbegrundelse.VisningsNavn, Environment.NewLine);
                                sb.AppendFormat("Fritagelsesbegrundelse Kode: {0}{1}", app.ServiceMaalStatistikType.Fritagelsesbegrundelse.Kode, Environment.NewLine);
                                sb.AppendFormat("Fritagelsesbegrundelse InterntNavn: {0}{1}", app.ServiceMaalStatistikType.Fritagelsesbegrundelse.InterntNavn, Environment.NewLine);
                                sb.AppendFormat("ServiceMål Dage: {0}{1}", app.ServiceMaalStatistikType.ServiceMaal.Dage, Environment.NewLine);
                                sb.AppendFormat("ServiceMål InterntNavn: {0}{1}", app.ServiceMaalStatistikType.ServiceMaal.InterntNavn, Environment.NewLine);
                                sb.AppendFormat("ServiceMål VisningsNavn: {0}{1}", app.ServiceMaalStatistikType.ServiceMaal.VisningsNavn, Environment.NewLine);
                                sb.AppendFormat("ServiceMål Kode: {0}{1}", app.ServiceMaalStatistikType.ServiceMaal.Kode, Environment.NewLine);
                                sb.AppendFormat("SagsbehandlingForbrugtDage: {0}{1}", app.ServiceMaalStatistikType.Statistik.SagsbehandlingForbrugtDage, Environment.NewLine);
                                sb.AppendFormat("SagsbehandlingForbrugtDageSpecified : {0}{1}", app.ServiceMaalStatistikType.Statistik.SagsbehandlingForbrugtDageSpecified, Environment.NewLine);
                                sb.AppendFormat("VisitationForbrugtDage : {0}{1}", app.ServiceMaalStatistikType.Statistik.VisitationForbrugtDage, Environment.NewLine);
                                sb.AppendFormat("VisitationForbrugtDageSpecified : {0}{1}", app.ServiceMaalStatistikType.Statistik.VisitationForbrugtDageSpecified, Environment.NewLine);
                                sb.AppendFormat("--------Servicemål END---------{0}", Environment.NewLine);
                            }
                            sb.AppendFormat("--------END---------{0}", Environment.NewLine);
                            sb.AppendLine("");

                        }
                    }
                    writer.Write(sb.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
            }
        }


        public static FuIndsendelseType GetApplicationTest(string ApplicationId)
        {
            X509FindType CertificateFindType = X509FindType.FindBySubjectName;
            string CertificateFindValue = "BOM eDoc (funktionscertifikat)";

            BOM.BOMSagsbehandling.SagsbehandlingServiceClient sag = GetSagsbehandlingServiceClientTest();
            sag.ChannelFactory.Credentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, CertificateFindType, CertificateFindValue);

            BOM.BOMSagsbehandling.ServiceMaalStatistikType ServiceMaalStatistik;
            BOM.BOMSagsbehandling.IndsendelseType indsendelse = sag.LaesAnsoegning(ApplicationId, out ServiceMaalStatistik);

            FuIndsendelseType fuIndelseType = new FuIndsendelseType(ServiceMaalStatistik, indsendelse);

            return fuIndelseType;
        }


        public static BOM.BOMSagsbehandling.SagsbehandlingServiceClient GetSagsbehandlingServiceClientTest()
        {
            X509FindType CertificateFindType = X509FindType.FindBySubjectName;
            string CertificateFindValue = "BOM eDoc (funktionscertifikat)";

            WSHttpBinding binding = new WSHttpBinding(SecurityMode.Transport);
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.MaxReceivedMessageSize = 2147483647;

            EndpointAddress remoteAddress = new EndpointAddress(string.Format("{0}SagsbehandlingV5.svc", "https://service.bygogmiljoe.dk/"));

            BOM.BOMSagsbehandling.SagsbehandlingServiceClient sag = new BOM.BOMSagsbehandling.SagsbehandlingServiceClient(binding, remoteAddress);
            sag.ChannelFactory.Credentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, CertificateFindType, CertificateFindValue);

            return sag;
        }

    }
}
