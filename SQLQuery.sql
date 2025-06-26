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
    Email VARCHAR(255) UNIQUE,
    Full_Name VARCHAR(255),
    Gender VARCHAR(255),
    Identity_Card VARCHAR(255),
    Image VARCHAR(255),
    Password VARCHAR(255),
    Phone_Number VARCHAR(255),
    Register_Date DATE,
    Role_ID INT,
    STATUS INT,
    USERNAME VARCHAR(50) UNIQUE,
    Rank_ID INT, -- Use Rank_ID to match FK constraint
    CONSTRAINT FK_Account_Role FOREIGN KEY (Role_ID) REFERENCES Roles(Role_ID),
    CONSTRAINT FK_Account_Rank FOREIGN KEY (Rank_ID) REFERENCES Rank(Rank_ID)
);

CREATE TABLE Invoice (
    Invoice_ID VARCHAR(10) PRIMARY KEY,
    Add_Score INT,
    BookingDate DATETIME,
    MovieName VARCHAR(255),
    Schedule_Show DATETIME,
    Schedule_Show_Time VARCHAR(255),
    Status INT,
	RoleId INT,
    Total_Money DECIMAL,
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
    Show_Date_ID INT PRIMARY KEY IDENTITY(1,1),
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
    Small_Image VARCHAR(255),
	TrailerUrl VARCHAR(255)
);

CREATE TABLE Schedule (
    Schedule_ID INT PRIMARY KEY IDENTITY(1,1),
    Schedule_Time VARCHAR(255)
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
    Cinema_Room_ID INT PRIMARY KEY IDENTITY(1,1),
    Cinema_Room_Name VARCHAR(255),
    Seat_Width INT,
    Seat_Length INT
);

ALTER TABLE Cinema_Room
ADD Seat_Quantity AS (Seat_Width * Seat_Length);

INSERT INTO Cinema_Room(Cinema_Room_Name) VALUES
('Screen 1'), ('Screen 2'), ('Screen 3'), ('Screen 4'), ('Screen 5'), ('Screen 6');

CREATE TABLE Movie_Show (
    Movie_Show_ID INT PRIMARY KEY IDENTITY(1,1),
    Movie_ID VARCHAR(10),
    Show_Date_ID INT,
    Schedule_ID INT,
    Cinema_Room_ID INT,
    FOREIGN KEY (Movie_ID) REFERENCES Movie(Movie_ID),
    FOREIGN KEY (Show_Date_ID) REFERENCES Show_Dates(Show_Date_ID),
    FOREIGN KEY (Schedule_ID) REFERENCES Schedule(Schedule_ID),
    FOREIGN KEY (Cinema_Room_ID) REFERENCES Cinema_Room(Cinema_Room_ID)
);

CREATE TABLE Seat_Type (
    Seat_Type_ID INT PRIMARY KEY IDENTITY(1,1),
    Type_Name VARCHAR(50),
	Price_Percent decimal(18,2) NOT NULL,
	ColorHex VARCHAR(7) NOT NULL DEFAULT '#FFFFFF'
);

CREATE TABLE Seat_Status (
    Seat_Status_ID INT PRIMARY KEY IDENTITY(1,1),
    Status_Name VARCHAR(50) -- e.g., 'Available', 'Booked'
);

CREATE TABLE Seat (
    Seat_ID INT PRIMARY KEY IDENTITY(1,1),
    Cinema_Room_ID INT,
    Seat_Column VARCHAR(5), -- e.g., 'A', 'B'
    Seat_Row INT,           -- e.g., 1, 2, ...
    Seat_Status_ID INT,
    Seat_Type_ID INT,
	SeatName VARCHAR(5),
    FOREIGN KEY (Cinema_Room_ID) REFERENCES Cinema_Room(Cinema_Room_ID),
    FOREIGN KEY (Seat_Status_ID) REFERENCES Seat_Status(Seat_Status_ID),
	FOREIGN KEY (Seat_Type_ID) REFERENCES Seat_Type(Seat_Type_ID)
);

