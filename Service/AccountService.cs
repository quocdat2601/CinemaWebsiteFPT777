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

        public AccountService(IAccountRepository repository, IEmployeeRepository employeeRepository, IMemberRepository memberRepository, IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _employeeRepository = employeeRepository;
            _memberRepository = memberRepository;
            _httpContextAccessor = httpContextAccessor;
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

        public bool Update1(string id, ProfileViewModel model)
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
            account.Status = model.Status;
            _repository.Update(account);
            _repository.Save();
            return true;
        }

        public ProfileViewModel GetDemoUser()
        {
            var account = _repository.GetById("AC007");
            if (account == null)
                return null;

            // 2) Map sang ProfileViewModel
            return new ProfileViewModel
            {
                Username = account.Username,
                FullName = account.FullName,
                DateOfBirth = (DateOnly)account.DateOfBirth,
                Gender = account.Gender,
                IdentityCard = account.IdentityCard,
                Email = account.Email,
                Address = account.Address,
                PhoneNumber = account.PhoneNumber,
                Password = account.Password  // chỉ để demo
            };
        }

        public Account? GetById(string id)
        {
            return _repository.GetById(id);
        }
    }
    }
