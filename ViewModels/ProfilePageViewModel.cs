using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MovieTheater.ViewModels
{
    public class ProfilePageViewModel
    {
        public ProfileUpdateViewModel Profile { get; set; }
        [ValidateNever]
        public RankInfoViewModel RankInfo { get; set; }
        [ValidateNever]
        public List<RankInfoViewModel> AllRanks { get; set; }
    }
}