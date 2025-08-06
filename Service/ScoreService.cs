using MovieTheater.Repository;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public class ScoreService : IScoreService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IMemberRepository _memberRepository;

        public ScoreService(IInvoiceRepository invoiceRepository, IMemberRepository memberRepository)
        {
            _invoiceRepository = invoiceRepository;
            _memberRepository = memberRepository;
        }

        public int GetCurrentScore(string accountId)
        {
            var member = _memberRepository.GetByAccountId(accountId);
            return member?.Score ?? 0;
        }

        public List<ScoreHistoryViewModel> GetScoreHistory(string accountId, DateTime? fromDate = null, DateTime? toDate = null, string? historyType = null)
        {
            var invoicesTask = (fromDate.HasValue || toDate.HasValue)
                ? _invoiceRepository.GetByDateRangeAsync(accountId, fromDate, toDate)
                : _invoiceRepository.GetByAccountIdAsync(accountId);
            invoicesTask.Wait();
            var invoices = invoicesTask.Result.ToList();

            var result = new List<ScoreHistoryViewModel>();

            foreach (var i in invoices)
            {
                if (i.Cancel) continue;
                if (historyType == "add" || string.IsNullOrEmpty(historyType) || historyType == "all")
                {
                    if (i.AddScore.HasValue && i.AddScore.Value > 0)
                    {
                        result.Add(new ScoreHistoryViewModel
                        {
                            DateCreated = i.BookingDate ?? DateTime.MinValue,
                            MovieName = i.MovieShow?.Movie?.MovieNameEnglish ?? "N/A",
                            Score = i.AddScore.Value,
                            Type = "add"
                        });
                    }
                }
                if (historyType == "use" || string.IsNullOrEmpty(historyType) || historyType == "all")
                {
                    if (i.UseScore.HasValue && i.UseScore.Value > 0)
                    {
                        result.Add(new ScoreHistoryViewModel
                        {
                            DateCreated = i.BookingDate ?? DateTime.MinValue,
                            MovieName = i.MovieShow?.Movie?.MovieNameEnglish ?? "N/A",
                            Score = i.UseScore.Value,
                            Type = "use"
                        });
                    }
                }
            }

            // Sắp xếp theo ngày giảm dần
            result = result.OrderByDescending(x => x.DateCreated).ToList();

            return result;
        }
    }
}