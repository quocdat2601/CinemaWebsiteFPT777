using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;

namespace MovieTheater.Controllers
{
   public class BookingController : Controller
   {
       private readonly IBookingService _bookingService;
       private readonly IMovieService _movieService;
       private readonly ISeatService _seatService;
       private readonly IAccountService _accountService;
       private readonly ISeatTypeService _seatTypeService;
       private readonly IMemberRepository _memberRepository;
       private readonly IInvoiceService _invoiceService;
       private readonly ICinemaService _cinemaService;
       private readonly ILogger<BookingController> _logger;
       private readonly IScheduleSeatRepository _scheduleSeatRepository;
       public BookingController(IBookingService bookingService,
                        IMovieService movieService,
                        ISeatService seatService,
                        IAccountService accountService,
                        ISeatTypeService seatTypeService,
                        IMemberRepository memberRepository,
                        ILogger<BookingController> logger,
                        IInvoiceService invoiceService,
                        ICinemaService cinemaService,
                        IScheduleSeatRepository scheduleSeatRepository)
       {
           _bookingService = bookingService;
           _movieService = movieService;
           _seatService = seatService;
           _accountService = accountService;
           _seatTypeService = seatTypeService;
           _memberRepository = memberRepository;
           _logger = logger;
           _invoiceService = invoiceService;
           _cinemaService = cinemaService;
           _scheduleSeatRepository = scheduleSeatRepository;
       }

       //GET: /api/booking/ticketbooking
       /// <summary>
       /// Hiển thị giao diện đặt vé.
       /// </summary>
       /// <param name="movieId">Id của phim nếu đã chọn trước.</param>
       /// <returns>View với danh sách phim có thể đặt vé.</returns>
       [HttpGet]
       public async Task<IActionResult> TicketBooking(string movieId = null)
       {
           var movies = await _bookingService.GetAvailableMoviesAsync();
           ViewBag.MovieList = movies;
           ViewBag.SelectedMovieId = movieId;

           if (!string.IsNullOrEmpty(movieId))
           {
               // Get movie shows for the selected movie
               var movieShows = _movieService.GetMovieShows(movieId);
                
               // Group by date and time
               var showsByDate = movieShows
                   .Where(ms => ms.Schedule != null && ms.Schedule.ScheduleTime.HasValue)
                   .GroupBy(ms => ms.ShowDate.ToString("dd/MM/yyyy"))
                   .ToDictionary(
                       g => g.Key,
                       g => g.Select(ms => ms.Schedule.ScheduleTime.Value.ToString("HH:mm"))
                             .Distinct()
                             .OrderBy(t => t)
                             .ToList()
                   );

               ViewBag.ShowsByDate = showsByDate;
           }

           return View();
       }

       //GET: /api/booking/getdates
       /// <summary>
       /// Trả về danh sách các ngày có suất chiếu cho phim.
       /// </summary>
       /// <param name="movieId">Id của phim.</param>
       /// <returns>Json danh sách ngày (yyyy-MM-dd).</returns>
       [HttpGet]
       public async Task<IActionResult> GetDates(string movieId)
       {
           var dates = await _bookingService.GetShowDatesAsync(movieId);
           return Json(dates.Select(d => d.ToString("yyyy-MM-dd")));
       }

