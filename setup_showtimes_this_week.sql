-- Combined script to set up show dates and movie links for this week

-- Select the database
USE MovieTheater;
GO

-- First, clear existing movie-date relationships
DELETE FROM Movie_Date;
GO

-- Then clear existing show dates
DELETE FROM Show_Dates;
GO

-- Reset the identity seed for Show_Dates
DBCC CHECKIDENT ('Show_Dates', RESEED, 0);
GO

-- Then add show dates for this week (March 20-26, 2024)
INSERT INTO Show_Dates (Show_Date, Date_Name) VALUES
('2024-03-20', 'Wednesday, March 20'),
('2024-03-21', 'Thursday, March 21'),
('2024-03-22', 'Friday, March 22'),
('2024-03-23', 'Saturday, March 23'),
('2024-03-24', 'Sunday, March 24'),
('2024-03-25', 'Monday, March 25'),
('2024-03-26', 'Tuesday, March 26');
GO

-- Update movie date ranges to create varied availability
UPDATE Movie
SET From_Date = '2024-03-20', To_Date = '2024-04-30'
WHERE Movie_ID IN ('MV001', 'MV002', 'MV003');
GO

UPDATE Movie
SET From_Date = '2024-03-20', To_Date = '2024-03-25'
WHERE Movie_ID IN ('MV004', 'MV005', 'MV006', 'MV007', 'MV008', 'MV009');
GO

-- Link a varied number of movies to each date this week, considering date ranges
-- Note: Movies with To_Date <= '2024-03-25' will not show on March 26th.
INSERT INTO Movie_Date (Movie_ID, Show_Date_ID) VALUES
-- 2024-03-20 (Wednesday) - ID 1: 3 movies (all available)
('MV001', 1), ('MV004', 1), ('MV007', 1),
-- 2024-03-21 (Thursday) - ID 2: 4 movies (all available)
('MV002', 2), ('MV005', 2), ('MV008', 2), ('MV001', 2),
-- 2024-03-22 (Friday) - ID 3: 2 movies (all available)
('MV003', 3), ('MV006', 3),
-- 2024-03-23 (Saturday) - ID 4: 4 movies (all available)
('MV007', 4), ('MV009', 4), ('MV002', 4), ('MV004', 4),
-- 2024-03-24 (Sunday) - ID 5: 1 movie (available)
('MV005', 5),
-- 2024-03-25 (Monday) - ID 6: 3 movies (all available)
('MV006', 6), ('MV008', 6), ('MV003', 6),
-- 2024-03-26 (Tuesday) - ID 7: 3 movies linked in Movie_Date, but only MV001, MV002, MV003 are available within their date ranges
('MV001', 7), ('MV004', 7), ('MV007', 7);
GO 