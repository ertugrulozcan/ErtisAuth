using System;
using NUnit.Framework;

namespace ErtisAuth.Tests.Identity
{
    [TestFixture]
    public class CryptographyTests
    {
        #region Test Methods

		[Test]
		public void EncryptDecrypt_Test()
		{
			const string message = "BQJSMDYGDYVAESTSTGDDODQBDDIFOOVH";
			const string key = "61e2ce5f936b5b4d6166e268";
			
			var crypto = ErtisAuth.Identity.Cryptography.StringCipher.Encrypt(message, key);
			Assert.That(!string.IsNullOrEmpty(crypto));
			
			Console.WriteLine(crypto);
			
			var message2 = ErtisAuth.Identity.Cryptography.StringCipher.Decrypt(crypto, key);
			Assert.That(message == message2);
		}
		
		#endregion
    }
}