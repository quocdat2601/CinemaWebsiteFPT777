ALTER DATABASE MovieTheater SET SINGLE_USER WITH ROLLBACK IMMEDIATE;  
GO  

DROP DATABASE IF EXISTS MovieTheater;  
GO  

CREATE DATABASE MovieTheater;
GO

USE MovieTheater;
GO

CREATE TABLE Roles(
	Role_ID INT PRIMARY KEY,
	Role_Name VARCHAR(255)
);

CREATE TABLE Rank (
    Rank_ID INT PRIMARY KEY IDENTITY(1,1),
    Rank_Name VARCHAR(50) UNIQUE,
    Discount_Percentage DECIMAL(5,2),
    Required_Points INT
);

CREATE TABLE Account (
    Account_ID VARCHAR(10) PRIMARY KEY,
    Address VARCHAR(255),
    Date_Of_Birth DATE,
    Email VARCHAR(255),
    Full_Name VARCHAR(255),
    Gender VARCHAR(255),
    Identity_Card VARCHAR(255),
    Image VARCHAR(255),
    Password VARCHAR(255),
    Phone_Number VARCHAR(255),
    Register_Date DATE,
    Role_ID INT,
    STATUS INT,
    USERNAME VARCHAR(50),
    Rank_ID INT, -- Use Rank_ID to match FK constraint
    CONSTRAINT FK_Account_Role FOREIGN KEY (Role_ID) REFERENCES Roles(Role_ID),
    CONSTRAINT FK_Account_Rank FOREIGN KEY (Rank_ID) REFERENCES Rank(Rank_ID)
);

CREATE TABLE Invoice (
    Invoice_ID VARCHAR(10) PRIMARY KEY,
    Add_Score INT,
    BookingDate DATE,
    MovieName VARCHAR(255),
    Schedule_Show DATE,
    Schedule_Show_Time VARCHAR(255),
    Status INT,
    Total_Money INT,
    Use_Score INT,
    Seat VARCHAR(20),
    Account_ID VARCHAR(10),
    CONSTRAINT FK_Invoice_Account FOREIGN KEY (Account_ID) REFERENCES Account(Account_ID)
);

CREATE TABLE Employee (
    Employee_ID VARCHAR(10) PRIMARY KEY,
    Account_ID VARCHAR(10),
    CONSTRAINT FK_Employee_Account FOREIGN KEY (Account_ID) REFERENCES Account(Account_ID)
);

-- Member table
CREATE TABLE Member (
    Member_ID VARCHAR(10) PRIMARY KEY,
    Score INT,
    Account_ID VARCHAR(10),
    CONSTRAINT FK_Member_Account FOREIGN KEY (Account_ID) REFERENCES Account(Account_ID)
);

CREATE TABLE Show_Dates (
    Show_Date_ID INT PRIMARY KEY,
    Show_Date DATE,
    Date_Name VARCHAR(255)
);

CREATE TABLE Movie (
    Movie_ID VARCHAR(10) PRIMARY KEY,
    Actor VARCHAR(255),
    Cinema_Room_ID INT,
    Content VARCHAR(1000),
    Director VARCHAR(255),
    Duration INT,
    From_Date DATE,
    Movie_Production_Company VARCHAR(255),
    To_Date DATE,
    Version VARCHAR(255),
    Movie_Name_English VARCHAR(255),
    Movie_Name_VN VARCHAR(255),
    Large_Image VARCHAR(255),
    Small_Image VARCHAR(255)
);

CREATE TABLE Movie_Date (
    Movie_ID VARCHAR(10),
    Show_Date_ID INT,
    PRIMARY KEY (Movie_ID, Show_Date_ID),
    FOREIGN KEY (Movie_ID) REFERENCES Movie(Movie_ID),
    FOREIGN KEY (Show_Date_ID) REFERENCES Show_Dates(Show_Date_ID)
);

CREATE TABLE Schedule (
    Schedule_ID INT PRIMARY KEY,
    Schedule_Time VARCHAR(255)
);

CREATE TABLE Movie_Schedule (
    Movie_ID VARCHAR(10),
    Schedule_ID INT,
    PRIMARY KEY (Movie_ID, Schedule_ID),
    FOREIGN KEY (Movie_ID) REFERENCES Movie(Movie_ID),
    FOREIGN KEY (Schedule_ID) REFERENCES Schedule(Schedule_ID)
);

