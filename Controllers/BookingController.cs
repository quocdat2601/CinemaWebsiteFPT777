using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace MovieTheater.Controllers
{
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

    public class GetEligiblePromotionsRequest
    {
        public string MemberId { get; set; }
        public int SeatCount { get; set; }
        public DateTime ShowDate { get; set; }
        public string MovieId { get; set; }
        public string MovieName { get; set; }
    }

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
        private readonly IScheduleSeatRepository _scheduleSeatRepository;
        private readonly ILogger<BookingController> _logger;
        private readonly VNPayService _vnPayService;
        private readonly IPointService _pointService;
        private readonly IRankService _rankService;
        private readonly IPromotionService _promotionService;
        private decimal promotionDiscount;

        public BookingController(IBookingService bookingService,
                         IMovieService movieService,
                         ISeatService seatService,
                         IAccountService accountService,
                         ISeatTypeService seatTypeService,
                         IMemberRepository memberRepository,
                         ILogger<BookingController> logger,
                         IInvoiceService invoiceService,
                         ICinemaService cinemaService,
                         IScheduleSeatRepository scheduleSeatRepository,
                         VNPayService vnPayService,
                         IPointService pointService,
                         IRankService rankService,
                         IPromotionService promotionService)
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
            _vnPayService = vnPayService;
            _pointService = pointService;
            _rankService = rankService;
            _promotionService = promotionService;
        }

        [HttpGet]
        public async Task<IActionResult> TicketBooking(string movieId = null)
        {
            var movies = await _bookingService.GetAvailableMoviesAsync();
            ViewBag.MovieList = movies;
            ViewBag.SelectedMovieId = movieId;

            if (!string.IsNullOrEmpty(movieId))
            {
                var movieShows = _movieService.GetMovieShows(movieId);
                var showsByDate = movieShows
                    .Where(ms => ms.ShowDate?.ShowDate1 != null && ms.Schedule?.ScheduleTime != null)
                    .GroupBy(ms => ms.ShowDate.ShowDate1.Value.ToString("dd/MM/yyyy"))
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(ms => ms.Schedule.ScheduleTime)
                              .Distinct()
                              .OrderBy(t => t)
                              .ToList()
                    );
                ViewBag.ShowsByDate = showsByDate;
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDates(string movieId)
        {
            var dates = await _bookingService.GetShowDatesAsync(movieId);
            return Json(dates.Select(d => d.ToString("yyyy-MM-dd")));
        }

        [HttpGet]
        public async Task<IActionResult> GetTimes(string movieId, DateTime date)
        {
            var times = await _bookingService.GetShowTimesAsync(movieId, date);
            return Json(times);
        }

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

            var movieShows = _movieService.GetMovieShows(movieId);
            var movieShow = movieShows.FirstOrDefault(ms =>
                ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(showDate) &&
                ms.Schedule?.ScheduleTime == showTime);

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

            // Reload account with rank
            var userAccount = _accountService.GetById(currentUser.AccountId);

            decimal earningRate = 1;
            decimal rankDiscountPercent = 0;
            if (userAccount?.Rank != null)
            {
                earningRate = userAccount.Rank.PointEarningPercentage ?? 1;
                rankDiscountPercent = userAccount.Rank.DiscountPercentage ?? 0;
            }
            ViewBag.EarningRate = earningRate;
            ViewBag.RankDiscountPercent = rankDiscountPercent;

            var seats = new List<SeatDetailViewModel>();
            foreach (var id in selectedSeatIds)
            {
                var seat = await _seatService.GetSeatByIdAsync(id);
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

            var subtotal = seats.Sum(s => s.Price);
            decimal rankDiscount = 0;
            if (userAccount?.Rank != null)
            {
                rankDiscount = subtotal * (rankDiscountPercent / 100m);
            }

            var totalPrice = subtotal - rankDiscount;

            var viewModel = new ConfirmBookingViewModel
            {
                MovieId = movieId,
                MovieName = movie.MovieNameEnglish,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                ShowDate = showDate,
                ShowTime = showTime,
                SelectedSeats = seats,
                Subtotal = subtotal,
                RankDiscount = rankDiscount,
                PromotionDiscount = 0,
                TotalPrice = totalPrice,
                FullName = currentUser.FullName,
                Email = currentUser.Email,
                IdentityCard = currentUser.IdentityCard,
                PhoneNumber = currentUser.PhoneNumber,
                CurrentScore = currentUser.Score,
                EarningRate = earningRate,
                RankDiscountPercent = rankDiscountPercent,
                SelectedPromotionId = null,
                SelectedPromotionTitle = "",
                SelectedPromotionDiscount = 0
            };

            return View("ConfirmBooking", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Confirm(ConfirmBookingViewModel model, string IsTestSuccess)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var userAccount = _accountService.GetById(userId);

                // Recalculate prices to prevent tampering
                var subtotal = model.SelectedSeats.Sum(s => s.Price);
                
                // Apply promotion discount if selected
                decimal promotionDiscount = model.SelectedPromotionDiscount;
                var priceAfterPromotion = subtotal - promotionDiscount;
                
                // Calculate rank discount on price after promotion discount
                decimal rankDiscount = 0;
                if (userAccount?.Rank != null)
                {
                    var rankDiscountPercent = userAccount.Rank.DiscountPercentage ?? 0;
                    rankDiscount = priceAfterPromotion * (rankDiscountPercent / 100m);
                }

                var priceAfterDiscount = priceAfterPromotion - rankDiscount;

                model.UseScore = Math.Min(model.UseScore, (int)(priceAfterDiscount / 1000));
                
                var finalPrice = priceAfterDiscount - (model.UseScore * 1000);

                var seatNames = model.SelectedSeats.Select(s => s.SeatName);
                string seatList = string.Join(",", seatNames);

                var invoice = new Invoice
                {
                    InvoiceId = await _bookingService.GenerateInvoiceIdAsync(),
                    AccountId = userId,
                    BookingDate = DateTime.Now,
                    MovieName = model.MovieName,
                    ScheduleShow = model.ShowDate,
                    ScheduleShowTime = model.ShowTime,
                    Status = InvoiceStatus.Incomplete,
                    TotalMoney = finalPrice,
                    UseScore = model.UseScore,
                    Seat = seatList
                };

                // Calculate earning rate from user rank
                decimal earningRate = 1;
                if (userAccount?.Rank != null)
                {
                    earningRate = userAccount.Rank.PointEarningPercentage ?? 1;
                }
                // Calculate points to earn using the same logic as admin
                int pointsToEarn = _pointService.CalculatePointsToEarn(finalPrice, earningRate);
                invoice.AddScore = pointsToEarn;

                await _bookingService.SaveInvoiceAsync(invoice);
                _accountService.CheckAndUpgradeRank(userId);

                var movieShow = _movieService.GetMovieShows(model.MovieId)
                    .FirstOrDefault(ms =>
                        ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(model.ShowDate) &&
                        ms.Schedule?.ScheduleTime == model.ShowTime);

                if (movieShow == null)
                {
                    return Json(new { success = false, message = "Movie show not found for the specified date and time." });
                }

                if (model.UseScore > 0)
                {
                    await _accountService.DeductScoreAsync(userId, model.UseScore);
                }

                // Lưu MovieShowId vào TempData để PaymentController sử dụng
                TempData["MovieShowId"] = movieShow.MovieShowId;

                // Store promotion information in TempData for Success page
                if (model.SelectedPromotionId.HasValue)
                {
                    TempData["PromotionId"] = model.SelectedPromotionId.Value;
                    TempData["PromotionTitle"] = model.SelectedPromotionTitle;
                    TempData["PromotionDiscount"] = promotionDiscount;
                }

                // Nếu là test success thì bỏ qua thanh toán, chuyển thẳng sang trang Success
                if (!string.IsNullOrEmpty(IsTestSuccess) && IsTestSuccess == "true")
                {
                    // Cập nhật status thành Completed cho test success
                    invoice.Status = InvoiceStatus.Completed;
                    await _bookingService.UpdateInvoiceAsync(invoice);

                    // Tạo ScheduleSeat sau khi đã cập nhật status
                    if (invoice.Status != InvoiceStatus.Incomplete)
                    {
                        var scheduleSeats = model.SelectedSeats.Select(seat => new ScheduleSeat
                        {
                            MovieShowId = movieShow.MovieShowId,
                            InvoiceId = invoice.InvoiceId,
                            SeatId = (int)seat.SeatId,
                            SeatStatusId = 2
                        });

                        await _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(scheduleSeats);
                    }

                    TempData["MovieName"] = model.MovieName;
                    TempData["ShowDate"] = model.ShowDate.ToString();
                    TempData["ShowTime"] = model.ShowTime;
                    TempData["Seats"] = seatList;
                    TempData["CinemaRoomName"] = model.CinemaRoomName;
                    TempData["InvoiceId"] = invoice.InvoiceId;
                    TempData["BookingTime"] = invoice.BookingDate.ToString();
                    TempData["OriginalPrice"] = subtotal.ToString();
                    TempData["UsedScore"] = model.UseScore.ToString();
                    TempData["FinalPrice"] = finalPrice.ToString();
                    TempData["PromotionDiscount"] = promotionDiscount.ToString();
                    TempData["RankDiscount"] = rankDiscount.ToString();
                    return RedirectToAction("Success");
                }

                // Tạo ScheduleSeat cho trường hợp thanh toán bình thường
                if (invoice.Status != InvoiceStatus.Incomplete)
                {
                    var scheduleSeats = model.SelectedSeats.Select(seat => new ScheduleSeat
                    {
                        MovieShowId = movieShow.MovieShowId,
                        InvoiceId = invoice.InvoiceId,
                        SeatId = (int)seat.SeatId,
                        SeatStatusId = 2
                    });

                    await _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(scheduleSeats);
                }

                // Always set CinemaRoomName in TempData before redirect
                TempData["CinemaRoomName"] = model.CinemaRoomName;

                return RedirectToAction("Payment", new { invoiceId = invoice.InvoiceId });
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
                    selectedSeatIds = model.SelectedSeats.Select(s => s.SeatName)
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success()
        {
            var invoiceId = TempData["InvoiceId"] as string;
            if (!string.IsNullOrEmpty(invoiceId))
            {
                var invoice = _invoiceService.GetById(invoiceId);
                if (invoice != null && invoice.Status == InvoiceStatus.Completed && invoice.AddScore.HasValue && invoice.AddScore.Value > 0)
                {
                    await _accountService.AddScoreAsync(invoice.AccountId, invoice.AddScore.Value);
                }
            }
            return View();
        }

        [HttpGet]
        public IActionResult Payment(string invoiceId)
        {
            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null)
            {
                return NotFound();
            }

            var sanitizedMovieName = Regex.Replace(invoice.MovieName, @"[^a-zA-Z0-9\s]", "");
            var viewModel = new PaymentViewModel
            {
                InvoiceId = invoice.InvoiceId,
                MovieName = invoice.MovieName,
                ShowDate = invoice.ScheduleShow ?? DateTime.MinValue,
                ShowTime = invoice.ScheduleShowTime,
                Seats = invoice.Seat,
                TotalAmount = invoice.TotalMoney ?? 0,
                OrderInfo = $"Payment for movie ticket {sanitizedMovieName} - {invoice.Seat.Replace(",", " ")}"
            };

            return View("Payment", viewModel);
        }

        [HttpPost]
        public IActionResult ProcessPayment(PaymentViewModel model)
        {
            try
            {
                var paymentUrl = _vnPayService.CreatePaymentUrl(
                    model.TotalAmount,
                    model.OrderInfo,
                    model.InvoiceId
                );

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment URL");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo URL thanh toán. Vui lòng thử lại sau.";
                return RedirectToAction("Payment", new { invoiceId = model.InvoiceId });
            }
        }

        [HttpGet]
        public IActionResult Failed()
        {
            var invoiceId = TempData["InvoiceId"] as string;
            if (!string.IsNullOrEmpty(invoiceId))
            {
                var invoice = _invoiceService.GetById(invoiceId);
                if (invoice != null && invoice.Status != InvoiceStatus.Incomplete)
                {
                    invoice.Status = InvoiceStatus.Incomplete;
                    invoice.UseScore = 0;
                    var context = new MovieTheater.Models.MovieTheaterContext();
                    context.Invoices.Update(invoice);
                    context.SaveChanges();
                }
            }
            return View();
        }

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
                var seat = await _seatService.GetSeatByIdAsync(id);
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
                ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(showDate) &&
                ms.Schedule?.ScheduleTime == showTime);

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

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ConfirmTicketForAdmin([FromBody] ConfirmTicketAdminViewModel model)
        {
            try
            {
                if (model.BookingDetails == null || model.BookingDetails.SelectedSeats == null)
                {
                    return Json(new { success = false, message = "Booking details or selected seats are missing." });
                }

                if (string.IsNullOrEmpty(model.MemberId))
                {
                    return Json(new { success = false, message = "Member check is required before confirming." });
                }

                Member member = null;
                if (!string.IsNullOrEmpty(model.MemberId))
                {
                    member = _memberRepository.GetByMemberId(model.MemberId);
                    if (member == null)
                    {
                        return Json(new { success = false, message = "Member not found. Please check member details again." });
                    }
                }

                // Prepare seat details first
                var seatNamesArr = (model.BookingDetails.SelectedSeats != null)
                    ? model.BookingDetails.SelectedSeats.Select(s => s.SeatName).ToArray()
                    : Array.Empty<string>();
                var seats = new List<SeatDetailViewModel>();
                foreach (var seatName in seatNamesArr)
                {
                    var seat = _seatService.GetSeatByName(seatName);
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

                // Calculate subtotal
                decimal subtotal = seats.Sum(s => s.Price);
                
                // Use promotion discount passed from frontend (already calculated)
                decimal promotionDiscount = model.PromotionDiscount;
                
                // Calculate price after promotion discount
                decimal priceAfterPromotion = subtotal - promotionDiscount;
                
                // Calculate rank discount on price after promotion discount
                decimal rankDiscount = 0;
                if (member?.Account?.Rank != null)
                {
                    var rankDiscountPercent = member.Account.Rank.DiscountPercentage ?? 0;
                    rankDiscount = priceAfterPromotion * (rankDiscountPercent / 100m);
                }
                
                // Calculate the max possible points used (cannot exceed price after all discounts)
                decimal priceAfterDiscount = priceAfterPromotion - rankDiscount;
                int usedScore = model.UsedScore;
                decimal usedScoreValue = Math.Min(usedScore * 1000, priceAfterDiscount); // Cap at price after discount
                decimal finalPrice = priceAfterDiscount - usedScoreValue;
                // Calculate points to earn using the same logic as user booking
                decimal earningRate = member?.Account?.Rank?.PointEarningPercentage ?? 1;
                int pointsToEarn = _pointService.CalculatePointsToEarn(finalPrice, earningRate);
                int addedScore = pointsToEarn;
                int addedScoreValue = addedScore * 1000;
                string memberId = member?.MemberId;
                string memberEmail = member?.Account?.Email;
                string memberIdentityCard = member?.Account?.IdentityCard;
                string memberPhone = member?.Account?.PhoneNumber;

                var invoice = new Invoice
                {
                    InvoiceId = await _bookingService.GenerateInvoiceIdAsync(),
                    AccountId = member.Account.AccountId,
                    AddScore = pointsToEarn,
                    BookingDate = DateTime.Now,
                    MovieName = model.BookingDetails.MovieName,
                    ScheduleShow = model.BookingDetails.ShowDate,
                    ScheduleShowTime = model.BookingDetails.ShowTime,
                    Status = InvoiceStatus.Completed,
                    TotalMoney = finalPrice,
                    UseScore = usedScore,
                    Seat = string.Join(", ", model.BookingDetails.SelectedSeats.Select(s => s.SeatName)),
                };

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
                _accountService.CheckAndUpgradeRank(member.AccountId);

                // Deduct score if used
                if (usedScore > 0 && member != null)
                {
                    await _accountService.DeductScoreAsync(member.Account.AccountId, usedScore);
                }
                // Add points (using new calculation)
                if (addedScore > 0)
                {
                    await _accountService.AddScoreAsync(invoice.AccountId, addedScore);
                }

                var movieShow = _movieService.GetMovieShows(model.BookingDetails.MovieId)
                    .FirstOrDefault(ms =>
                        ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(model.BookingDetails.ShowDate) &&
                        ms.Schedule?.ScheduleTime == model.BookingDetails.ShowTime);

                if (movieShow == null)
                {
                    return Json(new { success = false, message = "Movie show not found for the specified date and time." });
                }

                if (invoice.Status != InvoiceStatus.Incomplete)
                {
                    var scheduleSeats = model.BookingDetails.SelectedSeats.Select(seat => new ScheduleSeat
                    {
                        MovieShowId = movieShow.MovieShowId,
                        InvoiceId = invoice.InvoiceId,
                        SeatId = (int)seat.SeatId,
                        SeatStatusId = 2
                    });

                    await _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(scheduleSeats);
                }

                // Combine booking success and rank upgrade messages if both exist
                var messages = new List<string> { "Movie booked successfully!" };
                var rankUpMsg = HttpContext.Session.GetString("RankUpToastMessage");
                if (!string.IsNullOrEmpty(rankUpMsg))
                {
                    messages.Add(rankUpMsg);
                    HttpContext.Session.Remove("RankUpToastMessage");
                }
                TempData["ToastMessage"] = string.Join("<br/>", messages);

                var viewModel = new ConfirmTicketAdminViewModel
                {
                    BookingDetails = model.BookingDetails,
                    MemberCheckMessage = "",
                    ReturnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" }),
                    MemberId = memberId,
                    MemberEmail = memberEmail,
                    MemberIdentityCard = memberIdentityCard,
                    MemberPhone = memberPhone,
                    UsedScore = usedScore,
                    UsedScoreValue = usedScoreValue,
                    AddedScore = pointsToEarn,
                    AddedScoreValue = addedScoreValue,
                    Subtotal = subtotal,
                    RankDiscount = rankDiscount,
                    PromotionDiscount = promotionDiscount,
                    TotalPrice = finalPrice
                };

                // Store CinemaRoomName in TempData before redirect
                TempData["CinemaRoomName"] = roomName;

                return Json(new { 
                    success = true, 
                    redirectUrl = Url.Action("TicketBookingConfirmed", "Booking", new { 
                        invoiceId = invoice.InvoiceId,
                        subtotal = subtotal,
                        promotionDiscount = promotionDiscount,
                        rankDiscount = rankDiscount,
                        totalPrice = finalPrice,
                        usedScore = usedScore,
                        usedScoreValue = usedScoreValue,
                        addedScore = pointsToEarn,
                        addedScoreValue = addedScoreValue,
                        memberId = memberId,
                        memberEmail = memberEmail,
                        memberIdentityCard = memberIdentityCard,
                        memberPhone = memberPhone
                    }) 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Booking failed. Please try again later." });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult TicketBookingConfirmed(string invoiceId, 
            decimal subtotal = 0, 
            decimal promotionDiscount = 0, 
            decimal rankDiscount = 0, 
            decimal totalPrice = 0, 
            int usedScore = 0, 
            decimal usedScoreValue = 0, 
            int addedScore = 0, 
            decimal addedScoreValue = 0, 
            string memberId = "", 
            string memberEmail = "", 
            string memberIdentityCard = "", 
            string memberPhone = "")
        {
            if (string.IsNullOrEmpty(invoiceId))
                return View("TicketBookingConfirmed");

            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null)
            {
                return NotFound();
            }

            var member = _memberRepository.GetByAccountId(invoice.AccountId);

            var allMovies = _movieService.GetAll();
            var movie = allMovies.FirstOrDefault(m =>
                string.Equals(m.MovieNameEnglish?.Trim(), invoice.MovieName?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.MovieNameVn?.Trim(), invoice.MovieName?.Trim(), StringComparison.OrdinalIgnoreCase)
            );

            if (movie == null)
            {
                return NotFound("Movie not found for this invoice.");
            }

            var movieShows = _movieService.GetMovieShows(movie.MovieId);
            var movieShow = movieShows.FirstOrDefault(ms =>
                ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(invoice.ScheduleShow ?? DateTime.Now) &&
                ms.Schedule?.ScheduleTime == invoice.ScheduleShowTime);

            // Always reconstruct CinemaRoomName from the database, do NOT use TempData
            string cinemaRoomName = "N/A";
            var cinemaRoom = movieShow?.CinemaRoom;
            if (cinemaRoom != null)
            {
                cinemaRoomName = cinemaRoom.CinemaRoomName;
            }
            // Prepare seat details
            var seatNamesArr = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
            var seats = new List<SeatDetailViewModel>();
            foreach (var seatName in seatNamesArr)
            {
                var seat = _seatService.GetSeatByName(seatName);
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
            var bookingDetails = new ConfirmBookingViewModel
            {
                MovieName = invoice.MovieName,
                CinemaRoomName = cinemaRoomName,
                ShowDate = invoice.ScheduleShow ?? DateTime.Now,
                ShowTime = invoice.ScheduleShowTime,
                SelectedSeats = seats,
                TotalPrice = invoice.TotalMoney ?? 0,
                PricePerTicket = seats.Any() ? (invoice.TotalMoney ?? 0) / seats.Count : 0,
                InvoiceId = invoice.InvoiceId,
                ScoreUsed = invoice.UseScore ?? 0,
                Status = invoice.Status ?? InvoiceStatus.Incomplete,
                AddScore = invoice.AddScore ?? 0
            };

            string returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });

            // Use the calculated values passed as parameters
            var viewModel = new ConfirmTicketAdminViewModel
            {
                BookingDetails = bookingDetails,
                MemberCheckMessage = "",
                ReturnUrl = returnUrl,
                MemberId = memberId,
                MemberEmail = memberEmail,
                MemberIdentityCard = memberIdentityCard,
                MemberPhone = memberPhone,
                UsedScore = usedScore,
                UsedScoreValue = usedScoreValue,
                AddedScore = addedScore,
                AddedScoreValue = addedScoreValue,
                Subtotal = subtotal,
                RankDiscount = rankDiscount,
                PromotionDiscount = promotionDiscount,
                TotalPrice = totalPrice
            };

            return View("TicketBookingConfirmed", viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult CheckScoreForConversion([FromBody] ScoreConversionRequest request)
        {
            var prices = request.TicketPrices.OrderByDescending(p => p).ToList();
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

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public IActionResult TicketInfo(string invoiceId)
        {
            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null)
                return NotFound();

            // Use robust navigation: get schedule seats and their related MovieShow and CinemaRoom
            var scheduleSeats = _scheduleSeatRepository.GetByInvoiceId(invoiceId).ToList();

            string roomName = "N/A";
            if (scheduleSeats.Any())
            {
                var movieShow = scheduleSeats.First().MovieShow;
                if (movieShow != null && movieShow.CinemaRoom != null)
                {
                    roomName = movieShow.CinemaRoom.CinemaRoomName;
                }
            }

            var member = _memberRepository.GetByAccountId(invoice.AccountId);
            // Prepare seat details
            var seatNamesArr = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
            var seats = new List<SeatDetailViewModel>();
            foreach (var seatName in seatNamesArr)
            {
                var trimmedSeatName = seatName.Trim();
                var seat = _seatService.GetSeatByName(seatName);
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
                MovieName = invoice.MovieName,
                CinemaRoomName = roomName,
                ShowDate = invoice.ScheduleShow ?? DateTime.Now,
                ShowTime = invoice.ScheduleShowTime,
                SelectedSeats = seats,
                TotalPrice = invoice.TotalMoney ?? 0,
                PricePerTicket = seats.Any() ? (invoice.TotalMoney ?? 0) / seats.Count : 0,
                InvoiceId = invoice.InvoiceId,
                ScoreUsed = invoice.UseScore ?? 0,
                Status = invoice.Status ?? InvoiceStatus.Incomplete,
                AddScore = invoice.AddScore ?? 0
            };

            string returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });

            // Use TempData if present, otherwise recalculate
            decimal subtotal = TempData["Subtotal"] != null ? Convert.ToDecimal(TempData["Subtotal"]) : seats.Sum(s => s.Price);
            decimal rankDiscount = TempData["RankDiscount"] != null ? Convert.ToDecimal(TempData["RankDiscount"]) : 0;
            if (rankDiscount == 0 && member?.Account?.Rank != null)
            {
                var rankDiscountPercent = member.Account.Rank.DiscountPercentage ?? 0;
                rankDiscount = subtotal * (rankDiscountPercent / 100m);
            }
            int usedScore = TempData["UsedScore"] != null ? Convert.ToInt32(TempData["UsedScore"]) : (invoice.UseScore ?? 0);
            int usedScoreValue = TempData["UsedScoreValue"] != null ? Convert.ToInt32(TempData["UsedScoreValue"]) : (invoice.UseScore ?? 0) * 1000;
            int addedScore = TempData["AddedScore"] != null ? Convert.ToInt32(TempData["AddedScore"]) : (invoice.AddScore ?? 0);
            int addedScoreValue = TempData["AddedScoreValue"] != null ? Convert.ToInt32(TempData["AddedScoreValue"]) : (invoice.AddScore ?? 0) * 1000;
            decimal totalPrice = TempData["TotalPrice"] != null ? Convert.ToDecimal(TempData["TotalPrice"]) : (invoice.TotalMoney ?? 0);
            string memberId = TempData["MemberId"] as string ?? member?.MemberId;
            string memberEmail = TempData["MemberEmail"] as string ?? member?.Account?.Email;
            string memberIdentityCard = TempData["MemberIdentityCard"] as string ?? member?.Account?.IdentityCard;
            string memberPhone = TempData["MemberPhone"] as string ?? member?.Account?.PhoneNumber;

            var viewModel = new ConfirmTicketAdminViewModel
            {
                BookingDetails = bookingDetails,
                MemberCheckMessage = "",
                ReturnUrl = returnUrl,
                MemberId = memberId,
                MemberEmail = memberEmail,
                MemberIdentityCard = memberIdentityCard,
                MemberPhone = memberPhone,
                UsedScore = usedScore,
                UsedScoreValue = usedScoreValue,
                AddedScore = addedScore,
                AddedScoreValue = addedScoreValue,
                Subtotal = subtotal,
                RankDiscount = rankDiscount,
                PromotionDiscount = promotionDiscount,
                TotalPrice = totalPrice
            };

            return View("TicketBookingConfirmed", viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetAllMembers()
        {
            var members = _memberRepository.GetAll()
                .Select(m => new {
                    memberId = m.MemberId,
                    score = m.Score,
                    account = new
                    {
                        fullName = m.Account?.FullName,
                        identityCard = m.Account?.IdentityCard,
                        email = m.Account?.Email,
                        phoneNumber = m.Account?.PhoneNumber
                    }
                }).ToList();
            return Json(members);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("Booking/InitiateTicketSellingForMember/{id}")]
        public IActionResult InitiateTicketSellingForMember(string id)
        {
            // Store the member's AccountId in TempData to use in the ticket selling process
            TempData["InitiateTicketSellingForMemberId"] = id;

            var returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });
            return RedirectToAction("Select", "Showtime", new { returnUrl = returnUrl });
        }

        [Authorize(Roles = "Admin")]
        public IActionResult GetMemberDiscount(string memberId)
        {
            if (string.IsNullOrEmpty(memberId))
                return Json(new { discountPercent = 0, earningRate = 0 });
            var member = _memberRepository.GetByMemberId(memberId);
            decimal discountPercent = 0;
            decimal earningRate = 0;
            if (member?.Account?.Rank != null)
            {
                discountPercent = member.Account.Rank.DiscountPercentage ?? 0;
                earningRate = member.Account.Rank.PointEarningPercentage ?? 0;
            }
            return Json(new { discountPercent, earningRate });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> GetEligiblePromotions([FromBody] GetEligiblePromotionsRequest request)
        {
            // Allow memberId to be null for general promotions
            try
            {
                var eligiblePromotions = _promotionService.GetEligiblePromotionsForMember(
                    request.MemberId,
                    request.SeatCount,
                    request.ShowDate,
                    request.MovieId,
                    request.MovieName
                );
                
                var promotionData = eligiblePromotions.Select(p => new
                {
                    promotionId = p.PromotionId,
                    title = p.Title,
                    detail = p.Detail,
                    discountLevel = p.DiscountLevel,
                    startTime = p.StartTime?.ToString("dd/MM/yyyy"),
                    endTime = p.EndTime?.ToString("dd/MM/yyyy"),
                    image = p.Image
                }).ToList();

                return Json(new { success = true, promotions = promotionData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error getting eligible promotions: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetEligiblePromotionsForUser([FromBody] GetEligiblePromotionsRequest request)
        {
            // For user bookings, we don't have a memberId, so we pass null
            try
            {
                var eligiblePromotions = _promotionService.GetEligiblePromotionsForMember(
                    null, // No memberId for user bookings
                    request.SeatCount,
                    request.ShowDate,
                    request.MovieId,
                    request.MovieName
                );
                
                var promotionData = eligiblePromotions.Select(p => new
                {
                    promotionId = p.PromotionId,
                    title = p.Title,
                    detail = p.Detail,
                    discountLevel = p.DiscountLevel,
                    startTime = p.StartTime?.ToString("dd/MM/yyyy"),
                    endTime = p.EndTime?.ToString("dd/MM/yyyy"),
                    image = p.Image
                }).ToList();

                return Json(new { success = true, promotions = promotionData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error getting eligible promotions: " + ex.Message });
            }
        }
    }
}
