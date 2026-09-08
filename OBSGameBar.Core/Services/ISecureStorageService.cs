using System.Threading.Tasks;

namespace OBSGameBar.Core.Services
{
    public interface ISecureStorageService
    {
        Task SavePasswordAsync(string resource, string username, string password);
        Task<string> GetPasswordAsync(string resource, string username);
        Task DeletePasswordAsync(string resource, string username);
    }

    public class InMemorySecureStorageService : ISecureStorageService
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _storage =
            new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        public Task SavePasswordAsync(string resource, string username, string password)
        {
            _storage[$"{resource}::{username}"] = password;
            return Task.CompletedTask;
        }

        public Task<string> GetPasswordAsync(string resource, string username)
        {
            _storage.TryGetValue($"{resource}::{username}", out string val);
            return Task.FromResult(val);
        }

        public Task DeletePasswordAsync(string resource, string username)
        {
            _storage.TryRemove($"{resource}::{username}", out _);
            return Task.CompletedTask;
        }
    }
}
