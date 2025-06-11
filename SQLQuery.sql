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
	Price_Percent INT NOT NULL DEFAULT 100,
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

CREATE TABLE Schedule_Seat (
    Schedule_ID INT,
    Seat_ID INT,
    Seat_Status_ID INT,
    PRIMARY KEY (Schedule_ID, Seat_ID),
    FOREIGN KEY (Schedule_ID) REFERENCES Schedule(Schedule_ID) ON DELETE CASCADE,
    FOREIGN KEY (Seat_ID) REFERENCES Seat(Seat_ID),
    FOREIGN KEY (Seat_Status_ID) REFERENCES Seat_Status(Seat_Status_ID)
);

CREATE TABLE Ticket (
    Ticket_ID INT PRIMARY KEY IDENTITY(1,1),
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

INSERT INTO Cinema_Room(Cinema_Room_Name, Seat_Length, Seat_Width) VALUES
('Screen 1', 10, 8),
('Screen 2', 10, 8),
('Screen 3', 10, 8),
('Screen 4', 10, 8);

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
('10:00'),
('12:00'),
('14:00'),
('16:00'),
('18:00'),
('20:00');

INSERT INTO Movie_Show (Movie_ID, Show_Date_ID, Schedule_ID, Cinema_Room_ID) VALUES
('MV001', 1, 1, 1),  -- March 20, 10:00
('MV001', 1, 4, 1),  -- March 20, 16:00
('MV001', 2, 2, 1),  -- March 21, 12:00
('MV002', 1, 1, 2),  -- March 20, 10:00 (not conflicting with Screen 1)
('MV002', 2, 3, 2),  -- March 21, 14:00
('MV002', 3, 5, 2),  -- March 22, 18:00
('MV003', 1, 3, 1),  -- March 20, 14:00 (Screen 1 is free then)
('MV003', 2, 6, 1),  -- March 21, 20:00
('MV004', 1, 6, 3),  -- March 20, 20:00
('MV004', 2, 4, 3),  -- March 21, 16:00
('MV004', 3, 2, 3),  -- March 22, 12:00
('MV005', 6, 2, 2),  -- March 25, 12:00 (Screen 2 is free at that time)
('MV005', 6, 6, 3),  -- March 25, 20:00 (Screen 3 free at night)
('MV006', 2, 1, 4),  -- March 21, 10:00
('MV006', 2, 5, 4),  -- March 21, 18:00
('MV006', 3, 6, 4),  -- March 22, 20:00
('MV007', 9, 2, 4),  -- March 28, 12:00 (Screen 4 is free)
('MV007', 10, 4, 4), -- March 29, 16:00
('MV008', 10, 1, 1), -- March 29, 10:00
('MV008', 11, 5, 1), -- March 30, 18:00
('MV009', 11, 3, 3), -- March 30, 14:00
('MV009', 12, 1, 3); -- March 31, 10:00

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

INSERT INTO Invoice (
    Invoice_ID, Add_Score, BookingDate, MovieName, Schedule_Show,
    Schedule_Show_Time, Status, Total_Money, Use_Score, Seat, Account_ID
) VALUES
('INV001', 10, '2024-03-20', 'Avengers: Endgame', '2024-03-23', '18:00', 1, 150000, 0, 'A1', 'AC001'),
('INV002', 5, '2024-03-21', 'Kung Fu Panda 4', '2024-03-24', '20:00', 1, 100000, 10, 'B5', 'AC002'),
('INV003', 7, '2024-03-22', 'Interstellar', '2024-03-25', '19:30', 0, 120000, 5, 'C2', 'AC003'),
('INV004', 8, '2024-03-22', 'Inception', '2024-03-26', '21:00', 1, 130000, 0, 'D4', 'AC001'),
('INV005', 0, '2024-03-23', 'The Dark Knight', '2024-03-23', '17:00', 2, 90000, 15, 'E1', 'AC004'),
('INV2001', 10, '2025-06-01', 'Inside Out 2', '2025-06-01', '18:00', 1, 120000, 0, 'A5', 'AC002'),
('INV2002', 0, '2025-06-02', 'Kungfu Panda 4', '2025-06-02', '20:30', 1, 100000, 5, 'B2', 'AC002'),
('INV2003', 15, '2025-06-03', 'Dune: Part Two', '2025-06-03', '14:00', 1, 150000, 0, 'C1', 'AC002'),
('INV2004', 0, '2025-06-04', 'Godzilla x Kong', '2025-06-04', '16:00', 1, 110000, 8, 'A2', 'AC002'),
('INV2005', 20, '2025-06-05', 'Fast & Furious 10', '2025-06-05', '19:00', 1, 130000, 0, 'D5', 'AC002');

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
