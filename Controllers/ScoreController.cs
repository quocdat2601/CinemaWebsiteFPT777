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
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var member = _context.Members.FirstOrDefault(m => m.AccountId == accountId);
            ViewBag.CurrentScore = member?.Score ?? 0;

            var result = _context.Invoices
                .Where(i => i.AccountId == accountId && i.AddScore > 0)
                .Select(i => new ScoreHistoryViewModel
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

            var member = _context.Members.FirstOrDefault(m => m.AccountId == accountId);
            ViewBag.CurrentScore = member?.Score ?? 0;

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

            return View("~/Views/Account/Tabs/Score.cshtml", result);
        }

        [HttpGet]
        public IActionResult ScoreHistoryPartial(string fromDate, string toDate, string historyType)
        {
            var accountId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return Json(new { success = false, message = "Not logged in." });
            }

            // Get current score
            var member = _context.Members.FirstOrDefault(m => m.AccountId == accountId);
            var currentScore = member?.Score ?? 0;

            var query = _context.Invoices.Where(i => i.AccountId == accountId);

            // Parse dates if provided
            DateTime fromDateValue, toDateValue;
            bool hasFrom = DateTime.TryParse(fromDate, out fromDateValue);
            bool hasTo = DateTime.TryParse(toDate, out toDateValue);

            if (hasFrom && hasTo)
            {
                query = query.Where(i => i.BookingDate >= fromDateValue && i.BookingDate <= toDateValue);
            }

            var result = new List<object>();
            foreach (var i in query)
            {
                if (historyType != "use" && i.AddScore.HasValue && i.AddScore.Value > 0)
                {
                    result.Add(new
                    {
                        dateCreated = i.BookingDate ?? DateTime.MinValue,
                        movieName = i.MovieName ?? "N/A",
                        score = i.AddScore.Value,
                        type = "add"
                    });
                }
                if (historyType != "add" && i.UseScore.HasValue && i.UseScore.Value > 0)
                {
                    result.Add(new
                    {
                        dateCreated = i.BookingDate ?? DateTime.MinValue,
                        movieName = i.MovieName ?? "N/A",
                        score = i.UseScore.Value,
                        type = "use"
                    });
                }
            }

            result = result.OrderByDescending(x => ((dynamic)x).dateCreated).ToList();

            return Json(new { 
                success = true, 
                currentScore = currentScore,
                data = result 
            });
        }

    }
}