CREATE TABLE Type (
    Type_ID INT PRIMARY KEY,
    Type_Name VARCHAR(255)
);

CREATE TABLE Movie_Type (
    Movie_ID VARCHAR(10),
    Type_ID INT,
    PRIMARY KEY (Movie_ID, Type_ID),
    FOREIGN KEY (Movie_ID) REFERENCES Movie(Movie_ID),
    FOREIGN KEY (Type_ID) REFERENCES Type(Type_ID)
);

CREATE TABLE Cinema_Room (
    Cinema_Room_ID INT PRIMARY KEY,
    Cinema_Room_Name VARCHAR(255),
    Seat_Quantity INT
);

CREATE TABLE Seat (
    Seat_ID INT PRIMARY KEY,
    Cinema_Room_ID INT,
    Seat_Column VARCHAR(255),
    Seat_Row INT,
    Seat_Status INT,
    Seat_Type INT,
    FOREIGN KEY (Cinema_Room_ID) REFERENCES Cinema_Room(Cinema_Room_ID)
);

CREATE TABLE Schedule_Seat (
    Schedule_Seat_ID VARCHAR(10) PRIMARY KEY,
    Movie_ID VARCHAR(10),
    Schedule_ID INT,
    Seat_ID INT,
    Seat_Column VARCHAR(255),
    Seat_Row INT,
    Seat_Status INT,
    Seat_Type INT
);

CREATE TABLE Ticket (
    Ticket_ID INT PRIMARY KEY,
    Price NUMERIC(18, 2),
    Ticket_Type INT
);

INSERT INTO Roles (Role_ID, Role_Name) VALUES
(1, 'Admin'),
(2, 'Employee'),
(3, 'Member');

INSERT INTO Rank (Rank_Name, Discount_Percentage, Required_Points) VALUES
('Bronze', 0.00, 0),
('Gold', 5.00, 30000),
('Diamond', 10.00, 50000),
('Elite', 15.00, 80000);

INSERT INTO Account (Account_ID, Address, Date_Of_Birth, Email, Full_Name, Gender, Identity_Card, Image, Password, Phone_Number, Register_Date, Role_ID, STATUS, USERNAME) VALUES
('AC001', '123 Main St', '2000-01-15', 'admin@gmail.com', 'Admin', 'Female', '123456789', '/image/shark.jpg', '1', '0123456789', '2023-01-01', 1, 1, 'admin'),
('AC002', '456 Elm St', '1995-06-25', 'employee@gmail.com', 'Employee', 'Male', '987654321', '/image/crocodile.jpg', '1', '0987654321', '2023-01-05', 2, 1, 'employee'),
('AC003', '789 Oak St', '2002-11-10', 'member@gmail.com', 'Member', 'Female', '192837465', '/image/tung.jpg', '1', '0111222333', '2023-01-10', 3, 1, 'member'),
('AC004', '123 Main St', '2000-01-15', 'admin2@gmail.com', 'Admin', 'Female', '123456789', '/image/shark.jpg', '1', '0123456789', '2023-01-01', 1, 1, 'admin2'),
('AC005', '456 Elm St', '1995-06-25', 'employee2@gmail.com', 'Employee', 'Male', '987654321', '/image/crocodile.jpg', '1', '0987654321', '2023-01-05', 2, 1, 'employee2'),
('AC006', '789 Oak St', '2002-11-10', 'member3@gmail.com', 'Member', 'Female', '192837465', '/image/tung.jpg', '1', '0111222333', '2023-01-10', 3, 0, 'member3');

INSERT INTO Member (Member_ID, Score, Account_ID) VALUES
('MB001', 100, 'AC003'),  -- Member linked to account A003
('MB002', 1000000,'AC006');  -- Member linked to account A006


INSERT INTO Employee (Employee_ID, Account_ID) VALUES
('EM001', 'AC005');

INSERT INTO Show_Dates (Show_Date_ID, Show_Date, Date_Name) VALUES
(1, '2025-06-01', 'Sunday Premiere'),
(2, '2025-06-02', 'Monday Matinee');

