using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;

namespace MovieTheater.Controllers
{
    public class SeatTypeController : Controller
    {
        private readonly ISeatTypeService _seatTypeService;

        public SeatTypeController(ISeatTypeService seatTypeService)
        {
            _seatTypeService = seatTypeService;
        }
        // GET: SeatTypeController
        public ActionResult Index()
        {
            return View();
        }

        // GET: SeatTypeController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: SeatTypeController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: SeatTypeController/Create
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

        //GET: SeatTypeController/Edit/5
        //public ActionResult Edit(int id)
        //{
        //    return View();
        //}

        // POST: SeatTypeController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([FromForm] List<SeatType> seatTypes)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (seatTypes == null || !seatTypes.Any())
            {
                return BadRequest("No seat types provided");
            }

            try
            {
                foreach (var updated in seatTypes)
                {
                    if (updated.SeatTypeId > 0)
                    {
                        _seatTypeService.Update(updated);
                    }
                }
                _seatTypeService.Save();
                return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating seat types: {ex.Message}");
            }
        }

        // GET: SeatTypeController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: SeatTypeController/Delete/5
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
