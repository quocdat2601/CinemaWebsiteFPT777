using MovieTheater.Models;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public interface IAccountService
    {
        bool Register(RegisterViewModel model);
        bool Authenticate(string username, string password, out Account? account);
        public bool Update(string id, RegisterViewModel model);
        public ProfileUpdateViewModel GetCurrentUser();
        public bool VerifyCurrentPassword(string username, string currentPassword);
        public bool SendOtpEmail(string toEmail, string otp);
        public bool UpdatePassword(string accountId, string newPassword);
        public bool UpdatePasswordByUsername(string username, string newPassword);
        public bool VerifyOtp(string accountId, string otp);
        public bool StoreOtp(string accountId, string otp, DateTime expiry);
        public void ClearOtp(string accountId);
        public bool GetByUsername(string username);
        public Account? GetById(string id);
        Task DeductScoreAsync(string userId, int points, bool deductFromTotalPoints = false);
        Task AddScoreAsync(string userId, int points, bool addToTotalPoints = true);
        void CheckAndUpgradeRank(string accountId);
        string GetAndClearRankUpgradeNotification(string accountId);
    }
}
