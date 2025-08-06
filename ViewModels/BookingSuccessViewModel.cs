namespace MovieTheater.ViewModels
{
    public class BookingSuccessViewModel
    {
        public ConfirmBookingViewModel BookingDetails { get; set; }
        public string MemberId { get; set; }
        public string MemberEmail { get; set; }
        public string MemberIdentityCard { get; set; }
        public string MemberPhone { get; set; }
        public int UsedScore { get; set; }
        public int UsedScoreValue { get; set; }
        public int AddedScore { get; set; }
        public int AddedScoreValue { get; set; }
        public decimal Subtotal { get; set; }
        public decimal RankDiscount { get; set; }
        public decimal VoucherAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal RankDiscountPercent { get; set; }
        public List<FoodViewModel> SelectedFoods { get; set; }
        public decimal TotalFoodPrice { get; set; }
    }
}