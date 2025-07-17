using MovieTheater.Models;
using MovieTheater.Repository;
using Microsoft.EntityFrameworkCore;

namespace MovieTheater.Service
{
    public class PromotionService : IPromotionService
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly MovieTheaterContext _context;

        public PromotionService(IPromotionRepository promotionRepository, MovieTheaterContext context)
        {
            _promotionRepository = promotionRepository;
            _context = context;
        }

        public IEnumerable<Promotion> GetAll()
        {
            return _promotionRepository.GetAll();
        }

        public Promotion? GetById(int id)
        {
            return _promotionRepository.GetById(id);
        }

        public bool Add(Promotion promotion)
        {
            if (promotion == null)
                return false;

            _promotionRepository.Add(promotion);
            return true;
        }

        public bool Update(Promotion promotion)
        {
            if (promotion == null)
                return false;
            _promotionRepository.Update(promotion);
            _promotionRepository.Save();
            return true;
        }

        public bool Delete(int id)
        {
            _promotionRepository.Delete(id);
            return true;
        }

        public void Save()
        {
            _promotionRepository.Save();
        }

        public Promotion? GetBestPromotionForShowDate(DateOnly showDate)
        {
            var allPromotions = _promotionRepository.GetAll();
            var validPromotions = allPromotions
                .Where(p => p.IsActive &&
                            DateOnly.FromDateTime((DateTime)p.StartTime) <= showDate &&
                            DateOnly.FromDateTime((DateTime)p.EndTime) >= showDate &&
                            p.DiscountLevel.HasValue)
                .OrderByDescending(p => p.DiscountLevel)
                .ToList();
            return validPromotions.FirstOrDefault();
        }

        public Promotion? GetBestEligiblePromotionForBooking(PromotionCheckContext context)
        {
            var allPromotions = _context.Promotions.Include(p => p.PromotionConditions).Where(p => p.IsActive).ToList();
            foreach (var promotion in allPromotions.OrderByDescending(p => p.DiscountLevel ?? 0))
            {
                if (IsPromotionEligibleNew(promotion, context))
                    return promotion;
            }
            return null;
        }

        private bool IsPromotionEligibleNew(Promotion promotion, PromotionCheckContext context)
        {
            if (promotion.PromotionConditions == null || !promotion.PromotionConditions.Any()) return true;
            foreach (var condition in promotion.PromotionConditions)
            {
                switch (condition.TargetField?.ToLower())
                {
                    case "seat":
                        if (!int.TryParse(condition.TargetValue, out int seatTarget)) return false;
                        switch (condition.Operator)
                        {
                            case ">=": if (!(context.SeatCount >= seatTarget)) return false; break;
                            case "==": case "=": if (!(context.SeatCount == seatTarget)) return false; break;
                            case "<=": if (!(context.SeatCount <= seatTarget)) return false; break;
                            case "<": if (!(context.SeatCount < seatTarget)) return false; break;
                            case "!=": if (!(context.SeatCount != seatTarget)) return false; break;
                            default: return false;
                        }
                        break;
                    case "movienameenglish":
                        if (string.IsNullOrEmpty(context.MovieName)) return false;
                        switch (condition.Operator)
                        {
                            case "=": case "==": if (!context.MovieName.Equals(condition.TargetValue, StringComparison.OrdinalIgnoreCase)) return false; break;
                            case "!=": if (context.MovieName.Equals(condition.TargetValue, StringComparison.OrdinalIgnoreCase)) return false; break;
                            default: return false;
                        }
                        break;
                    case "movie_english_name":
                        if (string.IsNullOrEmpty(context.MovieName)) return false;
                        switch (condition.Operator)
                        {
                            case "=": case "==": if (!context.MovieName.Equals(condition.TargetValue, StringComparison.OrdinalIgnoreCase)) return false; break;
                            case "!=": if (context.MovieName.Equals(condition.TargetValue, StringComparison.OrdinalIgnoreCase)) return false; break;
                            default: return false;
                        }
                        break;
                    case "showdate":
                        if (!DateTime.TryParse(condition.TargetValue, out DateTime dateTarget)) return false;
                        switch (condition.Operator)
                        {
                            case "=": case "==": if (context.ShowDate.Date != dateTarget.Date) return false; break;
                            case "!=": if (context.ShowDate.Date == dateTarget.Date) return false; break;
                            case ">=": if (!(context.ShowDate.Date >= dateTarget.Date)) return false; break;
                            case "<=": if (!(context.ShowDate.Date <= dateTarget.Date)) return false; break;
                            default: return false;
                        }
                        break;
                    case "accountid":
                        // Nếu chưa chọn member, loại bỏ promotion này
                        if (string.IsNullOrEmpty(context.MemberId)) return false;
                        // Lấy accountId từ memberId
                        var member = _context.Members.FirstOrDefault(m => m.MemberId == context.MemberId);
                        if (member == null || string.IsNullOrEmpty(member.AccountId)) return false;
                        var accountId = member.AccountId;
                        // Kiểm tra trong bảng Invoice
                        var invoices = _context.Invoices.Where(i => i.AccountId == accountId);
                        if (string.IsNullOrEmpty(condition.TargetValue))
                        {
                            // Nếu targetValue là null, kiểm tra có invoice nào có AccountId đúng bằng accountId không
                            if (invoices.Any(i => i.AccountId == accountId)) return false;
                            break;
                        }
                        switch (condition.Operator)
                        {
                            case "=": case "==":
                                if (!invoices.Any(i => i.AccountId != null && i.AccountId.Equals(condition.TargetValue, StringComparison.OrdinalIgnoreCase))) return false;
                                break;
                            case "!=":
                                if (invoices.Any(i => i.AccountId != null && i.AccountId.Equals(condition.TargetValue, StringComparison.OrdinalIgnoreCase))) return false;
                                break;
                            default:
                                return false;
                        }
                        break;
                    // Thêm các điều kiện khác nếu cần
                }
            }
            return true;
        }
    }

    public class PromotionCheckContext {
        public string MemberId { get; set; }
        public int SeatCount { get; set; }
        public string MovieId { get; set; }
        public string MovieName { get; set; }
        public DateTime ShowDate { get; set; }
        // Thêm các trường khác nếu cần
    }
}
