using System.Threading.Tasks;

namespace EyewearStore_SWP391.Services
{
    public interface IOtpService
    {
        /// <summary>Generate a 6-digit OTP, store in TempData-like cache, return the code</summary>
        string GenerateOtp(string email);
        bool ValidateOtp(string email, string code);
        void InvalidateOtp(string email);
    }
}
