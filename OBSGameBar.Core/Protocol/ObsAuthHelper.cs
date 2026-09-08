using System;
using System.Security.Cryptography;
using System.Text;

namespace OBSGameBar.Core.Protocol
{
    public static class ObsAuthHelper
    {
        /// <summary>
        /// Generates the obs-websocket v5 challenge-response authentication token.
        /// Algorithm:
        /// 1. Compute Base64(SHA256(password + salt))
        /// 2. Compute Base64(SHA256(secret + challenge))
        /// </summary>
        public static string GenerateAuthResponse(string password, string salt, string challenge)
        {
            if (password == null) password = string.Empty;
            if (salt == null) salt = string.Empty;
            if (challenge == null) challenge = string.Empty;

            using (var sha256 = SHA256.Create())
            {
                byte[] passSaltBytes = Encoding.UTF8.GetBytes(password + salt);
                byte[] secretHashBytes = sha256.ComputeHash(passSaltBytes);
                string secretBase64 = Convert.ToBase64String(secretHashBytes);

                byte[] authChallengeBytes = Encoding.UTF8.GetBytes(secretBase64 + challenge);
                byte[] finalHashBytes = sha256.ComputeHash(authChallengeBytes);
                return Convert.ToBase64String(finalHashBytes);
            }
        }
    }
}
