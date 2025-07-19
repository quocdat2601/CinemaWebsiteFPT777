-- Script tạo showtime cho phim Dune hôm nay
-- Chạy script này trong SQL Server Management Studio hoặc sqlcmd

USE MovieTheater;

-- 1. Kiểm tra thông tin phim Dune
SELECT Movie_ID, Movie_Name_English, Movie_Name_VN 
FROM Movie 
WHERE Movie_Name_English LIKE '%Dune%';

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

-- 5. Tạo showtime cho Dune hôm nay
-- Thay thế 'MV009' bằng Movie_ID thực tế của Dune từ bước 1
-- Thay thế ngày hôm nay bằng ngày thực tế

-- Showtime 1: 19:00 (Schedule_ID = 21)
INSERT INTO Movie_Show (Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID)
VALUES ('MV009', 1, GETDATE(), 21, 1);

-- Showtime 2: 20:00 (Schedule_ID = 23) 
INSERT INTO Movie_Show (Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID)
VALUES ('MV009', 2, GETDATE(), 23, 1);

-- Showtime 3: 21:00 (Schedule_ID = 25)
INSERT INTO Movie_Show (Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID)
VALUES ('MV009', 3, GETDATE(), 25, 2);

-- 6. Kiểm tra showtime đã tạo
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
WHERE m.Movie_Name_English LIKE '%Dune%'
AND ms.Show_Date = CAST(GETDATE() AS DATE)
ORDER BY s.Schedule_Time; 