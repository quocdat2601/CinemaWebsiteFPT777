using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.ViewModels;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Service;
using Microsoft.AspNetCore.Authorization;

namespace MovieTheater.Controllers
{
    [Authorize]
    public class ScoreController : Controller
    {
        private readonly IScoreService _scoreService;

        public ScoreController(IScoreService scoreService)
        {
            _scoreService = scoreService;
        }

        /// <summary>
        /// Xem lịch sử điểm cộng
        /// </summary>
        /// <remarks>url: /Score/ScoreHistory (GET)</remarks>
        [HttpGet]
        public IActionResult ScoreHistory()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.CurrentScore = _scoreService.GetCurrentScore(accountId);
            var result = _scoreService.GetScoreHistory(accountId, null, null, "add");
            ViewBag.HistoryType = "add";
            return View("~/Views/Account/Tabs/Score.cshtml", result);
        }

        /// <summary>
        /// Xem lịch sử điểm theo khoảng ngày và loại điểm
        /// </summary>
        /// <remarks>url: /Score/ScoreHistory (POST)</remarks>
        [HttpPost]
        public IActionResult ScoreHistory(DateTime fromDate, DateTime toDate, string historyType)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.CurrentScore = _scoreService.GetCurrentScore(accountId);
            var result = _scoreService.GetScoreHistory(accountId, fromDate, toDate, historyType);
            if (!result.Any())
            {
                ViewBag.Message = "No score history found for the selected period.";
            }
            return View("~/Views/Account/Tabs/Score.cshtml", result);
        }

        /// <summary>
        /// Lấy lịch sử điểm (partial, ajax)
        /// </summary>
        /// <remarks>url: /Score/ScoreHistoryPartial (GET)</remarks>
        [HttpGet]
        public IActionResult ScoreHistoryPartial(string fromDate, string toDate, string historyType)
        {
            var accountId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return Json(new { success = false, message = "Not logged in." });
            }

            int currentScore = _scoreService.GetCurrentScore(accountId);
            DateTime? from = null, to = null;
            if (DateTime.TryParse(fromDate, out var f)) from = f;
            if (DateTime.TryParse(toDate, out var t)) to = t;
            var data = _scoreService.GetScoreHistory(accountId, from, to, historyType);
            var result = data.Select(i => new {
                dateCreated = i.DateCreated,
                movieName = i.MovieName,
                score = i.Score,
                type = i.Type
            }).OrderByDescending(x => x.dateCreated).ToList();
            return Json(new { success = true, currentScore = currentScore, data = result });
        }



    }
}
