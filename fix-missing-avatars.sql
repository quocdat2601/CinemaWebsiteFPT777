-- Script kiểm tra và sửa các avatar bị thiếu
-- Chạy script này trong SQL Server Management Studio hoặc database tool

-- 1. Kiểm tra các account có avatar bị thiếu
SELECT 
    AccountId,
    FullName,
    Email,
    Image,
    'Missing Avatar' as Status
FROM Account 
WHERE Image IS NOT NULL 
    AND Image LIKE '%227bf46%'
    OR Image LIKE '%/images/avatars/%'
    AND Image NOT IN (
        SELECT DISTINCT Image 
        FROM Account 
        WHERE Image IS NOT NULL
    );

-- 2. Cập nhật các avatar bị thiếu thành avatar mặc định
UPDATE Account 
SET Image = '/image/avatar.jpg'
WHERE Image IS NOT NULL 
    AND (
        Image LIKE '%227bf46%'
        OR Image LIKE '%/images/avatars/%'
    )
    AND Image NOT IN (
        SELECT DISTINCT Image 
        FROM Account 
        WHERE Image IS NOT NULL
    );

-- 3. Kiểm tra kết quả sau khi sửa
SELECT 
    AccountId,
    FullName,
    Email,
    Image,
    'Fixed Avatar' as Status
FROM Account 
WHERE Image = '/image/avatar.jpg'; 