CREATE TABLE Schedule_Seat (
    Movie_Show_ID INT,
	Invoice_ID VARCHAR(10),
    Seat_ID INT,
    Seat_Status_ID INT,
    PRIMARY KEY (Movie_Show_ID, Seat_ID),
    FOREIGN KEY (Movie_Show_ID) REFERENCES Movie_Show(Movie_Show_ID) ON DELETE CASCADE,
    FOREIGN KEY (Seat_ID) REFERENCES Seat(Seat_ID),
	FOREIGN KEY (Invoice_ID) REFERENCES Invoice(Invoice_ID),
    FOREIGN KEY (Seat_Status_ID) REFERENCES Seat_Status(Seat_Status_ID)
);

CREATE TABLE CoupleSeat (
    CoupleSeatId INT IDENTITY(1,1) PRIMARY KEY,
    FirstSeatId INT NOT NULL,
    SecondSeatId INT NOT NULL,
    -- Prevent mirrored duplicates
    CHECK (FirstSeatId < SecondSeatId),
    -- Prevent seat reuse
    CONSTRAINT UQ_CoupleSeat_First UNIQUE (FirstSeatId),
    CONSTRAINT UQ_CoupleSeat_Second UNIQUE (SecondSeatId),
    -- Prevent pairing the same seat with itself
    CHECK (FirstSeatId <> SecondSeatId),
    FOREIGN KEY (FirstSeatId) REFERENCES Seat(Seat_ID),
    FOREIGN KEY (SecondSeatId) REFERENCES Seat(Seat_ID)
);

INSERT INTO Seat_Type (Type_Name, Price_Percent, ColorHex) VALUES
('Normal', 40000, '#cccbc8'),
('VIP', 50000, '#fa7a7a'),
('Couple', 60000, '#ffa1f1'),
('Disabled', 0, '#2F2F2F');

INSERT INTO Seat_Status (Status_Name) VALUES
('Available'),
('Booked');

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
('AC001', '123 Main St', '2000-01-15', 'admin@gmail.com', 'Admin', 'Female', '123456789', '/image/profile.jpg', '1', '0123456789', '2023-01-01', 1, 1, 'admin'),
('AC002', '789 Oak St', '2002-11-10', 'member@gmail.com', 'Member', 'Female', '192837465', '/image/profile.jpg', '1', '0111222333', '2023-01-10', 3, 1, 'member'),
('AC003', '789 Oak St', '2002-11-10', 'member2@gmail.com', 'Member', 'Female', '132837465', '/image/profile.jpg', '1', '0111222333', '2023-01-10', 3, 0, 'member2'),
('AC004', '123 Street', '1999-01-01', 'minh.nguyen@example.com', 'Nguyen Hoang Minh', 'Male', '111111111', '/image/profile.jpg', '1', '0900000001', '2023-01-01', 2, 1, 'minhnguyen'),
('AC005', '123 Street', '1999-01-01', 'tue.phan@example.com', 'Phan Do Gia Tue', 'Male', '111111112', '/image/profile.jpg', '1', '0900000002', '2023-01-01', 2, 1, 'tuephan'),
('AC006', '123 Street', '1999-01-01', 'bao.nguyen@example.com', 'Nguyen Gia Bao', 'Male', '111111113', '/image/profile.jpg', '1', '0900000003', '2023-01-01', 2, 1, 'baonguyen'),
('AC007', '123 Street', '1999-01-01', 'quang.nguyen@example.com', 'Nguyen Quang Duy Quang', 'Male', '111111114', '/image/profile.jpg', '1', '0900000004', '2023-01-01', 2, 1, 'quangnguyen'),
('AC008', '123 Street', '1999-01-01', 'dat.nguyen@example.com', 'Nguyen Le Quoc Dat', 'Male', '111111115', '/image/profile.jpg', '1', '0900000005', '2023-01-01', 2, 1, 'datnguyen'),
('AC009', '123 Street', '1999-01-01', 'dat.thai@example.com', 'Thai Cong Dat', 'Male', '111111116', '/image/profile.jpg', '1', '0900000006', '2023-01-01', 2, 1, 'datthai');

INSERT INTO Member (Member_ID, Score, Account_ID) VALUES
('MB001', 10000000, 'AC002'),  
('MB002', 0,'AC003');  

-- Insert Employees
INSERT INTO Employee (Employee_ID, Account_ID) VALUES
('EM001', 'AC004'),
('EM002', 'AC005'),
('EM003', 'AC006'),
('EM004', 'AC007'),
('EM005', 'AC008'),
('EM006', 'AC009');

