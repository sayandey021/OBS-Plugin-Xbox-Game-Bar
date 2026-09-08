using Microsoft.VisualStudio.TestTools.UnitTesting;
using OBSGameBar.Core.Protocol;

namespace OBSGameBar.Tests
{
    [TestClass]
    public class ObsAuthTests
    {
        [TestMethod]
        public void TestGenerateAuthResponse_ProducesDeterministicHash()
        {
            // Test vectors
            string password = "superSecretPassword123";
            string salt = "G6mF7N6sBw==";
            string challenge = "D8s9A7f6H5g=";

            string auth1 = ObsAuthHelper.GenerateAuthResponse(password, salt, challenge);
            string auth2 = ObsAuthHelper.GenerateAuthResponse(password, salt, challenge);

            Assert.IsFalse(string.IsNullOrEmpty(auth1));
            Assert.AreEqual(auth1, auth2);
        }

        [TestMethod]
        public void TestGenerateAuthResponse_EmptyPassword_HandledSafely()
        {
            string auth = ObsAuthHelper.GenerateAuthResponse(null, "salt", "challenge");
            Assert.IsFalse(string.IsNullOrEmpty(auth));
        }

        [TestMethod]
        public void TestGenerateAuthResponse_DifferentPassword_ProducesDifferentHash()
        {
            string salt = "salt123";
            string challenge = "challenge456";

            string auth1 = ObsAuthHelper.GenerateAuthResponse("passwordA", salt, challenge);
            string auth2 = ObsAuthHelper.GenerateAuthResponse("passwordB", salt, challenge);

            Assert.AreNotEqual(auth1, auth2);
        }
    }
}
