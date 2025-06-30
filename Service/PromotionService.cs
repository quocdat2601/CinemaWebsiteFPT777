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

        public List<Promotion> GetEligiblePromotionsForMember(string memberId, int seatCount = 0, DateTime? showDate = null, string movieId = null, string movieName = null)
        {
            var today = DateTime.Today;
            string accountId = null;
            MovieTheater.Models.Member member = null;
            if (!string.IsNullOrEmpty(memberId))
            {
                member = _context.Members
                    .Include(m => m.Account)
                    .FirstOrDefault(m => m.MemberId == memberId);
                if (member != null && member.Account != null)
                    accountId = member.Account.AccountId;
            }

            // Get all promotion conditions
            var allPromotionConditions = _context.PromotionConditions
                .Include(pc => pc.Promotion)
                .Where(pc => pc.Promotion.IsActive)
                .ToList();

            // Group conditions by promotion ID
            var conditionsByPromotion = allPromotionConditions
                .GroupBy(pc => pc.PromotionId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // List of known date fields (expand as needed)
            var dateFields = new HashSet<string>(new[] {
                "dateofbirth", "registerdate", "bookingdate", "scheduleshow", "fromdate", "todate", "endtime", "starttime", "showdate1"
            }, StringComparer.OrdinalIgnoreCase);

            // Fetch movie if movieId is provided
            MovieTheater.Models.Movie movie = null;
            if (!string.IsNullOrEmpty(movieId))
            {
                movie = _context.Movies.FirstOrDefault(m => m.MovieId == movieId);
            }

            var eligiblePromotionIds = new List<int>();
            foreach (var promotionGroup in conditionsByPromotion)
            {
                int promotionId = promotionGroup.Key.Value;
                var conditions = promotionGroup.Value;
                bool allConditionsMet = true;

                // First check if today is between StartTime and EndTime
                var promotion = conditions.FirstOrDefault()?.Promotion;
                if (promotion != null)
                {
                    bool isWithinDateRange = true;
                    if (promotion.StartTime.HasValue)
                        isWithinDateRange = isWithinDateRange && today >= promotion.StartTime.Value.Date;
                    if (promotion.EndTime.HasValue)
                        isWithinDateRange = isWithinDateRange && today <= promotion.EndTime.Value.Date;
                    if (!isWithinDateRange)
                        continue;
                }

                foreach (var condition in conditions)
                {
                    bool conditionMet = false;

                    // If condition requires member/account and no member is selected, treat as not met
                    if ((condition.TargetEntity != null &&
                        (condition.TargetEntity.ToLower() == "member" || condition.TargetEntity.ToLower() == "account"))
                        && string.IsNullOrEmpty(memberId))
                    {
                        conditionMet = false;
                    }
                    // Check if Target_Field is "seat" - use existing seat logic
                    else if (condition.TargetField != null && condition.TargetField.ToLower() == "seat")
                    {
                        if (int.TryParse(condition.TargetValue, out int targetValue))
                        {
                            switch (condition.Operator)
                            {
                                case ">=": conditionMet = seatCount >= targetValue; break;
                                case ">": conditionMet = seatCount > targetValue; break;
                                case "<=": conditionMet = seatCount <= targetValue; break;
                                case "<": conditionMet = seatCount < targetValue; break;
                                case "==": case "=": conditionMet = seatCount == targetValue; break;
                                case "!=": conditionMet = seatCount != targetValue; break;
                                default: conditionMet = true; break;
                            }
                        }
                    }
                    // If Target_Field contains 'name', compare with movieName provided by user
                    else if (condition.TargetField != null && condition.TargetField.ToLower().Contains("name") && !string.IsNullOrEmpty(movieName))
                    {
                        string actual = movieName.Trim().ToLower();
                        string target = (condition.TargetValue ?? string.Empty).Trim().ToLower();
                        switch (condition.Operator)
                        {
                            case "==":
                            case "=":
                                conditionMet = actual == target;
                                break;
                            case "!=":
                                conditionMet = actual != target;
                                break;
                            case ">=":
                                conditionMet = string.Compare(actual, target, StringComparison.Ordinal) >= 0;
                                break;
                            case ">":
                                conditionMet = string.Compare(actual, target, StringComparison.Ordinal) > 0;
                                break;
                            case "<=":
                                conditionMet = string.Compare(actual, target, StringComparison.Ordinal) <= 0;
                                break;
                            case "<":
                                conditionMet = string.Compare(actual, target, StringComparison.Ordinal) < 0;
                                break;
                            default:
                                conditionMet = true;
                                break;
                        }
                    }
                    // If Target_Entity is movie and Target_Field is a date type, compare with movie's date field
                    else if (condition.TargetEntity != null && condition.TargetEntity.ToLower() == "movie" && movie != null && condition.TargetField != null && dateFields.Contains(condition.TargetField.ToLower()))
                    {
                        DateOnly? movieDate = null;
                        // Map field name to property
                        switch (condition.TargetField.ToLower())
                        {
                            case "fromdate":
                                if (movie.FromDate.HasValue) movieDate = movie.FromDate.Value;
                                break;
                            case "todate":
                                if (movie.ToDate.HasValue) movieDate = movie.ToDate.Value;
                                break;
                        }
                        if (movieDate.HasValue)
                        {
                            conditionMet = CompareDateValues(movieDate.Value, condition.Operator, condition.TargetValue);
                        }
                        else
                        {
                            conditionMet = false;
                        }
                    }
                    // If Target_Field is a date type, compare with showDate
                    else if (condition.TargetField != null && dateFields.Contains(condition.TargetField.ToLower()) && showDate.HasValue)
                    {
                        conditionMet = CompareDateValues(DateOnly.FromDateTime(showDate.Value), condition.Operator, condition.TargetValue);
                    }
                    // Check member conditions
                    else if (condition.TargetEntity != null && condition.TargetEntity.ToLower() == "member" && !string.IsNullOrEmpty(memberId))
                    {
                        conditionMet = CheckMemberCondition(condition, memberId);
                    }
                    // Check account conditions
                    else if (condition.TargetEntity != null && condition.TargetEntity.ToLower() == "account" && !string.IsNullOrEmpty(accountId))
                    {
                        conditionMet = CheckAccountCondition(condition, accountId);
                    }
                    // Check invoice conditions
                    else if (condition.TargetEntity != null && condition.TargetEntity.ToLower() == "invoice" && !string.IsNullOrEmpty(accountId))
                    {
                        conditionMet = CheckInvoiceCondition(condition, accountId);
                    }
                    // Default case - treat as met if no specific logic
                    else
                    {
                        conditionMet = true;
                    }

                    if (!conditionMet)
                    {
                        allConditionsMet = false;
                        break;
                    }
                }

                if (allConditionsMet)
                {
                    eligiblePromotionIds.Add(promotionId);
                }
            }

            // Return the actual promotion objects
            return _context.Promotions
                .Include(p => p.PromotionConditions)
                .Where(p => eligiblePromotionIds.Contains(p.PromotionId))
                .ToList();
        }

        private bool CheckCondition(PromotionCondition condition, string accountId)
        {
            if (string.IsNullOrEmpty(condition.TargetField) || string.IsNullOrEmpty(condition.TargetValue))
                return true;

            var account = _context.Accounts.FirstOrDefault(a => a.AccountId == accountId);
            if (account == null) return false;

            var propertyValue = GetPropertyValue(account, condition.TargetField);
            if (propertyValue == null) return true;

            return CompareValues(propertyValue.ToString(), condition.Operator, condition.TargetValue);
        }

        private bool CheckInvoiceCondition(PromotionCondition condition, string accountId)
        {
            if (string.IsNullOrEmpty(condition.TargetField) || string.IsNullOrEmpty(condition.TargetValue))
                return true;

            var invoices = _context.Invoices.Where(i => i.AccountId == accountId).ToList();
            if (!invoices.Any()) return false;

            foreach (var invoice in invoices)
            {
                var propertyValue = GetPropertyValue(invoice, condition.TargetField);
                if (propertyValue != null && CompareValues(propertyValue.ToString(), condition.Operator, condition.TargetValue))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckMemberCondition(PromotionCondition condition, string memberId)
        {
            if (string.IsNullOrEmpty(condition.TargetField) || string.IsNullOrEmpty(condition.TargetValue))
                return true;

            var member = _context.Members.FirstOrDefault(m => m.MemberId == memberId);
            if (member == null) return false;

            var propertyValue = GetPropertyValue(member, condition.TargetField);
            if (propertyValue == null) return true;

            return CompareValues(propertyValue.ToString(), condition.Operator, condition.TargetValue);
        }

        private bool CheckAccountCondition(PromotionCondition condition, string accountId)
        {
            if (string.IsNullOrEmpty(condition.TargetField) || string.IsNullOrEmpty(condition.TargetValue))
                return true;

            var account = _context.Accounts.FirstOrDefault(a => a.AccountId == accountId);
            if (account == null) return false;

            var propertyValue = GetPropertyValue(account, condition.TargetField);
            if (propertyValue == null) return true;

            return CompareValues(propertyValue.ToString(), condition.Operator, condition.TargetValue);
        }

        private object? GetPropertyValue(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);
            return property?.GetValue(obj);
        }

        private bool CompareValues(string actualValue, string operatorStr, string targetValue)
        {
            if (string.IsNullOrEmpty(actualValue) || string.IsNullOrEmpty(targetValue))
                return false;

            switch (operatorStr)
            {
                case "==":
                case "=":
                    return actualValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase);
                case "!=":
                    return !actualValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase);
                case ">=":
                    return string.Compare(actualValue, targetValue, StringComparison.OrdinalIgnoreCase) >= 0;
                case ">":
                    return string.Compare(actualValue, targetValue, StringComparison.OrdinalIgnoreCase) > 0;
                case "<=":
                    return string.Compare(actualValue, targetValue, StringComparison.OrdinalIgnoreCase) <= 0;
                case "<":
                    return string.Compare(actualValue, targetValue, StringComparison.OrdinalIgnoreCase) < 0;
                default:
                    return true;
            }
        }

        private bool CompareNumericValues(decimal actualValue, string operatorStr, string targetValue)
        {
            if (!decimal.TryParse(targetValue, out decimal target))
                return false;

            switch (operatorStr)
            {
                case ">=": return actualValue >= target;
                case ">": return actualValue > target;
                case "<=": return actualValue <= target;
                case "<": return actualValue < target;
                case "==":
                case "=": return actualValue == target;
                case "!=": return actualValue != target;
                default: return true;
            }
        }

        private bool CompareDateValues(DateOnly actualValue, string operatorStr, string targetValue)
        {
            if (string.IsNullOrEmpty(targetValue))
                return false;

            // Try to parse the target value as a date
            if (DateOnly.TryParse(targetValue, out DateOnly target))
            {
                switch (operatorStr)
                {
                    case ">=": return actualValue >= target;
                    case ">": return actualValue > target;
                    case "<=": return actualValue <= target;
                    case "<": return actualValue < target;
                    case "==":
                    case "=": return actualValue == target;
                    case "!=": return actualValue != target;
                    default: return true;
                }
            }

            // If parsing fails, try to compare as strings
            return CompareValues(actualValue.ToString("yyyy-MM-dd"), operatorStr, targetValue);
        }
    }
}
