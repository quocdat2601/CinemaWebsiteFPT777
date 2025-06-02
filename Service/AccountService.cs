using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MovieTheater.Service
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _repository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly EmailService _emailService;
        private readonly ILogger<AccountService> _logger;
        private static readonly Dictionary<string, (string Otp, DateTime Expiry)> _otpStore = new();

        public AccountService(IAccountRepository repository, IEmployeeRepository employeeRepository, IMemberRepository memberRepository, IHttpContextAccessor httpContextAccessor, EmailService emailService, ILogger<AccountService> logger)
        {
            _repository = repository;
            _employeeRepository = employeeRepository;
            _memberRepository = memberRepository;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _logger = logger;
        }

        public bool Register(RegisterViewModel model)
        {
            if (_repository.GetByUsername(model.Username) != null)
                return false;

            var hasher = new PasswordHasher<Account>();
            var HashedPW = hasher.HashPassword(null, model.Password);


            var account = new Account
            {
                Username = model.Username,
                Password = HashedPW,
                FullName = model.FullName,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                IdentityCard = model.IdentityCard,
                Email = model.Email,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                RegisterDate = DateOnly.FromDateTime(DateTime.Now),
                Status = 1,
                RoleId = model.RoleId, // 
                Image = model.Image
            };

            _repository.Add(account);
            _repository.Save();

            if (account.RoleId == 3) // Member
            {
                _memberRepository.Add(new Member
                {
                    Score = 0,
                    AccountId = account.AccountId
                });
                _memberRepository.Save();
            }
            else if (account.RoleId == 2) // Employee
            {
                _employeeRepository.Add(new Employee
                {
                    AccountId = account.AccountId
                });
                _employeeRepository.Save();
            }

            return true;
        }

        public bool Update(string id, RegisterViewModel model)
        {
            var account = _repository.GetById(id);
            if (account == null) return false;

            // Only check for duplicate username if the username is being changed
            if (model.Username != account.Username)
            {
                var duplicate = _repository.GetByUsername(model.Username);
                if (duplicate != null) 
                {
                    throw new Exception("Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");
                }
            }

            // Update fields but preserve password if not provided
            account.Username = model.Username;
            if (!string.IsNullOrEmpty(model.Password))
            {
                account.Password = model.Password;
            }
            account.FullName = model.FullName;
            account.DateOfBirth = model.DateOfBirth;
            account.Gender = model.Gender;
            account.IdentityCard = model.IdentityCard;
            account.Email = model.Email;
            account.Address = model.Address;
            account.PhoneNumber = model.PhoneNumber;
            account.RegisterDate = DateOnly.FromDateTime(DateTime.Now);
            account.Status = model.Status;

            if (!string.IsNullOrEmpty(model.Image))
                account.Image = model.Image;

            _repository.Update(account);
            _repository.Save();
            return true;
        }

        public bool Authenticate(string username, string password, out Account? account)
        {
            account = _repository.Authenticate(username /*, password*/);//account with password hasing

            if (account.Password.Length < 20)
            {
                var hashered = new PasswordHasher<Account>();
                account.Password = hashered.HashPassword(null, account.Password);
            }
            var hasher = new PasswordHasher<Account>();
            var result = hasher.VerifyHashedPassword(null, account.Password, password);
            if (result == PasswordVerificationResult.Success)
            {
                return account != null;
            }
            else
            {
                return false;
            }
        }

        public ProfileUpdateViewModel GetCurrentUser()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
                return null;

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return null;

            var account = _repository.GetById(userId);
            if (account == null)
                return null;

            bool isGoogleAccount = account.Password.IsNullOrEmpty();

            return new ProfileUpdateViewModel
            {
                AccountId = account.AccountId,
                Username = account.Username ?? string.Empty,
                FullName = account.FullName ?? string.Empty,
                DateOfBirth = account.DateOfBirth ?? DateOnly.FromDateTime(DateTime.Now),
                Gender = account.Gender ?? "unknown",
                IdentityCard = account.IdentityCard ?? string.Empty,
                Email = account.Email ?? string.Empty,
                Address = account.Address ?? string.Empty,
                PhoneNumber = account.PhoneNumber ?? string.Empty,
                Password = account.Password ?? string.Empty,
                IsGoogleAccount = isGoogleAccount
            };
        }
        public bool VerifyCurrentPassword(string username, string currentPassword)
        {
            var account = _repository.GetByUsername(username);
            if (account == null)
                return false;

            // If the password is not hashed (length < 20), hash it first
            if (account.Password.Length < 20)
            {
                var hasher = new PasswordHasher<Account>();
                account.Password = hasher.HashPassword(null, account.Password);
                _repository.Update(account);
                _repository.Save();
            }

            try
            {
                var hasher = new PasswordHasher<Account>();
                var result = hasher.VerifyHashedPassword(null, account.Password, currentPassword);
                return result == PasswordVerificationResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying password for user {username}: {ex.Message}");
                return false;
            }
        }

        // --- OTP Email Sending ---
        public bool SendOtpEmail(string toEmail, string otp)
        {
            try
            {
                var subject = "Your Password Change OTP Code";
                var body = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif; padding: 20px;'>
                            <h2 style='color: #333;'>Password Change Request</h2>
                            <p>You have requested to change your password. Please use the following OTP code to proceed:</p>
                            <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <h1 style='color: #007bff; margin: 0; text-align: center;'>{otp}</h1>
                            </div>
                            <p>This OTP will expire in 10 minutes.</p>
                            <p>If you did not request this password change, please ignore this email.</p>
                            <hr style='margin: 20px 0;'>
                            <p style='color: #666; font-size: 12px;'>This is an automated message, please do not reply.</p>
                        </body>
                    </html>";

                var result = _emailService.SendEmail(toEmail, subject, body);
                if (!result)
                {
                    _logger.LogError($"Failed to send OTP email to {toEmail}");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception while sending OTP email to {toEmail}: {ex.Message}");
                return false;
            }
        }

        // --- Password Update ---
        public bool UpdatePassword(string accountId, string newPassword)
        {
            var account = _repository.GetById(accountId);
            if (account == null) return false;

            var hasher = new PasswordHasher<Account>();
            account.Password = hasher.HashPassword(null, newPassword);
            
            _repository.Update(account);
            _repository.Save();
            return true;
        }

        public bool UpdatePasswordByUsername(string username, string newPassword)
        {
            var account = _repository.GetByUsername(username);
            if (account == null)
            {
                return false;
            }

            var hasher = new PasswordHasher<Account>();
            account.Password = hasher.HashPassword(null, newPassword);
            
            _repository.Update(account);
            _repository.Save();
            return true;
        }

        // --- OTP Storage and Verification ---
        public bool StoreOtp(string accountId, string otp, DateTime expiry)
        {
            try
            {
                _otpStore[accountId] = (otp, expiry);
                _logger.LogInformation($"[StoreOtp] accountId={accountId}, otp={otp}, expiry={expiry}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool VerifyOtp(string accountId, string otp)
        {
            if (!_otpStore.TryGetValue(accountId, out var otpData))
                return false;

            if (DateTime.UtcNow > otpData.Expiry)
            {
                _otpStore.Remove(accountId);
                return false;
            }

            _logger.LogInformation($"[VerifyOtp] accountId={accountId}, otp={otp}");
            return otpData.Otp == otp;
        }

        public void ClearOtp(string accountId)
        {
            _otpStore.Remove(accountId);
        }
        public bool GetByUsername(string username)
        {
            _repository.GetByUsername(username);
            return true;
        }
        public Account? GetById(string id)
        {
            return _repository.GetById(id);
        }
    }
}