       //GET: /api/booking/gettimes
       /// <summary>
       /// Trả về các khung giờ chiếu của phim trong một ngày.
       /// </summary>
       /// <param name="movieId">Id của phim.</param>
       /// <param name="date">Ngày chiếu.</param>
       /// <returns>Json danh sách giờ chiếu.</returns>
       [HttpGet]
       public async Task<IActionResult> GetTimes(string movieId, DateTime date)
       {
           var times = await _bookingService.GetShowTimesAsync(movieId, date);
           return Json(times);
       }
       //GET: /api/booking/information
       /// <summary>
       /// Hiển thị thông tin xác nhận đặt vé.
       /// </summary>
       /// <param name="movieId">Id phim được chọn.</param>
       /// <param name="showDate">Ngày chiếu.</param>
       /// <param name="showTime">Giờ chiếu.</param>
       /// <param name="selectedSeatIds">Danh sách ghế đã chọn.</param>
       /// <returns>View xác nhận đặt vé.</returns>
       [HttpGet]
       public async Task<IActionResult> Information(string movieId, DateTime showDate, string showTime, List<int>? selectedSeatIds)
       {
           if (selectedSeatIds == null || selectedSeatIds.Count == 0)
           {
               TempData["BookingError"] = "No seats were selected.";
               return RedirectToAction("TicketBooking", new { movieId });
           }

           var movie = _bookingService.GetById(movieId);
           if (movie == null)
           {
               return NotFound("Movie not found.");
           }

           // Get the specific movie show for this date and time
           var movieShows = _movieService.GetMovieShows(movieId);
           var movieShow = movieShows.FirstOrDefault(ms => 
               ms.ShowDate == DateOnly.FromDateTime(showDate) &&
               ms.Schedule != null &&
               ms.Schedule.ScheduleTime.HasValue &&
               ms.Schedule.ScheduleTime.Value.ToString("HH:mm") == showTime);

           if (movieShow == null)
           {
               return NotFound("Movie show not found for the specified date and time.");
           }

           var cinemaRoom = movieShow.CinemaRoom;
           if (cinemaRoom == null)
           {
               return NotFound("Cinema room not found for this movie show.");
           }

           var seatTypes = await _seatService.GetSeatTypesAsync();
           var currentUser = _accountService.GetCurrentUser();
           if (currentUser == null)
           {
               return RedirectToAction("Login", "Account");
           }

           var seats = new List<SeatDetailViewModel>();
           foreach (var id in selectedSeatIds)
           {
               var seat = _seatService.GetSeatById(id);
               if (seat == null) continue;

               var seatType = seatTypes.FirstOrDefault(t => t.SeatTypeId == seat.SeatTypeId);
               var price = seatType?.PricePercent ?? 0;

               seats.Add(new SeatDetailViewModel
               {
                   SeatId = seat.SeatId,
                   SeatName = seat.SeatName,
                   SeatType = seatType?.TypeName ?? "Standard",
                   Price = price
               });
           }

           var totalPrice = seats.Sum(s => s.Price);

           var viewModel = new ConfirmBookingViewModel
           {
               MovieId = movieId,
               MovieName = movie.MovieNameEnglish,
               CinemaRoomName = cinemaRoom.CinemaRoomName,
               ShowDate = showDate,
               ShowTime = showTime,
               SelectedSeats = seats,
               TotalPrice = totalPrice,

               FullName = currentUser.FullName,
               Email = currentUser.Email,
               IdentityCard = currentUser.IdentityCard,
               PhoneNumber = currentUser.PhoneNumber,
               CurrentScore = currentUser.Score
           };

           return View("ConfirmBooking", viewModel);
       }

