using System;
using System.Security.Cryptography.X509Certificates;

namespace XSockets.ClientAndroid.Helpers
{
    public static class CertificateHelpers
    {
        public static X509Certificate2 GetCertificateFromStore(string certName, StoreLocation location = StoreLocation.LocalMachine)
        {
            var store = new X509Store(location);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates;
                var currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                var signingCert = currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certName, false);
                return signingCert.Count == 0 ? null : signingCert[0];
            }
            finally
            {
                store.Close();
            }
        }
    }
}
