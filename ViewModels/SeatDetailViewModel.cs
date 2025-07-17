namespace MovieTheater.ViewModels
{
    public class SeatDetailViewModel
    {
        public int? SeatId { get; set; }
        public string SeatName { get; set; }
        public string SeatType { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? PromotionDiscount { get; set; }
        public decimal? PriceAfterPromotion { get; set; }
        public string PromotionName { get; set; }
        public int? SeatTypeId { get; set; }
    }

}
