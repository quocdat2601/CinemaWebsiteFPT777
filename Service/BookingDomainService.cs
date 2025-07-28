using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.AspNetCore.SignalR;

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
        private readonly MovieTheaterContext _context;
        private readonly IBookingPriceCalculationService _priceCalculationService;
        private readonly IVoucherService _voucherService;
        private readonly IHubContext<MovieTheater.Hubs.SeatHub> _seatHubContext;

        public BookingDomainService(
            IBookingService bookingService,
            IMovieService movieService,
            ISeatService seatService,
            IAccountService accountService,
            ISeatTypeService seatTypeService,
            IPromotionService promotionService,
            IFoodService foodService,
            MovieTheaterContext context,
            IBookingPriceCalculationService priceCalculationService,
            IVoucherService voucherService,
            IHubContext<MovieTheater.Hubs.SeatHub> seatHubContext)
        {
            _bookingService = bookingService;
            _movieService = movieService;
            _seatService = seatService;
            _accountService = accountService;
            _seatTypeService = seatTypeService;
            _promotionService = promotionService;
            _foodService = foodService;
            _context = context;
            _priceCalculationService = priceCalculationService;
            _voucherService = voucherService;
            _seatHubContext = seatHubContext;
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

            var movieShow = _movieService.GetMovieShowById(movieShowId);
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

            // Lấy promotion cho seat (chỉ lấy promotion không phải food)
            var promotionContext = new PromotionCheckContext
            {
                MemberId = member?.MemberId, // <-- Đúng định dạng MBxxx
                SeatCount = selectedSeatIds?.Count ?? 0,
                MovieId = movie?.MovieId,
                MovieName = movie?.MovieNameEnglish,
                ShowDate = showDate.ToDateTime(TimeOnly.MinValue)
            };

            // Thêm thông tin SeatType cho promotion context
            if (selectedSeatIds != null)
            {
                var selectedSeats = await GetSeatsByIdsAsync(selectedSeatIds);
                promotionContext.SelectedSeatTypeIds = selectedSeats.Select(s => s.SeatTypeId ?? 0).Distinct().ToList();
                promotionContext.SelectedSeatTypeNames = selectedSeats.Select(s => s.SeatType?.TypeName).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
                promotionContext.SelectedSeatTypePricePercents = selectedSeats.Select(s => s.SeatType?.PricePercent ?? 0).Distinct().ToList();
            }
            var bestPromotion = _promotionService.GetBestEligiblePromotionForBooking(promotionContext);
            decimal promotionDiscountPercent = bestPromotion?.DiscountLevel ?? 0;

            var seats = new List<SeatDetailViewModel>();
            var loadedSeats = await GetSeatsByIdsAsync(selectedSeatIds);
            foreach (var seat in loadedSeats)
            {
                if (seat == null) continue;
                var seatType = seat.SeatType; // Sử dụng SeatType đã được Include
                var price = seatType?.PricePercent ?? 0;
                // Apply version multiplier if available
                if (movieShow.Version != null)
                    price *= (decimal)movieShow.Version.Multi;
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
                var foodTuples = new List<(int FoodId, int Quantity, decimal Price)>();
                for (int i = 0; i < foodIds.Count; i++)
                {
                    var food = await _foodService.GetByIdAsync(foodIds[i]);
                    if (food != null)
                    {
                        foodTuples.Add((food.FoodId, foodQtys[i], food.Price));
                    }
                }
                var eligibleFoodPromotions = _promotionService.GetEligibleFoodPromotions(foodTuples);
                var foodDiscounts = _promotionService.ApplyFoodPromotionsToFoods(foodTuples, eligibleFoodPromotions);
                for (int i = 0; i < foodTuples.Count; i++)
                {
                    var food = await _foodService.GetByIdAsync(foodTuples[i].FoodId);
                    var discountInfo = foodDiscounts.FirstOrDefault(f => f.FoodId == foodTuples[i].FoodId);
                    var foodClone = new FoodViewModel
                    {
                        FoodId = food.FoodId,
                        Name = food.Name,
                        Price = discountInfo.DiscountedPrice,
                        Image = food.Image,
                        Description = food.Description,
                        Category = food.Category,
                        Status = food.Status,
                        CreatedDate = food.CreatedDate,
                        UpdatedDate = food.UpdatedDate,
                        Quantity = foodTuples[i].Quantity,
                        PromotionName = discountInfo.PromotionName,
                        PromotionDiscount = discountInfo.DiscountLevel,
                        OriginalPrice = discountInfo.OriginalPrice
                    };
                    selectedFoods.Add(foodClone);
                    totalFoodPrice += discountInfo.DiscountedPrice * foodTuples[i].Quantity;
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
                VersionId = movieShow.VersionId, // <-- Set VersionId here
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
            Console.WriteLine($"[DomainService] model.SelectedVoucherId: {model.SelectedVoucherId}, model.VoucherAmount: {model.VoucherAmount}");
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

            var seatViewModels = model.SelectedSeats;
            var priceResult = _priceCalculationService.CalculatePrice(
                seatViewModels,
                movieShow,
                user,
                model.VoucherAmount,
                model.UseScore,
                foods
            );

            decimal subtotal = model.Subtotal > 0 ? model.Subtotal : priceResult.Subtotal;
            decimal rankDiscount = model.RankDiscount > 0 ? model.RankDiscount : priceResult.RankDiscount;
            decimal rankDiscountPercent = model.RankDiscountPercent > 0 ? model.RankDiscountPercent : priceResult.RankDiscountPercent;
            decimal promotionDiscountPercent = model.PromotionDiscountPercent > 0 ? model.PromotionDiscountPercent : priceResult.PromotionDiscountPercent;
            decimal voucherAmount = model.VoucherAmount;
            int addScore = model.AddScore > 0 ? model.AddScore : priceResult.AddScore;
            decimal totalFoodPrice = model.TotalFoodPrice > 0 ? model.TotalFoodPrice : priceResult.TotalFoodPrice;
            decimal earningRate = model.EarningRate > 0 ? model.EarningRate : priceResult.RankDiscountPercent;
            decimal totalPrice = priceResult.TotalPrice;

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

            // Chuẩn bị dữ liệu promotion discount cho food
            var foodDiscounts = new List<object>();
            if (model.SelectedFoods != null)
            {
                foreach (var foodVm in model.SelectedFoods)
                {
                    // Lưu giống admin: chỉ foodId và discount
                    foodDiscounts.Add(new { foodId = foodVm.FoodId, discount = foodVm.PromotionDiscount });
                }
            }
            var promotionDiscountObj = new {
                seat = promotionDiscountPercent,
                food = foodDiscounts
            };
            string promotionDiscountJson = JsonConvert.SerializeObject(promotionDiscountObj);
            // Tính lại tổng food sau giảm
            decimal totalFoodDiscounted = model.SelectedFoods?.Sum(f => f.Price * f.Quantity) ?? 0;
            decimal finalTotalPrice = priceResult.SeatTotalAfterDiscounts + totalFoodDiscounted;
            if (finalTotalPrice < 0) finalTotalPrice = 0;
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                AccountId = userId,
                BookingDate = DateTime.Now,
                TotalMoney = finalTotalPrice, // <-- luôn là seat sau giảm + food sau giảm - discount
                MovieShowId = model.MovieShowId,
                Status = (isTestSuccess == "true") ? InvoiceStatus.Completed : InvoiceStatus.Incomplete,
                Seat = seatNames,
                SeatIds = seatIdsStr,
                RankDiscountPercentage = rankDiscountPercent,
                AddScore = addScore,
                UseScore = priceResult.UseScore,
                PromotionDiscount = promotionDiscountJson,
                VoucherId = selectedVoucherId // Use selectedVoucherId directly
            };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Only update scores and voucher if test success (real payment: do this after payment success)
            if (isTestSuccess == "true")
            {
                if (priceResult.AddScore > 0)
                {
                    await _accountService.AddScoreAsync(userId, priceResult.AddScore, true); // Pass isTestSuccess true
                }
                if (priceResult.UseScore > 0)
                {
                    await _accountService.DeductScoreAsync(userId, priceResult.UseScore, true); // Pass isTestSuccess true
                }
                // Always mark voucher as used if applied (SelectedVoucherId or VoucherId)
                string voucherIdToUse = !string.IsNullOrEmpty(model.SelectedVoucherId) ? model.SelectedVoucherId : invoice.VoucherId;
                if (!string.IsNullOrEmpty(voucherIdToUse))
                {
                    var voucher = _voucherService.GetById(voucherIdToUse);
                    if (voucher != null && (voucher.IsUsed == false))
                    {
                        voucher.IsUsed = true;
                        _voucherService.Update(voucher);
                    }
                }
            }

            // Đừng set SeatStatusId = 2 ở đây nữa, chỉ tạo ScheduleSeat nếu cần, không set trạng thái booked
            foreach (var seat in seats)
            {
                var scheduleSeat = new ScheduleSeat
                {
                    InvoiceId = invoice.InvoiceId,
                    SeatId = seat.SeatId,
                    MovieShowId = model.MovieShowId
                    // KHÔNG set SeatStatusId ở đây
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

            // Do NOT release hold here. Hold should only be released on payment success, seat deselect, or timeout.
            // foreach (var seat in seats)
            // {
            //     MovieTheater.Hubs.SeatHub.ReleaseHold(model.MovieShowId, seat.SeatId);
            // }

            // Calculate total price after all discounts (seat subtotal - rank discount - voucher - points)
            totalPrice = subtotal - rankDiscount - voucherAmount - (priceResult.UseScore * 1000);
            if (totalPrice < 0) totalPrice = 0;
            totalPrice += totalFoodPrice;
            Console.WriteLine($"subtotal: {subtotal}, rankDiscount: {rankDiscount}, voucherAmount: {voucherAmount}, usedScoreValue: {priceResult.UseScore * 1000}, totalFoodPrice: {totalFoodPrice}, totalPrice: {totalPrice}");

            return new BookingResult { Success = true, ErrorMessage = null, InvoiceId = invoice.InvoiceId, TotalPrice = totalPrice };
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

            // Parse the seat IDs from the invoice
            var seatIdList = (invoice.SeatIds ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.Parse(id.Trim()))
                .ToList();

            // Only include ScheduleSeat records for seats in the invoice's SeatIds
            var scheduleSeats = _context.ScheduleSeats
                .Include(ss => ss.Seat)
                .ThenInclude(s => s.SeatType)
                .Where(s => s.InvoiceId == invoiceId && seatIdList.Contains((int)s.SeatId))
                .ToList();

            var movieShow = invoice.MovieShow;
            int promotionDiscountPercent = 0;
            if (!string.IsNullOrEmpty(invoice.PromotionDiscount) && invoice.PromotionDiscount != "0")
            {
                try
                {
                    var promoObj = JsonConvert.DeserializeObject<dynamic>(invoice.PromotionDiscount);
                    promotionDiscountPercent = (int)(promoObj.seat ?? 0);
                }
                catch { promotionDiscountPercent = 0; }
            }
            var versionMulti = movieShow?.Version?.Multi ?? 1;
            var seatDetails = scheduleSeats.Select(ss => {
                var s = ss.Seat;
                decimal basePrice = s?.SeatType?.PricePercent ?? 0;
                decimal originalPrice = basePrice * versionMulti; // <-- nhân hệ số version
                decimal discount = Math.Round(originalPrice * (promotionDiscountPercent / 100m));
                decimal priceAfterPromotion = originalPrice - discount;
                return new SeatDetailViewModel
                {
                    SeatId = s?.SeatId,
                    SeatName = s?.SeatName,
                    SeatType = s?.SeatType?.TypeName,
                    Price = originalPrice,
                    OriginalPrice = originalPrice,
                    PromotionDiscount = discount,
                    PriceAfterPromotion = priceAfterPromotion,
                    PromotionName = (promotionDiscountPercent > 0) ? $"{promotionDiscountPercent}% Promo" : null
                };
            }).ToList();

            var foodInvoices = _context.FoodInvoices.Where(f => f.InvoiceId == invoiceId).ToList();
            var foodIds = foodInvoices.Select(f => f.FoodId).ToList();
            var foods = await GetFoodsByIdsAsync(foodIds);
            var selectedFoods = foodInvoices.Select(f => {
                var food = foods.FirstOrDefault(food => food.FoodId == f.FoodId);
                var originalPrice = food?.Price ?? f.Price;
                var discountedPrice = f.Price;
                var discountLevel = originalPrice > 0 ? Math.Round((originalPrice - discountedPrice) / originalPrice * 100, 2) : 0;
                return new FoodViewModel
                {
                    FoodId = f.FoodId,
                    Name = food?.Name ?? "N/A",
                    Quantity = f.Quantity,
                    Price = discountedPrice,
                    OriginalPrice = originalPrice,
                    PromotionDiscount = discountLevel,
                    PromotionName = discountLevel > 0 ? "Promotion" : null
                };
            }).ToList();

           
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
            decimal totalFoodPrice = selectedFoods.Sum(f => f.Price * f.Quantity);
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
                SelectedFoods = selectedFoods,
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
                SelectedFoods = selectedFoods,
                TotalFoodPrice = totalFoodPrice
            };
        }

        // Build the view model for admin ticket confirmation (GET)
        public async Task<ConfirmTicketAdminViewModel> BuildConfirmTicketAdminViewModelAsync(int movieShowId, List<int> selectedSeatIds, List<int> foodIds, List<int> foodQtys, string memberId = null)
        {
            var movieShow = _movieService.GetMovieShowById(movieShowId);
            if (movieShow == null) return null;
            var movie = movieShow.Movie;
            var cinemaRoom = movieShow.CinemaRoom;
            var seatTypes = await _seatService.GetSeatTypesAsync();
            var seats = new List<SeatDetailViewModel>();
            
            // Tính rank discount percent nếu có member
            decimal rankDiscountPercent = 0;
            if (!string.IsNullOrEmpty(memberId))
            {
                var member = _context.Members.Include(m => m.Account).ThenInclude(a => a.Rank).FirstOrDefault(m => m.MemberId == memberId);
                if (member?.Account?.Rank != null)
                {
                    rankDiscountPercent = member.Account.Rank.DiscountPercentage ?? 0;
                }
            }
            
            // --- PROMOTION LOGIC UPDATE START ---
            // Load thông tin SeatType cho promotion context
            var selectedSeats = await GetSeatsByIdsAsync(selectedSeatIds);
            var promotionContext = new PromotionCheckContext
            {
                MemberId = memberId, // <-- truyền memberId mới
                SeatCount = selectedSeatIds?.Count ?? 0,
                MovieId = movie?.MovieId,
                MovieName = movie?.MovieNameEnglish,
                ShowDate = movieShow.ShowDate.ToDateTime(TimeOnly.MinValue),
                SelectedSeatTypeIds = selectedSeats.Select(s => s.SeatTypeId ?? 0).Distinct().ToList(),
                SelectedSeatTypeNames = selectedSeats.Select(s => s.SeatType?.TypeName).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList(),
                SelectedSeatTypePricePercents = selectedSeats.Select(s => s.SeatType?.PricePercent ?? 0).Distinct().ToList()
            };
            var bestPromotion = _promotionService.GetBestEligiblePromotionForBooking(promotionContext);
            decimal promotionDiscountPercent = bestPromotion?.DiscountLevel ?? 0;
            foreach (var seat in selectedSeats)
            {
                if (seat == null) continue;
                var seatType = seat.SeatType; // Sử dụng SeatType đã được Include
                var price = seatType?.PricePercent ?? 0;
                // Apply version multiplier if available
                if (movieShow.Version != null)
                    price *= (decimal)movieShow.Version.Multi;
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
            List<Promotion> eligibleFoodPromotions = new List<Promotion>();
            if (foodIds != null && foodQtys != null && foodIds.Count == foodQtys.Count)
            {
                var foodTuples = new List<(int FoodId, int Quantity, decimal Price)>();
                for (int i = 0; i < foodIds.Count; i++)
                {
                    var food = await _foodService.GetByIdAsync(foodIds[i]);
                    if (food != null)
                    {
                        foodTuples.Add((food.FoodId, foodQtys[i], food.Price));
                    }
                }
                eligibleFoodPromotions = _promotionService.GetEligibleFoodPromotions(foodTuples);
                var foodDiscounts = _promotionService.ApplyFoodPromotionsToFoods(foodTuples, eligibleFoodPromotions);
                for (int i = 0; i < foodTuples.Count; i++)
                {
                    var food = await _foodService.GetByIdAsync(foodTuples[i].FoodId);
                    var discountInfo = foodDiscounts.FirstOrDefault(f => f.FoodId == foodTuples[i].FoodId);
                    var foodClone = new FoodViewModel
                    {
                        FoodId = food.FoodId,
                        Name = food.Name,
                        Price = discountInfo.DiscountedPrice,
                        Image = food.Image,
                        Description = food.Description,
                        Category = food.Category,
                        Status = food.Status,
                        CreatedDate = food.CreatedDate,
                        UpdatedDate = food.UpdatedDate,
                        Quantity = foodTuples[i].Quantity,
                        PromotionName = discountInfo.PromotionName,
                        PromotionDiscount = discountInfo.DiscountLevel,
                        OriginalPrice = discountInfo.OriginalPrice
                    };
                    selectedFoods.Add(foodClone);
                    totalFoodPrice += discountInfo.DiscountedPrice * foodTuples[i].Quantity;
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
                VersionId = movieShow.VersionId, // <-- Set VersionId here
                PromotionDiscountPercent = promotionDiscountPercent,
                RankDiscountPercent = rankDiscountPercent // SỬA: Thêm rank discount percent
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
                VoucherAmount = 0,
                EligibleFoodPromotions = eligibleFoodPromotions,
                TotalFoodDiscount = 0 // This will be calculated in ApplyFoodPromotionsToFoods
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

            // PHÂN BIỆT MEMBER VÀ GUEST
            bool isGuest = string.Equals(model.CustomerType, "guest", StringComparison.OrdinalIgnoreCase);
            string accountId = isGuest ? "GUEST" : null;
            string memberId = isGuest ? null : model.MemberId;

            if (!isGuest && string.IsNullOrEmpty(memberId))
            {
                return new BookingResult { Success = false, ErrorMessage = "Member check is required before confirming." };
            }
            var member = _context.Members.Include(m => m.Account).ThenInclude(a => a.Rank).FirstOrDefault(m => m.MemberId == model.MemberId);
            // Khi cần kiểm tra promotion, truyền đúng MemberId (MBxxx) vào context
            var promotionContext = new PromotionCheckContext
            {
                MemberId = member?.MemberId, // hoặc null nếu chưa chọn member
                SeatCount = model.BookingDetails.SelectedSeats?.Count ?? 0,
                MovieId = model.BookingDetails.MovieId,
                MovieName = model.BookingDetails.MovieName,
                ShowDate = model.BookingDetails.ShowDate.ToDateTime(TimeOnly.MinValue)
            };
            var bestPromotion = _promotionService.GetBestEligiblePromotionForBooking(promotionContext);
            decimal promotionDiscountPercent = bestPromotion?.DiscountLevel ?? 0;
            if (member == null)
            {
                return new BookingResult { Success = false, ErrorMessage = "Member not found. Please check member details again." };
            }
            if (!isGuest && member.Account == null)
            {
                return new BookingResult { Success = false, ErrorMessage = "Member account not found. Please check member details again." };
            }
            var seatIds = model.BookingDetails.SelectedSeats.Select(s => s.SeatId ?? 0).ToList();
            var seats = _context.Seats
                .Include(s => s.SeatType)
                .Where(s => seatIds.Contains(s.SeatId))
                .ToList();
            var movieShow = _movieService.GetMovieShowById(model.MovieShowId);
            var user = !isGuest ? member?.Account : null;
            List<Food> adminFoods = new List<Food>();
            if (model.SelectedFoods != null && model.SelectedFoods.Any())
            {
                var foodIds = model.SelectedFoods.Select(f => f.FoodId).ToList();
                adminFoods = new List<Food>();
                foreach (var id in foodIds)
                {
                    var food = await _foodService.GetDomainByIdAsync(id);
                    if (food != null)
                        adminFoods.Add(food);
                }
            }
            int usedScore = model.UsedScore;
            if (usedScore < 0) usedScore = 0;
            var seatViewModels = model.BookingDetails.SelectedSeats;
            var priceResult = _priceCalculationService.CalculatePrice(
                seatViewModels,
                movieShow,
                user,
                model.VoucherAmount,
                usedScore,
                adminFoods
            );
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
            int seatPromotionDiscount = (int?)priceResult.PromotionDiscountPercent ?? 0;
            var foodDiscounts = new List<object>();
            if (model.SelectedFoods != null)
            {
                foreach (var foodVm in model.SelectedFoods)
                {
                    foodDiscounts.Add(new { foodId = foodVm.FoodId, discount = foodVm.PromotionDiscount });
                }
            }
            var promotionDiscountObj = new {
                seat = seatPromotionDiscount,
                food = foodDiscounts
            };
            string promotionDiscountJson = JsonConvert.SerializeObject(promotionDiscountObj);
            // Tính lại tổng food sau giảm
            decimal totalFoodDiscounted = model.SelectedFoods?.Sum(f => f.Price * f.Quantity) ?? 0;
            decimal finalTotalPrice = priceResult.SeatTotalAfterDiscounts + totalFoodDiscounted;
            // Trừ tiếp voucher, điểm nếu cần (đã tính trong priceResult.SeatTotalAfterDiscounts)
            if (finalTotalPrice < 0) finalTotalPrice = 0;
            // TẠO INVOICE_ID TĂNG DẦN DẠNG INVxxx
            string invoiceId = await _bookingService.GenerateInvoiceIdAsync();
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                AccountId = isGuest ? "GUEST" : member.Account.AccountId,
                AddScore = isGuest ? 0 : priceResult.AddScore,
                BookingDate = DateTime.Now,
                Status = InvoiceStatus.Completed,
                TotalMoney = finalTotalPrice, // <-- luôn là seat sau giảm + food sau giảm - discount
                UseScore = priceResult.UseScore,
                Seat = string.Join(", ", seatViewModels.Select(s => s.SeatName)),
                SeatIds = string.Join(",", seatViewModels.Select(s => s.SeatId)),
                MovieShowId = model.MovieShowId,
                PromotionDiscount = promotionDiscountJson,
                VoucherId = isGuest ? null : adminVoucher?.VoucherId,
                RankDiscountPercentage = isGuest ? 0 : priceResult.RankDiscountPercent
            };
            await _bookingService.SaveInvoiceAsync(invoice);
            // Add ScheduleSeat records for each seat (admin flow fix) - SỬA: Thêm SeatStatusId = 2
            foreach (var seat in seats)
            {
                var scheduleSeat = new ScheduleSeat
                {
                    InvoiceId = invoice.InvoiceId,
                    SeatId = seat.SeatId,
                    MovieShowId = model.MovieShowId,
                    SeatStatusId = 2 // SỬA: Set trạng thái booked
                };
                _context.ScheduleSeats.Add(scheduleSeat);
            }
            await _context.SaveChangesAsync();
            if (!isGuest && priceResult.AddScore > 0)
            {
                await _accountService.AddScoreAsync(member.Account.AccountId, priceResult.AddScore);
            }
            if (!isGuest && priceResult.UseScore > 0)
            {
                await _accountService.DeductScoreAsync(member.Account.AccountId, priceResult.UseScore);
            }
            if (!isGuest) _accountService.CheckAndUpgradeRank(member.AccountId);
            if (!isGuest && priceResult.VoucherAmount > 0 && !string.IsNullOrEmpty(model.SelectedVoucherId))
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
            // Update or add ScheduleSeat records for each seat - SỬA: Thêm SeatStatusId = 2
            foreach (var seatVm in seatViewModels)
            {
                var existing = _context.ScheduleSeats
                    .FirstOrDefault(ss => ss.SeatId == seatVm.SeatId && ss.MovieShowId == model.BookingDetails.MovieShowId);
                if (existing != null)
                {
                    // SỬA: Set SeatStatusId = 2 cho trạng thái booked
                    existing.InvoiceId = invoice.InvoiceId;
                    existing.SeatStatusId = 2; // Set trạng thái booked
                    _context.ScheduleSeats.Update(existing);
                }
                else
                {
                    var scheduleSeat = new ScheduleSeat
                    {
                        InvoiceId = invoice.InvoiceId,
                        SeatId = seatVm.SeatId,
                        MovieShowId = model.BookingDetails.MovieShowId,
                        SeatStatusId = 2 // SỬA: Set trạng thái booked
                    };
                    _context.ScheduleSeats.Add(scheduleSeat);
                }
            }
            await _context.SaveChangesAsync();
            
            // SỬA: Thêm SignalR để thông báo cập nhật trạng thái ghế real-time
            if (_seatHubContext != null)
            {
                foreach (var seatVm in seatViewModels)
                {
                    if (seatVm.SeatId.HasValue)
                    {
                        await _seatHubContext.Clients.Group(model.MovieShowId.ToString()).SendAsync("SeatStatusChanged", seatVm.SeatId.Value, 2);
                    }
                }
            }
            
            // Do NOT release hold here. Hold should only be released on payment success, seat deselect, or timeout.
            // foreach (var seatVm in seatViewModels)
            // {
            //     MovieTheater.Hubs.SeatHub.ReleaseHold(model.BookingDetails.MovieShowId, seatVm.SeatId ?? 0);
            // }
            Console.WriteLine($"Created invoice with ID: {invoice.InvoiceId}");
            return new BookingResult { Success = true, InvoiceId = invoice.InvoiceId };
        }

        // Build the view model for the admin ticket booking confirmation page (GET)
        public async Task<ConfirmTicketAdminViewModel> BuildTicketBookingConfirmedViewModelAsync(string invoiceId)
        {
            try
            {
                if (string.IsNullOrEmpty(invoiceId))
                {
                    return null;
                }
                
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
            {
                return null;
            }
            var member = _context.Members.FirstOrDefault(m => m.AccountId == invoice.AccountId);
            var movieShow = invoice.MovieShow;
            var cinemaRoomName = movieShow.CinemaRoom?.CinemaRoomName ?? "N/A";
            var movieName = movieShow.Movie?.MovieNameEnglish ?? "N/A";
            var showDate = movieShow.ShowDate;
            var showTime = movieShow.Schedule?.ScheduleTime?.ToString() ?? "N/A";
            var versionName = movieShow.Version?.VersionName ?? "N/A";
            // Prepare seat details
            var seatIdArr = new List<int>();
            if (!string.IsNullOrEmpty(invoice.SeatIds))
            {
                try
                {
                    seatIdArr = invoice.SeatIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => int.Parse(id.Trim()))
                        .ToList();
                }
                catch (Exception ex)
                {
                    // Log error and return null
                    return null;
                }
            }
            var seats = new List<SeatDetailViewModel>();
            foreach (var seatId in seatIdArr)
            {
                var seat = _seatService.GetSeatById(seatId);
                if (seat == null) continue;
                var seatType = seat.SeatType;
                decimal originalPrice = seatType?.PricePercent ?? 0;
                // Apply version multiplier if available
                if (movieShow.Version != null)
                    originalPrice *= (decimal)movieShow.Version.Multi;
                int promotionDiscountPercent = 0;
                if (!string.IsNullOrEmpty(invoice.PromotionDiscount) && invoice.PromotionDiscount != "0")
                {
                    try
                    {
                        var promoObj = JsonConvert.DeserializeObject<dynamic>(invoice.PromotionDiscount);
                        promotionDiscountPercent = (int)(promoObj.seat ?? 0);
                    }
                    catch 
                    { 
                        promotionDiscountPercent = 0; 
                    }
                }
                decimal priceAfterPromotion = originalPrice;
                if (promotionDiscountPercent > 0)
                {
                    priceAfterPromotion = originalPrice * (1 - promotionDiscountPercent / 100m);
                }
                seats.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "N/A",
                    Price = priceAfterPromotion,
                    OriginalPrice = originalPrice,
                    PromotionDiscount = promotionDiscountPercent,
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
            var foodInvoices = _context.FoodInvoices.Where(f => f.InvoiceId == invoiceId).ToList();
            var foodIds = foodInvoices.Select(f => f.FoodId).ToList();
            var foods = await GetFoodsByIdsAsync(foodIds);
            var foodTuples = foodInvoices.Select(f => (f.FoodId, f.Quantity, f.Price)).ToList();
            var eligibleFoodPromotions = _promotionService.GetEligibleFoodPromotions(foodTuples);
            var foodDiscounts = _promotionService.ApplyFoodPromotionsToFoods(foodTuples, eligibleFoodPromotions);
            var selectedFoods = foodInvoices.Select(f => {
                var food = foods.FirstOrDefault(food => food.FoodId == f.FoodId);
                var originalPrice = food?.Price ?? f.Price; // Giá gốc từ bảng Food
                var discountedPrice = f.Price; // Giá sau giảm đã lưu trong FoodInvoice
                var discountLevel = originalPrice > 0 ? Math.Round((originalPrice - discountedPrice) / originalPrice * 100, 2) : 0;
                return new FoodViewModel
                {
                    FoodId = f.FoodId,
                    Name = food?.Name ?? "N/A",
                    Quantity = f.Quantity,
                    Price = discountedPrice,
                    OriginalPrice = originalPrice,
                    PromotionDiscount = discountLevel,
                    PromotionName = discountLevel > 0 ? "Promotion" : null
                };
            }).ToList();
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
            decimal totalFoodPrice = selectedFoods.Sum(f => f.Price * f.Quantity);
            decimal totalPrice = subtotal - rankDiscount - voucherAmount - usedScoreValue;
            if (totalPrice < 0) totalPrice = 0;
            decimal grandTotal = totalPrice + totalFoodPrice;
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
                TotalPrice = grandTotal,
                RankDiscountPercent = invoice.RankDiscountPercentage ?? 0,
                SelectedFoods = selectedFoods,
                TotalFoodPrice = totalFoodPrice
            };
            return viewModel;
            }
            catch (Exception ex)
            {
                // Log the exception and return null
                return null;
            }
        }
    }
} 