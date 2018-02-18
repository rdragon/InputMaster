using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace InputMaster.Instances
{
  /// <summary>
  /// Thread-safe.
  /// </summary>
  public class Cipher : ICipher
  {
    private readonly byte[] _key;
    private const int BlockSize = 128 / 8;
    private const int HmacSize = 256 / 8;

    public Cipher(byte[] key)
    {
      _key = key;
    }

    public byte[] Encrypt(byte[] data)
    {
      using (var aes = new AesManaged())
      {
        aes.KeySize = _key.Length * 8;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        var iv = Helper.GetRandomBytes(BlockSize);
        using (var transform = aes.CreateEncryptor(_key, iv))
        using (var memoryStream = new MemoryStream())
        using (var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
        using (var hmacObj = new HMACSHA256(_key))
        {
          memoryStream.Write(iv, 0, BlockSize);
          cryptoStream.Write(data, 0, data.Length);
          cryptoStream.FlushFinalBlock();
          memoryStream.Position = 0;
          var hmac = hmacObj.ComputeHash(memoryStream);
          memoryStream.Write(hmac, 0, hmac.Length);
          return memoryStream.ToArray();
        }
      }
    }

    public byte[] Decrypt(byte[] data)
    {
      var n = data.Length - HmacSize - BlockSize;
      if (n <= 0)
        throw new DecryptionFailedException($"Invalid data: length is too small.");
      using (var aes = new AesManaged())
      {
        using (var hmac = new HMACSHA256(_key))
        {
          var actualHmac = hmac.ComputeHash(data, 0, BlockSize + n);
          var expectedHmac = Helper.SubArray(data, BlockSize + n, HmacSize);
          if (!actualHmac.SequenceEqual(expectedHmac))
            throw new DecryptionFailedException($"Computed HMAC is different than given HMAC.");
        }
        aes.KeySize = _key.Length * 8;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        var iv = Helper.SubArray(data, 0, BlockSize);
        var buffer = new byte[data.Length];
        int readCount;
        using (var transform = aes.CreateDecryptor(_key, iv))
        using (var memoryStream = new MemoryStream(data, BlockSize, n))
        using (var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read))
          readCount = cryptoStream.Read(buffer, 0, buffer.Length);
        return Helper.SubArray(buffer, 0, readCount);
      }
    }
  }
}