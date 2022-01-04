using Fujitsu.eDoc.BOM.CaseHandler;
using Fujitsu.eDoc.Core;
using Fujitsu.eDoc.Integrations.Datafordeler.VUR;
using System.Configuration;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Fujitsu.eDoc.BOM.Integrations
{
    internal class BFENumberHandler
    {
        internal static int GetBFENumber(BOMCase c, BOMConfiguration configuration)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            FuVUR fuVur = new FuVUR();
            string vurEndpoint = configuration.GetVURendpoint();
            X509Certificate2 certificate = GetVurCertificate(configuration);

            int bfeNumber = fuVur.GetBFEnumber(vurEndpoint, certificate, c.Ejendomsnummer, c.EjendomKommunenr);

            return bfeNumber;
        }

        private static X509Certificate2 GetVurCertificate(BOMConfiguration configuration)
        {
            string serialNumber = configuration.GetVURCertificateSerial();
            if (string.IsNullOrEmpty(serialNumber))
            {
                throw new ConfigurationErrorsException($"SerialNumber for VUR certificate not found in BOM Configuration");
            }

            X509Certificate2 certificate = CertificateManager.GetCertificate(StoreName.My, StoreLocation.LocalMachine, X509FindType.FindBySerialNumber, serialNumber);
            if (certificate == null)
            {
                throw new ConfigurationErrorsException($"VUR Certificate not found in 'LocalMachine' > 'My' with serialnumber: '{serialNumber}'");
            }

            return certificate;
        }
    }
}