       //POST: /api/booking/confirm
       /// <summary>
       /// Xác nhận đặt vé, lưu hoá đơn vào database.
       /// </summary>
       /// <param name="model">Thông tin xác nhận đặt vé từ client.</param>
       /// <returns>Chuyển hướng tới trang thành công nếu đặt vé thành công.</returns>
       [HttpPost]
       public async Task<IActionResult> Confirm(ConfirmBookingViewModel model)
       {
           try
           {
               //TEST FAILED CASE
               //throw new Exception("Test exception");

               // Lấy Account ID từ JWT claims
               var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
               if (string.IsNullOrEmpty(userId))
                   return RedirectToAction("Login", "Account");

               // Tạo danh sách tên ghế
               var seatNames = model.SelectedSeats.Select(s => s.SeatName);
               string seatList = string.Join(",", seatNames);

               model.UseScore = Math.Min(model.UseScore, (int)model.TotalPrice); //GIỚI HẠN USE SCORE = TOTAL PRICE
               // Tạo đối tượng Invoice
               var invoice = new Invoice
               {
                   InvoiceId = await _bookingService.GenerateInvoiceIdAsync(),
                   AccountId = userId,
                   AddScore = (int)(model.TotalPrice * 0.01m),
                   BookingDate = DateTime.Now,
                   MovieName = model.MovieName,
                   ScheduleShow = model.ShowDate,
                   ScheduleShowTime = model.ShowTime,
                   Status = 1,
                   TotalMoney = model.TotalPrice - model.UseScore,
                   UseScore = model.UseScore,
                   Seat = seatList
               };

               // Lưu vào DB
               await _bookingService.SaveInvoiceAsync(invoice);

               // Get the MovieShowId for this movie, date and time
               var movieShow = _movieService.GetMovieShows(model.MovieId)
                   .FirstOrDefault(ms => 
                       ms.ShowDate == DateOnly.FromDateTime(model.ShowDate) &&
                       ms.Schedule != null &&
                       ms.Schedule.ScheduleTime.HasValue &&
                       ms.Schedule.ScheduleTime.Value.ToString("HH:mm") == model.ShowTime);

               if (movieShow == null)
               {
                   return Json(new { success = false, message = "Movie show not found for the specified date and time." });
               }

               // Mark all selected seats as booked
               var scheduleSeats = model.SelectedSeats.Select(seat => new ScheduleSeat
               {
                   MovieShowId = movieShow.MovieShowId,
                   InvoiceId = invoice.InvoiceId,
                   SeatId = (int)seat.SeatId,
                   SeatStatusId = 2
               });

               await _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(scheduleSeats);

               // GIẢM ĐIỂM NẾU USESCORE > 0
               if (model.UseScore > 0)
               {
                   await _accountService.DeductScoreAsync(userId, model.UseScore);
               }
               TempData["MovieName"] = model.MovieName;
               TempData["ShowDate"] = model.ShowDate.ToString("yyyy-MM-dd");
               TempData["ShowTime"] = model.ShowTime;
               TempData["Seats"] = string.Join(", ", model.SelectedSeats.Select(s => s.SeatName));
               TempData["BookingTime"] = DateTime.Now.ToString("g");
               TempData["InvoiceId"] = invoice.InvoiceId;
               TempData["CinemaRoomName"] = model.CinemaRoomName;
               TempData["OriginalPrice"] = model.TotalPrice.ToString();
               TempData["UsedScore"] = model.UseScore.ToString();
               TempData["FinalPrice"] = (model.TotalPrice - model.UseScore).ToString();
               TempData["ToastMessage"] = "Movie booked successfully!";

               return RedirectToAction("Success");
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Exception during booking ticket.");
               TempData["ErrorMessage"] = "Booking failed. Please try again later.";
               return RedirectToAction("Information", new
               {
                   movieId = model.MovieId,
                   showDate = model.ShowDate.ToString("yyyy-MM-dd"),
                   showTime = model.ShowTime,
                   selectedSeatIds = model.SelectedSeats.Select(s => s.SeatName) // Hoặc giữ lại Id nếu cần
               });
           }
       }

       //GET: /api/booking/success
       /// <summary>
       /// Trang hiển thị khi đặt vé thành công.
       /// </summary>
       /// <returns>View chúc mừng đặt vé thành công.</returns>
       [HttpGet]
       public IActionResult Success()
       {
           return View();
       }

