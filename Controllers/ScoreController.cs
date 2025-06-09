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
            return View("~/Views/Account/Tabs/Score.cshtml");
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
        public IActionResult ScoreHistoryPartial(DateTime fromDate, DateTime toDate, string historyType)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return Content("<div class='alert alert-danger'>Not logged in.</div>", "text/html");
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
            return PartialView("~/Views/Account/_ScoreHistoryPartial.cshtml", result);
        }

    }
}
