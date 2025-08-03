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
            // Lọc promotion KHÔNG có bất kỳ PromotionCondition nào với TargetEntity == "food"
            var seatPromotions = allPromotions
                .Where(p =>
                    p.PromotionConditions == null ||
                    !p.PromotionConditions.Any(c => c.TargetEntity != null && c.TargetEntity.ToLower() == "food")
                )
                .ToList();
            foreach (var promotion in seatPromotions.OrderByDescending(p => p.DiscountLevel ?? 0))
            {
                if (IsPromotionEligibleNew(promotion, context))
                    return promotion;
            }
            return null;
        }

        private bool IsPromotionEligibleNew(Promotion promotion, PromotionCheckContext context)
        {
            if (promotion.PromotionConditions == null || !promotion.PromotionConditions.Any()) return true;
            

            Console.WriteLine($"[PromotionService] Checking promotion: {promotion.Title}");
            Console.WriteLine($"[PromotionService] Context - SeatTypeNames: [{string.Join(", ", context.SelectedSeatTypeNames)}]");
            
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
                    case "seattypeid":
                        // Kiểm tra SeatTypeId của các ghế đã chọn
                        if (!int.TryParse(condition.TargetValue, out int seatTypeTarget)) return false;
                        var selectedSeatTypes = context.SelectedSeatTypeIds ?? new List<int>();
                        if (!selectedSeatTypes.Any()) return false;
                        switch (condition.Operator)
                        {
                            case "=": case "==": 
                                if (!selectedSeatTypes.Any(st => st == seatTypeTarget)) return false; break;
                            case "!=": 
                                if (selectedSeatTypes.Any(st => st == seatTypeTarget)) return false; break;
                            default: return false;
                        }
                        break;
                    case "typename":
                        // Kiểm tra TypeName của các ghế đã chọn
                        var selectedTypeNames = context.SelectedSeatTypeNames ?? new List<string>();
                        if (!selectedTypeNames.Any()) return false;
                        
                        Console.WriteLine($"[PromotionService] Checking TypeName condition: {condition.TargetValue} vs [{string.Join(", ", selectedTypeNames)}]");
                        
                        switch (condition.Operator)
                        {
                            case "=": case "==": 
                                var isMatch = selectedTypeNames.Any(tn => tn.Equals(condition.TargetValue, StringComparison.OrdinalIgnoreCase));
                                Console.WriteLine($"[PromotionService] TypeName match result: {isMatch}");
                                if (!isMatch) return false; break;
                            case "!=": 
                                var isNotMatch = selectedTypeNames.Any(tn => tn.Equals(condition.TargetValue, StringComparison.OrdinalIgnoreCase));
                                Console.WriteLine($"[PromotionService] TypeName not match result: {isNotMatch}");
                                if (isNotMatch) return false; break;
                            default: return false;
                        }
                        break;
                    case "pricepercent":
                        // Kiểm tra PricePercent của các ghế đã chọn
                        var selectedPricePercents = context.SelectedSeatTypePricePercents ?? new List<decimal>();
                        if (!selectedPricePercents.Any()) return false;
                        if (!decimal.TryParse(condition.TargetValue, out decimal priceTarget)) return false;
                        switch (condition.Operator)
                        {
                            case ">=": if (!selectedPricePercents.Any(pp => pp >= priceTarget)) return false; break;
                            case ">": if (!selectedPricePercents.Any(pp => pp > priceTarget)) return false; break;
                            case "<=": if (!selectedPricePercents.Any(pp => pp <= priceTarget)) return false; break;
                            case "<": if (!selectedPricePercents.Any(pp => pp < priceTarget)) return false; break;
                            case "=": case "==": if (!selectedPricePercents.Any(pp => pp == priceTarget)) return false; break;
                            case "!=": if (!selectedPricePercents.Any(pp => pp != priceTarget)) return false; break;
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
                    case "scheduleshow":
                    case "scheduleshowdate":
                        if (!DateTime.TryParse(condition.TargetValue, out DateTime scheduleTarget)) return false;
                        switch (condition.Operator)
                        {
                            case "=": case "==": if (context.ShowDate.Date != scheduleTarget.Date) return false; break;
                            case "!=": if (context.ShowDate.Date == scheduleTarget.Date) return false; break;
                            case ">=": if (!(context.ShowDate.Date >= scheduleTarget.Date)) return false; break;
                            case "<=": if (!(context.ShowDate.Date <= scheduleTarget.Date)) return false; break;
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

        // Lấy danh sách promotion hợp lệ cho food (chỉ xét TargetEntity)
        public List<Promotion> GetEligibleFoodPromotions(List<(int FoodId, int Quantity, decimal Price)> selectedFoods)
        {
            var allPromotions = _context.Promotions.Include(p => p.PromotionConditions).Where(p => p.IsActive).ToList();
            var foodPromotions = allPromotions
                .Where(p =>
                    p.PromotionConditions != null &&
                    p.PromotionConditions.Any(c => c.TargetEntity != null && c.TargetEntity.ToLower() == "food")
                )
                .ToList();
            return foodPromotions;
        }

        public bool IsPromotionEligible(Promotion promotion, PromotionCheckContext context)
        {
            if (promotion.PromotionConditions == null || !promotion.PromotionConditions.Any()) 
                return true;

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
                    case "seattypeid":
                        if (!int.TryParse(condition.TargetValue, out int seatTypeTarget)) return false;
                        var selectedSeatTypes = context.SelectedSeatTypeIds ?? new List<int>();
                        if (!selectedSeatTypes.Any()) return false;
                        switch (condition.Operator)
                        {
                            case "=": case "==": 
                                if (!selectedSeatTypes.Any(st => st == seatTypeTarget)) return false; break;
                            case "!=": 
                                if (selectedSeatTypes.Any(st => st == seatTypeTarget)) return false; break;
                            default: return false;
                        }
                        break;
                    case "typename":
                        var selectedTypeNames = context.SelectedSeatTypeNames ?? new List<string>();
                        if (!selectedTypeNames.Any()) return false;
                        switch (condition.Operator)
                        {
                            case "=": case "==": 
                                if (!selectedTypeNames.Any(tn => tn.Equals(condition.TargetValue, StringComparison.OrdinalIgnoreCase))) return false; break;
                            case "!=": 
                                if (selectedTypeNames.Any(tn => tn.Equals(condition.TargetValue, StringComparison.OrdinalIgnoreCase))) return false; break;
                            default: return false;
                        }
                        break;
                    case "pricepercent":
                        var selectedPricePercents = context.SelectedSeatTypePricePercents ?? new List<decimal>();
                        if (!selectedPricePercents.Any()) return false;
                        if (!decimal.TryParse(condition.TargetValue, out decimal priceTarget)) return false;
                        switch (condition.Operator)
                        {
                            case ">=": if (!selectedPricePercents.Any(pp => pp >= priceTarget)) return false; break;
                            case ">": if (!selectedPricePercents.Any(pp => pp > priceTarget)) return false; break;
                            case "<=": if (!selectedPricePercents.Any(pp => pp <= priceTarget)) return false; break;
                            case "<": if (!selectedPricePercents.Any(pp => pp < priceTarget)) return false; break;
                            case "=": case "==": if (!selectedPricePercents.Any(pp => pp == priceTarget)) return false; break;
                            case "!=": if (!selectedPricePercents.Any(pp => pp != priceTarget)) return false; break;
                            default: return false;
                        }
                        break;
                    case "accountid":
                        if (string.IsNullOrEmpty(context.MemberId)) return false;
                        var member = _context.Members.FirstOrDefault(m => m.MemberId == context.MemberId);
                        if (member == null || string.IsNullOrEmpty(member.AccountId)) return false;
                        var accountId = member.AccountId;
                        var invoices = _context.Invoices.Where(i => i.AccountId == accountId);
                        if (string.IsNullOrEmpty(condition.TargetValue))
                        {
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
                }
            }
            return true;
        }

        public List<Promotion> GetEligiblePromotionsForMember(PromotionCheckContext context)
        {
            var allPromotions = _context.Promotions.Include(p => p.PromotionConditions).Where(p => p.IsActive).ToList();
            var eligiblePromotions = new List<Promotion>();

            foreach (var promotion in allPromotions)
            {
                if (IsPromotionEligible(promotion, context))
                {
                    eligiblePromotions.Add(promotion);
                }
            }

            return eligiblePromotions;
        }

        // Áp dụng promotion cho từng món ăn riêng biệt, trả về danh sách món đã giảm giá và tên promotion
        public List<(int FoodId, decimal OriginalPrice, decimal DiscountedPrice, string PromotionName, decimal DiscountLevel)> ApplyFoodPromotionsToFoods(
            List<(int FoodId, int Quantity, decimal Price)> selectedFoods, List<Promotion> eligiblePromotions)
        {
            var result = new List<(int FoodId, decimal OriginalPrice, decimal DiscountedPrice, string PromotionName, decimal DiscountLevel)>();
            foreach (var food in selectedFoods)
            {
                decimal bestDiscount = 0;
                string promoName = null;
                decimal discountLevel = 0;
                foreach (var promo in eligiblePromotions)
                {
                    // Kiểm tra điều kiện cho từng promotion
                    bool eligible = true;
                    foreach (var cond in promo.PromotionConditions)
                    {
                        if (cond.TargetEntity?.ToLower() == "food" && cond.TargetField?.ToLower() == "price")
                        {
                            if (decimal.TryParse(cond.TargetValue, out decimal targetValue))
                            {
                                switch (cond.Operator)
                                {
                                    case ">=":
                                        if (!(food.Price >= targetValue)) eligible = false;
                                        break;
                                    case ">":
                                        if (!(food.Price > targetValue)) eligible = false;
                                        break;
                                    case "<=":
                                        if (!(food.Price <= targetValue)) eligible = false;
                                        break;
                                    case "<":
                                        if (!(food.Price < targetValue)) eligible = false;
                                        break;
                                    case "==":
                                    case "=":
                                        if (!(food.Price == targetValue)) eligible = false;
                                        break;
                                    case "!=":
                                        if (!(food.Price != targetValue)) eligible = false;
                                        break;
                                }
                            }
                        }
                        // Có thể bổ sung điều kiện khác nếu cần
                        if (!eligible) break;
                    }
                    if (eligible && promo.DiscountLevel.HasValue && promo.DiscountLevel.Value > bestDiscount)
                    {
                        bestDiscount = promo.DiscountLevel.Value;
                        promoName = promo.Title;
                        discountLevel = promo.DiscountLevel.Value;
                    }
                }
                decimal discountedPrice = food.Price;
                if (bestDiscount > 0)
                {
                    discountedPrice = food.Price * (1 - bestDiscount / 100m);
                }
                result.Add((food.FoodId, food.Price, discountedPrice, promoName, discountLevel));
            }
            return result;
        }
    }

    public class PromotionCheckContext {
        public string MemberId { get; set; }
        public int SeatCount { get; set; }
        public string MovieId { get; set; }
        public string MovieName { get; set; }
        public DateTime ShowDate { get; set; }
        // Thêm các trường cho SeatType
        public List<int> SelectedSeatTypeIds { get; set; } = new List<int>();
        public List<string> SelectedSeatTypeNames { get; set; } = new List<string>();
        public List<decimal> SelectedSeatTypePricePercents { get; set; } = new List<decimal>();
        // Thêm các trường khác nếu cần
    }
}
