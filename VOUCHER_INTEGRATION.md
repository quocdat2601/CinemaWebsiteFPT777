# Voucher Integration for Booking System

## Overview
This document describes the voucher integration feature that allows users to apply vouchers during the ticket booking process.

## Features Implemented

### 1. Voucher Selection Modal
- Users can click "Select Voucher" button to open a modal showing their available vouchers
- Vouchers are displayed as cards with code, value, and expiry date
- Users can select a voucher by clicking on it
- Clear voucher option to remove selected voucher

### 2. Price Calculation Flow
The system follows this order for price calculation:
1. **Original Price** - Sum of all selected seat prices
2. **Voucher Discount** - Applied first (like cash)
3. **Promotion Discount** - Percentage-based (placeholder for future)
4. **Rank Discount** - Percentage-based member benefit
5. **Points Usage** - Convert points to VND (1 point = 1,000 VND)
6. **Final Price** - Used for point earning calculation

### 3. Voucher Management
- Vouchers are automatically updated when used
- Remaining value is deducted from voucher
- Voucher is marked as used when fully consumed
- Only available vouchers (not expired, not used, belonging to user) are shown

## API Endpoints

### Get Available Vouchers
```
GET /Voucher/GetAvailableVouchers
Authorization: Required
Returns: JSON array of available vouchers for current user
```

### Voucher Properties
```json
{
  "id": "VC001",
  "code": "TEST50K", 
  "remainingValue": 50000,
  "expirationDate": "2024-02-15",
  "image": "/images/vouchers/voucher.jpg"
}
```

## Database Schema

### Voucher Table
- `Voucher_ID` (PK) - Unique voucher identifier
- `Account_ID` (FK) - Owner of the voucher
- `Code` - Voucher code/name
- `Value` - Original voucher value
- `RemainingValue` - Current available value
- `CreatedDate` - When voucher was created
- `ExpiryDate` - When voucher expires
- `IsUsed` - Whether voucher is fully used
- `Image` - Voucher image path

## Frontend Implementation

### ConfirmBooking.cshtml
- Added voucher selection button
- Modal for voucher selection
- Updated price breakdown display
- Real-time price calculation with voucher

### JavaScript Functions
- `loadVouchers()` - Loads available vouchers via AJAX
- `updatePriceFlow()` - Calculates final price with all discounts
- Voucher selection and clearing functionality

## Backend Implementation

### BookingController.cs
- Updated `Confirm` method to handle voucher processing
- Added voucher validation and update logic
- Integrated voucher into price calculation flow

### VoucherController.cs
- Added `GetAvailableVouchers` endpoint
- Returns only valid vouchers for current user

### Services
- `IVoucherService` and `VoucherService` - Voucher business logic
- `IVoucherRepository` and `VoucherRepository` - Data access

## CSS Styling

### Voucher Modal
- Responsive card layout
- Hover effects and selection states
- Clean, modern design

### Price Breakdown
- Clear display of all discount types
- Color-coded information
- Responsive layout

## Testing

### Creating Test Vouchers
Run the following SQL (replace 'AC001' with actual AccountId):
```sql
INSERT INTO Voucher (Voucher_ID, Account_ID, Code, Value, RemainingValue, CreatedDate, ExpiryDate, IsUsed, Image)
VALUES 
('VC001', 'AC001', 'TEST50K', 50000, 50000, GETDATE(), DATEADD(day, 30, GETDATE()), 0, '/images/vouchers/voucher.jpg'),
('VC002', 'AC001', 'TEST100K', 100000, 100000, GETDATE(), DATEADD(day, 30, GETDATE()), 0, '/images/vouchers/voucher.jpg');
```

### Testing Flow
1. Login with user account that has vouchers
2. Go to ticket booking
3. Select seats and proceed to confirmation
4. Click "Select Voucher" button
5. Choose a voucher from the modal
6. Verify price calculation updates correctly
7. Complete booking and verify voucher is updated

## Future Enhancements

### Promotion Integration
- Add promotion selection similar to voucher
- Implement promotion percentage calculation
- Update price flow to include promotions

### Voucher Types
- Different voucher types (percentage vs fixed amount)
- Conditional vouchers (minimum purchase requirements)
- Stackable vs non-stackable vouchers

### Admin Features
- Bulk voucher creation
- Voucher usage analytics
- Voucher expiration management

## Security Considerations

### Validation
- Voucher ownership verification
- Expiry date checking
- Usage status validation
- Price tampering prevention

### Data Integrity
- Transaction-based voucher updates
- Rollback on booking failure
- Audit trail for voucher usage

## Error Handling

### Common Scenarios
- No available vouchers
- Voucher expired during booking
- Insufficient voucher value
- Network errors during voucher loading

### User Feedback
- Clear error messages
- Loading states
- Success confirmations
- Price breakdown visibility 