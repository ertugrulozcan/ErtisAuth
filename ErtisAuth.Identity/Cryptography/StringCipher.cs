using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Linq;

namespace ErtisAuth.Identity.Cryptography
{
    public static class StringCipher
    {
        #region Constants
        
        private const int BLOCK_SIZE = 128;
        
        private const int CHUNK_SIZE = BLOCK_SIZE / 8;

        private const int ITERATION_COUNT = 1000;
        
        private static readonly Encoding ENCODING = Encoding.ASCII;
        
        private static readonly HashAlgorithmName HASH_ALGORITHM = HashAlgorithmName.SHA256;

        #endregion
        
        #region Methods

        public static string Encrypt(string plainText, string passPhrase, CipherOptions options = null)
        {
            return Encrypt(
                plainText,
                passPhrase,
                options?.BlockSize,
                options?.IterationCount,
                options?.Encoding,
                options?.HashAlgorithm);
        }

        private static string Encrypt(
            string plainText, 
            string passPhrase, 
            int? blockSize = null, 
            int? iterationCount = null, 
            Encoding encoding = null, 
            HashAlgorithmName? hashAlgorithm = null)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return plainText;
            }
            
            var chunkSize = blockSize != null ? blockSize.Value / 8 : CHUNK_SIZE;
            var saltStringBytes = GenerateRandomEntropy(chunkSize);
            var ivStringBytes = GenerateRandomEntropy(chunkSize);
            var plainTextBytes = (encoding ?? ENCODING).GetBytes(plainText);
            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, iterationCount ?? ITERATION_COUNT, hashAlgorithm ?? HASH_ALGORITHM))
            {
                var keyBytes = password.GetBytes(chunkSize);
                using (var symmetricKey = Aes.Create())
                {
                    symmetricKey.BlockSize = blockSize ?? BLOCK_SIZE;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                
                                var cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                
                                var encrypted = Convert.ToBase64String(cipherTextBytes);
                                var encoded = Encode(encrypted);
                                return encoded;
                            }
                        }
                    }
                }
            }
        }
        
        public static string Decrypt(string cipherText, string passPhrase, CipherOptions options = null)
        {
            return Decrypt(
                cipherText,
                passPhrase,
                options?.BlockSize,
                options?.IterationCount,
                options?.Encoding,
                options?.HashAlgorithm);
        }

        private static string Decrypt(
            string cipherText, 
            string passPhrase, 
            int? blockSize = null, 
            int? iterationCount = null, 
            Encoding encoding = null, 
            HashAlgorithmName? hashAlgorithm = null)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return cipherText;
            }
            
            cipherText = Decode(cipherText);
            
            var chunkSize = blockSize != null ? blockSize.Value / 8 : CHUNK_SIZE;
            var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(chunkSize).ToArray();
            var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(chunkSize).Take(chunkSize).ToArray();
            var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip(chunkSize * 2).Take(cipherTextBytesWithSaltAndIv.Length - chunkSize * 2).ToArray();

            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, iterationCount ?? ITERATION_COUNT, hashAlgorithm ?? HASH_ALGORITHM))
            {
                var keyBytes = password.GetBytes(chunkSize);
                using (var symmetricKey = Aes.Create())
                {
                    symmetricKey.BlockSize = blockSize ?? BLOCK_SIZE;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var decryptTransform = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, decryptTransform, CryptoStreamMode.Read))
                            {
                                var plainTextBytes = new byte[cipherTextBytes.Length];
                                var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                memoryStream.Close();
                                cryptoStream.Close();
                                
                                var decrypted = (encoding ?? ENCODING).GetString(plainTextBytes, 0, decryptedByteCount);
                                return decrypted;
                            }
                        }
                    }
                }
            }
        }

        private static byte[] GenerateRandomEntropy(int chunkSize)
        {
            var randomBytes = new byte[chunkSize];
            using (var rngCsp = RandomNumberGenerator.Create())
            {
                rngCsp.GetBytes(randomBytes);
            }
            
            return randomBytes;
        }
        
        private static string Encode(string str)
        {
            var stringBuilder = new StringBuilder();
            foreach (var c in str)
            {
                if (char.IsDigit(c) || char.IsLetter(c))
                {
                    stringBuilder.Append(c);
                }
                else
                {
                    var ascii = (int) c;
                    stringBuilder.Append($"${ascii}$");
                }
            }

            return stringBuilder.ToString();
        }
        
        private static string Decode(string str)
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (c == '$')
                {
                    var number = string.Empty;
                    do
                    {
                        i++;
                        c = str[i];

                        if (c != '$')
                        {
                            number += c;   
                        }
                    } 
                    while (c != '$');

                    if (int.TryParse(number, out var ascii))
                    {
                        c = (char) ascii;
                        stringBuilder.Append(c);
                    }
                    else
                    {
                        throw new CryptographicException("Encoded message contains non-ascii blocks!");
                    }
                }
                else
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString();
        }

        #endregion
    }

    public class CipherOptions
    {
        #region Properties

        public int BlockSize { get; init; } 
        
        public int IterationCount { get; init; } 
        
        public Encoding Encoding { get; init; } 
        
        public HashAlgorithmName HashAlgorithm { get; init; }

        #endregion
    }
}