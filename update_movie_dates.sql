-- Update movie date ranges to create varied availability
UPDATE Movie
SET From_Date = '2024-03-20', To_Date = '2024-04-30'
WHERE Movie_ID IN ('MV001', 'MV002', 'MV003');

UPDATE Movie
SET From_Date = '2024-03-20', To_Date = '2024-03-25'
WHERE Movie_ID IN ('MV004', 'MV005', 'MV006', 'MV007', 'MV008', 'MV009'); 