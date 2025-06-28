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

        public List<Promotion> GetEligiblePromotionsForMember(string? memberId, int seatCount = 0, DateTime? showDate = null, string? movieId = null, string? movieName = null)
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

        private bool CheckInvoiceCondition(PromotionCondition condition, string accountId)
        {
            var query = _context.Invoices.Where(i => i.AccountId == accountId);

            switch (condition.TargetField.ToLower())
            {
                case "accountid":
                    // If target_value is null, check if accountID has never appeared in Invoice table
                    if (string.IsNullOrEmpty(condition.TargetValue) || condition.TargetValue.ToLower() == "null")
                    {
                        var hasInvoices = query.Any();
                        switch (condition.Operator)
                        {
                            case "=":
                            case "==":
                                return !hasInvoices; // accountID should NOT exist in Invoice table
                            case "!=":
                                return hasInvoices; // accountID should exist in Invoice table
                            default:
                                return true;
                        }
                    }
                    else
                    {
                        return CompareValues(accountId, condition.Operator, condition.TargetValue);
                    }
                case "totalmoney":
                    var totalMoney = query.Sum(i => i.TotalMoney ?? 0);
                    return CompareNumericValues(totalMoney, condition.Operator, condition.TargetValue);
                case "count":
                    var count = query.Count();
                    return CompareNumericValues(count, condition.Operator, condition.TargetValue);
                default:
                    return true;
            }
        }

        private bool CheckMemberCondition(PromotionCondition condition, string memberId)
        {
            var member = _context.Members
                .Include(m => m.Account)
                .FirstOrDefault(m => m.MemberId == memberId);

            if (member == null) return false;

            switch (condition.TargetField.ToLower())
            {
                case "score":
                    return CompareNumericValues(member.Score ?? 0, condition.Operator, condition.TargetValue);
                case "accountid":
                    // If target_value is null, check if accountID has never appeared in Member table
                    if (string.IsNullOrEmpty(condition.TargetValue) || condition.TargetValue.ToLower() == "null")
                    {
                        var hasMember = member != null;
                        switch (condition.Operator)
                        {
                            case "=":
                            case "==":
                                return !hasMember; // accountID should NOT exist in Member table
                            case "!=":
                                return hasMember; // accountID should exist in Member table
                            default:
                                return true;
                        }
                    }
                    else
                    {
                        return CompareValues(member.AccountId, condition.Operator, condition.TargetValue);
                    }
                default:
                    return true;
            }
        }

        private bool CheckAccountCondition(PromotionCondition condition, string accountId)
        {
            var account = _context.Accounts.FirstOrDefault(a => a.AccountId == accountId);
            if (account == null) return false;

            switch (condition.TargetField.ToLower())
            {
                case "accountid":
                    // If target_value is null, check if accountID has never appeared in Account table
                    if (string.IsNullOrEmpty(condition.TargetValue) || condition.TargetValue.ToLower() == "null")
                    {
                        var hasAccount = account != null;
                        switch (condition.Operator)
                        {
                            case "=":
                            case "==":
                                return !hasAccount; // accountID should NOT exist in Account table
                            case "!=":
                                return hasAccount; // accountID should exist in Account table
                            default:
                                return true;
                        }
                    }
                    else
                    {
                        return CompareValues(account.AccountId, condition.Operator, condition.TargetValue);
                    }
                case "registerdate":
                    if (account.RegisterDate.HasValue)
                    {
                        return CompareDateValues(account.RegisterDate.Value, condition.Operator, condition.TargetValue);
                    }
                    return false;
                default:
                    return true;
            }
        }

        private bool CompareValues(string actualValue, string operatorStr, string targetValue)
        {
            // If target_value is null or "null", treat it as null for string comparisons
            if (string.IsNullOrEmpty(targetValue) || targetValue.ToLower() == "null")
                targetValue = null;

            switch (operatorStr)
            {
                case "==":
                case "=":
                    return actualValue == targetValue;
                case "!=":
                    return actualValue != targetValue;
                case ">=":
                    return string.Compare(actualValue, targetValue) >= 0;
                case ">":
                    return string.Compare(actualValue, targetValue) > 0;
                case "<=":
                    return string.Compare(actualValue, targetValue) <= 0;
                case "<":
                    return string.Compare(actualValue, targetValue) < 0;
                default:
                    return true;
            }
        }

        private bool CompareNumericValues(decimal actualValue, string operatorStr, string targetValue)
        {
            // If target_value is null or "null", treat it as 0
            if (string.IsNullOrEmpty(targetValue) || targetValue.ToLower() == "null")
                targetValue = "0";

            if (!decimal.TryParse(targetValue, out decimal target))
                return false;

            switch (operatorStr)
            {
                case "==":
                case "=":
                    return actualValue == target;
                case "!=":
                    return actualValue != target;
                case ">=":
                    return actualValue >= target;
                case ">":
                    return actualValue > target;
                case "<=":
                    return actualValue <= target;
                case "<":
                    return actualValue < target;
                default:
                    return true;
            }
        }

        private bool CompareDateValues(DateOnly actualValue, string operatorStr, string targetValue)
        {
            if (string.IsNullOrEmpty(targetValue) || targetValue.ToLower() == "null")
                return false;

            if (!DateOnly.TryParse(targetValue, out DateOnly target))
                return false;

            switch (operatorStr)
            {
                case "==":
                case "=":
                    return actualValue == target;
                case "!=":
                    return actualValue != target;
                case ">=":
                    return actualValue >= target;
                case ">":
                    return actualValue > target;
                case "<=":
                    return actualValue <= target;
                case "<":
                    return actualValue < target;
                default:
                    return true;
            }
        }
    }
}
