using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;

namespace MovieTheater.Controllers
{
    public class BookingController : Controller
    {
        private readonly IBookingService _service;

        public BookingController(IBookingService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> TicketBooking(string movieId = null)
        {
            var movies = await _service.GetAvailableMoviesAsync();
            ViewBag.MovieList = movies;
            ViewBag.SelectedMovieId = movieId;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDates(string movieId)
        {
            var dates = await _service.GetShowDatesAsync(movieId);
            return Json(dates.Select(d => d.ToString("yyyy-MM-dd")));
        }

        [HttpGet]
        public async Task<IActionResult> GetTimes(string movieId, DateTime date)
        {
            var times = await _service.GetShowTimesAsync(movieId, date);
            return Json(times);
        }

    }
}
