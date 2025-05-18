using MovieTheater.Models;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public interface IAccountService
    {
        bool Register(RegisterViewModel model);
        bool Authenticate(string username, string password, out Account? account);
    }

}
