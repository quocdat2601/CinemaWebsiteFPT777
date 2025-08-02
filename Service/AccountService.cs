using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using MovieTheater.Helpers;
using System.Collections.Concurrent;
using System.Security.Claims;

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
        private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry)> _otpStore = new();
        private static readonly ConcurrentDictionary<string, string> _pendingRankNotifications = new();
        private readonly MovieTheaterContext _context;

        public AccountService(IAccountRepository repository, IEmployeeRepository employeeRepository, IMemberRepository memberRepository, IHttpContextAccessor httpContextAccessor, EmailService emailService, ILogger<AccountService> logger, MovieTheaterContext context)
        {
            _repository = repository;
            _employeeRepository = employeeRepository;
            _memberRepository = memberRepository;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _logger = logger;
            _context = context;
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
                Image = string.IsNullOrEmpty(model.Image) ? "/image/profile.jpg" : model.Image
            };

            _repository.Add(account);
            _repository.Save();

            if (account.RoleId == 3) // Member
            {
                // Set initial rank (Bronze)
                var bronzeRank = _context.Ranks.OrderBy(r => r.RequiredPoints).FirstOrDefault();
                if (bronzeRank != null)
                {
                    account.RankId = bronzeRank.RankId;
                    _context.SaveChanges();
                }

                _memberRepository.Add(new Member
                {
                    Score = 0,
                    TotalPoints = 0,
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
                    throw new Exception("Username already exists. Please choose a different one.");
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
            if (model.Status.HasValue)
            {
                account.Status = model.Status;
            }

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/avatars");
                string sanitizedFileName = PathSecurityHelper.SanitizeFileName(model.ImageFile.FileName);
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + sanitizedFileName;
                
                string? secureFilePath = PathSecurityHelper.CreateSecureFilePath(uploadsFolder, uniqueFileName);
                if (secureFilePath == null)
                {
                    return false; // Invalid file path
                }
                
                using (var stream = new FileStream(secureFilePath, FileMode.Create))
                {
                    model.ImageFile.CopyTo(stream);
                }
                account.Image = $"/images/avatars/{uniqueFileName}";
            }
            else if (!string.IsNullOrEmpty(model.Image) && model.ImageFile == null)
            {
                account.Image = model.Image;
            }

            _repository.Update(account);
            _repository.Save();
            return true;
        }

        //public bool Authenticate(string username, string password, out Account? account)
        //{
        //    account = _repository.Authenticate(username /*, password*/);//account with password hasing

        //    if (account.Password.Length < 20)
        //    {
        //        var hashered = new PasswordHasher<Account>();
        //        account.Password = hashered.HashPassword(null, account.Password);
        //    }
        //    var hasher = new PasswordHasher<Account>();
        //    var result = hasher.VerifyHashedPassword(null, account.Password, password);
        //    if (result == PasswordVerificationResult.Success)
        //    {
        //        return account != null;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        //CHECK ACCOUNT NULL BEFORE USING
        public bool Authenticate(string username, string password, out Account? account)
        {
            account = _repository.Authenticate(username);

            if (account == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(account.Password) && account.Password.Length < 20)
            {
                var hashered = new PasswordHasher<Account>();
                account.Password = hashered.HashPassword(null, account.Password);
            }

            var hasher = new PasswordHasher<Account>();
            var result = hasher.VerifyHashedPassword(null, account.Password, password);

            return result == PasswordVerificationResult.Success;
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
                IsGoogleAccount = isGoogleAccount,
                Score = account.Members.FirstOrDefault(m => m.AccountId == account.AccountId)?.Score ?? 0,
                Image = account.Image
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
                _otpStore.TryRemove(accountId, out _);
                return false;
            }

            _logger.LogInformation($"[VerifyOtp] accountId={{accountId}}, otp={{otp}}", accountId, otp);
            return otpData.Otp == otp;
        }

        public void ClearOtp(string accountId)
        {
            _otpStore.TryRemove(accountId, out _);
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

        public async Task DeductScoreAsync(string userId, int points, bool deductFromTotalPoints = false)
        {
            var account = _repository.GetById(userId);
            if (account == null) return;
            var member = account.Members.FirstOrDefault();
            if (member != null && member.Score >= points)
            {
                member.Score -= points;
                if (deductFromTotalPoints)
                {
                    member.TotalPoints -= points; // Only deduct from lifetime points if requested (e.g., on cancel)
                    if (member.TotalPoints < 0) member.TotalPoints = 0; // Prevent negative
                }
                _memberRepository.Update(member);
                _memberRepository.Save();
                CheckAndUpgradeRank(userId); // This will handle both upgrades and downgrades
            }
        }

        public async Task AddScoreAsync(string userId, int points, bool addToTotalPoints = true)
        {
            var account = _repository.GetById(userId);
            if (account == null) return;
            var member = account.Members.FirstOrDefault();
            if (member != null)
            {
                member.Score += points;
                if (addToTotalPoints)
                {
                    member.TotalPoints += points;
                }
                _memberRepository.Update(member);
                _memberRepository.Save();

                // Check and upgrade rank immediately after points are added
                CheckAndUpgradeRank(userId);
            }
        }

        public string GetAndClearRankUpgradeNotification(string accountId)
        {
            if (_pendingRankNotifications.TryRemove(accountId, out var message))
            {
                return message;
            }
            return null;
        }

        public void CheckAndUpgradeRank(string accountId)
        {
            var member = _context.Members
                .Include(m => m.Account)
                .ThenInclude(a => a.Rank)
                .FirstOrDefault(m => m.AccountId == accountId);

            if (member?.Account == null) return;

            var newRank = _context.Ranks
                .Where(r => r.RequiredPoints <= member.TotalPoints)
                .OrderByDescending(r => r.RequiredPoints)
                .FirstOrDefault();

            if (newRank != null && member.Account.RankId != newRank.RankId)
            {
                var oldRankName = member.Account.Rank?.RankName ?? "Not Ranked";
                member.Account.RankId = newRank.RankId;
                _context.SaveChanges();

                var memberMessage = $"Congratulations! You've been upgraded to {newRank.RankName} rank!";
                _pendingRankNotifications[accountId] = memberMessage;

                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null && httpContext.User.IsInRole("Admin"))
                {
                    var adminMessage = $"Member {member.Account.FullName} has been upgraded to {newRank.RankName} rank!";
                    httpContext.Session.SetString("RankUpToastMessage", adminMessage);
                }
            }
        }

        // --- Google Account Helper ---
        public Account GetOrCreateGoogleAccount(string email, string? name, string? givenName, string? surname, string? picture)
        {
            var user = _repository.GetAccountByEmail(email);
            if (user == null)
            {
                // Set initial rank (Bronze)
                var bronzeRank = _context.Ranks.OrderBy(r => r.RequiredPoints).FirstOrDefault();
                user = new Account
                {
                    Email = email,
                    FullName = name ?? $"{givenName} {surname}".Trim() ?? "Google User",
                    Username = email,
                    RoleId = 3,
                    Status = 1,
                    RegisterDate = DateOnly.FromDateTime(DateTime.Now),
                    Image = !string.IsNullOrEmpty(picture) ? picture : "/image/profile.jpg",
                    Password = null, // Set Password to null for Google login
                    RankId = bronzeRank?.RankId // Always set the lowest rank if available
                };
                _repository.Add(user);
                _repository.Save();
                _memberRepository.Add(new Member
                {
                    Score = 0,
                    TotalPoints = 0,
                    AccountId = user.AccountId
                });
                _memberRepository.Save();
            }
            return _repository.GetAccountByEmail(email); // always return latest
        }

        public bool HasMissingProfileInfo(Account user)
        {
            return !user.DateOfBirth.HasValue || user.DateOfBirth.Value == DateOnly.MinValue ||
                   string.IsNullOrWhiteSpace(user.Gender) ||
                   string.IsNullOrWhiteSpace(user.IdentityCard) ||
                   string.IsNullOrWhiteSpace(user.Address) ||
                   string.IsNullOrWhiteSpace(user.PhoneNumber);
        }

        public async Task SignInUserAsync(HttpContext httpContext, Account user)
        {
            string roleName = user.RoleId switch
            {
                1 => "Admin",
                2 => "Employee",
                3 => "Member",
                _ => "Guest"
            };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.AccountId),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Username ?? string.Empty),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("Status", user.Status.ToString()),
                new Claim("Email", user.Email ?? string.Empty),
                new Claim("Image", user.Image ?? "/image/profile.jpg")
            };
            var identity = new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await httpContext.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        public async Task SignOutUserAsync(HttpContext httpContext)
        {
            await httpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Kiểm tra và sửa các avatar bị thiếu trong database
        /// </summary>
        /// <returns>Số lượng avatar đã được sửa</returns>
        public async Task<int> FixMissingAvatarsAsync()
        {
            try
            {
                var accountsWithMissingAvatars = await _context.Accounts
                    .Where(a => a.Image != null && a.Image.Contains("/images/avatars/"))
                    .ToListAsync();

                int fixedCount = 0;
                foreach (var account in accountsWithMissingAvatars)
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", account.Image.TrimStart('/'));
                    if (!File.Exists(imagePath))
                    {
                        account.Image = "/image/avatar.jpg";
                        fixedCount++;
                    }
                }

                if (fixedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Fixed {fixedCount} missing avatars");
                }

                return fixedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing missing avatars");
                return 0;
            }
        }
    }
}

