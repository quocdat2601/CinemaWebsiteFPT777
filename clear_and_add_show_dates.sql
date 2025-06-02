-- First, clear existing movie-date relationships
DELETE FROM Movie_Date;

-- Then clear existing show dates
DELETE FROM Show_Dates;

-- Reset the identity seed for Show_Dates
DBCC CHECKIDENT ('Show_Dates', RESEED, 0);

-- Then add show dates for this week
INSERT INTO Show_Dates (Show_Date, Date_Name) VALUES
('2024-03-20', 'Wednesday, March 20'),
('2024-03-21', 'Thursday, March 21'),
('2024-03-22', 'Friday, March 22'),
('2024-03-23', 'Saturday, March 23'),
('2024-03-24', 'Sunday, March 24'),
('2024-03-25', 'Monday, March 25'),
('2024-03-26', 'Tuesday, March 26'); 