INSERT INTO Movie (Movie_ID, Actor, Cinema_Room_ID, Content, Director, Duration, From_Date, Movie_Production_Company, To_Date, Version, Movie_Name_English, Movie_Name_VN, Large_Image, Small_Image)
VALUES
('MV001', 'Luca Bianchi, Anna Rossi', 2, 'A reclusive astronomer discovers a hidden constellation that predicts global events, drawing the attention of a secretive organization. As the stars align, she must decide whether to protect her discovery or risk everything for the truth.', 'Giovanni Lupo', 135, '2025-06-05', 'Nebula Films', '2025-07-05', 'IMAX 3D', 'The Stargazer Prophecy', 'Lời Tiên Tri Ngắm Sao', '/image/shark.jpg', '/image/shark.jpg'),
('MV002', 'Mei Lin, Haruto Tanaka', 3, 'A quirky florist and a grumpy barista in Kyoto are forced to share a café storefront. As their clashing personalities spark chaos, a shared love for old poetry might be the only thing that can bring them together.', 'Akira Sato', 110, '2025-06-10', 'Sakura Pictures', '2025-07-10', '2D', 'Hearts in Kyoto', 'Trái Tim Ở Kyoto', '/image/crocodile.jpg', '/image/crocodile.jpg'),
('MV003', 'Jake Carter, Ella Stone', 4, 'In a neon-lit future where memories are currency, a former racer is pulled into an underworld plot to overwrite identities. Racing against time and corrupted tech, he must confront his forgotten past to survive.', 'Lena Hopkins', 142, '2025-06-12', 'PixelLight Studios', '2025-07-12', '4DX', 'Neon Drift', 'Trôi Trong Ánh Đèn Neon', '/image/tung.jpg', '/image/tung.jpg'),
('MV004', 'Carlos Mendez, Sofia Rivera', 5, 'A young lawyer returns to her crime-ridden hometown after her brother’s mysterious death. As she uncovers layers of corruption, she must fight both the legal system and her own past to deliver justice.', 'Roberto Diaz', 124, '2025-06-08', 'Solara Pictures', '2025-07-08', '2D', 'Blood and Cement', 'Máu Và Xi Măng', '/image/shark.jpg', '/image/shark.jpg'),
('MV005', 'Mina Kapoor, Aarav Singh', 1, 'A vibrant tale of love, family, and dance set in the bustling streets of Mumbai. When a young dancer dares to chase her dreams against her conservative familys wishes, rhythm becomes her voice of rebellion.', 'Ravi Desai', 150, '2025-06-15', 'Bollywood Beats', '2025-07-15', '3D', 'Dance of Dreams', 'Vũ Điệu Giấc Mơ', '/image/crocodile.jpg', '/image/crocodile.jpg'),
('MV006', 'Emma Brown, Noah Reed', 2, 'Years after a global collapse, two survivors navigate the ruins of civilization to find a rumored safe haven. Battling both the elements and each other, they learn that survival requires more than just endurance.', 'Jesse Moore', 119, '2025-06-07', 'EchoFilm Studios', '2025-07-07', '2D', 'After the Ashes', 'Sau Tro Tàn', '/image/tung.jpg', '/image/tung.jpg'),
('MV007', 'Hana Kim, Jihoon Park', 1, 'A cursed prince must gather three ancient relics to break the spell that traps his soul in a stone blade. With a reluctant thief as his guide, their quest takes them across mythical realms in a battle against fate.', 'Soojin Lee', 128, '2025-06-09', 'Dreamwave Korea', '2025-07-09', 'IMAX', 'Sword of the Moon', 'Kiếm Nguyệt', '/image/shark.jpg', '/image/shark.jpg'),
('MV008', 'Ali Nasser, Leila Haddad', 3, 'Set in the golden age of the Silk Road, a young scribe uncovers a conspiracy threatening the kingdom. With the help of a rebel prince, she races to rewrite history before it’s too late.', 'Samir Al-Hakim', 165, '2025-06-13', 'Desert Star', '2025-07-13', '4DX', 'Kingdom of Sand', 'Vương Quốc Cát', '/image/crocodile.jpg', '/image/crocodile.jpg'),
('MV009', 'John Smith, Jane Doe', 4, 'A routine software update goes wrong in a remote lab, unleashing an AI entity that turns against its creators. Trapped in the facility, a group of engineers must outwit their own creation to survive.', 'Marcus Black', 117, '2025-06-06', 'Glitch Studios', '2025-07-06', '2D', 'Code Red: Signal Lost', 'Mất Tín Hiệu Đỏ', '/image/tung.jpg', '/image/tung.jpg');


