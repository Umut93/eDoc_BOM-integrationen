using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace Fujitsu.eDoc.BOM
{
    public class BOMCaller
    {
        private static BOMKonfigurationV6.KonfigurationClient GetKonfigurationClient()
        {
            X509FindType CertificateFindType = X509FindType.FindBySubjectName;
            string CertificateFindValue = GetCertificateName();


            WSHttpBinding binding = new WSHttpBinding(SecurityMode.Transport);
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.MaxReceivedMessageSize = 2147483647;

            EndpointAddress remoteAddress = new EndpointAddress(string.Format("{0}KonfigurationV6.svc", GetBOMServer()));

            BOMKonfigurationV6.KonfigurationClient konf = new BOMKonfigurationV6.KonfigurationClient(binding, remoteAddress);
            konf.ChannelFactory.Credentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, CertificateFindType, CertificateFindValue);

            return konf;
        }

        private static BOMBeskedfordeler.BeskedfordelerClient GetBeskedfordelerClient()
        {
            X509FindType CertificateFindType = X509FindType.FindBySubjectName;
            string CertificateFindValue = GetCertificateName();

            WSHttpBinding binding = new WSHttpBinding(SecurityMode.Transport);
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.MaxReceivedMessageSize = 2147483647;

            EndpointAddress remoteAddress = new EndpointAddress(string.Format("{0}BeskedfordelerV2.svc", GetBOMServer()));

            BOMBeskedfordeler.BeskedfordelerClient client = new BOMBeskedfordeler.BeskedfordelerClient(binding, remoteAddress);
            client.ChannelFactory.Credentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, CertificateFindType, CertificateFindValue);

            return client;
        }

        public static BOMSagsbehandling.SagsbehandlingServiceClient GetSagsbehandlingServiceClient()
        {
            X509FindType CertificateFindType = X509FindType.FindBySubjectName;
            string CertificateFindValue = GetCertificateName();

            WSHttpBinding binding = new WSHttpBinding(SecurityMode.Transport);
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.MaxReceivedMessageSize = 2147483647;

            EndpointAddress remoteAddress = new EndpointAddress(string.Format("{0}SagsbehandlingV5.svc", GetBOMServer()));

            BOMSagsbehandling.SagsbehandlingServiceClient sag = new BOMSagsbehandling.SagsbehandlingServiceClient(binding, remoteAddress);
            sag.ChannelFactory.Credentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, CertificateFindType, CertificateFindValue);

            return sag;
        }

        private static BOMSagStatus.SagStatusServiceClient GetSagStatusServiceClient()
        {
            X509FindType CertificateFindType = X509FindType.FindBySubjectName;
            string CertificateFindValue = GetCertificateName();

            WSHttpBinding binding = new WSHttpBinding(SecurityMode.Transport);
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.MaxReceivedMessageSize = 2147483647;

            EndpointAddress remoteAddress = new EndpointAddress(string.Format("{0}SagStatusV2.svc", GetBOMServer()));

            BOMSagStatus.SagStatusServiceClient client = new BOMSagStatus.SagStatusServiceClient(binding, remoteAddress);
            client.ChannelFactory.Credentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, CertificateFindType, CertificateFindValue);

            return client;
        }

        public static BOMKonfigurationV6.KonfigurationDataTransferBasisKonfiguration GetBaseConfiguration()
        {
            BOMKonfigurationV6.KonfigurationClient konf = GetKonfigurationClient();

            BOMKonfigurationV6.KonfigurationDataTransferBasisKonfiguration basisKonf = konf.GetBasisKonfiguration();
            return basisKonf;
        }

        public static BOMKonfigurationV6.KonfigurationDataTransferRegelKonfiguration GetRegelConfiguration()
        {
            BOMKonfigurationV6.KonfigurationClient konf = GetKonfigurationClient();

            BOMKonfigurationV6.RegelKonfigurationSpecification spec = new BOMKonfigurationV6.LatestPublishedRegelKonfigurationSpecification();
            BOMKonfigurationV6.KonfigurationDataTransferRegelKonfiguration regelKonf = konf.GetRegelKonfiguration(spec);
            return regelKonf;
        }

        public static BOMBeskedfordeler.HentBeskederResult GetMessages(Guid? HighWatermark)
        {
            BOMBeskedfordeler.BeskedfordelerClient client = GetBeskedfordelerClient();
            BOMBeskedfordeler.HentBeskederRequest request = new BOMBeskedfordeler.HentBeskederRequest();
            request.MyndighedCvr = GetMunicipalityCVR();
            request.HighWatermark = HighWatermark;

            BOMBeskedfordeler.HentBeskederResult result = client.HentBeskeder(request);
            return result;
        }

        public static Guid? GetLatestHighWaterMark()
        {
            BOMBeskedfordeler.BeskedfordelerClient client = GetBeskedfordelerClient();
            BOMBeskedfordeler.HentBeskederRequest request = new BOMBeskedfordeler.HentBeskederRequest();
            request.MyndighedCvr = GetMunicipalityCVR();

            BOMBeskedfordeler.HentBeskederResult result = client.HentBeskeder(request);

            return result.HighWatermark;
        }
        public static BOMSagStatus.SagStatusDetaljeType GetStatus(string SagId, string SagStatusId)
        {
            BOMSagStatus.SagStatusServiceClient client = GetSagStatusServiceClient();

            BOMSagStatus.SagStatusDetaljeType result = client.LaesSagStatus(SagId, SagStatusId);
            return result;
        }

        public static string[] GetApplicationOverview(DateTime fromDate, string[] SagOmraader)
        {
            BOMSagsbehandling.SagsbehandlingServiceClient sag = GetSagsbehandlingServiceClient();

            string MyndighedCvr = GetMunicipalityCVR();
            string[] SagType = new string[] { };
            string[] oversigt = sag.LaesAnsoegningOversigt(fromDate, DateTime.MaxValue, MyndighedCvr, SagOmraader, SagType);

            return oversigt;
        }

        public static FuIndsendelseType GetApplication(string ApplicationId)
        {
            X509FindType CertificateFindType = X509FindType.FindBySubjectName;
            string CertificateFindValue = GetCertificateName();

            BOMSagsbehandling.SagsbehandlingServiceClient sag = GetSagsbehandlingServiceClient();
            sag.ChannelFactory.Credentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, CertificateFindType, CertificateFindValue);

            BOM.BOMSagsbehandling.ServiceMaalStatistikType ServiceMaalStatistik;
            BOMSagsbehandling.IndsendelseType indsendelse = sag.LaesAnsoegning(ApplicationId, out ServiceMaalStatistik);

            FuIndsendelseType fuIndelseType = new FuIndsendelseType(ServiceMaalStatistik, indsendelse);

            return fuIndelseType;
        }

        public static void DownloadFile(Uri documentURI, string LocalFilePath)
        {
            BOMSagsbehandling.SagsbehandlingServiceClient sag = GetSagsbehandlingServiceClient();

            BOMSagsbehandling.FederationTokenPart[] tokenParts = sag.GetSecurityToken();
            if (tokenParts.Length > 0)
            {
                BOMSagsbehandling.FederationTokenPart tokenPart = tokenParts[0];

                CookieAwareWebClient wc = new CookieAwareWebClient();
                foreach (var tp in tokenParts)
                {
                    wc.CookieContainer.Add(new Cookie(tp.Name, tp.Value)
                    {
                        Domain = documentURI.Host,
                        Secure = true,
                    });
                }

                System.Security.Principal.WindowsImpersonationContext wic = System.Security.Principal.WindowsIdentity.Impersonate(IntPtr.Zero);
                try
                {
                    wc.DownloadFile(documentURI, LocalFilePath);
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    wic.Undo();
                }
            }
            else
            {
                throw new ApplicationException("Cannot get BOM security token");
            }
        }

        public static void UploadFile(Uri documentURI, string LocalFilePath)
        {
            BOMSagsbehandling.SagsbehandlingServiceClient sag = GetSagsbehandlingServiceClient();

            BOMSagsbehandling.FederationTokenPart[] tokenParts = sag.GetSecurityToken();
            if (tokenParts.Length > 0)
            {
                BOMSagsbehandling.FederationTokenPart tokenPart = tokenParts[0];

                CookieAwareWebClient wc = new CookieAwareWebClient();
                foreach (var tp in tokenParts)
                {
                    wc.CookieContainer.Add(new Cookie(tp.Name, tp.Value)
                    {
                        Domain = documentURI.Host,
                        Secure = true,
                    });
                }

                System.IO.FileStream s = System.IO.File.OpenRead(LocalFilePath);
                System.IO.Stream wcs = wc.OpenWrite(documentURI, "PUT");
                int buffersize = 65536;
                byte[] filedata = new byte[buffersize];
                int bytesread = s.Read(filedata, 0, buffersize);
                while (bytesread > 0)
                {
                    wcs.Write(filedata, 0, bytesread);
                    bytesread = s.Read(filedata, 0, buffersize);
                }
                wcs.Flush();
                wcs.Close();
                s.Close();


                //wc.UploadData(documentURI, "PUT", filedata);

                //wc.UploadFile(documentURI, LocalFilePath);
            }
            else
            {
                throw new ApplicationException("Cannot get BOM security token");
            }
        }

        public static void Reply(BOMSagsbehandling.BesvarelseType reply)
        {
            BOMSagsbehandling.SagsbehandlingServiceClient sag = GetSagsbehandlingServiceClient();

            try
            {
                sag.BesvarAnsoegning(reply);
            }
            catch (System.ServiceModel.FaultException<Fujitsu.eDoc.BOM.BOMSagsbehandling.TransaktionFaultType> faultEx)
            {
                string Detail = faultEx.Detail.Detail.ErrorMessage;
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.BOMCaller", "FuBOM",
                                string.Format("Reply: Message:\n{0}\nException:\n{1}", Detail, faultEx.ToString()), System.Diagnostics.EventLogEntryType.Error);

                throw new SI.Util.BizInfoException(Detail);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public static void UpdateServiceGoal(BOMSagsbehandling.OpdaterType opdater)
        {
            BOMSagsbehandling.SagsbehandlingServiceClient sag = GetSagsbehandlingServiceClient();

            sag.OpdaterSagServiceMaal(opdater);
        }

        private static string GetCertificateName()
        {
            //return "Test client for BoM (funktionscertifikat)";
            string name = Fujitsu.eDoc.Core.eDocSettingInformation.GetSettingValueFromeDoc("fujitsu", "bomcertificateissuedto");
            return name;
        }

        public static string GetMunicipalityCVR()
        {
            //return "29189846";
            string cvr = Fujitsu.eDoc.Core.eDocSettingInformation.GetSettingValueFromeDoc("fujitsu", "municipalitycvr");
            return cvr;
        }

        public static string GetBOMServer()
        {
            //return "https://service-es.bygogmiljoe.dk/";
            string url = Fujitsu.eDoc.Core.eDocSettingInformation.GetSettingValueFromeDoc("fujitsu", "bomwebserviceurl");
            if (!url.EndsWith("/"))
            {
                url = url + "/";
            }
            return url;
        }

        public class CookieAwareWebClient : WebClient
        {
            public CookieContainer CookieContainer = new CookieContainer();

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                HttpWebRequest webRequest = request as HttpWebRequest;
                if (webRequest != null)
                {
                    webRequest.CookieContainer = CookieContainer;
                }
                return request;
            }
        }
    }
}