       // /// Admin: Select seat for ticket selling
       // /// url: /Booking/ConfirmTicketForAdmin
       [Authorize(Roles = "Admin")]
       [HttpGet]
       public async Task<IActionResult> ConfirmTicketForAdmin(string movieId, DateTime showDate, string showTime, List<int>? selectedSeatIds)
       {
           if (selectedSeatIds == null || selectedSeatIds.Count == 0)
           {
               TempData["ErrorMessage"] = "No seats were selected.";
               return RedirectToAction("MainPage", new { tab = "TicketSellingMg" });
           }

           var movie = _bookingService.GetById(movieId);
           if (movie == null)
           {
               return NotFound("Movie not found.");
           }

           var seatTypes = await _seatService.GetSeatTypesAsync();
           var seats = new List<SeatDetailViewModel>();

           foreach (var id in selectedSeatIds)
           {
               var seat = _seatService.GetSeatById(id);
               if (seat == null) continue;

               var seatType = seatTypes.FirstOrDefault(t => t.SeatTypeId == seat.SeatTypeId);
               var price = seatType?.PricePercent ?? 0;

               seats.Add(new SeatDetailViewModel
               {
                   SeatId = seat.SeatId,
                   SeatName = seat.SeatName,
                   SeatType = seatType?.TypeName ?? "Standard",
                   Price = price
               });
           }
           var movieShows = _movieService.GetMovieShows(movieId);
           var movieShow = movieShows.FirstOrDefault(ms =>
               ms.ShowDate == DateOnly.FromDateTime(showDate) &&
               ms.Schedule != null &&
               ms.Schedule.ScheduleTime.HasValue &&
               ms.Schedule.ScheduleTime.Value.ToString("HH:mm") == showTime);

           if (movieShow == null)
           {
               return NotFound("Movie show not found for the specified date and time.");
           }

           var cinemaRoom = movieShow.CinemaRoom;
           if (cinemaRoom == null)
           {
               return NotFound("Cinema room not found for this movie show.");
           }
           var totalPrice = seats.Sum(s => s.Price);

           var bookingDetails = new ConfirmBookingViewModel
           {
               MovieId = movieId,
               MovieName = movie.MovieNameEnglish,
               CinemaRoomName = cinemaRoom.CinemaRoomName,
               ShowDate = showDate,
               ShowTime = showTime,
               SelectedSeats = seats,
               TotalPrice = totalPrice,
               PricePerTicket = seats.Any() ? totalPrice / seats.Count : 0
           };

           var adminConfirmUrl = Url.Action("ConfirmTicketForAdmin", "Admin");
           var viewModel = new ConfirmTicketAdminViewModel
           {
               BookingDetails = bookingDetails,
               MemberCheckMessage = "",
               ReturnUrl = Url.Action("Select", "Seat", new
               {
                   movieId = movieId,
                   date = showDate.ToString("yyyy-MM-dd"),
                   time = showTime,
                   returnUrl = adminConfirmUrl
               })
           };
           return View("ConfirmTicketAdmin", viewModel);
       }

       // /// Admin: Check member details for ticket selling
       // /// url: /Booking/CheckMemberDetails
       [Authorize(Roles = "Admin")]
       [HttpPost]
       public async Task<IActionResult> CheckMemberDetails([FromBody] MemberCheckRequest request)
       {
           var member = _memberRepository.GetByIdentityCard(request.MemberInput)
               ?? _memberRepository.GetByMemberId(request.MemberInput)
               ?? _memberRepository.GetByAccountId(request.MemberInput);

           if (member == null || member.Account == null)
           {
               return Json(new { success = false, message = "No member has found!" });
           }

           return Json(new
           {
               success = true,
               memberId = member.MemberId,
               fullName = member.Account.FullName,
               identityCard = member.Account.IdentityCard,
               phoneNumber = member.Account.PhoneNumber,
               memberScore = member.Score
           });
       }

