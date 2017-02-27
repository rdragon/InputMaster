using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Linq;

namespace InputMaster
{
  public static class Cipher
  {
    private const int Keysize = 256;
    private const string Identifier = "TextEditor\n";

    public static void Encrypt(FileInfo file, string plainText, string passPhrase)
    {
      var saltStringBytes = Generate256BitsOfRandomEntropy();
      var ivStringBytes = Generate256BitsOfRandomEntropy();
      using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, Config.CipherDerivationIterations))
      {
        var keyBytes = password.GetBytes(Keysize / 8);
        using (var symmetricKey = new RijndaelManaged())
        {
          symmetricKey.BlockSize = 256;
          symmetricKey.Mode = CipherMode.CBC;
          symmetricKey.Padding = PaddingMode.PKCS7;
          using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
          {
            using (var fileStream = file.Open(FileMode.Create, FileAccess.Write))
            {
              fileStream.Write(saltStringBytes, 0, saltStringBytes.Length);
              fileStream.Write(ivStringBytes, 0, ivStringBytes.Length);
              var plainTextBytes = Encoding.UTF8.GetBytes(Identifier + plainText);
              fileStream.Write(BitConverter.GetBytes(plainTextBytes.Length), 0, 4);
              using (var cryptoStream = new CryptoStream(fileStream, encryptor, CryptoStreamMode.Write))
              {
                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                cryptoStream.FlushFinalBlock();
              }
            }
          }
        }
      }
    }

    public static string Decrypt(FileInfo file, string passPhrase)
    {
      var saltStringBytes = new byte[32];
      var ivStringBytes = new byte[32];
      var lengthBytes = new byte[4];
      try
      {
        using (var fileStream = file.Open(FileMode.Open, FileAccess.Read))
        {
          Helper.RequireEqual(fileStream.Read(saltStringBytes, 0, 32), "read bytes", 32);
          Helper.RequireEqual(fileStream.Read(ivStringBytes, 0, 32), "read bytes", 32);
          Helper.RequireEqual(fileStream.Read(lengthBytes, 0, 4), "read bytes", 4);
          using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, Config.CipherDerivationIterations))
          {
            var keyBytes = password.GetBytes(Keysize / 8);
            using (var symmetricKey = new RijndaelManaged())
            {
              symmetricKey.BlockSize = 256;
              symmetricKey.Mode = CipherMode.CBC;
              symmetricKey.Padding = PaddingMode.PKCS7;
              using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
              {
                using (var cryptoStream = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read))
                {
                  var plainTextByteCount = BitConverter.ToInt32(lengthBytes, 0);
                  var plainTextBytes = new byte[plainTextByteCount];
                  Helper.RequireEqual(cryptoStream.Read(plainTextBytes, 0, plainTextByteCount), "read bytes", plainTextByteCount);
                  string s;
                  try
                  {
                    s = Encoding.UTF8.GetString(plainTextBytes, 0, plainTextByteCount);
                  }
                  catch (ArgumentException)
                  {
                    return null;
                  }
                  if (s.StartsWith(Identifier))
                  {
                    return s.Substring(Identifier.Length);
                  }
                  else
                  {
                    return null;
                  }
                }
              }
            }
          }
        }
      }
      catch (CryptographicException)
      {
        // A hack to catch a CryptographicException with message "Padding is invalid and can't be removed" when password is invalid. It looks like the exception is thrown during a dispose. However, manually disposing all IDisposables doesn't throw anything, and the exception is still only raised when "return null;" is called. Strange.
        return null;
      }
    }

    private static byte[] Generate256BitsOfRandomEntropy()
    {
      var randomBytes = new byte[32];
      using (var rngCsp = new RNGCryptoServiceProvider())
      {
        rngCsp.GetBytes(randomBytes);
      }
      return randomBytes;
    }
  }
}