INSERT INTO Show_Dates (Show_Date, Date_Name) VALUES
('2024-03-20', 'Wednesday, March 20'),
('2024-03-21', 'Thursday, March 21'),
('2024-03-22', 'Friday, March 22'),
('2024-03-23', 'Saturday, March 23'),
('2024-03-24', 'Sunday, March 24'),
('2024-03-25', 'Monday, March 25'),
('2024-03-26', 'Tuesday, March 26'),
('2024-03-27', 'Wednesday, March 27'),
('2024-03-28', 'Thursday, March 28'),
('2024-03-29', 'Friday, March 29'),
('2024-03-30', 'Saturday, March 30'),
('2024-03-31', 'Sunday, March 31'),
('2024-04-01', 'Monday, April 1'),
('2024-04-02', 'Tuesday, April 2');

INSERT INTO Movie (Movie_ID, Actor, Content, Director, Duration, From_Date, Movie_Production_Company, To_Date, Version, Movie_Name_English, Movie_Name_VN, Large_Image, Small_Image, TrailerUrl)
VALUES
('MV001', 'Cillian Murphy, Emily Blunt', 'The story of American scientist J. Robert Oppenheimer and his role in the development of the atomic bomb.', 'Christopher Nolan', 180, '2023-07-21', 'Universal Pictures', '2024-03-25', 'IMAX', 'Oppenheimer', 'Oppenheimer', '/image/open.jpg', '/image/open.jpg', 'https://www.youtube.com/embed/uYPbbksJxIg'),
('MV002', 'Tom Holland, Zendaya', 'Peter Parker seeks help from Doctor Strange after his identity is revealed, leading to multiverse chaos.', 'Jon Watts', 148, '2024-03-20', 'Marvel Studios', '2024-04-25', '3D', 'Spider-Man: No Way Home', 'Người Nhện: Không Còn Nhà', '/image/spider.jpg', '/image/spider.jpg', 'https://www.youtube.com/embed/rt-2cxAiPJk'),
('MV003', 'Timothée Chalamet, Zendaya', 'Paul Atreides unites with the Fremen to seek revenge against the conspirators who destroyed his family.', 'Denis Villeneuve', 166, '2024-03-01', 'Legendary Pictures', '2024-03-21', 'IMAX 3D', 'Dune: Part Two', 'Hành Tinh Cát: Phần Hai', '/image/dune.jpg', '/image/dune.jpg', 'https://www.youtube.com/embed/Way9Dexny3w'),
('MV004', 'Margot Robbie, Ryan Gosling', 'Barbie suffers a crisis that leads her to question her world and her existence.', 'Greta Gerwig', 114, '2023-07-21', 'Warner Bros.', '2024-03-27', '2D', 'Barbie', 'Barbie', '/image/barbie.jpg', '/image/barbie.jpg', 'https://www.youtube.com/embed/pBk4NYhWNMM'),
('MV005', 'Michelle Yeoh, Ke Huy Quan', 'A woman is swept into a multiverse adventure where she must connect with different versions of herself.', 'Daniel Kwan, Daniel Scheinert', 139, '2024-03-25', 'A24', '2024-04-02', '2D', 'Everything Everywhere All at Once', 'Mọi Thứ Mọi Nơi Tất Cả Cùng Lúc', '/image/everything.jpg', '/image/everything.jpg', 'https://www.youtube.com/embed/wxN1T1uxQ2g'),
('MV006', 'Sam Worthington, Zoe Saldana', 'Jake Sully lives with his family on Pandora and must protect them from a new threat.', 'James Cameron', 192, '2024-03-27', '20th Century Studios', '2024-04-02', '3D', 'Avatar: The Way of Water', 'Avatar: Dòng Chảy Của Nước', '/image/avatar.jpg', '/image/avatar.jpg', 'https://www.youtube.com/embed/d9MyW72ELq0'),
('MV007', 'Robert Pattinson, Zoë Kravitz', 'Batman uncovers corruption in Gotham while pursuing the Riddler, a sadistic killer.', 'Matt Reeves', 176, '2024-03-28', 'Warner Bros.', '2024-04-02', '2D', 'The Batman', 'Người Dơi', '/image/batman.jpg', '/image/batman.jpg', 'https://www.youtube.com/embed/mqqft2x_Aa4'),
('MV008', 'Tom Cruise, Miles Teller', 'Pete "Maverick" Mitchell trains Top Gun graduates for a high-stakes mission.', 'Joseph Kosinski', 131, '2024-03-29', 'Paramount Pictures', '2024-04-02', 'IMAX', 'Top Gun: Maverick', 'Phi Công Siêu Đẳng Maverick', '/image/topgun.jpg', '/image/topgun.jpg', 'https://www.youtube.com/embed/giXco2jaZ_4'),
('MV009', 'Song Kang-ho, Choi Woo-shik', 'A poor family schemes to become employed by a wealthy family and infiltrate their household.', 'Bong Joon-ho', 132, '2024-03-30', 'CJ Entertainment', '2024-04-02', '2D', 'Parasite', 'Ký Sinh Trùng', '/image/parasite.jpg', '/image/parasite.jpg', 'https://www.youtube.com/embed/5xH0HfJHsaY');

