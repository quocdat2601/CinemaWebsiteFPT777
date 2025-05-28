using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    public class SeatController : Controller
    {
        private readonly ICinemaService _cinemaService;
        private readonly ISeatService _seatService;
        private readonly ISeatTypeService _seatTypeService;

        public SeatController(ICinemaService cinemaService, ISeatService seatService, ISeatTypeService seatTypeService)
        {
            _cinemaService = cinemaService;
            _seatService = seatService;
            _seatTypeService = seatTypeService;
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

            if (cinemaRoom == null)
                return NotFound();

            var viewModel = new SeatEditViewModel
            {
                CinemaRoomId = cinemaId,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                SeatWidth = cinemaRoom.SeatWidth,
                SeatLength = cinemaRoom.SeatLength,
                Seats = seats
            };

            return View(viewModel);
        }


        // POST: SeatController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSeatTypes([FromBody] List<SeatUpdateViewModel> updatedSeats)
        {
            if (updatedSeats == null || !updatedSeats.Any())
                return BadRequest("No seat data submitted");

            foreach (var updated in updatedSeats)
            {
                var seat = await _seatService.GetSeatByIdAsync(updated.SeatId);
                if (seat != null)
                {
                    var seatType = _seatTypeService.GetById(updated.SeatTypeId);
                    if (seatType != null)
                    {
                        seat.SeatTypeId = seatType.SeatTypeId;
                        _seatService.UpdateSeatAsync(seat);
                    }
                }
            }

            _seatService.Save();

            return Ok();
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
