using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Microsoft.Extensions.Logging;
using MovieTheater.Repository;

namespace MovieTheater.Controllers
{
    public class SeatController : Controller
    {
        private readonly ICinemaService _cinemaService;
        private readonly ISeatService _seatService;
        private readonly ISeatTypeService _seatTypeService;
        private readonly ICoupleSeatService _coupleSeatService;
        private readonly IMovieService _movieService;
        private readonly ILogger<SeatController> _logger;
        private readonly IScheduleSeatRepository _scheduleSeatRepository;

        public SeatController(
            ICinemaService cinemaService,
            ISeatService seatService,
            ISeatTypeService seatTypeService,
            ICoupleSeatService coupleSeatService,
            IMovieService movieService,
            ILogger<SeatController> logger,
            IScheduleSeatRepository scheduleSeatRepository)
        {
            _cinemaService = cinemaService;
            _seatService = seatService;
            _seatTypeService = seatTypeService;
            _coupleSeatService = coupleSeatService;
            _movieService = movieService;
            _logger = logger;
            _scheduleSeatRepository = scheduleSeatRepository;
        }
        // GET: SeatController
        public ActionResult Index()
        {
            return View();
        }

        // GET: SeatController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: SeatController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: SeatController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        [HttpGet("Seat/Edit/{cinemaId}")]
        public async Task<IActionResult> Edit(int cinemaId)
        {
            var seats = await _seatService.GetSeatsByRoomIdAsync(cinemaId);
            var cinemaRoom = _cinemaService.GetById(cinemaId);
            ViewBag.SeatTypes = _seatTypeService.GetAll();
            ViewBag.CoupleSeats = await _coupleSeatService.GetAllCoupleSeatsAsync();

            if (cinemaRoom == null)
                return NotFound();

            var viewModel = new ShowroomEditViewModel
            {
                CinemaRoomId = cinemaId,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                SeatWidth = (int)cinemaRoom.SeatWidth,
                SeatLength = (int)cinemaRoom.SeatLength,
                Seats = seats
            };

            return View(viewModel);
        }

        [HttpGet("Seat/View/{cinemaId}")]
        public async Task<IActionResult> View(int cinemaId)
        {
            var seats = await _seatService.GetSeatsByRoomIdAsync(cinemaId);
            var cinemaRoom = _cinemaService.GetById(cinemaId);
            var seatTypes = _seatTypeService.GetAll().ToList();

            if (cinemaRoom == null)
                return NotFound();

            // Lấy tất cả movie show của phòng này, lấy movieShowId mới nhất
            var movieShows = _movieService.GetMovieShow().Where(ms => ms.CinemaRoomId == cinemaRoom.CinemaRoomId).ToList();
            var latestMovieShow = movieShows.OrderByDescending(ms => ms.MovieShowId).FirstOrDefault();
            List<int> bookedSeats = new List<int>();
            if (latestMovieShow != null)
            {
                var scheduleSeats = await _scheduleSeatRepository.GetScheduleSeatsByMovieShowAsync(latestMovieShow.MovieShowId);
                bookedSeats = scheduleSeats.Where(s => s.SeatStatusId == 2 && s.SeatId.HasValue).Select(s => s.SeatId.Value).ToList();
            }
            ViewBag.BookedSeats = bookedSeats;

            var viewModel = new SeatSelectionViewModel
            {
                CinemaRoomId = cinemaId,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                SeatWidth = (int)cinemaRoom.SeatWidth,
                SeatLength = (int)cinemaRoom.SeatLength,
                Seats = seats,
                SeatTypes = seatTypes
            };

            return View(viewModel);
        }