INSERT INTO Schedule (Schedule_Time) VALUES
('09:00'), ('09:30'), ('10:00'), ('10:30'), ('11:00'),
('11:30'), ('12:00'), ('12:30'), ('13:00'), ('13:30'),
('14:00'), ('14:30'), ('15:00'), ('15:30'), ('16:00'),
('16:30'), ('17:00'), ('17:30'), ('18:00'), ('18:30'),
('19:00'), ('19:30'), ('20:00'), ('20:30'), ('21:00'),
('21:30'), ('22:00'), ('22:30');

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

INSERT INTO Movie_Show (Show_Date_ID, Schedule_ID, Movie_ID, Cinema_Room_ID) VALUES
(1, 1, 'MV001', 1), (1, 2, 'MV001', 3), (1, 3, 'MV001', 5), (1, 4, 'MV001', 1),
(1, 5, 'MV001', 3), (1, 6, 'MV001', 5), (1, 7, 'MV001', 1), (1, 8, 'MV001', 3),

(1, 1, 'MV002', 2), (1, 2, 'MV002', 4), (1, 3, 'MV002', 6), (1, 4, 'MV002', 2),
(1, 5, 'MV002', 4), (1, 6, 'MV002', 6), (1, 7, 'MV002', 2), (1, 8, 'MV002', 4),

(1, 9,  'MV003', 1), (1, 10, 'MV003', 3), (1, 11, 'MV003', 5), (1, 12, 'MV003', 1),
(1, 13, 'MV003', 3), (1, 14, 'MV003', 5), (1, 15, 'MV003', 1), (1, 16, 'MV003', 3),

(1, 17, 'MV004', 2), (1, 18, 'MV004', 4), (1, 19, 'MV004', 6), (1, 20, 'MV004', 2),
(1, 21, 'MV004', 4), (1, 22, 'MV004', 6), (1, 23, 'MV004', 2), (1, 24, 'MV004', 4),

(2, 1, 'MV001', 1), (2, 2, 'MV001', 3), (2, 3, 'MV001', 5), (2, 4, 'MV001', 1),
(2, 5, 'MV001', 3), (2, 6, 'MV001', 5), (2, 7, 'MV001', 1), (2, 8, 'MV001', 3),

(2, 1, 'MV002', 2), (2, 2, 'MV002', 4), (2, 3, 'MV002', 6), (2, 4, 'MV002', 2),
(2, 5, 'MV002', 4), (2, 6, 'MV002', 6), (2, 7, 'MV002', 2), (2, 8, 'MV002', 4),

(2, 9,  'MV003', 1), (2, 10, 'MV003', 3), (2, 11, 'MV003', 5), (2, 12, 'MV003', 1),
(2, 13, 'MV003', 3), (2, 14, 'MV003', 5), (2, 15, 'MV003', 1), (2, 16, 'MV003', 3),

(2, 17, 'MV004', 2), (2, 18, 'MV004', 4), (2, 19, 'MV004', 6), (2, 20, 'MV004', 2),
(2, 21, 'MV004', 4), (2, 22, 'MV004', 6), (2, 23, 'MV004', 2), (2, 24, 'MV004', 4),

(3, 1, 'MV005', 1), (3, 2, 'MV005', 3), (3, 3, 'MV005', 5), (3, 4, 'MV005', 1),
(3, 5, 'MV005', 3), (3, 6, 'MV005', 5), (3, 7, 'MV005', 1), (3, 8, 'MV005', 3),

