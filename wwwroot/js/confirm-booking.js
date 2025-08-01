$(document).ready(function () {
    // Get data from global variables set by the view
    var originalTotal = window.originalTotal || 0;
    var foodPrices = window.foodPrices || 0;
    var discountPercent = window.discountPercent || 0;
    var earningRate = window.earningRate || 0;
    var currentScore = window.currentScore || 0;
    var pointValue = 1000;
    let voucherModalInstance = null;
    let selectedVoucher = null;
    let selectedPromotion = null;

    // Voucher modal handlers
    $('#openVoucherModalBtn').click(function () {
        if (!voucherModalInstance) {
            voucherModalInstance = new bootstrap.Modal(document.getElementById('voucherModal'));
        }
        loadVouchers();
        voucherModalInstance.show();
    });

    $('#clearVoucherBtn').click(function () {
        selectedVoucher = null;
        $('#selectedVoucherInfo').html('');
        $('.voucher-card').removeClass('selected');
        updatePriceFlow();
        if (voucherModalInstance) voucherModalInstance.hide();
    });

    // Load vouchers function
    function loadVouchers() {
        $('#voucherListContainer').html('<div class="col-12 text-center"><div class="loading-spinner"></div><p class="mt-2">Loading vouchers...</p></div>');

        $.ajax({
            url: '/Voucher/GetAvailableVouchers',
            method: 'GET',
            success: function (data) {
                let html = '';
                if (data.length === 0) {
                    html = '<div class="col-12"><div class="empty-state"><i class="fas fa-ticket-alt"></i><p>No available vouchers found.</p></div></div>';
                } else {
                    data.forEach(v => {
                        html += `
                                <div class="col-md-6 mb-3">
                                  <div class="voucher-card" data-id="${v.id}" data-value="${v.value}">
                                    <div class="voucher-icon">
                                      <i class="fas fa-ticket-alt"></i>
                                    </div>
                                    <div class="voucher-code">${v.code}</div>
                                    <div class="voucher-value">${v.value.toLocaleString()} VND</div>
                                    <div class="voucher-expiry">
                                      <i class="fas fa-calendar-alt me-1"></i>
                                      Expires: ${v.expirationDate}
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
                    updatePriceFlow();
                    if (voucherModalInstance) voucherModalInstance.hide();
                });
            },
            error: function () {
                $('#voucherListContainer').html('<div class="col-12"><div class="empty-state"><i class="fas fa-exclamation-triangle"></i><p class="text-danger">Error loading vouchers. Please try again.</p></div></div>');
            }
        });
    }

    function renderSelectedVoucherInfo() {
        if (selectedVoucher) {
            $('#selectedVoucherInfo').html(`
                        <div class="selected-voucher">
                            <div class="d-flex align-items-center flex-grow-1">
                                <div class="voucher-icon-large me-3">
                                    <i class="fas fa-ticket-alt text-success" style="font-size: 2rem;"></i>
                                </div>
                                <div class="flex-grow-1">
                                    <div class="fw-semibold text-light mb-1" style="font-size: 1.1rem;">Selected Voucher</div>
                                    <div class="voucher-code-display mb-2" style="font-size: 1.2rem; font-weight: 600; color: var(--cinema-gold);">${selectedVoucher.id}</div>
                                    <div class="voucher-value-display" style="font-size: 1.3rem; font-weight: 700; color: var(--cinema-green);">${selectedVoucher.value.toLocaleString()} VND</div>
                                </div>
                            </div>
                            <div class="voucher-actions">
                                <button type="button" class="remove-voucher" title="Remove voucher">
                                    <i class="fas fa-times"></i>
                                </button>
                            </div>
                        </div>
                    `);

            $('.remove-voucher').off('click').on('click', function () {
                selectedVoucher = null;
                $('#selectedVoucherInfo').html('');
                $('.voucher-card').removeClass('selected');
                updatePriceFlow();
            });
        } else {
            $('#selectedVoucherInfo').html('');
        }
    }

    function updateMaxUsablePoints(discountedTotal) {
        var maxPoints = Math.floor((discountedTotal * 0.9) / pointValue);
        maxPoints = Math.min(maxPoints, currentScore);

        // Minimum points validation - must be at least 20 points
        var minPoints = 20;
        if (maxPoints < minPoints) {
            maxPoints = 0; // Cannot use points if max is less than minimum
        }

        $('#maxUsablePoints').text(maxPoints + ' pts');
        $('#useScore').attr('max', maxPoints);
        $('#useScore').attr('min', maxPoints > 0 ? minPoints : 0);
        return maxPoints;
    }

    function updatePriceFlow() {
        var originalTotal = window.originalTotal || 0;
        var subtotal = window.subtotal || 0;
        var foodPrices = window.foodPrices || 0;

        // Calculate discounts
        var rankDiscountPercent = window.discountPercent || 0;
        var rankDiscountAmount = Math.round(subtotal * (rankDiscountPercent / 100));
        var afterRank = subtotal - rankDiscountAmount;
        if (afterRank < 0) afterRank = 0;

        var promotionPercent = selectedPromotion ? selectedPromotion.discountPercent : 0;
        var promoDiscountAmount = Math.round(afterRank * (promotionPercent / 100));
        var afterPromo = afterRank - promoDiscountAmount;
        if (afterPromo < 0) afterPromo = 0;

        var voucherAmount = selectedVoucher ? selectedVoucher.value : 0;
        var afterVoucher = afterPromo - voucherAmount;
        if (afterVoucher < 0) afterVoucher = 0;

        var useScore = parseInt($('#useScore').val()) || 0;
        var maxUsablePoints = updateMaxUsablePoints(afterVoucher);
        var valid = true;

        // Validate minimum points requirement
        if (useScore > 0 && useScore < 20) {
            useScore = 0;
            $('#useScore').val(0);
            $('#pointValidation').html('<i class="fas fa-exclamation-triangle me-1"></i>Minimum points required: 20 points').show();
            valid = false;
        }

        if (useScore > maxUsablePoints) {
            useScore = maxUsablePoints;
            $('#useScore').val(useScore);
        }

        if (useScore < 0) useScore = 0;

        var pointsValue = useScore * pointValue;
        if (useScore > 0 && pointsValue > afterVoucher) {
            useScore = Math.floor(afterVoucher / pointValue);
            $('#useScore').val(useScore);
        }

        pointsValue = useScore * pointValue;
        var seatFinal = afterVoucher - pointsValue;
        if (seatFinal < 0) seatFinal = 0;

        var finalTotal = seatFinal + parseInt(foodPrices);

        // Update UI
        $('#originalTotal').text(Number(originalTotal).toLocaleString() + ' VND');
        $('#totalPriceDisplay').text(finalTotal.toLocaleString() + ' VND');

        // Show/hide discount rows
        if (rankDiscountPercent > 0) {
            $('#rankDiscountRow').show();
            $('#rankDiscountDisplay').text(`-${rankDiscountAmount.toLocaleString()} VND (${rankDiscountPercent}%)`);
        } else {
            $('#rankDiscountRow').hide();
        }

        if (voucherAmount > 0) {
            $('#voucherDiscountRow').show();
            $('#voucherDiscountDisplay').text(`-${voucherAmount.toLocaleString()} VND`);
        } else {
            $('#voucherDiscountRow').hide();
        }

        if (promotionPercent > 0) {
            $('#promotionDiscountRow').show();
            $('#promotionDiscountDisplay').text(`-${promoDiscountAmount.toLocaleString()} VND (${promotionPercent}%)`);
        } else {
            $('#promotionDiscountRow').hide();
        }

        if (useScore > 0 && valid) {
            $('#pointsUsedRow').show();
            $('#pointsUsedDisplay').text(`-${(useScore * pointValue).toLocaleString()} VND`);
        } else {
            $('#pointsUsedRow').hide();
        }

        // Points earning preview
        var pointsToEarn = 0;
        if (valid) {
            pointsToEarn = Math.round(seatFinal * earningRate / 100 / pointValue);
        }

        if (pointsToEarn > 0) {
            $('#pointsEarnedPreview').html(`<i class="fas fa-star text-warning me-1"></i>You will earn <strong>${pointsToEarn}</strong> point${pointsToEarn === 1 ? '' : 's'} from this transaction`).show();
        } else {
            $('#pointsEarnedPreview').hide();
        }

        // Savings display
        if (valid && useScore > 0) {
            var savings = useScore * pointValue;
            $('#pointSavings').html(`<i class="fas fa-piggy-bank me-1"></i>Using <strong>${useScore}</strong> points saves you <strong>${savings.toLocaleString()} VND</strong>`).show();
        } else {
            $('#pointSavings').hide();
        }

        // Update hidden fields
        $('#confirmBtn').prop('disabled', !valid);
        $('#hiddenUseScore').val(valid ? useScore : 0);
        $('#hiddenVoucherId').val(selectedVoucher ? selectedVoucher.id : '');
        $('#hiddenVoucherAmount').val(selectedVoucher ? selectedVoucher.value : 0);
        $('#hiddenPromotionId').val(selectedPromotion ? selectedPromotion.id : '');
    }

    $('#useScore').on('input', function () {
        var max = parseInt($(this).attr('max')) || 0;
        var min = parseInt($(this).attr('min')) || 0;
        var val = parseInt($(this).val()) || 0;

        // Validate minimum points (20 points)
        if (val > 0 && val < 20) {
            $('#pointValidation').html('<i class="fas fa-exclamation-triangle me-1"></i>Minimum points required: 20 points').show();
            $('#confirmBtn').prop('disabled', true);
            return;
        } else {
            $('#pointValidation').hide();
        }

        // Validate maximum points
        if (val > max) {
            $(this).val(max);
            val = max;
        }

        // Clear validation if value is valid
        if (val === 0 || (val >= 20 && val <= max)) {
            $('#pointValidation').hide();
            $('#confirmBtn').prop('disabled', false);
        }

        updatePriceFlow();
    });

    $('#confirmBtn').on('click', function (e) {
        $(this).html('<i class="fas fa-spinner fa-spin me-2"></i>Processing...').prop('disabled', true);
        updatePriceFlow();
    });

    // Initialize
    updatePriceFlow();
});

function testSuccess() {
    var useScoreInput = document.getElementById('useScore');
    var hiddenUseScore = document.getElementById('hiddenUseScore');
    var isTestSuccess = document.getElementById('isTestSuccess');

    if (useScoreInput && hiddenUseScore) {
        hiddenUseScore.value = useScoreInput.value || 0;
    }

    if (isTestSuccess) {
        isTestSuccess.value = "true";
    }

    document.querySelector('form[method="post"]').submit();
}