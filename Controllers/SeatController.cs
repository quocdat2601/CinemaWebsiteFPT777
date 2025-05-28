using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