(3, 1, 'MV006', 2), (3, 2, 'MV006', 4), (3, 3, 'MV006', 6), (3, 4, 'MV006', 2),
(3, 5, 'MV006', 4), (3, 6, 'MV006', 6), (3, 7, 'MV006', 2), (3, 8, 'MV006', 4),

(3, 9,  'MV007', 1), (3, 10, 'MV007', 3), (3, 11, 'MV007', 5), (3, 12, 'MV007', 1),
(3, 13, 'MV007', 3), (3, 14, 'MV007', 5), (3, 15, 'MV007', 1), (3, 16, 'MV007', 3),

(3, 17, 'MV008', 2), (3, 18, 'MV008', 4), (3, 19, 'MV008', 6), (3, 20, 'MV008', 2),
(3, 21, 'MV008', 4), (3, 22, 'MV008', 6), (3, 23, 'MV008', 2), (3, 24, 'MV008', 4),

(4, 1, 'MV005', 1), (4, 2, 'MV005', 3), (4, 3, 'MV005', 5), (4, 4, 'MV005', 1),
(4, 5, 'MV005', 3), (4, 6, 'MV005', 5), (4, 7, 'MV005', 1), (4, 8, 'MV005', 3),

(4, 1, 'MV006', 2), (4, 2, 'MV006', 4), (4, 3, 'MV006', 6), (4, 4, 'MV006', 2),
(4, 5, 'MV006', 4), (4, 6, 'MV006', 6), (4, 7, 'MV006', 2), (4, 8, 'MV006', 4),

(4, 9,  'MV007', 1), (4, 10, 'MV007', 3), (4, 11, 'MV007', 5), (4, 12, 'MV007', 1),
(4, 13, 'MV007', 3), (4, 14, 'MV007', 5), (4, 15, 'MV007', 1), (4, 16, 'MV007', 3),

(4, 17, 'MV008', 2), (4, 18, 'MV008', 4), (4, 19, 'MV008', 6), (4, 20, 'MV008', 2),
(4, 21, 'MV008', 4), (4, 22, 'MV008', 6), (4, 23, 'MV008', 2), (4, 24, 'MV008', 4),

(5, 1, 'MV009', 1), (5, 2, 'MV009', 2), (5, 3, 'MV009', 3), (5, 4, 'MV009', 4),
(5, 5, 'MV009', 5), (5, 6, 'MV009', 6), (5, 7, 'MV009', 1), (5, 8, 'MV009', 2),

(6, 9,  'MV009', 3), (6, 10, 'MV009', 4), (6, 11, 'MV009', 5), (6, 12, 'MV009', 6),
(6, 13, 'MV009', 1), (6, 14, 'MV009', 2), (6, 15, 'MV009', 3), (6, 16, 'MV009', 4);

CREATE TABLE Promotion (
    Promotion_ID INT PRIMARY KEY,
    Detail VARCHAR(255),
    Discount_Level INT,
    End_Time DATETIME,
    Image VARCHAR(255),
    Start_Time DATETIME,
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

CREATE TABLE Wishlist (
    Account_ID VARCHAR(10),
    Movie_ID VARCHAR(10),
    PRIMARY KEY (Account_ID, Movie_ID),
    FOREIGN KEY (Account_ID) REFERENCES Account(Account_ID),
    FOREIGN KEY (Movie_ID) REFERENCES Movie(Movie_ID)
);

-- Create Food table
CREATE TABLE Food (
    FoodId INT IDENTITY(1,1) PRIMARY KEY,
    Category VARCHAR(50) NOT NULL, -- food, drink, combo
    Name VARCHAR(255) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Description VARCHAR(500),
    Image VARCHAR(255),
    Status BIT NOT NULL DEFAULT 1, -- 1 = active, 0 = inactive
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedDate DATETIME
);

-- Insert sample food data
INSERT INTO Food (Category, Name, Price, Description, Status) VALUES
('food', 'Popcorn', 45000, 'Fresh buttered popcorn', 1),
('drink', 'Coca Cola', 25000, 'Cold Coca Cola 500ml', 1),
('combo', 'Popcorn + Coke', 65000, 'Popcorn with Coca Cola', 1),
('food', 'Nachos', 55000, 'Cheese nachos with salsa', 1),
('drink', 'Pepsi', 25000, 'Cold Pepsi 500ml', 1),
('combo', 'Nachos + Pepsi', 75000, 'Nachos with Pepsi', 1);