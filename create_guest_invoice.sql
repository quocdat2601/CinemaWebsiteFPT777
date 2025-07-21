-- Script tạo Invoice cho Guest sử dụng dữ liệu có sẵn
-- Chạy script này để tạo dữ liệu test cho Booking Management

-- 1. Kiểm tra dữ liệu có sẵn
PRINT '=== KIỂM TRA DỮ LIỆU CÓ SẴN ===';

SELECT 'Movie' as TableName, COUNT(*) as RecordCount FROM Movie
UNION ALL
SELECT 'Movie_Show' as TableName, COUNT(*) as RecordCount FROM Movie_Show
UNION ALL
SELECT 'Account' as TableName, COUNT(*) as RecordCount FROM Account
UNION ALL
SELECT 'Invoice' as TableName, COUNT(*) as RecordCount FROM Invoice;

-- 2. Tạo Invoice test cho Guest
IF NOT EXISTS (SELECT 1 FROM Invoice WHERE Invoice_ID = 'GUEST001')
BEGIN
    -- Sử dụng Movie_Show_ID đầu tiên có sẵn
    DECLARE @MovieShowId INT = (SELECT TOP 1 Movie_Show_ID FROM Movie_Show ORDER BY Movie_Show_ID);
    
    IF @MovieShowId IS NOT NULL
    BEGIN
        INSERT INTO Invoice (Invoice_ID, Account_ID, Add_Score, BookingDate, Status, Total_Money, Use_Score, Seat, Seat_IDs, Movie_Show_Id, Promotion_Discount, Voucher_ID, RankDiscountPercentage, Cancel, CancelDate, CancelBy)
        VALUES ('GUEST001', 'GUEST', 0, GETDATE(), 1, 88000, 0, 'D5, E5', '20,21', @MovieShowId, '0', NULL, 0, 0, NULL, NULL);
        PRINT 'Đã tạo Invoice với ID = GUEST001';
        PRINT 'Sử dụng Movie_Show_ID = ' + CAST(@MovieShowId AS VARCHAR);
    END
    ELSE
    BEGIN
        PRINT 'Không thể tạo Invoice vì không có Movie_Show nào';
    END
END
ELSE
BEGIN
    PRINT 'Invoice với ID = GUEST001 đã tồn tại';
END

-- 3. Tạo thêm Invoice test cho Guest
IF NOT EXISTS (SELECT 1 FROM Invoice WHERE Invoice_ID = 'GUEST002')
BEGIN
    -- Sử dụng Movie_Show_ID thứ hai có sẵn
    DECLARE @MovieShowId2 INT = (SELECT TOP 1 Movie_Show_ID FROM Movie_Show WHERE Movie_Show_ID > (SELECT MIN(Movie_Show_ID) FROM Movie_Show) ORDER BY Movie_Show_ID);
    
    IF @MovieShowId2 IS NOT NULL
    BEGIN
        INSERT INTO Invoice (Invoice_ID, Account_ID, Add_Score, BookingDate, Status, Total_Money, Use_Score, Seat, Seat_IDs, Movie_Show_Id, Promotion_Discount, Voucher_ID, RankDiscountPercentage, Cancel, CancelDate, CancelBy)
        VALUES ('GUEST002', 'GUEST', 0, GETDATE(), 1, 96000, 0, 'A1, A2', '1,2', @MovieShowId2, '0', NULL, 0, 0, NULL, NULL);
        PRINT 'Đã tạo Invoice với ID = GUEST002';
        PRINT 'Sử dụng Movie_Show_ID = ' + CAST(@MovieShowId2 AS VARCHAR);
    END
    ELSE
    BEGIN
        PRINT 'Không thể tạo Invoice thứ hai vì không có Movie_Show thứ hai';
    END
END
ELSE
BEGIN
    PRINT 'Invoice với ID = GUEST002 đã tồn tại';
END

-- 4. Kiểm tra kết quả
PRINT '=== KẾT QUẢ ===';

SELECT 'Invoice' as TableName, COUNT(*) as RecordCount FROM Invoice;

-- Hiển thị Invoice mới tạo
SELECT 
    Invoice_ID as InvoiceId,
    Account_ID as AccountId,
    Movie_Show_Id as MovieShowId,
    Status,
    Total_Money as TotalMoney,
    Seat,
    BookingDate
FROM Invoice 
WHERE Invoice_ID IN ('GUEST001', 'GUEST002')
ORDER BY Invoice_ID; 