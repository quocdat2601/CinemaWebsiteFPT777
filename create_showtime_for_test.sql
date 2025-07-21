-- Script tạo showtime cho test QR code
-- Chạy script này trong SQL Server Management Studio

USE MovieTheater;

-- 1. Kiểm tra thông tin phim có sẵn
SELECT Movie_ID, Movie_Name_English, Movie_Name_VN, Duration
FROM Movie 
WHERE Movie_Name_English IS NOT NULL;

-- 2. Kiểm tra cinema rooms có sẵn
SELECT Cinema_Room_ID, Cinema_Room_Name, Seat_Width, Seat_Length 
FROM Cinema_Room 
WHERE Status_ID = 1; -- Active rooms

-- 3. Kiểm tra schedules có sẵn
SELECT Schedule_ID, Schedule_Time 
FROM Schedule 
ORDER BY Schedule_Time;

-- 4. Kiểm tra versions có sẵn
SELECT Version_ID, Version_Name 
FROM Version;

-- 5. Tạo showtime cho hôm nay (21/07/2025)
-- Showtime 1: MV001 (Barbie) - 19:00 - Room 1
INSERT INTO Movie_Show (Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID)
VALUES ('MV001', 1, '2025-07-21', 21, 1);

-- Showtime 2: MV002 (Batman) - 20:00 - Room 2  
INSERT INTO Movie_Show (Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID)
VALUES ('MV002', 2, '2025-07-21', 23, 1);

-- Showtime 3: MV004 (Everything Everywhere) - 21:00 - Room 3
INSERT INTO Movie_Show (Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID)
VALUES ('MV004', 3, '2025-07-21', 25, 2);

-- Showtime 4: MV009 (Dune) - 19:30 - Room 4
INSERT INTO Movie_Show (Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID)
VALUES ('MV009', 4, '2025-07-21', 22, 1);

-- 6. Tạo showtime cho ngày mai (22/07/2025)
-- Showtime 5: MV001 (Barbie) - 20:00 - Room 1
INSERT INTO Movie_Show (Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID)
VALUES ('MV001', 1, '2025-07-22', 23, 1);

-- Showtime 6: MV002 (Batman) - 21:00 - Room 2
INSERT INTO Movie_Show (Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID)
VALUES ('MV002', 2, '2025-07-22', 25, 1);

-- Showtime 7: MV004 (Everything Everywhere) - 19:00 - Room 3
INSERT INTO Movie_Show (Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID)
VALUES ('MV004', 3, '2025-07-22', 21, 2);

-- Showtime 8: MV009 (Dune) - 20:30 - Room 4
INSERT INTO Movie_Show (Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID)
VALUES ('MV009', 4, '2025-07-22', 24, 1);

-- 7. Kiểm tra showtime đã tạo
SELECT 
    ms.Movie_Show_ID,
    m.Movie_Name_English,
    cr.Cinema_Room_Name,
    ms.Show_Date,
    s.Schedule_Time,
    v.Version_Name
FROM Movie_Show ms
JOIN Movie m ON ms.Movie_ID = m.Movie_ID
JOIN Cinema_Room cr ON ms.Cinema_Room_ID = cr.Cinema_Room_ID
JOIN Schedule s ON ms.Schedule_ID = s.Schedule_ID
JOIN Version v ON ms.Version_ID = v.Version_ID
WHERE ms.Show_Date IN ('2025-07-21', '2025-07-22')
ORDER BY ms.Show_Date, s.Schedule_Time;

-- 8. Kiểm tra ghế có sẵn cho các phòng
SELECT 
    cr.Cinema_Room_Name,
    COUNT(s.Seat_ID) as TotalSeats,
    COUNT(CASE WHEN s.Seat_Status_ID = 1 THEN 1 END) as AvailableSeats
FROM Cinema_Room cr
LEFT JOIN Seat s ON cr.Cinema_Room_ID = s.Cinema_Room_ID
WHERE cr.Cinema_Room_ID IN (1, 2, 3, 4)
GROUP BY cr.Cinema_Room_ID, cr.Cinema_Room_Name;

-- 9. Tạo một số ghế cho phòng 1 nếu chưa có
IF NOT EXISTS (SELECT 1 FROM Seat WHERE Cinema_Room_ID = 1)
BEGIN
    -- Tạo ghế cho phòng 1 (8 hàng x 10 cột)
    DECLARE @row INT = 1;
    DECLARE @col INT = 1;
    
    WHILE @row <= 8
    BEGIN
        SET @col = 1;
        WHILE @col <= 10
        BEGIN
            INSERT INTO Seat (SeatName, Cinema_Room_ID, Seat_Type_ID, Seat_Status_ID)
            VALUES (CHAR(64 + @row) + CAST(@col AS VARCHAR(2)), 1, 1, 1);
            SET @col = @col + 1;
        END
        SET @row = @row + 1;
    END
END

-- 10. Tạo một số ghế cho phòng 2 nếu chưa có
IF NOT EXISTS (SELECT 1 FROM Seat WHERE Cinema_Room_ID = 2)
BEGIN
    -- Tạo ghế cho phòng 2 (6 hàng x 8 cột)
    DECLARE @row2 INT = 1;
    DECLARE @col2 INT = 1;
    
    WHILE @row2 <= 6
    BEGIN
        SET @col2 = 1;
        WHILE @col2 <= 8
        BEGIN
            INSERT INTO Seat (SeatName, Cinema_Room_ID, Seat_Type_ID, Seat_Status_ID)
            VALUES (CHAR(64 + @row2) + CAST(@col2 AS VARCHAR(2)), 2, 1, 1);
            SET @col2 = @col2 + 1;
        END
        SET @row2 = @row2 + 1;
    END
END

-- 11. Tạo một số ghế cho phòng 3 nếu chưa có
IF NOT EXISTS (SELECT 1 FROM Seat WHERE Cinema_Room_ID = 3)
BEGIN
    -- Tạo ghế cho phòng 3 (7 hàng x 9 cột)
    DECLARE @row3 INT = 1;
    DECLARE @col3 INT = 1;
    
    WHILE @row3 <= 7
    BEGIN
        SET @col3 = 1;
        WHILE @col3 <= 9
        BEGIN
            INSERT INTO Seat (SeatName, Cinema_Room_ID, Seat_Type_ID, Seat_Status_ID)
            VALUES (CHAR(64 + @row3) + CAST(@col3 AS VARCHAR(2)), 3, 1, 1);
            SET @col3 = @col3 + 1;
        END
        SET @row3 = @row3 + 1;
    END
END

-- 12. Tạo một số ghế cho phòng 4 nếu chưa có
IF NOT EXISTS (SELECT 1 FROM Seat WHERE Cinema_Room_ID = 4)
BEGIN
    -- Tạo ghế cho phòng 4 (8 hàng x 12 cột)
    DECLARE @row4 INT = 1;
    DECLARE @col4 INT = 1;
    
    WHILE @row4 <= 8
    BEGIN
        SET @col4 = 1;
        WHILE @col4 <= 12
        BEGIN
            INSERT INTO Seat (SeatName, Cinema_Room_ID, Seat_Type_ID, Seat_Status_ID)
            VALUES (CHAR(64 + @row4) + CAST(@col4 AS VARCHAR(2)), 4, 1, 1);
            SET @col4 = @col4 + 1;
        END
        SET @row4 = @row4 + 1;
    END
END

PRINT 'Showtime và ghế đã được tạo thành công!';
PRINT 'Bây giờ bạn có thể test QR code thanh toán.'; 