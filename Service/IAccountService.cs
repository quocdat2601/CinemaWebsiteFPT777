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
        // Thêm các method phục vụ controller
        Account GetOrCreateGoogleAccount(string email, string? name, string? givenName, string? surname, string? picture);
        bool HasMissingProfileInfo(Account user);
        Task SignInUserAsync(HttpContext httpContext, Account user);
        Task SignOutUserAsync(HttpContext httpContext);

        // Forget password methods
        bool SendForgetPasswordOtp(string email);
        bool VerifyForgetPasswordOtp(string email, string otp);
        bool ResetPassword(string email, string newPassword);
        Account? GetAccountByEmail(string email);

        // New AJAX-based forget password methods
        bool SendForgetPasswordOtpEmail(string email, string otp);
        bool StoreForgetPasswordOtp(string email, string otp, DateTime expiry);
        void ClearForgetPasswordOtp(string email);

        public void ToggleStatus(string accountId);
    }
}
