-- Script tạo dữ liệu test nhanh
-- Chạy script này để có dữ liệu test cho Booking Management

-- Thêm account test
IF NOT EXISTS (SELECT 1 FROM Account WHERE Account_ID = 'AC001')
BEGIN
    INSERT INTO Account (Account_ID, Full_Name, Email, Phone_Number, Identity_Card, Address, Register_Date, Role_ID, Password)
    VALUES ('AC001', 'Nguyen Van A', 'nguyenvana@email.com', '0123456789', '123456789', 'Ha Noi', GETDATE(), 3, 'password123');
END

-- Thêm invoice test cho Guest
IF NOT EXISTS (SELECT 1 FROM Invoice WHERE Invoice_ID = 'GUEST001')
BEGIN
    INSERT INTO Invoice (Invoice_ID, Account_ID, Add_Score, BookingDate, Status, Total_Money, Use_Score, Seat, Seat_IDs, Movie_Show_Id, Promotion_Discount, Voucher_ID, RankDiscountPercentage, Cancel, CancelDate, CancelBy)
    VALUES ('GUEST001', 'GUEST', 0, GETDATE(), 1, 88000, 0, 'D5, E5', '20,21', 1, '0', NULL, 0, 0, NULL, NULL);
END

-- Kiểm tra kết quả
SELECT 'Account' as TableName, COUNT(*) as RecordCount FROM Account
UNION ALL
SELECT 'Invoice' as TableName, COUNT(*) as RecordCount FROM Invoice;

SELECT 
    Invoice_ID as InvoiceId,
    Account_ID as AccountId,
    Status,
    Total_Money as TotalMoney,
    Seat,
    BookingDate
FROM Invoice 
ORDER BY BookingDate DESC; 