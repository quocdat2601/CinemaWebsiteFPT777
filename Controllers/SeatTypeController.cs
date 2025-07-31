using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;
using Microsoft.AspNetCore.Authorization;

namespace MovieTheater.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SeatTypeController : Controller
    {
        private readonly ISeatTypeService _seatTypeService;

        public SeatTypeController(ISeatTypeService seatTypeService)
        {
            _seatTypeService = seatTypeService;
        }
        /// <summary>
        /// Trang danh sách loại ghế
        /// </summary>
        /// <remarks>url: /SeatType/Index (GET)</remarks>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Xem chi tiết loại ghế
        /// </summary>
        /// <remarks>url: /SeatType/Details (GET)</remarks>
        public ActionResult Details(int id)
        {
            return View();
        }

        /// <summary>
        /// Trang tạo loại ghế mới
        /// </summary>
        /// <remarks>url: /SeatType/Create (GET)</remarks>
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Tạo loại ghế mới
        /// </summary>
        /// <remarks>url: /SeatType/Create (POST)</remarks>
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

        /// <summary>
        /// Sửa loại ghế
        /// </summary>
        /// <remarks>url: /SeatType/Edit (POST)</remarks>
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
                return RedirectToAction("MainPage", "Admin", new { tab = "VersionMg" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating seat types: {ex.Message}");
            }
        }

        /// <summary>
        /// Trang xóa loại ghế
        /// </summary>
        /// <remarks>url: /SeatType/Delete (GET)</remarks>
        public ActionResult Delete(int id)
        {
            return View();
        }

        /// <summary>
        /// Xóa loại ghế
        /// </summary>
        /// <remarks>url: /SeatType/Delete (POST)</remarks>
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
