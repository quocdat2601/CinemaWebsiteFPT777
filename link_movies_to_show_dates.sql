-- Clear existing movie-date relationships
DELETE FROM Movie_Date;

-- Link a varied number of movies to each date this week, considering date ranges
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
-- 2024-03-26 (Tuesday) - ID 7: 3 movies linked, but only MV001, MV002, MV003 are available
('MV001', 7), ('MV004', 7), ('MV007', 7); 