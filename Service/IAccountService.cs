using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public interface IAccountService
    {
        bool Register(RegisterViewModel model);
        bool Authenticate(string username, string password, out Account? account);
        public bool Update(string id, RegisterViewModel model);
        public bool UpdateAccount(string id, ProfileViewModel model);
        ProfileViewModel GetCurrentUser();
        public Account? GetById(string id);
        bool SendOtpEmail(string toEmail, string otp);
        bool UpdatePasswordByUsername(string username, string newPassword);
    }

}
