# Voucher Functionality Test Guide

## Prerequisites
1. Ensure you have a user account with vouchers
2. Make sure the database has test vouchers (see SQLQuery.sql)
3. Verify all dependencies are properly registered in Program.cs

## Test Cases

### Test Case 1: Basic Voucher Selection
**Objective**: Verify voucher modal opens and displays available vouchers

**Steps**:
1. Login to the application
2. Navigate to ticket booking
3. Select seats and proceed to confirmation page
4. Click "Select Voucher" button
5. Verify modal opens with voucher cards
6. Check that vouchers show correct information (code, value, expiry)

**Expected Result**: Modal opens, vouchers are displayed correctly

### Test Case 2: Voucher Selection and Price Calculation
**Objective**: Verify voucher selection updates price calculation

**Steps**:
1. Follow steps 1-4 from Test Case 1
2. Click on a voucher card
3. Verify modal closes
4. Check that selected voucher info appears
5. Verify price breakdown shows voucher discount
6. Confirm final price is reduced by voucher amount

**Expected Result**: Price calculation updates correctly with voucher discount

### Test Case 3: Price Calculation Flow
**Objective**: Verify correct order of price calculations

**Steps**:
1. Select a voucher with value less than total price
2. Enter some points to use
3. Verify price breakdown shows:
   - Original Price
   - Voucher discount (applied first)
   - Rank discount (applied after voucher)
   - Points used (applied last)
   - Final price

**Expected Result**: Price calculations follow the correct order

### Test Case 4: Voucher Clearing
**Objective**: Verify voucher can be cleared

**Steps**:
1. Select a voucher
2. Click "Clear Voucher" button
3. Verify voucher selection is removed
4. Check that price calculation reverts to original

**Expected Result**: Voucher is cleared and price reverts

### Test Case 5: Booking with Voucher
**Objective**: Verify voucher is properly applied during booking

**Steps**:
1. Select a voucher
2. Complete the booking process
3. Check that voucher remaining value is reduced
4. Verify voucher is marked as used if fully consumed

**Expected Result**: Voucher is properly updated after booking

### Test Case 6: No Available Vouchers
**Objective**: Verify handling when user has no vouchers

**Steps**:
1. Login with account that has no vouchers
2. Go to booking confirmation
3. Click "Select Voucher"
4. Verify appropriate message is shown

**Expected Result**: "No available vouchers found" message is displayed

### Test Case 7: Expired Voucher
**Objective**: Verify expired vouchers are not shown

**Steps**:
1. Create a voucher with past expiry date
2. Login and go to booking
3. Click "Select Voucher"
4. Verify expired voucher is not shown

**Expected Result**: Expired vouchers are filtered out

### Test Case 8: Voucher Value Exceeds Total Price
**Objective**: Verify voucher is limited to total price

**Steps**:
1. Select a voucher with value higher than total price
2. Verify voucher discount equals total price
3. Confirm final price is 0

**Expected Result**: Voucher is capped at total price amount

## API Testing

### Test GetAvailableVouchers Endpoint
```bash
GET /Voucher/GetAvailableVouchers
Headers: 
  Authorization: Bearer <token>
  Cookie: .AspNetCore.Identity.Application=<session_cookie>
```

**Expected Response**:
```json
[
  {
    "id": "VC001",
    "code": "TEST50K",
    "remainingValue": 50000,
    "expirationDate": "2024-02-15",
    "image": "/images/vouchers/voucher.jpg"
  }
]
```

## Database Verification

### Check Voucher Updates
After booking with voucher, verify:
```sql
SELECT Voucher_ID, RemainingValue, IsUsed 
FROM Voucher 
WHERE Voucher_ID = 'VC001'
```

### Check Invoice Creation
Verify invoice is created with correct final price:
```sql
SELECT InvoiceId, TotalMoney, UseScore, AddScore 
FROM Invoice 
WHERE AccountId = 'AC001' 
ORDER BY BookingDate DESC
```

## Common Issues and Solutions

### Issue 1: Voucher Modal Not Opening
**Cause**: Bootstrap modal not initialized
**Solution**: Check if Bootstrap JS is loaded properly

### Issue 2: Vouchers Not Loading
**Cause**: API endpoint not accessible
**Solution**: Verify authentication and authorization

### Issue 3: Price Calculation Incorrect
**Cause**: JavaScript calculation error
**Solution**: Check browser console for errors

### Issue 4: Voucher Not Updated After Booking
**Cause**: Database transaction issue
**Solution**: Verify voucher service is properly injected

## Performance Testing

### Load Testing
- Test with multiple users selecting vouchers simultaneously
- Verify modal performance with many vouchers
- Check API response time under load

### Memory Testing
- Monitor memory usage during voucher operations
- Check for memory leaks in JavaScript

## Security Testing

### Authorization Testing
- Try to access voucher API without authentication
- Verify users can only see their own vouchers
- Test voucher ownership validation

### Input Validation
- Test with malformed voucher IDs
- Verify SQL injection protection
- Check XSS protection in voucher display

## Browser Compatibility

### Tested Browsers
- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

### Mobile Testing
- Test voucher modal on mobile devices
- Verify responsive design
- Check touch interactions 