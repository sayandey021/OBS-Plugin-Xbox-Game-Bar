using System;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using OBSGameBar.Core.Services;

namespace OBSGameBar.Services
{
    public class UwpPasswordVaultStorage : ISecureStorageService
    {
        private readonly PasswordVault _vault = new PasswordVault();

        public Task SavePasswordAsync(string resource, string username, string password)
        {
            if (string.IsNullOrEmpty(resource)) resource = "OBSGameBar";
            if (string.IsNullOrEmpty(username)) username = "WebSocket";

            try
            {
                // Remove existing if present
                try
                {
                    var existing = _vault.Retrieve(resource, username);
                    if (existing != null)
                    {
                        _vault.Remove(existing);
                    }
                }
                catch { }

                if (!string.IsNullOrEmpty(password))
                {
                    var cred = new PasswordCredential(resource, username, password);
                    _vault.Add(cred);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UwpPasswordVaultStorage] SavePassword error: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public Task<string> GetPasswordAsync(string resource, string username)
        {
            if (string.IsNullOrEmpty(resource)) resource = "OBSGameBar";
            if (string.IsNullOrEmpty(username)) username = "WebSocket";

            try
            {
                var cred = _vault.Retrieve(resource, username);
                if (cred != null)
                {
                    cred.RetrievePassword();
                    return Task.FromResult(cred.Password);
                }
            }
            catch
            {
                // Credential not found in vault - normal on first run
            }

            return Task.FromResult<string>(null);
        }

        public Task DeletePasswordAsync(string resource, string username)
        {
            if (string.IsNullOrEmpty(resource)) resource = "OBSGameBar";
            if (string.IsNullOrEmpty(username)) username = "WebSocket";

            try
            {
                var cred = _vault.Retrieve(resource, username);
                if (cred != null)
                {
                    _vault.Remove(cred);
                }
            }
            catch { }

            return Task.CompletedTask;
        }
    }
}
