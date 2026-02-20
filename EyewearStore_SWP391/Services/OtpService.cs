using System;
using System.Collections.Concurrent;

namespace EyewearStore_SWP391.Services
{
    /// <summary>
    /// In-memory OTP store. For production, replace with Redis or distributed cache.
    /// </summary>
    public class OtpService : IOtpService
    {
        private static readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> _store = new();
        private const int ExpiryMinutes = 10;

        public string GenerateOtp(string email)
        {
            var code = new Random().Next(100000, 999999).ToString();
            _store[email.ToLower()] = (code, DateTime.UtcNow.AddMinutes(ExpiryMinutes));
            return code;
        }

        public bool ValidateOtp(string email, string code)
        {
            var key = email.ToLower();
            if (!_store.TryGetValue(key, out var entry)) return false;
            if (DateTime.UtcNow > entry.Expiry) { _store.TryRemove(key, out _); return false; }
            if (entry.Code != code.Trim()) return false;
            _store.TryRemove(key, out _);           // one-time use
            return true;
        }

        public void InvalidateOtp(string email) => _store.TryRemove(email.ToLower(), out _);
    }
}
