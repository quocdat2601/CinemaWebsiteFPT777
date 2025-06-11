using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.ViewModels;
using System.Security.Claims;

namespace MovieTheater.Controllers
{
    public class ScoreController : Controller
    {
        private readonly MovieTheaterContext _context;

        public ScoreController(
       MovieTheaterContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult ScoreHistory()
        {
            var accountId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy toàn bộ Invoice của Account_ID, mặc định là Score Adding
            var query = _context.Invoices
                .Where(i => i.AccountId == accountId && i.AddScore > 0);

            var result = query.Select(i => new ScoreHistoryViewModel
            {
                DateCreated = i.BookingDate ?? DateTime.MinValue,
                MovieName = i.MovieName ?? "N/A",
                Score = i.AddScore ?? 0
            }).ToList();

            ViewBag.HistoryType = "add";
            return View("~/Views/Account/Tabs/Score.cshtml", result);
        }

        [HttpPost]
        public IActionResult ScoreHistory(DateTime fromDate, DateTime toDate, string historyType)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.Invoices
                .Where(i => i.AccountId == accountId &&
                            i.BookingDate >= fromDate &&
                            i.BookingDate <= toDate);

            if (historyType == "add")
            {
                query = query.Where(i => i.AddScore > 0);
            }
            else if (historyType == "use")
            {
                query = query.Where(i => i.UseScore > 0);
            }

            var result = query.Select(i => new ScoreHistoryViewModel
            {
                DateCreated = i.BookingDate ?? DateTime.MinValue,
                MovieName = i.MovieName ?? "N/A",
                Score = historyType == "add" ? (i.AddScore ?? 0) : (i.UseScore ?? 0)
            }).ToList();

            if (!result.Any())
            {
                ViewBag.Message = "No score history found for the selected period.";
            }
            System.Diagnostics.Debug.WriteLine("AccountId: " + accountId);
            // ... sau khi lấy result
            System.Diagnostics.Debug.WriteLine("Result count: " + result.Count);
            return View("~/Views/Account/Tabs/Score.cshtml", result);
        }

        [HttpGet]
        public IActionResult ScoreHistoryPartial(string fromDate, string toDate, string historyType)
        {
            var accountId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return Content("<div class='alert alert-danger'>Not logged in.</div>", "text/html");
            }
            var query = _context.Invoices.Where(i => i.AccountId == accountId);

            // Parse ngày nếu có
            DateTime fromDateValue, toDateValue;
            bool hasFrom = DateTime.TryParse(fromDate, out fromDateValue);
            bool hasTo = DateTime.TryParse(toDate, out toDateValue);

            if (hasFrom && hasTo)
            {
                query = query.Where(i => i.BookingDate >= fromDateValue && i.BookingDate <= toDateValue);
            }

            var result = new List<ScoreHistoryViewModel>();
            foreach (var i in query)
            {
                if (i.AddScore.HasValue && i.AddScore.Value > 0)
                {
                    result.Add(new ScoreHistoryViewModel
                    {
                        DateCreated = i.BookingDate ?? DateTime.MinValue,
                        MovieName = i.MovieName ?? "N/A",
                        Score = i.AddScore.Value,
                        Type = "add"
                    });
                }
                if (i.UseScore.HasValue && i.UseScore.Value > 0)
                {
                    result.Add(new ScoreHistoryViewModel
                    {
                        DateCreated = i.BookingDate ?? DateTime.MinValue,
                        MovieName = i.MovieName ?? "N/A",
                        Score = i.UseScore.Value,
                        Type = "use"
                    });
                }
            }
            // Sắp xếp theo ngày giảm dần
            result = result.OrderByDescending(x => x.DateCreated).ToList();

            // Nếu filter theo historyType thì chỉ trả về đúng loại
            if (!string.IsNullOrEmpty(historyType) && (historyType == "add" || historyType == "use"))
            {
                result = result.Where(x => x.Type == historyType).ToList();
            }

            return PartialView("~/Views/Account/_ScoreHistoryPartial.cshtml", result);
        }

    }
}
