-- Script tạo showtime đơn giản cho test QR code
USE MovieTheater;

-- 1. Tạo showtime cho hôm nay
INSERT INTO Movie_Show (Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID)
VALUES ('MV001', 1, GETDATE(), 21, 1);

-- 2. Tạo showtime cho ngày mai
INSERT INTO Movie_Show (Movie_ID, Cinema_Room_ID, Show_Date, Schedule_ID, Version_ID)
VALUES ('MV002', 2, DATEADD(day, 1, GETDATE()), 23, 1);

-- 3. Kiểm tra showtime đã tạo
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
WHERE ms.Show_Date >= CAST(GETDATE() AS DATE)
ORDER BY ms.Show_Date, s.Schedule_Time;

PRINT 'Showtime đã được tạo thành công!';
PRINT 'Bây giờ bạn có thể test QR code thanh toán.'; 