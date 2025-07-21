-- Script kiểm tra cấu trúc database
-- Chạy script này trước để xem database có những bảng nào

-- Kiểm tra tất cả bảng trong database
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- Kiểm tra cấu trúc bảng Account
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Account'
ORDER BY ORDINAL_POSITION;

-- Kiểm tra cấu trúc bảng Invoice
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Invoice'
ORDER BY ORDINAL_POSITION;

-- Kiểm tra dữ liệu hiện tại
SELECT 'Account' as TableName, COUNT(*) as RecordCount FROM Account
UNION ALL
SELECT 'Invoice' as TableName, COUNT(*) as RecordCount FROM Invoice; 