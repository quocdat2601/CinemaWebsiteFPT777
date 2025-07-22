-- Script tạo invoice test cho Booking Management
-- Chạy script này để có dữ liệu test

-- Thêm invoice test cho Member (chỉ thêm nếu chưa tồn tại)
IF NOT EXISTS (SELECT 1 FROM Invoice WHERE Invoice_ID = 'INV006')
BEGIN
    INSERT INTO Invoice (Invoice_ID, Account_ID, Add_Score, BookingDate, Status, Total_Money, Use_Score, Seat, Seat_IDs, Movie_Show_Id, Promotion_Discount, Voucher_ID, RankDiscountPercentage, Cancel, CancelDate, CancelBy)
    VALUES ('INV006', 'AC001', 100, GETDATE(), 1, 64000, 0, 'A1, A2', '1,2', 1, '0', NULL, 0, 0, NULL, NULL);
END

IF NOT EXISTS (SELECT 1 FROM Invoice WHERE Invoice_ID = 'INV007')
BEGIN
    INSERT INTO Invoice (Invoice_ID, Account_ID, Add_Score, BookingDate, Status, Total_Money, Use_Score, Seat, Seat_IDs, Movie_Show_Id, Promotion_Discount, Voucher_ID, RankDiscountPercentage, Cancel, CancelDate, CancelBy)
    VALUES ('INV007', 'AC002', 150, GETDATE(), 1, 96000, 0, 'B3, B4, B5', '3,4,5', 1, '0', NULL, 0, 0, NULL, NULL);
END

IF NOT EXISTS (SELECT 1 FROM Invoice WHERE Invoice_ID = 'INV008')
BEGIN
    INSERT INTO Invoice (Invoice_ID, Account_ID, Add_Score, BookingDate, Status, Total_Money, Use_Score, Seat, Seat_IDs, Movie_Show_Id, Promotion_Discount, Voucher_ID, RankDiscountPercentage, Cancel, CancelDate, CancelBy)
    VALUES ('INV008', 'AC003', 80, GETDATE(), 1, 32000, 0, 'C1', '6', 1, '0', NULL, 0, 0, NULL, NULL);
END

IF NOT EXISTS (SELECT 1 FROM Invoice WHERE Invoice_ID = 'INV009')
BEGIN
    INSERT INTO Invoice (Invoice_ID, Account_ID, Add_Score, BookingDate, Status, Total_Money, Use_Score, Seat, Seat_IDs, Movie_Show_Id, Promotion_Discount, Voucher_ID, RankDiscountPercentage, Cancel, CancelDate, CancelBy)
    VALUES ('INV009', 'AC004', 200, GETDATE(), 1, 128000, 0, 'D1, D2, D3, D4', '7,8,9,10', 1, '0', NULL, 0, 0, NULL, NULL);
END

IF NOT EXISTS (SELECT 1 FROM Invoice WHERE Invoice_ID = 'INV010')
BEGIN
    INSERT INTO Invoice (Invoice_ID, Account_ID, Add_Score, BookingDate, Status, Total_Money, Use_Score, Seat, Seat_IDs, Movie_Show_Id, Promotion_Discount, Voucher_ID, RankDiscountPercentage, Cancel, CancelDate, CancelBy)
    VALUES ('INV010', 'AC005', 120, GETDATE(), 1, 48000, 0, 'E1, E2', '11,12', 1, '0', NULL, 0, 0, NULL, NULL);
END

-- Thêm invoice test cho Guest (QR Payment) sử dụng ID ngắn hơn
IF NOT EXISTS (SELECT 1 FROM Invoice WHERE Invoice_ID = 'DEMO001')
BEGIN
    INSERT INTO Invoice (Invoice_ID, Account_ID, Add_Score, BookingDate, Status, Total_Money, Use_Score, Seat, Seat_IDs, Movie_Show_Id, Promotion_Discount, Voucher_ID, RankDiscountPercentage, Cancel, CancelDate, CancelBy)
    VALUES ('DEMO001', 'GUEST', 0, GETDATE(), 1, 64000, 0, 'A10, A11', '20,21', 1, '0', NULL, 0, 0, NULL, NULL);
END

IF NOT EXISTS (SELECT 1 FROM Invoice WHERE Invoice_ID = 'TEST001')
BEGIN
    INSERT INTO Invoice (Invoice_ID, Account_ID, Add_Score, BookingDate, Status, Total_Money, Use_Score, Seat, Seat_IDs, Movie_Show_Id, Promotion_Discount, Voucher_ID, RankDiscountPercentage, Cancel, CancelDate, CancelBy)
    VALUES ('TEST001', 'GUEST', 0, GETDATE(), 1, 96000, 0, 'B10, B11, B12', '22,23,24', 1, '0', NULL, 0, 0, NULL, NULL);
END

IF NOT EXISTS (SELECT 1 FROM Invoice WHERE Invoice_ID = 'GUEST001')
BEGIN
    INSERT INTO Invoice (Invoice_ID, Account_ID, Add_Score, BookingDate, Status, Total_Money, Use_Score, Seat, Seat_IDs, Movie_Show_Id, Promotion_Discount, Voucher_ID, RankDiscountPercentage, Cancel, CancelDate, CancelBy)
    VALUES ('GUEST001', 'GUEST', 0, GETDATE(), 1, 32000, 0, 'C10', '25', 1, '0', NULL, 0, 0, NULL, NULL);
END

-- Kiểm tra kết quả
SELECT 
    Invoice_ID as InvoiceId,
    Account_ID as AccountId,
    Status,
    Total_Money as TotalMoney,
    Seat,
    BookingDate
FROM Invoice 
ORDER BY BookingDate DESC; 