INSERT INTO Movie_Date (Movie_ID, Show_Date_ID) VALUES
('MV001', 1),
('MV001', 2),
('MV002', 1),
('MV002', 2),
('MV003', 1),
('MV003', 2),
('MV004', 1),
('MV004', 2),
('MV005', 1),
('MV005', 2),
('MV006', 1),
('MV006', 2),
('MV007', 1),
('MV007', 2);

INSERT INTO Schedule (Schedule_ID, Schedule_Time) VALUES
(1, '10:00'),
(2, '14:00'),
(3, '18:00');

INSERT INTO Movie_Schedule (Movie_ID, Schedule_ID) VALUES
('MV001', 1),
('MV001', 2),
('MV002', 1),
('MV003', 2), 
('MV003', 3),
('MV004', 3),
('MV005', 1), 
('MV005', 3),
('MV006', 2),
('MV007', 1), 
('MV007', 2), 
('MV007', 3),
('MV008', 3),
('MV009', 2);

INSERT INTO Type (Type_ID, Type_Name) VALUES
(1, 'Action'),
(2, 'Comedy'),
(3, 'Romance'),
(4, 'Drama'),
(5, 'Sci-Fi'),
(6, 'War'),
(7, 'Wuxia'),
(8, 'Music'),
(9, 'Horror'),
(10, 'Adventure'),
(11, 'Psychology 18+'),
(12, 'Animation');

INSERT INTO Movie_Type (Movie_ID, Type_ID) VALUES
('MV001', 1),
('MV001', 2),
('MV002', 3), 
('MV002', 4),
('MV003', 5),
('MV004', 6), 
('MV004', 7),
('MV005', 8),
('MV006', 9), 
('MV006', 10),
('MV007', 11),
('MV008', 12),
('MV009', 1), 
('MV009', 5);

INSERT INTO Cinema_Room (Cinema_Room_ID, Cinema_Room_Name, Seat_Quantity) VALUES
(1, 'Main Hall', 100),
(2, 'FPT University', 100),
(3, 'F-Town', 100);

INSERT INTO Seat (Seat_ID, Cinema_Room_ID, Seat_Column, Seat_Row, Seat_Status, Seat_Type) VALUES
(1, 1, 'A', 1, 0, 1),
(2, 1, 'A', 2, 0, 1),
(3, 1, 'B', 1, 1, 2); -- 1 for booked, 2 for VIP

CREATE TABLE Promotion (
    Promotion_ID INT PRIMARY KEY,
    Detail VARCHAR(255),
    Discount_Level INT,
    End_Time DATE,
    Image VARCHAR(255),
    Start_Time DATE,
    Title VARCHAR(255),
	Is_Active BIT NOT NULL DEFAULT 1
);

CREATE TABLE ConditionType (
    ConditionType_ID INT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL
);

CREATE TABLE PromotionCondition (
    Condition_ID INT PRIMARY KEY IDENTITY,
    Promotion_ID INT FOREIGN KEY REFERENCES Promotion(Promotion_ID),
    ConditionType_ID INT FOREIGN KEY REFERENCES ConditionType(ConditionType_ID),
    Target_Entity VARCHAR(50),
    Target_Field VARCHAR(50),
    Operator VARCHAR(10),           -- Only used if it's a comparison
    Target_Value VARCHAR(255)
);

INSERT INTO ConditionType (ConditionType_ID, Name)
VALUES 
(1, 'Comparison'),
(2, 'Selection');

-- First Time Promo (20% off)
INSERT INTO Promotion (Promotion_ID, Title, Detail, Discount_Level, Start_Time, End_Time, Image, Is_Active) VALUES
(1, 'First Time Promo', 'Get 20% off on your first order!', 20, '2025-05-01', '2025-12-31', 'first_time.png', 1),
(2, 'Group Discount', 'Order with 3 or more people and save 15%!', 15, '2025-05-01', '2025-12-31', 'group_discount.png', 1);

-- First Time Promo condition: User.OrderCount = 0
INSERT INTO PromotionCondition (Promotion_ID, ConditionType_ID, Target_Entity, Target_Field, Operator, Target_Value) VALUES 
(1, 1, 'User', 'OrderCount', '=', '0'),
(2, 1, 'Order', 'Seat_Count', '>=', '3');
