using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    public class SeatController : Controller
    {
        private readonly ICinemaService _cinemaService;
        private readonly ISeatService _seatService;
        private readonly ISeatTypeService _seatTypeService;
        private readonly ICoupleSeatService _coupleSeatService;
        private readonly IMovieService _movieService;

        public SeatController(
            ICinemaService cinemaService, 
            ISeatService seatService, 
            ISeatTypeService seatTypeService,
            ICoupleSeatService coupleSeatService,
            IMovieService movieService)
        {
            _cinemaService = cinemaService;
            _seatService = seatService;
            _seatTypeService = seatTypeService;
            _coupleSeatService = coupleSeatService;
            _movieService = movieService;
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
            ViewBag.SeatTypes = _seatTypeService.GetAll();
            ViewBag.CoupleSeats = await _coupleSeatService.GetAllCoupleSeatsAsync();
            ViewBag.MovieId = movieId;

            var viewModel = new ShowroomEditViewModel
            {
                CinemaRoomId = movie.CinemaRoomId.Value,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                SeatLength = cinemaRoom.SeatLength ?? 0,
                SeatWidth = cinemaRoom.SeatWidth ?? 0,
                Seats = seats,
                MovieName = movie.MovieNameEnglish
            };

            return View("View", viewModel);
        }

        [HttpGet("Seat/Select")]
        public async Task<IActionResult> Select(string movieId, DateTime date, string time, string returnUrl)
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
            var bookedSeats = await _seatService.GetBookedSeatsAsync(movieId, date, time);

            var viewModel = new SeatSelectionViewModel
            {
                MovieId = movieId,
                MovieName = movie.MovieNameEnglish,
                ShowDate = date,
                ShowTime = time,
                CinemaRoomId = movie.CinemaRoomId.Value,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                SeatLength = cinemaRoom.SeatLength ?? 0,
                SeatWidth = cinemaRoom.SeatWidth ?? 0,
                Seats = seats,
                SeatTypes = _seatTypeService.GetAll().ToList()
            };

            ViewBag.BookedSeats = bookedSeats;
            ViewBag.CoupleSeats = await _coupleSeatService.GetAllCoupleSeatsAsync();
            viewModel.ReturnUrl = returnUrl;

            return View("~/Views/Showtime/SelectSeat.cshtml", viewModel);
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