       // /// Admin: Confirm ticket booking and convert score
       // /// url: /Booking/ConfirmTicketForAdmin
       [Authorize(Roles = "Admin")]
       [HttpPost]
       public async Task<IActionResult> ConfirmTicketForAdmin([FromBody] ConfirmTicketAdminViewModel model)
       {
           if (model.BookingDetails == null || model.BookingDetails.SelectedSeats == null)
           {
               return Json(new { success = false, message = "Booking details or selected seats are missing." });
           }

           if (string.IsNullOrEmpty(model.MemberId))
           {
               return Json(new { success = false, message = "Member check is required before confirming." });
           }

           try
           {
               // Retrieve the member again to ensure latest score if conversion is involved
               Member member = null;
               if (!string.IsNullOrEmpty(model.MemberId))
               {
                   member = _memberRepository.GetByMemberId(model.MemberId);
                   if (member == null)
                   {
                       return Json(new { success = false, message = "Member not found. Please check member details again." });
                   }
               }

               decimal discount = 0;
               int scoreUsed = 0;
               List<int> convertedTicketIndexes = new List<int>();
               if (member != null && model.BookingDetails.SelectedSeats != null && model.BookingDetails.SelectedSeats.Count > 0 && model.TicketsToConvert > 0)
               {
                   // Sort tickets by price descending and take the number to convert
                   var sortedSeats = model.BookingDetails.SelectedSeats
                       .OrderByDescending(s => s.Price)
                       .Take(model.TicketsToConvert)
                       .ToList();

                   var totalScoreNeeded = (int)sortedSeats.Sum(s => s.Price);

                   if (member.Score >= totalScoreNeeded)
                   {
                       discount = sortedSeats.Sum(s => s.Price);
                       scoreUsed = totalScoreNeeded;
                       convertedTicketIndexes = sortedSeats.Select(s => model.BookingDetails.SelectedSeats.IndexOf(s)).ToList();
                       member.Score -= scoreUsed;
                       _memberRepository.Update(member);
                   }
                   else
                   {
                       // Not enough score, handle error (shouldn't happen if frontend check is correct)
                       return Json(new { success = false, message = "Member score is not enough to convert into ticket" });
                   }
               }

               var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
               var currentUser = _accountService.GetById(currentUserId);
               var invoice = new Invoice
               {
                   InvoiceId = await _bookingService.GenerateInvoiceIdAsync(),
                   AccountId = member?.Account?.AccountId ?? currentUserId, // Use member's AccountId for DB FK
                   AddScore = (int)((model.BookingDetails.TotalPrice - discount) * 0.1m), // Add score based on discounted price
                   BookingDate = DateTime.Now,
                   MovieName = model.BookingDetails.MovieName,
                   ScheduleShow = model.BookingDetails.ShowDate,
                   ScheduleShowTime = model.BookingDetails.ShowTime,
                   Status = 1,
                   TotalMoney = model.BookingDetails.TotalPrice - discount,
                   UseScore = scoreUsed,
                   Seat = string.Join(", ", model.BookingDetails.SelectedSeats.Select(s => s.SeatName)),
                   RoleId = currentUser?.RoleId // Set RoleId to the current user's role
               };

               // Fix roomName logic
               string roomName = "N/A";
               if (!string.IsNullOrEmpty(model.BookingDetails.MovieId))
               {
                   var movie = _movieService.GetById(model.BookingDetails.MovieId);
                   if (movie != null && movie.CinemaRoomId.HasValue)
                   {
                       var room = _cinemaService.GetById(movie.CinemaRoomId.Value);
                       roomName = room?.CinemaRoomName ?? "N/A";
                   }
               }

               await _bookingService.SaveInvoiceAsync(invoice);

               // Get the MovieShowId for this movie, date and time
               var movieShow = _movieService.GetMovieShows(model.BookingDetails.MovieId)
                   .FirstOrDefault(ms => 
                       ms.ShowDate == DateOnly.FromDateTime(model.BookingDetails.ShowDate) &&
                       ms.Schedule != null &&
                       ms.Schedule.ScheduleTime.HasValue &&
                       ms.Schedule.ScheduleTime.Value.ToString("HH:mm") == model.BookingDetails.ShowTime);

               if (movieShow == null)
               {
                   return Json(new { success = false, message = "Movie show not found for the specified date and time." });
               }

               // Mark all selected seats as booked
               var scheduleSeats = model.BookingDetails.SelectedSeats.Select(seat => new ScheduleSeat
               {
                   MovieShowId = movieShow.MovieShowId,
                   InvoiceId = invoice.InvoiceId,
                   SeatId = (int)seat.SeatId,
                   SeatStatusId = 2
               });

               await _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(scheduleSeats);

               TempData["ToastMessage"] = "Movie booked successfully!";

               // Redirect to confirmation page with invoiceId
               return Json(new { success = true, redirectUrl = Url.Action("TicketBookingConfirmed", "Booking", new { invoiceId = invoice.InvoiceId }) });
           }
           catch (Exception ex)
           {
               // _logger.LogError(ex, "Exception during admin ticket confirmation.");
               return Json(new { success = false, message = "Booking failed. Please try again later." });
           }
       }