        [HttpGet("Seat/ViewByMovie/{movieId}")]
        public async Task<IActionResult> ViewByMovie(string movieId)
        {
            var movie = _movieService.GetById(movieId);
            if (movie == null || !movie.CinemaRoomId.HasValue)
            {
                return NotFound();
            }

            var cinemaRoom = _cinemaService.GetById(movie.CinemaRoomId.Value);
            if (cinemaRoom == null)
            {
                return NotFound();
            }

            var seats = await _seatService.GetSeatsByRoomIdAsync(movie.CinemaRoomId.Value);
            var seatTypes = _seatTypeService.GetAll().ToList();
            ViewBag.MovieId = movieId;

            // Lấy movie show mới nhất cho phòng này
            var movieShows = _movieService.GetMovieShow().Where(ms => ms.CinemaRoomId == cinemaRoom.CinemaRoomId).ToList();
            var latestMovieShow = movieShows.OrderByDescending(ms => ms.MovieShowId).FirstOrDefault();
            List<int> bookedSeats = new List<int>();
            if (latestMovieShow != null)
            {
                var scheduleSeats = await _scheduleSeatRepository.GetScheduleSeatsByMovieShowAsync(latestMovieShow.MovieShowId);
                bookedSeats = scheduleSeats.Where(s => s.SeatStatusId == 2 && s.SeatId.HasValue).Select(s => s.SeatId.Value).ToList();
            }
            ViewBag.BookedSeats = bookedSeats;

            var viewModel = new SeatSelectionViewModel
            {
                MovieId = movieId,
                MovieName = movie.MovieNameEnglish,
                CinemaRoomId = movie.CinemaRoomId.Value,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                SeatLength = cinemaRoom.SeatLength ?? 0,
                SeatWidth = cinemaRoom.SeatWidth ?? 0,
                Seats = seats,
                SeatTypes = seatTypes
            };

            return View("View", viewModel);
        }

        [HttpGet]
        [Route("Seat/Select")]
        public async Task<IActionResult> Select([FromQuery] string movieId, [FromQuery] string date, [FromQuery] string time)
        {            
            var movie = _movieService.GetById(movieId);
            if (movie == null)
            {
                return NotFound();
            }

            // Parse the date from dd/MM/yyyy format
            if (!DateTime.TryParseExact(date, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
            {
                return BadRequest("Invalid date format. Please use dd/MM/yyyy format.");
            }

            // Get all movie shows for this movie
            var movieShows = _movieService.GetMovieShows(movieId);

            // Get the specific movie show for this date and time
            var movieShow = movieShows.FirstOrDefault(ms => 
                ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(parsedDate) && 
                ms.Schedule?.ScheduleTime == time);

            if (movieShow == null)
            {
                _logger.LogWarning($"No movie show found for movie {movieId} with date {date} and time {time}");
                return NotFound("Movie show not found for the specified date and time.");
            }


            var cinemaRoom = movieShow.CinemaRoom;
            if (cinemaRoom == null)
            {
                _logger.LogWarning($"No cinema room found for movie show {movieShow.MovieShowId}");
                return NotFound("Cinema room not found for this movie show.");
            }

            var seats = await _seatService.GetSeatsByRoomIdAsync(cinemaRoom.CinemaRoomId);
            var seatTypes = _seatTypeService.GetAll().ToList();

            // Get booked seats for this movie show (SeatStatusId == 2)
            var bookedScheduleSeats = await _scheduleSeatRepository.GetScheduleSeatsByMovieShowAsync(movieShow.MovieShowId);
            var bookedSeats = bookedScheduleSeats.Where(s => s.SeatStatusId == 2 && s.SeatId.HasValue).Select(s => s.SeatId.Value).ToList();
            
            // Log để debug
            _logger.LogInformation($"MovieShowId: {movieShow.MovieShowId}, Total ScheduleSeats: {bookedScheduleSeats.Count()}, Booked Seats: {string.Join(", ", bookedSeats)}");
            
            ViewBag.BookedSeats = bookedSeats;
            ViewBag.MovieShow = movieShow;

            var viewModel = new SeatSelectionViewModel
            {
                MovieId = movieId,
                MovieName = movie.MovieNameEnglish,
                MovieShowId = movieShow.MovieShowId,
                ShowDate = parsedDate,
                ShowTime = time,
                CinemaRoomId = cinemaRoom.CinemaRoomId,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                SeatLength = cinemaRoom.SeatLength ?? 0,
                SeatWidth = cinemaRoom.SeatWidth ?? 0,
                Seats = seats,
                SeatTypes = seatTypes
            };

            return View("View", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSeatTypes([FromBody] List<SeatTypeUpdateModel> updates)
        {
            foreach (var update in updates)
            {
                var seat = await _seatService.GetSeatByIdAsync(update.SeatId);
                if (seat != null)
                {
                    seat.SeatTypeId = update.NewSeatTypeId;
                }
            }

            _seatService.Save();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCoupleSeat([FromBody] CoupleSeat coupleSeat)
        {
            try
            {
                await _coupleSeatService.CreateCoupleSeatAsync(coupleSeat.FirstSeatId, coupleSeat.SecondSeatId);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: SeatController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: SeatController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}