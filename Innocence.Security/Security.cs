using System.Security.Cryptography;

namespace Innocence.Security
{
    public static class Security
    {
        public static byte[] RSAEncrypt(byte[] data, RSAParameters RSAKeyInfo, bool isOAEPPadding = false)
        {
            byte[] encryptedData;
            using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
            {
                RSA.ImportParameters(RSAKeyInfo);
                encryptedData = RSA.Encrypt(data, isOAEPPadding);
            }
            return encryptedData;
        }

        public static byte[] RSADecrypt(byte[] data, RSAParameters RSAKeyInfo, bool isOAEPPadding = false)
        {

            byte[] decryptedData;
            using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
            {
                RSA.ImportParameters(RSAKeyInfo);
                decryptedData = RSA.Decrypt(data, isOAEPPadding);
            }
            return decryptedData;
        }

        public static void RSAEncryptFile()
        {

        }

        public static void RSADecryptFile()
        {

        }
    }
}

