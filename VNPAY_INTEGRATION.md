# VNPay Payment Integration Guide

## Overview
The project has been integrated with VNPay payment gateway to handle movie ticket payments after users confirm their bookings.

## Configuration

### 1. VNPay Configuration in appsettings.json
```json
{
  "VNPay": {
    "TmnCode": "VVHLKKC6",
    "HashSecret": "E3N6K0GJZEDZKTV7LHZ0DUMZRSGV3XZS",
    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "Command": "pay",
    "CurrCode": "VND",
    "Version": "2.1.0",
    "Locale": "vn",
    "ReturnUrl": "https://localhost:7001/api/Payment/vnpay-return",
    "IpnUrl": "https://localhost:7001/api/Payment/vnpay-ipn",
    "ExpiredTime": 15
  }
}
```

### 2. Configuration Parameters
- **TmnCode**: Website code at VNPAY
- **HashSecret**: Secret string to create signature
- **BaseUrl**: VNPay payment gateway URL (sandbox for testing)
- **ReturnUrl**: Callback URL when payment is completed
- **IpnUrl**: URL to receive notifications from VNPay (server-to-server)
- **ExpiredTime**: Payment expiration time (minutes)

## Workflow

### 1. Booking
1. User selects movie, date, time and seats
2. Confirms booking information
3. System creates invoice with "Payment Pending" status (Status = 1)
4. Redirects to Payment page

### 2. Payment
1. Payment page displays booking information and "Pay Now" button
2. Click payment button → redirects to VNPay
3. User makes payment on VNPay
4. VNPay calls back to ReturnUrl or IpnUrl

### 3. Callback Processing
1. **ReturnUrl** (client-side): Displays result to user
2. **IpnUrl** (server-side): Updates invoice status
3. If payment is successful:
   - Updates invoice status to "Payment Successful" (Status = 2)
   - Adds points for customer
   - Redirects to Success page

## Files Created/Updated

### Controllers
- `BookingController.cs`: Added VNPayService, modified Confirm action, added Payment action
- `PaymentController.cs`: Handles VNPay callbacks

### Models
- `VNPayConfig.cs`: VNPay configuration model
- `PaymentViewModel.cs`: ViewModel for Payment page

### Services
- `VNPayService.cs`: Creates payment URL and validates signature
- `InvoiceService.cs`: Added status update methods
- `AccountService.cs`: Added point addition methods

### Repository
- `InvoiceRepository.cs`: Added status update methods
- `AccountRepository.cs`: Added point addition methods

### Views
- `Payment.cshtml`: Page to display payment information
- `Success.cshtml`: Updated to display ticket information

### CSS
- `payment.css`: Styles for Payment page

## Invoice Status
- **Status = 1**: Payment Pending
- **Status = 2**: Payment Successful
- **Status = 3**: Payment Failed

## Important Notes

### 1. Test Environment
- Use VNPay sandbox for testing
- Callback URLs must be HTTPS or localhost
- Can test with VNPay test cards

### 2. Security
- HashSecret must be kept secure
- Validate signature from VNPay
- Check ResponseCode = "00" to confirm success

### 3. Error Handling
- Display clear error messages to users
- Log errors for debugging
- Have retry mechanism when needed

### 4. Production
- Change BaseUrl to production
- Update ReturnUrl and IpnUrl
- Use real TmnCode and HashSecret
- Configure SSL certificate

## Testing
1. Run the application
2. Login with Member account
3. Book a movie ticket
4. Confirm booking
5. Go to Payment page
6. Click "Pay Now"
7. Make payment on VNPay (can use test cards)
8. Check callback and status update

## Troubleshooting
- Check logs for debugging
- Verify VNPay signature
- Check callback URL configuration
- Ensure database connection
- Check file permissions 