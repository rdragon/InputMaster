using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace InputMaster.TextEditor
{
  public class Cipher : ICipher
  {
    private static readonly int Keysize = 256;
    private static readonly string Identifier = "TextEditor\n";
    private readonly string Password;

    public Cipher()
    {
      Password = Env.Config.KeyFile == null ? "" : File.ReadAllText(Env.Config.KeyFile.FullName);
      if (Env.Config.AskForPassword || string.IsNullOrEmpty(Password))
      {
        Password += Helper.GetStringLine("Password", isPassword: true) ?? "";
      }
    }

    public Cipher(string password)
    {
      Helper.ForbidNull(password, nameof(password));
      Password = password;
    }

    public Task EncryptAsync(string file, string plaintext)
    {
      return Task.Run(() => Encrypt(file, plaintext));
    }

    private void Encrypt(string file, string plaintext)
    {
      var saltStringBytes = Generate256BitsOfRandomEntropy();
      var ivStringBytes = Generate256BitsOfRandomEntropy();
      using (var key = new Rfc2898DeriveBytes(Password, saltStringBytes, Env.Config.CipherDerivationIterations))
      using (var symmetricKey = new RijndaelManaged())
      {
        var keyBytes = key.GetBytes(Keysize / 8);
        symmetricKey.BlockSize = 256;
        symmetricKey.Mode = CipherMode.CBC;
        symmetricKey.Padding = PaddingMode.PKCS7;
        using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
        using (var fileStream = File.Open(file, FileMode.Create, FileAccess.Write))
        {
          fileStream.Write(saltStringBytes, 0, saltStringBytes.Length);
          fileStream.Write(ivStringBytes, 0, ivStringBytes.Length);
          var plaintextBytes = Encoding.UTF8.GetBytes(Identifier + plaintext);
          fileStream.Write(BitConverter.GetBytes(plaintextBytes.Length), 0, 4);
          using (var cryptoStream = new CryptoStream(fileStream, encryptor, CryptoStreamMode.Write))
          {
            cryptoStream.Write(plaintextBytes, 0, plaintextBytes.Length);
            cryptoStream.FlushFinalBlock();
          }
        }
      }
    }

    public Task<string> DecryptAsync(string file)
    {
      return Task.Run(() =>
      {
        try
        {
          return Decrypt(file);
        }
        catch (Exception ex)
        {
          throw new DecryptionFailedException($"Could not decrypt file '{file}' possibly due to invalid password.", ex);
        }
      });
    }

    private string Decrypt(string file)
    {
      var saltStringBytes = new byte[32];
      var ivStringBytes = new byte[32];
      var lengthBytes = new byte[4];
      using (var fileStream = File.Open(file, FileMode.Open, FileAccess.Read))
      {
        Helper.RequireEqual(fileStream.Read(saltStringBytes, 0, 32), "read bytes", 32);
        Helper.RequireEqual(fileStream.Read(ivStringBytes, 0, 32), "read bytes", 32);
        Helper.RequireEqual(fileStream.Read(lengthBytes, 0, 4), "read bytes", 4);
        using (var key = new Rfc2898DeriveBytes(Password, saltStringBytes, Env.Config.CipherDerivationIterations))
        using (var symmetricKey = new RijndaelManaged())
        {
          var keyBytes = key.GetBytes(Keysize / 8);
          symmetricKey.BlockSize = 256;
          symmetricKey.Mode = CipherMode.CBC;
          symmetricKey.Padding = PaddingMode.PKCS7;
          using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
          using (var cryptoStream = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read))
          {
            var plaintextByteCount = BitConverter.ToInt32(lengthBytes, 0);
            var plaintextBytes = new byte[plaintextByteCount];
            Helper.RequireEqual(cryptoStream.Read(plaintextBytes, 0, plaintextByteCount), "read bytes",
              plaintextByteCount);
            var s = Encoding.UTF8.GetString(plaintextBytes, 0, plaintextByteCount);
            return s.StartsWith(Identifier) ? s.Substring(Identifier.Length) : throw new FormatException();
          }
        }
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