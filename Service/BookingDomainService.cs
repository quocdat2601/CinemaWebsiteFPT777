using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MovieTheater.Service
{
    public class BookingDomainService : IBookingDomainService
    {
        private readonly IBookingService _bookingService;
        private readonly IMovieService _movieService;
        private readonly ISeatService _seatService;
        private readonly IAccountService _accountService;
        private readonly ISeatTypeService _seatTypeService;
        private readonly IPromotionService _promotionService;
        private readonly IFoodService _foodService;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly MovieTheaterContext _context;
        private readonly IBookingPriceCalculationService _priceCalculationService;
        private readonly IVoucherService _voucherService;

        public BookingDomainService(
            IBookingService bookingService,
            IMovieService movieService,
            ISeatService seatService,
            IAccountService accountService,
            ISeatTypeService seatTypeService,
            IPromotionService promotionService,
            IFoodService foodService,
            IInvoiceRepository invoiceRepository,
            MovieTheaterContext context,
            IBookingPriceCalculationService priceCalculationService,
            IVoucherService voucherService
        )
        {
            _bookingService = bookingService;
            _movieService = movieService;
            _seatService = seatService;
            _accountService = accountService;
            _seatTypeService = seatTypeService;
            _promotionService = promotionService;
            _foodService = foodService;
            _invoiceRepository = invoiceRepository;
            _context = context;
            _priceCalculationService = priceCalculationService;
            _voucherService = voucherService;
        }

        private async Task<List<Seat>> GetSeatsByIdsAsync(List<int> seatIds)
        {
            return await _context.Seats
                .Include(s => s.SeatType)
                .Where(s => seatIds.Contains(s.SeatId))
                .ToListAsync();
        }

        private async Task<List<Food>> GetFoodsByIdsAsync(List<int> foodIds)
        {
            return await _context.Foods.Where(f => foodIds.Contains(f.FoodId)).ToListAsync();
        }

        private decimal CalculateSeatPrice(Seat seat, MovieShow movieShow)
        {
            // Assume a base price, e.g., 100, or fetch from movieShow if available
            decimal basePrice = 100;
            decimal seatTypePercent = seat.SeatType?.PricePercent ?? 100;
            decimal versionMulti = movieShow.Version?.Multi ?? 1;
            return basePrice * (seatTypePercent / 100m) * versionMulti;
        }

        public async Task<ConfirmBookingViewModel> BuildConfirmBookingViewModelAsync(
            string movieId, DateOnly showDate, string showTime, List<int> selectedSeatIds, int movieShowId, List<int>? foodIds, List<int>? foodQtys, string userId)
        {
            var movie = _bookingService.GetById(movieId);
            if (movie == null) return null;

            var movieShows = _movieService.GetMovieShows(movieId);
            var movieShow = movieShows.FirstOrDefault(ms =>
                ms.ShowDate == showDate &&
                ms.Schedule?.ScheduleTime?.ToString("HH:mm") == showTime);
            if (movieShow == null) return null;

            var cinemaRoom = movieShow.CinemaRoom;
            if (cinemaRoom == null) return null;

            var seatTypes = await _seatService.GetSeatTypesAsync();
            var userAccount = _accountService.GetById(userId);
            if (userAccount == null) return null;

            var member = _context.Members.FirstOrDefault(m => m.AccountId == userId);

            decimal earningRate = 1;
            decimal rankDiscountPercent = 0;
            if (userAccount?.Rank != null)
            {
                earningRate = userAccount.Rank.PointEarningPercentage ?? 1;
                rankDiscountPercent = userAccount.Rank.DiscountPercentage ?? 0;
            }

            var bestPromotion = _promotionService.GetBestPromotionForShowDate(showDate);
            decimal promotionDiscountPercent = bestPromotion?.DiscountLevel ?? 0;

            var seats = new List<SeatDetailViewModel>();
            foreach (var id in selectedSeatIds)
            {
                var seat = _seatService.GetSeatById(id);
                if (seat == null) continue;
                var seatType = seatTypes.FirstOrDefault(t => t.SeatTypeId == seat.SeatTypeId);
                var price = seatType?.PricePercent ?? 0;
                decimal discount = Math.Round(price * (promotionDiscountPercent / 100m));
                decimal priceAfterPromotion = price - discount;
                string promotionName = bestPromotion != null && promotionDiscountPercent > 0 ? bestPromotion.Title : null;
                seats.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "Standard",
                    Price = priceAfterPromotion,
                    OriginalPrice = price,
                    PromotionDiscount = discount,
                    PriceAfterPromotion = priceAfterPromotion,
                    PromotionName = promotionName
                });
            }

            decimal subtotal = seats.Sum(s => s.Price);
            decimal rankDiscount = 0;
            if (userAccount?.Rank != null)
            {
                rankDiscount = subtotal * (rankDiscountPercent / 100m);
            }
            decimal totalPriceSeats = subtotal - rankDiscount;

            // Food
            List<FoodViewModel> selectedFoods = new List<FoodViewModel>();
            decimal totalFoodPrice = 0;
            if (foodIds != null && foodQtys != null && foodIds.Count == foodQtys.Count)
            {
                for (int i = 0; i < foodIds.Count; i++)
                {
                    var food = await _foodService.GetByIdAsync(foodIds[i]);
                    if (food != null)
                    {
                        var foodClone = new FoodViewModel
                        {
                            FoodId = food.FoodId,
                            Name = food.Name,
                            Price = food.Price,
                            Image = food.Image,
                            Description = food.Description,
                            Category = food.Category,
                            Status = food.Status,
                            CreatedDate = food.CreatedDate,
                            UpdatedDate = food.UpdatedDate,
                            Quantity = foodQtys[i]
                        };
                        selectedFoods.Add(foodClone);
                        totalFoodPrice += food.Price * foodQtys[i];
                    }
                }
            }

            var viewModel = new ConfirmBookingViewModel
            {
                MovieId = movieId,
                MovieName = movie.MovieNameEnglish,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                ShowDate = showDate,
                ShowTime = showTime,
                VersionName = movieShow.Version?.VersionName ?? "N/A",
                SelectedSeats = seats,
                Subtotal = subtotal,
                RankDiscount = rankDiscount,
                TotalPrice = totalPriceSeats + totalFoodPrice,
                FullName = userAccount.FullName,
                Email = userAccount.Email,
                IdentityCard = userAccount.IdentityCard,
                PhoneNumber = userAccount.PhoneNumber,
                CurrentScore = member?.Score ?? 0,
                EarningRate = earningRate,
                RankDiscountPercent = rankDiscountPercent,
                PromotionDiscountPercent = promotionDiscountPercent,
                MovieShowId = movieShowId,
                SelectedFoods = selectedFoods,
                TotalFoodPrice = totalFoodPrice
            };

            return viewModel;
        }

        public async Task<BookingResult> ConfirmBookingAsync(ConfirmBookingViewModel model, string userId, string isTestSuccess)
        {
            if (model == null || string.IsNullOrEmpty(userId) || model.SelectedSeats == null || !model.SelectedSeats.Any())
            {
                return new BookingResult { Success = false, ErrorMessage = "Invalid booking data.", InvoiceId = null };
            }

            var user = _accountService.GetById(userId);
            if (user == null)
            {
                return new BookingResult { Success = false, ErrorMessage = "User not found.", InvoiceId = null };
            }

            var seatIds = model.SelectedSeats.Select(s => s.SeatId ?? 0).ToList();
            var seats = await GetSeatsByIdsAsync(seatIds);
            List<Food> foods = new List<Food>();
            if (model.SelectedFoods != null && model.SelectedFoods.Any())
            {
                var foodIds = model.SelectedFoods.Select(f => f.FoodId).ToList();
                foods = await GetFoodsByIdsAsync(foodIds);
            }

            var movieShow = _context.MovieShows.Include(ms => ms.Version).FirstOrDefault(ms => ms.MovieShowId == model.MovieShowId);

            // Use the view model's seat list (with correct prices after promotion)
            var seatViewModels = model.SelectedSeats;
            var priceResult = _priceCalculationService.CalculatePrice(
                seatViewModels,
                movieShow,
                user,
                model.VoucherAmount,
                model.UseScore,
                foods
            );

            // Prefer posted values if present and valid
            decimal subtotal = model.Subtotal > 0 ? model.Subtotal : priceResult.Subtotal;
            decimal rankDiscount = model.RankDiscount > 0 ? model.RankDiscount : priceResult.RankDiscount;
            decimal rankDiscountPercent = model.RankDiscountPercent > 0 ? model.RankDiscountPercent : priceResult.RankDiscountPercent;
            decimal promotionDiscountPercent = model.PromotionDiscountPercent > 0 ? model.PromotionDiscountPercent : priceResult.PromotionDiscountPercent;
            decimal voucherAmount = model.VoucherAmount > 0 ? model.VoucherAmount : priceResult.VoucherAmount;
            int addScore = model.AddScore > 0 ? model.AddScore : priceResult.AddScore;
            decimal totalFoodPrice = model.TotalFoodPrice > 0 ? model.TotalFoodPrice : priceResult.TotalFoodPrice;
            decimal earningRate = model.EarningRate > 0 ? model.EarningRate : priceResult.RankDiscountPercent;
            decimal totalPrice = priceResult.TotalPrice;

            // Normalize voucher ID (member flow)
            string selectedVoucherId = string.IsNullOrWhiteSpace(model.SelectedVoucherId) || model.SelectedVoucherId == "null"
                ? null
                : model.SelectedVoucherId;
            Voucher memberVoucher = null;
            if (!string.IsNullOrEmpty(selectedVoucherId))
            {
                memberVoucher = _voucherService.GetById(selectedVoucherId);
                if (memberVoucher == null)
                {
                    return new BookingResult { Success = false, ErrorMessage = "Selected voucher does not exist." };
                }
            }
            var invoiceId = await _bookingService.GenerateInvoiceIdAsync();
            var seatNames = string.Join(", ", seats.Select(s => s.SeatName));
            var seatIdsStr = string.Join(",", seats.Select(s => s.SeatId));
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                AccountId = userId,
                BookingDate = DateTime.Now,
                TotalMoney = priceResult.SeatTotalAfterDiscounts,
                MovieShowId = model.MovieShowId,
                Status = InvoiceStatus.Completed,
                Seat = seatNames,
                Seat_IDs = seatIdsStr,
                RankDiscountPercentage = rankDiscountPercent,
                AddScore = addScore,
                UseScore = priceResult.UseScore,
                PromotionDiscount = (int?)promotionDiscountPercent,
                VoucherId = memberVoucher?.VoucherId
            };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Update member's score
            if (priceResult.AddScore > 0)
            {
                await _accountService.AddScoreAsync(userId, priceResult.AddScore);
            }
            if (priceResult.UseScore > 0)
            {
                await _accountService.DeductScoreAsync(userId, priceResult.UseScore);
            }

            // Mark voucher as used if applicable (member flow)
            if (priceResult.VoucherAmount > 0 && !string.IsNullOrEmpty(model.SelectedVoucherId))
            {
                var voucher = _voucherService.GetById(model.SelectedVoucherId);
                if (voucher != null && (voucher.IsUsed == false))
                {
                    voucher.IsUsed = true;
                    _voucherService.Update(voucher);
                }
            }

            foreach (var seat in seats)
            {
                var scheduleSeat = new ScheduleSeat
                {
                    InvoiceId = invoice.InvoiceId,
                    SeatId = seat.SeatId,
                    MovieShowId = model.MovieShowId,
                    SeatStatusId = 2 // Booked
                };
                _context.ScheduleSeats.Add(scheduleSeat);
            }
            if (model.SelectedFoods != null)
            {
                foreach (var foodVm in model.SelectedFoods)
                {
                    var foodInvoice = new FoodInvoice
                    {
                        InvoiceId = invoice.InvoiceId,
                        FoodId = foodVm.FoodId,
                        Quantity = foodVm.Quantity,
                        Price = foodVm.Price
                    };
                    _context.FoodInvoices.Add(foodInvoice);
                }
            }
            await _context.SaveChangesAsync();

            return new BookingResult { Success = true, ErrorMessage = null, InvoiceId = invoice.InvoiceId };
        }

        public async Task<BookingSuccessViewModel> BuildSuccessViewModelAsync(string invoiceId, string userId)
        {
            var invoice = _context.Invoices
                .Include(i => i.Account)
                .Include(i => i.MovieShow)
                .ThenInclude(ms => ms.Movie)
                .Include(i => i.MovieShow)
                .ThenInclude(ms => ms.CinemaRoom)
                .Include(i => i.MovieShow)
                .ThenInclude(ms => ms.Version)
                .Include(i => i.MovieShow)
                .ThenInclude(ms => ms.Schedule)
                .FirstOrDefault(i => i.InvoiceId == invoiceId);
            if (invoice == null)
                return null;

            var user = _accountService.GetById(userId);
            if (user == null)
                return null;

            var scheduleSeats = _context.ScheduleSeats.Where(s => s.InvoiceId == invoiceId).ToList();
            var seatIds = scheduleSeats.Select(s => s.SeatId).Where(id => id.HasValue).Select(id => id.Value).ToList();
            var seats = await GetSeatsByIdsAsync(seatIds);
            var foodInvoices = _context.FoodInvoices.Where(f => f.InvoiceId == invoiceId).ToList();
            var foodIds = foodInvoices.Select(f => f.FoodId).ToList();
            var foods = await GetFoodsByIdsAsync(foodIds);

            var movieShow = invoice.MovieShow;
            var promotionDiscountPercent = invoice.PromotionDiscount ?? 0;
            var seatDetails = seats.Select(s => {
                decimal originalPrice = s.SeatType?.PricePercent ?? 0;
                decimal discount = Math.Round(originalPrice * (promotionDiscountPercent / 100m));
                decimal priceAfterPromotion = originalPrice - discount;
                return new SeatDetailViewModel
                {
                    SeatId = s.SeatId,
                    SeatName = s.SeatName,
                    SeatType = s.SeatType?.TypeName,
                    Price = priceAfterPromotion,
                    OriginalPrice = originalPrice,
                    PromotionDiscount = discount,
                    PriceAfterPromotion = priceAfterPromotion,
                    PromotionName = (promotionDiscountPercent > 0) ? $"{promotionDiscountPercent}% Promo" : null
                };
            }).ToList();
            var foodViewModels = foodInvoices.Select(f => new FoodViewModel
            {
                FoodId = f.FoodId,
                Name = foods.FirstOrDefault(food => food.FoodId == f.FoodId)?.Name,
                Quantity = f.Quantity,
                Price = f.Price
            }).ToList();

            // Calculate all discounts and points for display
            decimal subtotal = seatDetails.Sum(s => s.Price);
            decimal rankDiscountPercent = invoice.RankDiscountPercentage ?? 0;
            decimal rankDiscount = subtotal * (rankDiscountPercent / 100m);
            decimal voucherAmount = 0;
            if (!string.IsNullOrEmpty(invoice.VoucherId))
            {
                var voucher = _context.Vouchers.FirstOrDefault(v => v.VoucherId == invoice.VoucherId);
                if (voucher != null)
                {
                    voucherAmount = voucher.Value;
                }
            }
            int usedScore = invoice.UseScore ?? 0;
            decimal usedScoreValue = usedScore * 1000;
            int addScore = invoice.AddScore ?? 0;
            decimal addScoreValue = addScore * 1000;
            decimal totalFoodPrice = foodViewModels.Sum(f => f.Price * f.Quantity);
            decimal totalPrice = subtotal - rankDiscount - voucherAmount - usedScoreValue;
            if (totalPrice < 0) totalPrice = 0;
            decimal grandTotal = totalPrice + totalFoodPrice;

            var bookingDetails = new ConfirmBookingViewModel
            {
                MovieName = movieShow.Movie?.MovieNameEnglish,
                CinemaRoomName = movieShow.CinemaRoom?.CinemaRoomName,
                ShowDate = movieShow.ShowDate,
                ShowTime = movieShow.Schedule?.ScheduleTime?.ToString(),
                VersionName = movieShow.Version?.VersionName,
                SelectedSeats = seatDetails,
                SelectedFoods = foodViewModels,
                Subtotal = subtotal,
                RankDiscount = rankDiscount,
                RankDiscountPercent = rankDiscountPercent,
                PromotionDiscountPercent = promotionDiscountPercent,
                VoucherAmount = voucherAmount,
                TotalFoodPrice = totalFoodPrice,
                TotalPrice = grandTotal,
                AddScore = addScore,
                ScoreUsed = usedScore,
                EarningRate = user?.Rank?.PointEarningPercentage ?? 1,
                InvoiceId = invoice.InvoiceId,
                BookingDate = invoice.BookingDate,
                Status = invoice.Status
            };

            return new BookingSuccessViewModel
            {
                BookingDetails = bookingDetails,
                MemberId = user.AccountId,
                MemberEmail = user.Email,
                MemberIdentityCard = user.IdentityCard,
                MemberPhone = user.PhoneNumber,
                UsedScore = usedScore,
                UsedScoreValue = (int)usedScoreValue,
                AddedScore = addScore,
                AddedScoreValue = (int)addScoreValue,
                Subtotal = subtotal,
                RankDiscount = rankDiscount,
                VoucherAmount = voucherAmount,
                TotalPrice = grandTotal,
                SelectedFoods = foodViewModels,
                TotalFoodPrice = totalFoodPrice
            };
        }

        // Build the view model for admin ticket confirmation (GET)
        public async Task<ConfirmTicketAdminViewModel> BuildConfirmTicketAdminViewModelAsync(int movieShowId, List<int> selectedSeatIds, List<int> foodIds, List<int> foodQtys)
        {
            var movieShow = _movieService.GetMovieShowById(movieShowId);
            if (movieShow == null) return null;
            var movie = movieShow.Movie;
            var cinemaRoom = movieShow.CinemaRoom;
            var seatTypes = await _seatService.GetSeatTypesAsync();
            var seats = new List<SeatDetailViewModel>();
            var bestPromotion = _promotionService.GetBestPromotionForShowDate(movieShow.ShowDate);
            decimal promotionDiscountPercent = bestPromotion?.DiscountLevel ?? 0;
            foreach (var id in selectedSeatIds)
            {
                var seat = _seatService.GetSeatById(id);
                if (seat == null) continue;
                var seatType = seatTypes.FirstOrDefault(t => t.SeatTypeId == seat.SeatTypeId);
                var price = seatType?.PricePercent ?? 0;
                decimal discount = Math.Round(price * (promotionDiscountPercent / 100m));
                decimal priceAfterPromotion = price - discount;
                string promotionName = bestPromotion != null && promotionDiscountPercent > 0 ? bestPromotion.Title : null;
                seats.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "Standard",
                    Price = priceAfterPromotion,
                    OriginalPrice = price,
                    PromotionDiscount = discount,
                    PriceAfterPromotion = priceAfterPromotion,
                    PromotionName = promotionName
                });
            }
            var totalPrice = seats.Sum(s => s.Price);
            // Handle selected foods
            List<FoodViewModel> selectedFoods = new List<FoodViewModel>();
            decimal totalFoodPrice = 0;
            if (foodIds != null && foodQtys != null && foodIds.Count == foodQtys.Count)
            {
                for (int i = 0; i < foodIds.Count; i++)
                {
                    var food = await _foodService.GetByIdAsync(foodIds[i]);
                    if (food != null)
                    {
                        var foodClone = new FoodViewModel
                        {
                            FoodId = food.FoodId,
                            Name = food.Name,
                            Price = food.Price,
                            Image = food.Image,
                            Description = food.Description,
                            Category = food.Category,
                            Status = food.Status,
                            CreatedDate = food.CreatedDate,
                            UpdatedDate = food.UpdatedDate,
                            Quantity = foodQtys[i]
                        };
                        selectedFoods.Add(foodClone);
                        totalFoodPrice += food.Price * foodQtys[i];
                    }
                }
            }
            var bookingDetails = new ConfirmBookingViewModel
            {
                MovieId = movie.MovieId,
                MovieName = movie.MovieNameEnglish,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                ShowDate = movieShow.ShowDate,
                ShowTime = movieShow.Schedule?.ScheduleTime?.ToString("HH:mm"),
                SelectedSeats = seats,
                TotalPrice = totalPrice,
                PricePerTicket = seats.Any() ? totalPrice / seats.Count : 0,
                MovieShowId = movieShowId,
                VersionName = movieShow.Version.VersionName,
                PromotionDiscountPercent = promotionDiscountPercent
            };
            var viewModel = new ConfirmTicketAdminViewModel
            {
                BookingDetails = bookingDetails,
                MemberCheckMessage = string.Empty,
                ReturnUrl = string.Empty, // Set in controller if needed
                MovieShowId = movieShowId,
                SelectedFoods = selectedFoods,
                TotalFoodPrice = totalFoodPrice,
                UsedScore = 0,
                VoucherAmount = 0
            };
            return viewModel;
        }

        // Handle the admin ticket confirmation (POST)
        public async Task<BookingResult> ConfirmTicketForAdminAsync(ConfirmTicketAdminViewModel model)
        {
            if (model.BookingDetails == null || model.BookingDetails.SelectedSeats == null)
            {
                return new BookingResult { Success = false, ErrorMessage = "Booking details or selected seats are missing." };
            }
            if (string.IsNullOrEmpty(model.MemberId))
            {
                return new BookingResult { Success = false, ErrorMessage = "Member check is required before confirming." };
            }
            var member = _context.Members
                .Include(m => m.Account)
                .ThenInclude(a => a.Rank)
                .FirstOrDefault(m => m.MemberId == model.MemberId);
            if (member == null)
            {
                return new BookingResult { Success = false, ErrorMessage = "Member not found. Please check member details again." };
            }
            if (member.Account == null)
            {
                return new BookingResult { Success = false, ErrorMessage = "Member account not found. Please check member details again." };
            }
            var seatIds = model.BookingDetails.SelectedSeats.Select(s => s.SeatId ?? 0).ToList();
            var seats = _context.Seats
                .Include(s => s.SeatType)
                .Where(s => seatIds.Contains(s.SeatId))
                .ToList();
            var movieShow = _movieService.GetMovieShowById(model.MovieShowId);
            var user = member?.Account;
            List<Food> adminFoods = new List<Food>();
            if (model.SelectedFoods != null && model.SelectedFoods.Any())
            {
                var foodIds = model.SelectedFoods.Select(f => f.FoodId).ToList();
                adminFoods = (await Task.WhenAll(foodIds.Select(id => _foodService.GetDomainByIdAsync(id)))).Where(f => f != null).ToList();
            }
            // Calculate seat subtotal and max usable points (backend validation)
            decimal seatSubtotal = seats.Sum(s => s.SeatType?.PricePercent ?? 0);
            decimal rankDiscountPercent = user?.Rank?.DiscountPercentage ?? 0;
            decimal rankDiscount = seatSubtotal * (rankDiscountPercent / 100m);
            if (movieShow != null)
            {
                var promotion = _promotionService.GetBestPromotionForShowDate(movieShow.ShowDate);
            }
            int usedScore = model.UsedScore;
            if (usedScore < 0) usedScore = 0;
            // Use the view model's seat list (with correct prices after promotion)
            var seatViewModels = model.BookingDetails.SelectedSeats;
            var priceResult = _priceCalculationService.CalculatePrice(
                seatViewModels,
                movieShow,
                user,
                model.VoucherAmount,
                usedScore,
                adminFoods
            );
            // Normalize voucher ID (admin flow)
            string adminSelectedVoucherId = string.IsNullOrWhiteSpace(model.SelectedVoucherId) || model.SelectedVoucherId == "null"
                ? null
                : model.SelectedVoucherId;
            Voucher adminVoucher = null;
            if (!string.IsNullOrEmpty(adminSelectedVoucherId))
            {
                adminVoucher = _voucherService.GetById(adminSelectedVoucherId);
                if (adminVoucher == null)
                {
                    return new BookingResult { Success = false, ErrorMessage = "Selected voucher does not exist." };
                }
            }
            // Only use the value from the view model
            int promotionDiscountPercent = (int)model.BookingDetails.PromotionDiscountPercent;
            var invoice = new Invoice
            {
                InvoiceId = await _bookingService.GenerateInvoiceIdAsync(),
                AccountId = member.Account.AccountId,
                AddScore = priceResult.AddScore,
                BookingDate = DateTime.Now,
                Status = InvoiceStatus.Completed,
                TotalMoney = priceResult.SeatTotalAfterDiscounts, // Only seat price after discounts
                UseScore = priceResult.UseScore,
                Seat = string.Join(", ", seatViewModels.Select(s => s.SeatName)),
                Seat_IDs = string.Join(",", seatViewModels.Select(s => s.SeatId)),
                MovieShowId = model.MovieShowId,
                PromotionDiscount = promotionDiscountPercent, // save the percent used
                VoucherId = adminVoucher?.VoucherId,
                RankDiscountPercentage = priceResult.RankDiscountPercent
            };
            await _bookingService.SaveInvoiceAsync(invoice);
            if (priceResult.AddScore > 0)
            {
                await _accountService.AddScoreAsync(member.Account.AccountId, priceResult.AddScore);
            }
            if (priceResult.UseScore > 0)
            {
                await _accountService.DeductScoreAsync(member.Account.AccountId, priceResult.UseScore);
            }
            _accountService.CheckAndUpgradeRank(member.AccountId);
            if (priceResult.VoucherAmount > 0 && !string.IsNullOrEmpty(model.SelectedVoucherId))
            {
                var voucher = _voucherService.GetById(model.SelectedVoucherId);
                if (voucher != null && (voucher.IsUsed == false))
                {
                    voucher.IsUsed = true;
                    _voucherService.Update(voucher);
                }
            }
            // Save food invoices
            if (model.SelectedFoods != null && model.SelectedFoods.Any())
            {
                foreach (var foodVm in model.SelectedFoods)
                {
                    var foodInvoice = new FoodInvoice
                    {
                        InvoiceId = invoice.InvoiceId,
                        FoodId = foodVm.FoodId,
                        Quantity = foodVm.Quantity,
                        Price = foodVm.Price
                    };
                    _context.FoodInvoices.Add(foodInvoice);
                }
                await _context.SaveChangesAsync();
            }
            return new BookingResult { Success = true, InvoiceId = invoice.InvoiceId };
        }

        // Build the view model for the admin ticket booking confirmation page (GET)
        public async Task<ConfirmTicketAdminViewModel> BuildTicketBookingConfirmedViewModelAsync(string invoiceId)
        {
            var invoice = _context.Invoices
                .Include(i => i.Account)
                .Include(i => i.MovieShow)
                .ThenInclude(ms => ms.Movie)
                .Include(i => i.MovieShow)
                .ThenInclude(ms => ms.CinemaRoom)
                .Include(i => i.MovieShow)
                .ThenInclude(ms => ms.Version)
                .Include(i => i.MovieShow)
                .ThenInclude(ms => ms.Schedule)
                .FirstOrDefault(i => i.InvoiceId == invoiceId);
            if (invoice == null) return null;
            var member = _context.Members.FirstOrDefault(m => m.AccountId == invoice.AccountId);
            var movieShow = invoice.MovieShow;
            var cinemaRoomName = movieShow.CinemaRoom?.CinemaRoomName ?? "N/A";
            var movieName = movieShow.Movie?.MovieNameEnglish ?? "N/A";
            var showDate = movieShow.ShowDate;
            var showTime = movieShow.Schedule?.ScheduleTime?.ToString() ?? "N/A";
            var versionName = movieShow.Version?.VersionName ?? "N/A";
            // Prepare seat details
            var seatIdArr = invoice.Seat_IDs
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.Parse(id.Trim()))
                .ToList();
            var seats = new List<SeatDetailViewModel>();
            foreach (var seatId in seatIdArr)
            {
                var seat = _seatService.GetSeatById(seatId);
                if (seat == null) continue;
                var seatType = seat.SeatTypeId.HasValue ? _seatTypeService.GetById(seat.SeatTypeId.Value) : null;
                decimal originalPrice = seatType?.PricePercent ?? 0;
                decimal seatPromotionDiscount = invoice.PromotionDiscount ?? 0;
                decimal priceAfterPromotion = originalPrice;
                if (seatPromotionDiscount > 0)
                {
                    priceAfterPromotion = originalPrice * (1 - seatPromotionDiscount / 100m);
                }
                seats.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "N/A",
                    Price = priceAfterPromotion,
                    OriginalPrice = originalPrice,
                    PromotionDiscount = seatPromotionDiscount,
                    PriceAfterPromotion = priceAfterPromotion
                });
            }
            var bookingDetails = new ConfirmBookingViewModel
            {
                MovieName = movieName,
                CinemaRoomName = cinemaRoomName,
                ShowDate = showDate,
                ShowTime = showTime,
                VersionName = versionName,
                SelectedSeats = seats,
                TotalPrice = invoice.TotalMoney ?? 0,
                PricePerTicket = seats.Any() ? (invoice.TotalMoney ?? 0) / seats.Count : 0,
                InvoiceId = invoice.InvoiceId,
                ScoreUsed = invoice.UseScore ?? 0,
                Status = invoice.Status ?? InvoiceStatus.Incomplete, // Ensure status is set from DB
                AddScore = invoice.AddScore ?? 0
            };
            // Food
            var selectedFoods = _context.FoodInvoices.Where(f => f.InvoiceId == invoiceId).ToList()
                .Select(f => {
                    var food = _context.Foods.FirstOrDefault(food => food.FoodId == f.FoodId);
                    return new FoodViewModel
                    {
                        FoodId = f.FoodId,
                        Name = food?.Name ?? "N/A",
                        Quantity = f.Quantity,
                        Price = f.Price
                    };
                }).ToList();
            decimal totalFoodPrice = selectedFoods.Sum(f => f.Price * f.Quantity);
            // Calculate discounts and totals
            decimal subtotal = seats.Sum(s => s.Price);
            decimal rankDiscount = 0;
            if (invoice.RankDiscountPercentage.HasValue && invoice.RankDiscountPercentage.Value > 0)
            {
                var rankDiscountPercent = invoice.RankDiscountPercentage ?? 0;
                rankDiscount = subtotal * (rankDiscountPercent / 100m);
            }
            int usedScore = invoice.UseScore ?? 0;
            int usedScoreValue = usedScore * 1000;
            int addedScore = invoice.AddScore ?? 0;
            int addedScoreValue = addedScore * 1000;
            decimal voucherAmount = 0;
            if (!string.IsNullOrEmpty(invoice.VoucherId))
            {
                var voucher = _context.Vouchers.FirstOrDefault(v => v.VoucherId == invoice.VoucherId);
                if (voucher != null)
                {
                    voucherAmount = voucher.Value;
                }
            }
            decimal totalPrice = subtotal - rankDiscount - voucherAmount - usedScoreValue;
            if (totalPrice < 0) totalPrice = 0;
            var viewModel = new ConfirmTicketAdminViewModel
            {
                BookingDetails = bookingDetails,
                MemberCheckMessage = string.Empty,
                // Set default ReturnUrl to Booking Management
                ReturnUrl = "/Admin/MainPage?tab=BookingMg",
                MemberId = member?.MemberId,
                MemberEmail = member?.Account?.Email,
                MemberIdentityCard = member?.Account?.IdentityCard,
                MemberPhone = member?.Account?.PhoneNumber,
                UsedScore = usedScore,
                UsedScoreValue = usedScoreValue,
                AddedScore = addedScore,
                AddedScoreValue = addedScoreValue,
                Subtotal = subtotal,
                RankDiscount = rankDiscount,
                VoucherAmount = voucherAmount,
                TotalPrice = totalPrice,
                RankDiscountPercent = invoice.RankDiscountPercentage ?? 0,
                SelectedFoods = selectedFoods,
                TotalFoodPrice = totalFoodPrice
            };
            return viewModel;
        }
    }
} 