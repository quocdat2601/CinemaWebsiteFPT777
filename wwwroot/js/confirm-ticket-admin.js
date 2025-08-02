$(document).ready(function () {
    console.log('Document ready - initializing guest functionality');

    // Test guest section visibility
    $('input[name="customerType"]').change(function () {
        console.log('Customer type changed to:', $(this).val());
    });
    // Customer type selection
    $('input[name="customerType"]').change(function () {
        const customerType = $(this).val();
        if (customerType === 'member') {
            $('#memberSelectionSection').show();
            $('#guestInfoSection').hide();
            $('#guestName').prop('disabled', true);
            $('#guestPhone').prop('disabled', true);
            $('#memberDetails').hide();
            // Clear guest info
            $('#guestName').val('');
            $('#guestPhone').val('');
            $('#guestValidationMessage').hide();

            // SỬA: Reset rank discount khi chuyển sang Member (sẽ được set lại khi chọn member)
            currentDiscountPercent = 0;
            currentEarningRate = 0;
            $('#rankDiscountPercent').val(0);
            $('#discountInfo').html('Rank discount: 0% (-0 VND)');
        } else {
            $('#memberSelectionSection').hide();
            $('#guestInfoSection').show();
            $('#guestName').val('').prop('disabled', false);
            $('#guestPhone').val('').prop('disabled', false);
            $('#memberDetails').hide();
            // Clear member info
            $('#memberId').val('');
            $('#memberFullName').val('');
            $('#memberIdentityCard').val('');
            $('#memberPhoneNumber').val('');
            $('#memberScore').val('');
            $('#accountId').val('');

            // SỬA: Reset rank discount khi chuyển sang Guest
            currentDiscountPercent = 0;
            currentEarningRate = 0;
            $('#rankDiscountPercent').val(0);
            selectedAccountId = null;
            selectedVoucher = null;
            $('#selectedVoucherInfo').text('');
            $('#selectedVoucherId').val('');
            $('#voucherAmountHidden').val('0');
            $('#useScore').val(0);

            // Disable voucher button for guest
            $('#openVoucherModalBtn').prop('disabled', true);

            // Reset buttons for guest
            $('#confirmTicketAdminBtn').prop('disabled', true);
            $('#createQRCodeBtn').prop('disabled', true);

            // Update price calculation
            updatePriceAll();

            // SỬA: Ẩn rank discount info cho guest
            $('#discountInfo').html('');
        }
        updateConfirmButtonState();
    });

    // Open modal using Bootstrap 5 API
    let memberModalInstance = null;
    let selectedAccountId = null;
    $('#openMemberModalBtn').click(function () {
        if (!memberModalInstance) {
            memberModalInstance = new bootstrap.Modal(document.getElementById('memberModal'));
        }
        memberModalInstance.show();
        loadMembers();
    });

    // Load all members into the modal table
    function loadMembers() {
        $.getJSON('/Booking/GetAllMembers', function (members) {
            renderMemberTable(members);
            $('#memberSearch').off('input').on('input', function () {
                const keyword = $(this).val().toLowerCase().trim();
                const filtered = members.filter(m =>
                    (m.account.fullName && m.account.fullName.toLowerCase().includes(keyword)) ||
                    (m.account.email && m.account.email.toLowerCase().includes(keyword)) ||
                    (m.account.phoneNumber && m.account.phoneNumber.toLowerCase().includes(keyword)) ||
                    (m.account.identityCard && m.account.identityCard.toLowerCase().includes(keyword))
                );
                renderMemberTable(filtered);
            });
        });
    }

    function renderMemberTable(members) {
        const tbody = $('#memberTable tbody');
        tbody.empty();
        if (members.length === 0) {
            tbody.append('<tr><td colspan="6" class="text-center">No members found.</td></tr>');
            return;
        }
        members.forEach(m => {
            const row = `<tr>
                        <td>${m.memberId}</td>
                        <td>${m.account.fullName || ''}</td>
                        <td>${m.account.identityCard || ''}</td>
                        <td>${m.account.email || ''}</td>
                        <td>${m.account.phoneNumber || ''}</td>
                        <td><button type="button" class="btn btn-sm btn-success select-member-btn" data-member='${JSON.stringify(m)}'>Select</button></td>
                    </tr>`;
            tbody.append(row);
        });
        // Attach select event
        $('.select-member-btn').off('click').on('click', function () {
            const member = JSON.parse($(this).attr('data-member'));

            // Reload page với member đã chọn
            const request = {
                MovieShowId: window.movieShowId,
                SelectedSeatIds: window.selectedSeatIds,
                FoodIds: window.foodIds,
                FoodQtys: window.foodQtys,
                MemberId: member.memberId
            };

            // Cập nhật selectedAccountId trước khi reload
            selectedAccountId = member.account.accountId;

            $.ajax({
                url: window.reloadWithMemberUrl,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(request),
                success: function (response) {
                    if (response.success) {
                        window.location.href = response.redirectUrl;
                    } else {
                        alert('Error: ' + response.message);
                    }
                },
                error: function (xhr, status, error) {
                    console.error("AJAX Error:", error);
                    alert('Error reloading page with member');
                }
            });

            if (memberModalInstance) memberModalInstance.hide();
        });
    }

    let memberSelected = false;
    let guestSelected = false;
    function updateConfirmButtonState() {
        const customerType = $('input[name="customerType"]:checked').val();

        if (customerType === 'member') {
            // Check if a member is selected
            memberSelected = !!$('#memberId').val();
            // Validate score
            var used = parseInt($('#useScore').val()) || 0;
            var maxPoints = updateMaxUsablePoints();
            var validScore = true;
            if (used === 0) {
                validScore = true;
            } else if (used < 20) {
                validScore = false;
            } else if (used > maxPoints) {
                validScore = false;
            }
            // Enable only if member is selected and score is valid
            $('#confirmTicketAdminBtn').prop('disabled', !(memberSelected && validScore));
            $('#createQRCodeBtn').prop('disabled', true); // Disable QR for members
        } else {
            // Guest customer - KHÔNG cần validate tên/sđt
            $('#confirmTicketAdminBtn').prop('disabled', false);
            $('#createQRCodeBtn').prop('disabled', false);

    
            console.log('Guest validation:', {
                guestName: $('#guestName').val(),
                guestPhone: $('#guestPhone').val(),
                guestNameLength: $('#guestName').val().length,
                guestPhoneLength: $('#guestPhone').val().length,
                guestSelected: guestSelected
            });
        }
    }

    function fillMemberInfo(member) {
        $('#memberId').val(member.memberId);
        $('#memberFullName').val(member.account.fullName);
        $('#memberIdentityCard').val(member.account.identityCard);
        $('#memberPhoneNumber').val(member.account.phoneNumber);
        $('#memberScore').val(member.score);
        $('#accountId').val(member.account.accountId || '');
        selectedAccountId = member.account.accountId;

        // SỬA: Lấy rank discount từ API thay vì từ member object
        $.getJSON('/Booking/GetMemberDiscount', { memberId: member.memberId }, function (data) {
            $('#rankDiscountPercent').val(data.discountPercent || 0);
            currentDiscountPercent = data.discountPercent || 0;
            currentEarningRate = data.earningRate || 0;
            updatePriceAll();

            // SỬA: Hiển thị rank discount info khi chọn member
            var originalTotal = parseInt(window.originalTotal);
            if (data.discountPercent > 0) {
                $('#discountInfo').html(`Rank discount: ${data.discountPercent}% (-${Math.round(originalTotal * (data.discountPercent / 100)).toLocaleString()} VND)`);
            } else {
                $('#discountInfo').html('Rank discount: 0% (-0 VND)');
            }
        });

        $('#memberDetails').show();
        updateMaxUsablePoints();
        onMemberSelected(member.memberId);
        $('#openVoucherModalBtn').prop('disabled', false);
        // Reset voucher selection when member changes
        selectedVoucher = null;
        $('#selectedVoucherInfo').text('');
        $('#selectedVoucherId').val('');
        $('#voucherAmountHidden').val('0');
        $('.voucher-card').removeClass('selected');
        updatePriceAll();
        fetchEligiblePromotions(); // Call this function when member is selected
        updateConfirmButtonState(); // <-- update button state
    }

    // Confirm Ticket Admin Button Click
    $('#confirmTicketAdminBtn').click(function () {
        const customerType = $('input[name="customerType"]:checked').val();

        // SỬA: Tính toán giá cuối cùng từ JavaScript
        var originalTotal = parseInt(window.originalTotal);
        var totalFoodPrice = window.totalFoodPrice;
        var discountPercent = currentDiscountPercent || 0;
        var discountAmount = 0;

        // Tính rank discount
        if (customerType === 'member' && originalTotal > 0) {
            discountAmount = Math.round(originalTotal * (discountPercent / 100));
        }
        var afterRank = originalTotal - discountAmount;
        if (afterRank < 0) afterRank = 0;

        // Tính voucher amount
        var voucherAmount = selectedVoucher ? selectedVoucher.value : 0;
        var afterVoucher = afterRank - voucherAmount;
        if (afterVoucher < 0) afterVoucher = 0;

        // Tính điểm sử dụng
        var useScore = parseInt($('#useScore').val()) || 0;
        var pointValue = 1000;
        var savings = useScore * pointValue;
        var finalSeatPrice = afterVoucher - savings;
        if (finalSeatPrice < 0) finalSeatPrice = 0;

        // Tổng cuối cùng
        var finalTotal = finalSeatPrice + totalFoodPrice;

        var model = {
            BookingDetails: {
                MovieId: window.movieId,
                MovieName: window.movieName,
                CinemaRoomName: window.cinemaRoomName,
                ShowDate: window.showDate,
                ShowTime: window.showTime,
                SelectedSeats: window.selectedSeats,
                TotalPrice: finalTotal, // SỬA: Gửi giá cuối cùng
                PricePerTicket: window.pricePerTicket,
                MovieShowId: window.movieShowId,
                PromotionDiscountPercent: discountPercent, // SỬA: Gửi discount hiện tại
            },
            CustomerType: customerType,
            MemberIdInput: customerType === 'member' ? $('#memberInput').val() : '',
            MemberId: customerType === 'member' ? $('#memberId').val() : '',
            MemberFullName: customerType === 'member' ? $('#memberFullName').val() : $('#guestName').val(),
            MemberIdentityCard: customerType === 'member' ? $('#memberIdentityCard').val() : '',
            MemberPhoneNumber: customerType === 'member' ? $('#memberPhoneNumber').val() : $('#guestPhone').val(),
            MemberScore: customerType === 'member' ? (parseInt($('#memberScore').val()) || 0) : 0,
            UsedScore: customerType === 'member' ? (parseInt($('#useScore').val()) || 0) : 0,
            SelectedVoucherId: customerType === 'member' ? $('#selectedVoucherId').val() : '',
            VoucherAmount: customerType === 'member' ? (parseInt($('#voucherAmountHidden').val()) || 0) : 0,
            AccountId: customerType === 'member' ? $('#accountId').val() : '',
            RankDiscountPercent: customerType === 'member' ? (parseFloat($('#rankDiscountPercent').val()) || 0) : 0,
            MovieShowId: window.movieShowId,
            SelectedFoods: window.selectedFoods,
            TotalFoodPrice: window.totalFoodPrice
        };

        $.ajax({
            url: window.confirmTicketAdminUrl,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            success: function (response) {
                if (response.success) {
                    // Redirect to the backend-confirmed invoice page
                    window.location.href = response.redirectUrl;
                } else {
                    $('#memberMessage').text(response.message).show();
                }
            },
            error: function (xhr, status, error) {
                console.error("AJAX Error:", error);
                $('#memberMessage').text("An error occurred during booking confirmation.").show();
            }
        });
    });

    // Create QR Code button for Guest
    $('#createQRCodeBtn').click(function () {
        const customerType = $('input[name="customerType"]:checked').val();

        // SỬA: Tính toán giá cuối cùng từ JavaScript (tương tự như Confirm button)
        var originalTotal = parseInt(window.originalTotal);
        var totalFoodPrice = window.totalFoodPrice;
        var discountPercent = currentDiscountPercent || 0;
        var discountAmount = 0;

        // Tính rank discount
        if (customerType === 'member' && originalTotal > 0) {
            discountAmount = Math.round(originalTotal * (discountPercent / 100));
        }
        var afterRank = originalTotal - discountAmount;
        if (afterRank < 0) afterRank = 0;

        // Tính voucher amount
        var voucherAmount = selectedVoucher ? selectedVoucher.value : 0;
        var afterVoucher = afterRank - voucherAmount;
        if (afterVoucher < 0) afterVoucher = 0;

        // Tính điểm sử dụng
        var useScore = parseInt($('#useScore').val()) || 0;
        var pointValue = 1000;
        var savings = useScore * pointValue;
        var finalSeatPrice = afterVoucher - savings;
        if (finalSeatPrice < 0) finalSeatPrice = 0;

        // Tổng cuối cùng
        var finalTotal = finalSeatPrice + totalFoodPrice;

        var model = {
            BookingDetails: {
                MovieId: window.movieId,
                MovieName: window.movieName,
                CinemaRoomName: window.cinemaRoomName,
                ShowDate: window.showDate,
                ShowTime: window.showTime,
                SelectedSeats: window.selectedSeats,
                TotalPrice: finalTotal, // SỬA: Gửi giá cuối cùng
                PricePerTicket: window.pricePerTicket,
                MovieShowId: window.movieShowId,
                PromotionDiscountPercent: discountPercent, // SỬA: Gửi discount hiện tại
            },
            CustomerType: customerType,
            MemberIdInput: customerType === 'member' ? $('#memberInput').val() : '',
            MemberId: customerType === 'member' ? $('#memberId').val() : '',
            MemberFullName: customerType === 'member' ? $('#memberFullName').val() : $('#guestName').val(),
            MemberIdentityCard: customerType === 'member' ? $('#memberIdentityCard').val() : '',
            MemberPhoneNumber: customerType === 'member' ? $('#memberPhoneNumber').val() : $('#guestPhone').val(),
            MemberScore: customerType === 'member' ? (parseInt($('#memberScore').val()) || 0) : 0,
            UsedScore: customerType === 'member' ? (parseInt($('#useScore').val()) || 0) : 0,
            SelectedVoucherId: customerType === 'member' ? $('#selectedVoucherId').val() : '',
            VoucherAmount: customerType === 'member' ? (parseInt($('#voucherAmountHidden').val()) || 0) : 0,
            AccountId: customerType === 'member' ? $('#accountId').val() : '',
            RankDiscountPercent: customerType === 'member' ? (parseFloat($('#rankDiscountPercent').val()) || 0) : 0,
            MovieShowId: window.movieShowId,
            SelectedFoods: window.selectedFoods,
            TotalFoodPrice: window.totalFoodPrice
        };

        // Submit form để redirect đến QR code page
        var form = $('<form>', {
            'method': 'POST',
            'action': window.createQRCodeUrl
        });

        // Thêm input hidden cho model data
        form.append($('<input>', {
            'type': 'hidden',
            'name': 'modelData',
            'value': JSON.stringify(model)
        }));

        $('body').append(form);
        form.submit();
    });

    // Remove ticketsToConvert logic, add useScore logic
    function updateMaxUsablePoints() {
        // This should be calculated from backend, but for now, estimate:
        var totalPrice = window.originalTotal;
        var memberScore = parseInt($('#memberScore').val()) || 0;
        var maxPoints = Math.floor((totalPrice * 0.9) / 1000);
        maxPoints = Math.min(maxPoints, memberScore);
        $('#maxUsablePoints').text(maxPoints);
        return maxPoints;
    }
    $('#useScore').on('input', function () {
        updatePriceAll();
        updateConfirmButtonState(); // <-- update button state
    });
    // Set initial value
    $('#useScore').val(0);

    // Track current earning rate and discount percent
    var currentEarningRate = 0;
    var currentDiscountPercent = window.serverPromotionDiscountPercent || 0; // SỬA: Lấy từ model ban đầu
    let selectedVoucher = null;
    let selectedPromotion = null;
    function updatePriceAll() {
        var originalTotal = parseInt(window.originalTotal);
        // Áp dụng rank benefit trước
        var totalFoodPrice = window.totalFoodPrice;

        console.log('Original Total:', originalTotal);
        console.log('Total Food Price:', totalFoodPrice);

        // Áp dụng rank benefit trước (chỉ cho seat, không cho food)
        var discountPercent = currentDiscountPercent || 0;
        var discountAmount = 0;

        // SỬA: Chỉ tính rank discount cho member
        var customerType = $('input[name="customerType"]:checked').val();
        if (customerType === 'member' && originalTotal > 0) {
            discountAmount = Math.round(originalTotal * (discountPercent / 100));
        }
        var afterRank = originalTotal - discountAmount;
        if (afterRank < 0) afterRank = 0;

        console.log('Rank Discount Percent:', discountPercent);
        console.log('Rank Discount Amount:', discountAmount);
        console.log('After Rank Discount:', afterRank);

        // SỬA: Không áp dụng promotion discount thêm lần nữa vì đã được áp dụng từ server
        // var promotionDiscountPercent = selectedPromotion ? selectedPromotion.value : 0;
        // var promotionDiscountAmount = 0;
        // if (afterRank > 0) {
        //     promotionDiscountAmount = Math.round(afterRank * (promotionDiscountPercent / 100));
        // }
        // var afterPromotion = afterRank - promotionDiscountAmount;
        // if (afterPromotion < 0) afterPromotion = 0;
        var afterPromotion = afterRank; // SỬA: Bỏ qua promotion discount vì đã được áp dụng

        // Sau đó mới áp dụng voucher (chỉ cho seat, không cho food)
        var voucherAmount = selectedVoucher ? selectedVoucher.value : 0;
        var afterVoucher = afterPromotion - voucherAmount;
        if (afterVoucher < 0) afterVoucher = 0;

        console.log('Voucher Amount:', voucherAmount);
        console.log('After Voucher:', afterVoucher);
        console.log('Selected Voucher:', selectedVoucher);

        // Điểm (chỉ cho seat, không cho food)
        var useScore = parseInt($('#useScore').val()) || 0;
        var pointValue = 1000;
        var savings = useScore * pointValue;
        var finalSeatPrice = afterVoucher - savings;
        if (finalSeatPrice < 0) finalSeatPrice = 0;

        console.log('Use Score:', useScore);
        console.log('Savings:', savings);
        console.log('Final Seat Price:', finalSeatPrice);

        // Tổng cuối cùng = seat price + food price
        var finalTotal = finalSeatPrice + totalFoodPrice;

        console.log('Final Total:', finalTotal);
        console.log('Expected calculation:');
        console.log('  Original Total:', originalTotal);
        console.log('  Rank Discount:', discountAmount);
        console.log('  After Rank:', afterRank);
        console.log('  Voucher Amount:', voucherAmount);
        console.log('  After Voucher:', afterVoucher);
        console.log('  Use Score Value:', savings);
        console.log('  Final Seat Price:', finalSeatPrice);
        console.log('  Food Price:', totalFoodPrice);
        console.log('  Final Total:', finalTotal);

        // Update hiển thị giá
        var seatOnlyTotal = originalTotal;
        if (voucherAmount > 0 || discountAmount > 0 || useScore > 0) { // SỬA: Bỏ promotionDiscountAmount
            $('#originalTotal').css({ 'text-decoration': 'line-through', 'color': '#888', 'font-size': '1.1em' }).text(originalTotal.toLocaleString() + ' VND');
            $('#discountedTotal').show().css({ 'font-size': '1.3em', 'font-weight': 'bold', 'color': '#1a7f37' }).text(finalSeatPrice.toLocaleString() + ' VND');
            seatOnlyTotal = finalSeatPrice;
        } else {
            $('#originalTotal').css({ 'text-decoration': 'none', 'color': '#1a7f37', 'font-size': '1.3em' }).text(originalTotal.toLocaleString() + ' VND');
            $('#discountedTotal').hide();
        }

        // Hiển thị tổng cuối cùng bao gồm food
        if (totalFoodPrice > 0) {
            var totalDisplay = $('#discountedTotal').is(':visible') ?
                $('#discountedTotal').text() + ' + ' + totalFoodPrice.toLocaleString() + ' VND (Food) = ' + finalTotal.toLocaleString() + ' VND' :
                originalTotal.toLocaleString() + ' VND + ' + totalFoodPrice.toLocaleString() + ' VND (Food) = ' + finalTotal.toLocaleString() + ' VND';

            if (!$('#totalWithFood').length) {
                $('#discountedTotal').after('<div id="totalWithFood" style="font-size: 1.1em; font-weight: bold; color: #007bff; margin-top: 0.5rem;"></div>');
            }
            $('#totalWithFood').text('Total: ' + finalTotal.toLocaleString() + ' VND');
        } else {
            $('#totalWithFood').remove();
        }

        // Hiển thị voucher
        let discountInfoHtml = '';
        // Hiển thị rank discount - chỉ hiển thị cho member
        var customerType = $('input[name="customerType"]:checked').val();
        if (customerType === 'member' && discountPercent > 0 && originalTotal > 0) {
            discountInfoHtml += `Rank discount: ${discountPercent}% (-${discountAmount.toLocaleString()} VND)`;
        } else {
            // SỬA: Ẩn rank discount info cho guest
            if (customerType === 'guest') {
                discountInfoHtml += ''; // Không hiển thị rank discount cho guest
            } else {
                discountInfoHtml += 'Rank discount: 0% (-0 VND)';
            }
        }

        if (voucherAmount > 0) {
            discountInfoHtml += `<div class='text-success'>Voucher: -${voucherAmount.toLocaleString()} VND</div>`;
        }
        $('#discountInfo').html(discountInfoHtml);

        // Savings (chỉ khi dùng điểm)
        if (useScore > 0) {
            $('#pointSavings').text(`Using ${useScore} points saves you ${savings.toLocaleString()} VND`);
        } else {
            $('#pointSavings').text('');
        }

        // Tính điểm thưởng (chỉ dựa trên seat price, không bao gồm food)
        var earningRate = currentEarningRate || 0;
        var pointsToEarn = Math.round((finalSeatPrice) * earningRate / 100 / pointValue);
        $('#pointsEarnedPreview').text(`You will earn ${pointsToEarn} points from this transaction`).addClass('text-success');

        // Validate điểm (chỉ dựa trên seat price, không bao gồm food)
        var memberScore = parseInt($('#memberScore').val()) || 0;
        var maxUsablePoints = Math.floor((afterVoucher * 0.9) / pointValue);
        maxUsablePoints = Math.min(maxUsablePoints, memberScore);
        $('#maxUsablePoints').text(maxUsablePoints);
        var validationMsg = '';
        var valid = true;
        if (useScore === 0) {
            validationMsg = '';
            valid = true;
        } else if (useScore < 20) { validationMsg = 'Minimum 20 points required.'; valid = false; }
        else if (useScore > maxUsablePoints) { validationMsg = `You can use up to ${maxUsablePoints} points for this order.`; valid = false; }
        $('#pointValidation').text(validationMsg);
        // At the end of updatePriceAll, set the hidden input value:
        $('#rankDiscountPercent').val(discountPercent); // SỬA: Set rank discount percent thay vì promotion
    }

    function onMemberSelected(memberId) {
        $.getJSON('/Booking/GetMemberDiscount', { memberId: memberId }, function (data) {
            currentDiscountPercent = data.discountPercent || 0;
            currentEarningRate = data.earningRate || 0;
            updatePriceAll();
        });
    }
    // Guest info validation
    $('#guestName, #guestPhone').on('input', function () {
        console.log('Guest input changed:', $(this).attr('id'), 'value:', $(this).val());
        updateConfirmButtonState();
        validateGuestInfo();
    });

    // Also trigger on blur to ensure validation
    $('#guestName, #guestPhone').on('blur', function () {
        updateConfirmButtonState();
        validateGuestInfo();
    });

    function validateGuestInfo() {
        var guestName = $('#guestName').val().trim();
        var guestPhone = $('#guestPhone').val().trim();
        var message = '';

        if (guestName.length === 0 && guestPhone.length === 0) {
            message = '';
        } else if (guestName.length === 0) {
            message = 'Please enter guest name.';
        } else if (guestPhone.length === 0) {
            message = 'Please enter phone number.';
        } else if (guestPhone.length < 10) {
            message = 'Phone number must be at least 10 digits.';
        }

        if (message.length > 0) {
            $('#guestValidationMessage').text(message).show();
        } else {
            $('#guestValidationMessage').hide();
        }
    }

    // On page load, show initial state
    updatePriceAll();
    updateConfirmButtonState(); // <-- update button state

    // Nếu đã có member được chọn
    if ($('#memberId').val()) {
        // Cập nhật selectedAccountId
        selectedAccountId = $('#accountId').val();
        // Enable nút chọn voucher
        $('#openVoucherModalBtn').prop('disabled', false);
        // SỬA: Cập nhật rank discount từ server nếu đã có member
        var memberId = $('#memberId').val();
        if (memberId) {
            $.getJSON('/Booking/GetMemberDiscount', { memberId: memberId }, function (data) {
                $('#rankDiscountPercent').val(data.discountPercent || 0);
                currentDiscountPercent = data.discountPercent || 0;
                currentEarningRate = data.earningRate || 0;
                updatePriceAll();
            });
        }
        // Fetch promotions
        fetchEligiblePromotions();
    } else {
        // SỬA: Reset rank discount nếu không có member (Guest)
        currentDiscountPercent = 0;
        currentEarningRate = 0;
        $('#rankDiscountPercent').val(0);
        $('#openVoucherModalBtn').prop('disabled', true);
        updatePriceAll();
    }

    // VOUCHER PICKER LOGIC
    let voucherModalInstance = null;
    $('#openVoucherModalBtn').click(function () {
        if (!selectedAccountId) {
            alert('Please select a member first!');
            return;
        }
        if (!voucherModalInstance) {
            voucherModalInstance = new bootstrap.Modal(document.getElementById('voucherModal'));
        }
        loadVouchers(selectedAccountId);
        voucherModalInstance.show();
    });
    $('#clearVoucherBtn').click(function () {
        selectedVoucher = null;
        $('#selectedVoucherInfo').text('');
        $('#selectedVoucherId').val('');
        $('#voucherAmountHidden').val('0');
        $('.voucher-card').removeClass('selected');
        updatePriceAll();
        if (voucherModalInstance) voucherModalInstance.hide();
    });
    function loadVouchers(accountId) {
        $.ajax({
            url: '/Voucher/GetAvailableVouchers?accountId=' + encodeURIComponent(accountId),
            method: 'GET',
            success: function (data) {
                let html = '';
                if (data.length === 0) {
                    html = '<div class="col-12 text-center"><p class="text-muted">No available vouchers found.</p></div>';
                } else {
                    data.forEach(v => {
                        html += `
                                <div class="col-md-4 mb-3">
                                  <div class="card voucher-card d-flex flex-column align-items-center" data-id="${v.id}" data-value="${v.value}">
                                    <img src="${v.image ? v.image : '/images/vouchers/voucher.jpg'}" class="voucher-img-top mt-3 mb-2" alt="Voucher Image" style="width: 80px; height: 80px; object-fit: cover; border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,0.08);">
                                    <div class="card-body text-center p-2">
                                      <h5 class="card-title mb-1" style="color:#007bff;font-weight:600;">${v.code}</h5>
                                      <p class="card-text mb-1">Value: <span style="font-weight:600;">${v.value.toLocaleString()} VND</span></p>
                                      <p class="card-text mb-1">Expires: <span style="font-size:0.95em;">${v.expirationDate}</span></p>
                                    </div>
                                  </div>
                                </div>`;
                    });
                }
                $('#voucherListContainer').html(html);
                $('.voucher-card').click(function () {
                    $('.voucher-card').removeClass('selected');
                    $(this).addClass('selected');
                    selectedVoucher = {
                        id: $(this).data('id'),
                        value: $(this).data('value')
                    };
                    renderSelectedVoucherInfo();
                    $('#selectedVoucherId').val(selectedVoucher.id);
                    $('#voucherAmountHidden').val(selectedVoucher.value);
                    updatePriceAll();
                    if (voucherModalInstance) voucherModalInstance.hide();
                });
            },
            error: function () {
                $('#voucherListContainer').html('<div class="col-12 text-center"><p class="text-danger">Error loading vouchers.</p></div>');
            }
        });
    }

    // Update price and point info when points are entered or member is selected
    // This function is now called by updateConfirmButtonState

    function renderSelectedVoucherInfo() {
        if (selectedVoucher) {
            $('#selectedVoucherInfo').html(`
                        <span class='fw-bold'>Selected Voucher:</span> 
                        <span class='badge bg-primary text-light'>${selectedVoucher.id}</span> - 
                        <span class='fw-bold text-success'>${selectedVoucher.value.toLocaleString()} VND</span>
                        <button type="button" id="removeVoucherBtn" class="remove-voucher-btn" title="Remove voucher">
                            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
                        </button>
                    `);
            $('#removeVoucherBtn').off('click').on('click', function () {
                selectedVoucher = null;
                $('#selectedVoucherInfo').text('');
                $('#selectedVoucherId').val('');
                $('#voucherAmountHidden').val('0');
                $('.voucher-card').removeClass('selected');
                updatePriceAll();
            });
        } else {
            $('#selectedVoucherInfo').text('');
        }
    }

    // Thêm hàm fetchEligiblePromotions và renderPromotionList vào section Scripts
    // Gọi fetchEligiblePromotions mỗi khi chọn member, ghế, ngày chiếu
    function fetchEligiblePromotions() {
        var model = {
            MovieId: window.movieId,
            ShowDate: window.showDate,
            ShowTime: window.showTime,
            MemberId: $('#memberId').val(),
            AccountId: $('#accountId').val(),
            SelectedSeatIds: window.selectedSeatIds
        };

        $.ajax({
            url: window.getEligiblePromotionsUrl,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            success: function (response) {
                if (response.success) {
                    renderPromotionList(response.eligiblePromotions);
                } else {
                    $('#promotionListContainer').html('<div class="col-12 text-center"><p class="text-muted">No eligible promotions found.</p></div>');
                }
            },
            error: function (xhr, status, error) {
                console.error("AJAX Error fetching promotions:", error);
                $('#promotionListContainer').html('<div class="col-12 text-center"><p class="text-danger">Error fetching promotions.</p></div>');
            }
        });
    }

    function renderPromotionList(promotions) {
        if (promotions.length > 0) {
            // Tự động chọn promotion có discount cao nhất
            const bestPromotion = promotions.reduce((best, current) =>
                (current.value > best.value) ? current : best
            );

            selectedPromotion = {
                id: bestPromotion.id,
                name: bestPromotion.name,
                type: bestPromotion.type,
                value: bestPromotion.value
            };
            $('#selectedPromotionId').val(selectedPromotion.id);
            updatePriceAll();
        }
    }
});