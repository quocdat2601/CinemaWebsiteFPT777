using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MovieTheater.Controllers
{
    [Authorize]
    public class TicketController : Controller
    {
        private readonly ITicketService _ticketService;

        // Constants for string literals
        private const string LOGIN_ACTION = "Login";
        private const string ACCOUNT_CONTROLLER = "Account";
        private const string TOAST_MESSAGE = "ToastMessage";
        private const string ERROR_MESSAGE = "ErrorMessage";
        private const string INDEX_ACTION = "Index";

        public TicketController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        //[HttpGet]
        //public IActionResult History()
        //{
        //    // Redirect /Ticket/History to /Ticket/Index
        //    return RedirectToAction(INDEX_ACTION);
        //}

        //[HttpGet]
        //public async Task<IActionResult> Index()
        //{
        //    var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (string.IsNullOrEmpty(accountId))
        //        return RedirectToAction(LOGIN_ACTION, ACCOUNT_CONTROLLER);

        //    var bookings = await _ticketService.GetUserTicketsAsync(accountId);
        //    return View(bookings);
        //}

        [HttpGet]
        public async Task<IActionResult> Booked()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction(LOGIN_ACTION, ACCOUNT_CONTROLLER);

            var bookings = await _ticketService.GetUserTicketsAsync(accountId, 1); // 1 = Completed
            return View(INDEX_ACTION, bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Canceled()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction(LOGIN_ACTION, ACCOUNT_CONTROLLER);

            var bookings = await _ticketService.GetUserTicketsAsync(accountId, 0); // 0 = Incomplete
            return View(INDEX_ACTION, bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction(LOGIN_ACTION, ACCOUNT_CONTROLLER);

            var bookingDetails = await _ticketService.GetTicketDetailsAsync(id, accountId);
            if (bookingDetails == null)
                return NotFound();

            return View(bookingDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string id, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction(LOGIN_ACTION, ACCOUNT_CONTROLLER);

            var (success, messages) = await _ticketService.CancelTicketAsync(id, accountId);
            TempData[success ? TOAST_MESSAGE : ERROR_MESSAGE] = string.Join("<br/>", messages);

            // Không redirect, chỉ reload trang hiện tại để hiển thị trạng thái đã hủy
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            
            // Redirect về trang hiện tại (reload)
            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

        [HttpGet]
        public async Task<IActionResult> HistoryPartial(DateTime? fromDate, DateTime? toDate, string status = "all")
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return Json(new { success = false, message = "Not logged in." });

            var result = await _ticketService.GetHistoryPartialAsync(accountId, fromDate, toDate, status);
            return Json(new { success = true, data = result });
        }

        public IActionResult Test()
        {
            return Content("Test OK");
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Employee")]
        public async Task<IActionResult> CancelByAdmin(string id, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Admin";
            var (success, messages) = await _ticketService.CancelTicketByAdminAsync(id, currentRole);
            TempData[success ? TOAST_MESSAGE : ERROR_MESSAGE] = string.Join("<br/>", messages);

            // Không redirect, chỉ reload trang hiện tại để hiển thị trạng thái đã hủy
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            
            // Redirect về trang hiện tại (reload)
            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }
    }
}