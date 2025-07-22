-- Script tạo account test cho Booking Management
-- Chạy script này trước khi chạy create_test_invoices.sql

-- Thêm account test cho Member (chỉ thêm nếu chưa tồn tại)
IF NOT EXISTS (SELECT 1 FROM Account WHERE Account_ID = 'AC001')
BEGIN
    INSERT INTO Account (Account_ID, Full_Name, Email, Phone_Number, Identity_Card, Address, Register_Date, Role_ID, Password)
    VALUES ('AC001', 'Nguyen Van A', 'nguyenvana@email.com', '0123456789', '123456789', 'Ha Noi', GETDATE(), 3, 'password123');
END

IF NOT EXISTS (SELECT 1 FROM Account WHERE Account_ID = 'AC002')
BEGIN
    INSERT INTO Account (Account_ID, Full_Name, Email, Phone_Number, Identity_Card, Address, Register_Date, Role_ID, Password)
    VALUES ('AC002', 'Tran Thi B', 'tranthib@email.com', '0987654321', '987654321', 'Ho Chi Minh', GETDATE(), 3, 'password123');
END

IF NOT EXISTS (SELECT 1 FROM Account WHERE Account_ID = 'AC003')
BEGIN
    INSERT INTO Account (Account_ID, Full_Name, Email, Phone_Number, Identity_Card, Address, Register_Date, Role_ID, Password)
    VALUES ('AC003', 'Le Van C', 'levanc@email.com', '0555666777', '555666777', 'Da Nang', GETDATE(), 3, 'password123');
END

IF NOT EXISTS (SELECT 1 FROM Account WHERE Account_ID = 'AC004')
BEGIN
    INSERT INTO Account (Account_ID, Full_Name, Email, Phone_Number, Identity_Card, Address, Register_Date, Role_ID, Password)
    VALUES ('AC004', 'Pham Thi D', 'phamthid@email.com', '0333444555', '333444555', 'Hai Phong', GETDATE(), 3, 'password123');
END

IF NOT EXISTS (SELECT 1 FROM Account WHERE Account_ID = 'AC005')
BEGIN
    INSERT INTO Account (Account_ID, Full_Name, Email, Phone_Number, Identity_Card, Address, Register_Date, Role_ID, Password)
    VALUES ('AC005', 'Hoang Van E', 'hoangvane@email.com', '0777888999', '777888999', 'Can Tho', GETDATE(), 3, 'password123');
END

-- Kiểm tra kết quả
SELECT 
    Account_ID as AccountId,
    FullName,
    PhoneNumber,
    IdentityCard,
    RoleId
FROM Account 
WHERE Account_ID IN ('AC001', 'AC002', 'AC003', 'AC004', 'AC005')
ORDER BY Account_ID; 