using System.Security.Cryptography;
using System.Text;

namespace Terraria_Players_Editor.Services;

/// <summary>
/// Handles AES-128 CBC encryption/decryption for .plr files.
/// The key "h3y_gUyZ" encoded as UTF-16 LE serves as both key and IV.
/// </summary>
public static class PlrCrypto
{
    private static readonly byte[] AesKey = Encoding.Unicode.GetBytes("h3y_gUyZ");

    /// <summary>Decrypt a .plr file's contents.</summary>
    public static byte[] Decrypt(byte[] encryptedData)
    {
        using var aes = Aes.Create();
        aes.KeySize = 128;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = AesKey;
        aes.IV = AesKey;

        using var msInput = new MemoryStream(encryptedData);
        using var cs = new CryptoStream(msInput, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var msOutput = new MemoryStream();
        cs.CopyTo(msOutput);
        return msOutput.ToArray();
    }

    /// <summary>Encrypt data for writing to a .plr file.</summary>
    public static byte[] Encrypt(byte[] plainData)
    {
        using var aes = Aes.Create();
        aes.KeySize = 128;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = AesKey;
        aes.IV = AesKey;

        using var msOutput = new MemoryStream();
        using (var cs = new CryptoStream(msOutput, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(plainData, 0, plainData.Length);
        }
        // CryptoStream needs to be flushed/closed before reading MemoryStream
        msOutput.Position = 0;
        // We need to return after the CryptoStream is disposed
        var result = msOutput.ToArray();
        return result;
    }
}
