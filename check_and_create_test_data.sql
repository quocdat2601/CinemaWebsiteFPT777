-- Script kiểm tra và tạo dữ liệu test
-- Chạy script này để tạo dữ liệu test cho Booking Management

-- 1. Kiểm tra dữ liệu hiện có
PRINT '=== KIỂM TRA DỮ LIỆU HIỆN CÓ ===';

-- Kiểm tra Movie_Show
SELECT 'Movie_Show' as TableName, COUNT(*) as RecordCount FROM Movie_Show;
SELECT TOP 5 Movie_Show_ID, Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID 
FROM Movie_Show ORDER BY Movie_Show_ID;

-- Kiểm tra Account
SELECT 'Account' as TableName, COUNT(*) as RecordCount FROM Account;
SELECT TOP 5 Account_ID, Full_Name, Email, Phone_Number FROM Account ORDER BY Account_ID;

-- Kiểm tra Invoice
SELECT 'Invoice' as TableName, COUNT(*) as RecordCount FROM Invoice;
SELECT TOP 5 Invoice_ID, Account_ID, Movie_Show_Id, Status, Total_Money FROM Invoice ORDER BY Invoice_ID;

-- 2. Tạo dữ liệu test nếu cần
PRINT '=== TẠO DỮ LIỆU TEST ===';

-- Tạo Movie_Show test nếu chưa có
IF NOT EXISTS (SELECT 1 FROM Movie_Show WHERE Movie_Show_ID = 1)
BEGIN
    -- Kiểm tra xem có Movie nào không
    IF EXISTS (SELECT 1 FROM Movie WHERE Movie_ID = 'M001')
    BEGIN
        -- Kiểm tra xem có Cinema_Room nào không
        IF EXISTS (SELECT 1 FROM Cinema_Room WHERE Cinema_Room_ID = 1)
        BEGIN
            -- Kiểm tra xem có Schedule nào không
            IF EXISTS (SELECT 1 FROM Schedule WHERE Schedule_ID = 1)
            BEGIN
                -- Kiểm tra xem có Version nào không
                IF EXISTS (SELECT 1 FROM Version WHERE Version_ID = 1)
                BEGIN
                    INSERT INTO Movie_Show (Movie_Show_ID, Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID)
                    VALUES (1, 'M001', 1, '2025-01-20', 1, 1);
                    PRINT 'Đã tạo Movie_Show với ID = 1';
                END
                ELSE
                BEGIN
                    PRINT 'Không có Version với ID = 1. Vui lòng tạo Version trước.';
                END
            END
            ELSE
            BEGIN
                PRINT 'Không có Schedule với ID = 1. Vui lòng tạo Schedule trước.';
            END
        END
        ELSE
        BEGIN
            PRINT 'Không có Cinema_Room với ID = 1. Vui lòng tạo Cinema_Room trước.';
        END
    END
    ELSE
    BEGIN
        PRINT 'Không có Movie với ID = M001. Vui lòng tạo Movie trước.';
    END
END
ELSE
BEGIN
    PRINT 'Movie_Show với ID = 1 đã tồn tại';
END

-- Tạo Account test nếu chưa có
IF NOT EXISTS (SELECT 1 FROM Account WHERE Account_ID = 'AC001')
BEGIN
    INSERT INTO Account (Account_ID, Full_Name, Email, Phone_Number, Identity_Card, Address, Register_Date, Role_ID, Password)
    VALUES ('AC001', 'Nguyen Van A', 'nguyenvana@email.com', '0123456789', '123456789', 'Ha Noi', GETDATE(), 3, 'password123');
    PRINT 'Đã tạo Account với ID = AC001';
END
ELSE
BEGIN
    PRINT 'Account với ID = AC001 đã tồn tại';
END

-- Tạo Invoice test cho Guest (chỉ khi có Movie_Show_ID = 1)
IF NOT EXISTS (SELECT 1 FROM Invoice WHERE Invoice_ID = 'GUEST001')
BEGIN
    IF EXISTS (SELECT 1 FROM Movie_Show WHERE Movie_Show_ID = 1)
    BEGIN
        INSERT INTO Invoice (Invoice_ID, Account_ID, Add_Score, BookingDate, Status, Total_Money, Use_Score, Seat, Seat_IDs, Movie_Show_Id, Promotion_Discount, Voucher_ID, RankDiscountPercentage, Cancel, CancelDate, CancelBy)
        VALUES ('GUEST001', 'GUEST', 0, GETDATE(), 1, 88000, 0, 'D5, E5', '20,21', 1, '0', NULL, 0, 0, NULL, NULL);
        PRINT 'Đã tạo Invoice với ID = GUEST001';
    END
    ELSE
    BEGIN
        PRINT 'Không thể tạo Invoice vì không có Movie_Show với ID = 1';
    END
END
ELSE
BEGIN
    PRINT 'Invoice với ID = GUEST001 đã tồn tại';
END

-- 3. Kiểm tra kết quả cuối cùng
PRINT '=== KẾT QUẢ CUỐI CÙNG ===';

SELECT 'Account' as TableName, COUNT(*) as RecordCount FROM Account
UNION ALL
SELECT 'Movie_Show' as TableName, COUNT(*) as RecordCount FROM Movie_Show
UNION ALL
SELECT 'Invoice' as TableName, COUNT(*) as RecordCount FROM Invoice;

SELECT 
    Invoice_ID as InvoiceId,
    Account_ID as AccountId,
    Movie_Show_Id as MovieShowId,
    Status,
    Total_Money as TotalMoney,
    Seat,
    BookingDate
FROM Invoice 
ORDER BY BookingDate DESC; 