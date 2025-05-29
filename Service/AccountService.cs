using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using System.Net;
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

        public AccountService(
            IAccountRepository repository, 
            IEmployeeRepository employeeRepository, 
            IMemberRepository memberRepository, 
            IHttpContextAccessor httpContextAccessor,
            EmailService emailService)
        {
            _repository = repository;
            _employeeRepository = employeeRepository;
            _memberRepository = memberRepository;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
        }

        public bool Register(RegisterViewModel model)
        {
            if (_repository.GetByUsername(model.Username) != null)
                return false;

            var account = new Account
            {
                Username = model.Username,
                Password = model.Password,
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
            account.Username = model.Username;
            account.Password = model.Password;
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
            account = _repository.Authenticate(username, password);
            return account != null;
        }

        public ProfileViewModel GetCurrentUser()
        {
            // 1. Lấy userId từ Claims
            var user = _httpContextAccessor.HttpContext.User;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return null;

            // 2. Query repository
            var account = _repository.GetById(userId);
            if (account == null)
                return null;

            // 3. Map sang ViewModel
            return new ProfileViewModel
            {
                AccountId = account.AccountId,
                Username = account.Username,
                FullName = account.FullName,
                DateOfBirth = (DateOnly)account.DateOfBirth,
                Gender = account.Gender,
                IdentityCard = account.IdentityCard,
                Email = account.Email,
                Address = account.Address,
                PhoneNumber = account.PhoneNumber,
                Password = account.Password
            };
        }

        public bool UpdateAccount(string id, ProfileViewModel model)
        {
            var account = _repository.GetById(id);
            if (account == null) return false;

            var duplicate = _repository.GetAll().Any(a => a.Username == model.Username && a.AccountId != id);
            if (duplicate)
            {
                throw new Exception("Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");
            }

            account.Username = model.Username;
            //account.Password = model.Password;
            account.FullName = model.FullName;
            account.DateOfBirth = model.DateOfBirth;
            account.Gender = model.Gender;
            account.IdentityCard = model.IdentityCard;
            account.Email = model.Email;
            account.Address = model.Address;
            account.PhoneNumber = model.PhoneNumber;
            //account.Status = model.Status;
            _repository.Update(account);
            _repository.Save();
            return true;
        }

        public Account? GetById(string id)
        {
            return _repository.GetById(id);
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
                            <p>This OTP will expire in 5 minutes.</p>
                            <p>If you did not request this password change, please ignore this email.</p>
                            <hr style='margin: 20px 0;'>
                            <p style='color: #666; font-size: 12px;'>This is an automated message, please do not reply.</p>
                        </body>
                    </html>";

                return _emailService.SendEmail(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // --- Password Update ---
        public bool UpdatePassword(string accountId, string newPassword)
        {
            var account = _repository.GetById(accountId);
            if (account == null) return false;
            account.Password = newPassword;
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
            account.Password = newPassword;
            _repository.Update(account);
            _repository.Save();
            return true;
        }
    }
}
