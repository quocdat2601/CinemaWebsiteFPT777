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
    Schedule_ID INT PRIMARY KEY IDENTITY(1,1),
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
    Cinema_Room_ID INT PRIMARY KEY IDENTITY(1,1),
    Cinema_Room_Name VARCHAR(255),
    Seat_Width INT,
    Seat_Length INT
);

ALTER TABLE Cinema_Room
ADD Seat_Quantity AS (Seat_Width * Seat_Length);

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

INSERT INTO Cinema_Room (Cinema_Room_Name, Seat_Width, Seat_Length)
VALUES ('Room 1', 5, 4);

INSERT INTO Seat_Type (Type_Name) VALUES
('Normal'),
('VIP'),
('Couple');

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

INSERT INTO Show_Dates (Show_Date, Date_Name) VALUES
('2025-06-01', 'Sunday Premiere'),
('2025-06-02', 'Monday Matinee');

INSERT INTO Movie (Movie_ID, Actor, Cinema_Room_ID, Content, Director, Duration, From_Date, Movie_Production_Company, To_Date, Version, Movie_Name_English, Movie_Name_VN, Large_Image, Small_Image)
VALUES
('MV001', 'Cillian Murphy, Emily Blunt', 1, 'The story of American scientist J. Robert Oppenheimer and his role in the development of the atomic bomb.', 'Christopher Nolan', 180, '2023-07-21', 'Universal Pictures', '2023-09-21', 'IMAX', 'Oppenheimer', 'Oppenheimer', '/image/open.jpg', '/image/open.jpg'),
('MV002', 'Tom Holland, Zendaya', 2, 'Peter Parker seeks help from Doctor Strange after his identity is revealed, leading to multiverse chaos.', 'Jon Watts', 148, '2021-12-17', 'Marvel Studios', '2022-02-17', '3D', 'Spider-Man: No Way Home', 'Người Nhện: Không Còn Nhà', '/image/spider.jpg', '/image/spider.jpg'),
('MV003', 'Timothée Chalamet, Zendaya', 3, 'Paul Atreides unites with the Fremen to seek revenge against the conspirators who destroyed his family.', 'Denis Villeneuve', 166, '2024-03-01', 'Legendary Pictures', '2024-05-01', 'IMAX 3D', 'Dune: Part Two', 'Hành Tinh Cát: Phần Hai', '/image/dune.jpg', '/image/dune.jpg'),
('MV004', 'Margot Robbie, Ryan Gosling', 4, 'Barbie suffers a crisis that leads her to question her world and her existence.', 'Greta Gerwig', 114, '2023-07-21', 'Warner Bros.', '2023-09-21', '2D', 'Barbie', 'Barbie', '/image/barbie.jpg', '/image/barbie.jpg'),
('MV005', 'Michelle Yeoh, Ke Huy Quan', 5, 'A woman is swept into a multiverse adventure where she must connect with different versions of herself.', 'Daniel Kwan, Daniel Scheinert', 139, '2022-03-11', 'A24', '2022-05-11', '2D', 'Everything Everywhere All at Once', 'Mọi Thứ Mọi Nơi Tất Cả Cùng Lúc', '/image/everything.jpg', '/image/everything.jpg'),
('MV006', 'Sam Worthington, Zoe Saldana', 1, 'Jake Sully lives with his family on Pandora and must protect them from a new threat.', 'James Cameron', 192, '2022-12-16', '20th Century Studios', '2023-02-16', '3D', 'Avatar: The Way of Water', 'Avatar: Dòng Chảy Của Nước', '/image/avatar.jpg', '/image/avatar.jpg'),
('MV007', 'Robert Pattinson, Zoë Kravitz', 2, 'Batman uncovers corruption in Gotham while pursuing the Riddler, a sadistic killer.', 'Matt Reeves', 176, '2022-03-04', 'Warner Bros.', '2022-05-04', '2D', 'The Batman', 'Người Dơi', '/image/batman.jpg', '/image/batman.jpg'),
('MV008', 'Tom Cruise, Miles Teller', 3, 'Pete "Maverick" Mitchell trains Top Gun graduates for a high-stakes mission.', 'Joseph Kosinski', 131, '2022-05-27', 'Paramount Pictures', '2022-07-27', 'IMAX', 'Top Gun: Maverick', 'Phi Công Siêu Đẳng Maverick', '/image/topgun.jpg', '/image/topgun.jpg'),
('MV009', 'Song Kang-ho, Choi Woo-shik', 4, 'A poor family schemes to become employed by a wealthy family and infiltrate their household.', 'Bong Joon-ho', 132, '2019-05-30', 'CJ Entertainment', '2019-07-30', '2D', 'Parasite', 'Ký Sinh Trùng', '/image/parasite.jpg', '/image/parasite.jpg');

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

INSERT INTO Schedule (Schedule_Time) VALUES
('10:00'),
('12:00'),
('14:00'),
('16:00'),
('18:00'),
('20:00');

INSERT INTO Movie_Schedule (Movie_ID, Schedule_ID) VALUES
('MV001', 1),
('MV001', 2),
('MV002', 3),
('MV003', 4), 
('MV003', 5),
('MV004', 1),
('MV005', 1), 
('MV005', 2),
('MV006', 3),
('MV007', 3), 
('MV007', 4), 
('MV007', 5),
('MV008', 1),
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