       // /// Admin: Show confirmation after ticket booking
       // /// url: /Booking/TicketBookingConfirmed
       [Authorize(Roles = "Admin")]
       [HttpGet]
       public IActionResult TicketBookingConfirmed(string invoiceId)
       {
           if (string.IsNullOrEmpty(invoiceId))
               return View("TicketBookingConfirmed"); // fallback, but not recommended

           var invoice = _invoiceService.GetById(invoiceId);
           if (invoice == null)
               return NotFound();

           var member = _memberRepository.GetByAccountId(invoice.AccountId);

           var allMovies = _movieService.GetAll();
           var movie = allMovies.FirstOrDefault(m => m.MovieNameEnglish == invoice.MovieName || m.MovieNameVn == invoice.MovieName);

           var movieShows = _movieService.GetMovieShows(movie.MovieId);
           var movieShow = movieShows.FirstOrDefault(ms =>
               ms.ShowDate == DateOnly.FromDateTime(invoice.ScheduleShow ?? DateTime.Now) &&
               ms.Schedule != null &&
               ms.Schedule.ScheduleTime.HasValue &&
               ms.Schedule.ScheduleTime.Value.ToString("HH:mm") == invoice.ScheduleShowTime);

           if (movieShow == null)
           {
               return NotFound("Movie show not found for the specified date and time.");
           }

           var cinemaRoom = movieShow.CinemaRoom;
           if (cinemaRoom == null)
           {
               return NotFound("Cinema room not found for this movie show.");
           }
           // Prepare seat details
           var seatNames = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
           var seats = new List<SeatDetailViewModel>();
           foreach (var seatName in seatNames)
           {
               var trimmedSeatName = seatName.Trim();
               var seat = _seatService.GetSeatByName(trimmedSeatName);
               if (seat == null)
               {
                   System.Diagnostics.Debug.WriteLine($"[TicketBookingConfirmed] Seat not found: '{trimmedSeatName}'");
               }
               SeatType seatType = null;
               if (seat != null && seat.SeatTypeId.HasValue)
               {
                   seatType = _seatTypeService.GetById(seat.SeatTypeId.Value);
               }
               seats.Add(new SeatDetailViewModel
               {
                   SeatId = seat.SeatId,
                   SeatName = trimmedSeatName,
                   SeatType = seatType?.TypeName ?? "N/A",
                   Price = seatType?.PricePercent ?? 0
               });
           }

           // Calculate tickets converted by score
           int ticketsConverted = 0;
           if (invoice.UseScore.HasValue && invoice.UseScore.Value > 0 && seats.Count > 0)
           {
               // Sort seats by price descending and count how many could be converted by the used score
               var sortedSeats = seats.OrderByDescending(s => s.Price).ToList();
               decimal runningScore = invoice.UseScore.Value;
               foreach (var seat in sortedSeats)
               {
                   if (runningScore >= seat.Price)
                   {
                       ticketsConverted++;
                       runningScore -= seat.Price;
                   }
                   else
                   {
                       break;
                   }
               }
           }

           var bookingDetails = new ConfirmBookingViewModel
           {
               MovieId = movie?.MovieId,
               MovieName = invoice.MovieName,
               CinemaRoomName = cinemaRoom.CinemaRoomName,
               ShowDate = invoice.ScheduleShow ?? DateTime.Now,
               ShowTime = invoice.ScheduleShowTime,
               SelectedSeats = seats,
               TotalPrice = invoice.TotalMoney ?? 0,
               PricePerTicket = seats.Any() ? (invoice.TotalMoney ?? 0) / seats.Count : 0,
               InvoiceId = invoice.InvoiceId,
               ScoreUsed = invoice.UseScore ?? 0,
               TicketsConverted = ticketsConverted > 0 ? ticketsConverted.ToString() : null
           };

           // Determine return URL based on user role
           string returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });

           var viewModel = new ConfirmTicketAdminViewModel
           {
               BookingDetails = bookingDetails,
               MemberCheckMessage = "",
               ReturnUrl = returnUrl,
               MemberId = member?.MemberId,
               MemberEmail = member?.Account?.Email,
               MemberIdentityCard = member?.Account?.IdentityCard,
               MemberPhone = member?.Account?.PhoneNumber
           };

           return View("TicketBookingConfirmed", viewModel);
       }

       // /// Admin: Check score for ticket conversion
       // /// url: /Booking/CheckScoreForConversion
       [Authorize(Roles = "Admin")]
       [HttpPost]
       public IActionResult CheckScoreForConversion([FromBody] ScoreConversionRequest request)
       {
           var prices = request.TicketPrices.OrderByDescending(p => p).ToList(); // Convert most expensive first
           if (request.TicketsToConvert > prices.Count)
               return Json(new { success = false, message = "Not enough tickets selected." });

           var selected = prices.Take(request.TicketsToConvert).ToList();
           var totalNeeded = (int)selected.Sum();

           if (request.MemberScore >= totalNeeded)
           {
               return Json(new { success = true, ticketsConverted = request.TicketsToConvert, scoreNeeded = totalNeeded, tickets = selected });
           }
           else
           {
               return Json(new { success = false, message = "Member score is not enough to convert into ticket", scoreNeeded = totalNeeded });
           }
       }

       // /// Admin/Employee: Show detailed ticket information
       // /// url: /Booking/TicketInfo
       [Authorize(Roles = "Admin,Employee")]
       [HttpGet]
       public IActionResult TicketInfo(string invoiceId)
       {
           if (string.IsNullOrEmpty(invoiceId))
               return NotFound();

           var invoice = _invoiceService.GetById(invoiceId);
           if (invoice == null)
           {
               _logger.LogError($"Invoice not found: {invoiceId}");
               return NotFound();
           }

           // Get the first schedule seat to access movie show and cinema room info
           var scheduleSeats = _scheduleSeatRepository.GetByInvoiceId(invoiceId).ToList();

           // Find the movie first
           var allMovies = _movieService.GetAll();
           var movie = allMovies.FirstOrDefault(m => m.MovieNameEnglish == invoice.MovieName || m.MovieNameVn == invoice.MovieName);
           if (movie == null)
           {
               return NotFound("Movie not found for this invoice.");
           }

           // Then find the movie show
           var movieShow = _movieService.GetMovieShows(movie.MovieId)
               .FirstOrDefault(ms => 
                   ms.ShowDate == DateOnly.FromDateTime(invoice.ScheduleShow ?? DateTime.Now) &&
                   ms.Schedule != null &&
                   ms.Schedule.ScheduleTime.HasValue &&
                   ms.Schedule.ScheduleTime.Value.ToString("HH:mm") == invoice.ScheduleShowTime);

           if (movieShow == null)
           {
               return NotFound("Movie show not found for this invoice.");
           }

           var firstScheduleSeat = scheduleSeats.FirstOrDefault();
           if (firstScheduleSeat == null)
           {
                
               // Create schedule seats from the seat names in the invoice
               var seatNames = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
               var newScheduleSeats = new List<ScheduleSeat>();
                
               foreach (var seatName in seatNames)
               {
                   var trimmedSeatName = seatName.Trim();
                   var seat = _seatService.GetSeatByName(trimmedSeatName);
                   if (seat == null)
                   {
                       continue;
                   }

                   newScheduleSeats.Add(new ScheduleSeat
                   {
                       MovieShowId = movieShow.MovieShowId,
                       InvoiceId = invoice.InvoiceId,
                       SeatId = seat.SeatId,
                       SeatStatusId = 2
                   });
               }

               if (newScheduleSeats.Any())
               {
                   _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(newScheduleSeats).Wait();
                   scheduleSeats = newScheduleSeats;
               }
               else
               {
                   return NotFound("No valid seats found for this invoice.");
               }
           }

           var cinemaRoom = movieShow.CinemaRoom;
           if (cinemaRoom == null)
           {
               return NotFound("Cinema room not found.");
           }

           var member = _memberRepository.GetByAccountId(invoice.AccountId);
           var seats = new List<SeatDetailViewModel>();
            
           foreach (var scheduleSeat in scheduleSeats)
           {
               var seat = _seatService.GetSeatById(scheduleSeat.SeatId);
               if (seat == null)
               {
                   continue;
               }

               SeatType seatType = null;
               if (seat.SeatTypeId.HasValue)
               {
                   seatType = _seatTypeService.GetById(seat.SeatTypeId.Value);
               }

               seats.Add(new SeatDetailViewModel
               {
                   SeatId = seat.SeatId,
                   SeatName = seat.SeatName,
                   SeatType = seatType?.TypeName ?? "N/A",
                   Price = seatType?.PricePercent ?? 0
               });
           }

           // Calculate tickets converted by score
           int ticketsConverted = 0;
           if (invoice.UseScore.HasValue && invoice.UseScore.Value > 0 && seats.Count > 0)
           {
               var sortedSeats = seats.OrderByDescending(s => s.Price).ToList();
               decimal runningScore = invoice.UseScore.Value;
               foreach (var seat in sortedSeats)
               {
                   if (runningScore >= seat.Price)
                   {
                       ticketsConverted++;
                       runningScore -= seat.Price;
                   }
                   else
                   {
                       break;
                   }
               }
           }

           var bookingDetails = new ConfirmBookingViewModel
           {
               MovieId = movieShow.MovieId,
               MovieName = invoice.MovieName,
               CinemaRoomName = cinemaRoom.CinemaRoomName,
               ShowDate = invoice.ScheduleShow ?? DateTime.Now,
               ShowTime = invoice.ScheduleShowTime,
               SelectedSeats = seats,
               TotalPrice = invoice.TotalMoney ?? 0,
               PricePerTicket = seats.Any() ? (invoice.TotalMoney ?? 0) / seats.Count : 0,
               InvoiceId = invoice.InvoiceId,
               ScoreUsed = invoice.UseScore ?? 0,
               TicketsConverted = ticketsConverted > 0 ? ticketsConverted.ToString() : null,
               FullName = member?.Account?.FullName,
               Email = member?.Account?.Email,
               IdentityCard = member?.Account?.IdentityCard,
               PhoneNumber = member?.Account?.PhoneNumber,
               CurrentScore = member?.Score ?? 0
           };

           string returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });

           var viewModel = new ConfirmTicketAdminViewModel
           {
               BookingDetails = bookingDetails,
               MemberCheckMessage = "",
               ReturnUrl = returnUrl,
               MemberId = member?.MemberId,
               MemberEmail = member?.Account?.Email,
               MemberIdentityCard = member?.Account?.IdentityCard,
               MemberPhone = member?.Account?.PhoneNumber
           };

           return View("TicketBookingConfirmed", viewModel);
       }

       public class MemberCheckRequest
       {
           public string MemberInput { get; set; }
       }

       public class ScoreConversionRequest
       {
           public List<decimal> TicketPrices { get; set; }
           public int TicketsToConvert { get; set; }
           public int MemberScore { get; set; }
       }
   }
}
