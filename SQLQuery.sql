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
    Rank_ID INT IDENTITY(1,1) NOT NULL,
	Rank_Name VARCHAR(50) NULL,
    Discount_Percentage DECIMAL(5,2) NULL,
    Required_Points INT NULL,
    PointEarningPercentage DECIMAL(5,2) NOT NULL DEFAULT (0),
    ColorGradient NVARCHAR(200) NOT NULL DEFAULT ('linear-gradient(135deg, #4e54c8 0%, #6c63ff 50%, #8f94fb 100%)'),
    IconClass NVARCHAR(50) NOT NULL DEFAULT ('fa-crown'),
    CONSTRAINT PK_Rank PRIMARY KEY CLUSTERED (Rank_ID ASC),
	CONSTRAINT UQ_Rank_RankName UNIQUE (Rank_Name)
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
	Total_Points INT NOT NULL DEFAULT (0),
    CONSTRAINT FK_Member_Account FOREIGN KEY (Account_ID) REFERENCES Account(Account_ID)
);

ALTER TABLE [dbo].[Member]
ADD CONSTRAINT [CK_Member_TotalPoints]
CHECK ([Total_Points] >= ISNULL([Score], 0));
GO

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
    Movie_Name_English VARCHAR(255),
    Movie_Name_VN VARCHAR(255),
    Large_Image VARCHAR(255),
    Small_Image VARCHAR(255),
	TrailerUrl VARCHAR(255)
);

CREATE TABLE Schedule (
    Schedule_ID INT PRIMARY KEY IDENTITY(1,1),
    Schedule_Time TIME
);

CREATE TABLE Type (
    Type_ID INT PRIMARY KEY,
    Type_Name VARCHAR(255)
);

CREATE TABLE Version (
    Version_ID INT PRIMARY KEY,
    Version_Name VARCHAR(255),
	Multi DECIMAL
);

CREATE TABLE Movie_Version (
    Movie_ID VARCHAR(10),
    Version_ID INT,
    PRIMARY KEY (Movie_ID, Version_ID),
    FOREIGN KEY (Movie_ID) REFERENCES Movie(Movie_ID),
    FOREIGN KEY (Version_ID) REFERENCES Version(Version_ID)
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
    Seat_Length INT,
	Version_ID INT,
	FOREIGN KEY (Version_ID) REFERENCES Version(Version_ID)
);

ALTER TABLE Cinema_Room
ADD Seat_Quantity AS (Seat_Width * Seat_Length);

CREATE TABLE Movie_Show (
    Movie_Show_ID INT PRIMARY KEY IDENTITY(1,1),
    Movie_ID VARCHAR(10) NOT NULL,
    Cinema_Room_ID INT NOT NULL,
    Show_Date DATE NOT NULL,
    Schedule_ID INT NOT NULL,
	Version_ID INT NOT NULL,
	FOREIGN KEY (Version_ID) REFERENCES Version(Version_ID),
    FOREIGN KEY (Movie_ID) REFERENCES Movie(Movie_ID),
    FOREIGN KEY (Cinema_Room_ID) REFERENCES Cinema_Room(Cinema_Room_ID),
    FOREIGN KEY (Schedule_ID) REFERENCES Schedule(Schedule_ID)
);

CREATE TABLE Voucher (
    Voucher_ID VARCHAR(10) PRIMARY KEY,           
    Account_ID VARCHAR(10) NOT NULL,               
    Code NVARCHAR(20) UNIQUE NOT NULL,
    Value DECIMAL(18,2) NOT NULL,
    CreatedDate DATETIME NOT NULL,
    ExpiryDate DATETIME NOT NULL,
    IsUsed BIT DEFAULT 0,
    Image VARCHAR(255) NULL,
    CONSTRAINT FK_Voucher_Account FOREIGN KEY (Account_ID) REFERENCES [dbo].[Account](Account_ID)
);

CREATE TABLE Invoice (
    Invoice_ID VARCHAR(10) PRIMARY KEY,
    Add_Score INT,
    BookingDate DATETIME,
    Status INT,
    RoleId INT,
    Total_Money DECIMAL,
    Use_Score INT,
    Seat VARCHAR(30),
    Account_ID VARCHAR(10),
    Movie_Show_Id INT, 
	Promotion_Discount INT DEFAULT 0,
	Voucher_ID VARCHAR(10) NULL,
    CONSTRAINT FK_Invoice_Account FOREIGN KEY (Account_ID) REFERENCES Account(Account_ID),
	FOREIGN KEY (Movie_Show_ID) REFERENCES Movie_Show(Movie_Show_ID),
	FOREIGN KEY (Voucher_ID) REFERENCES Voucher(Voucher_ID)
);

CREATE TABLE Seat_Type (
    Seat_Type_ID INT PRIMARY KEY IDENTITY(1,1),
    Type_Name VARCHAR(50),
	Price_Percent DECIMAL NOT NULL DEFAULT 100,
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
	Schedule_Seat_ID INT PRIMARY KEY IDENTITY(1,1),
    Movie_Show_ID INT,
	Invoice_ID VARCHAR(10),
    Seat_ID INT,
    Seat_Status_ID INT,
	HoldUntil DATETIME NULL,
    HoldBy NVARCHAR(100) NULL,
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
('Booked'),
('Held');

INSERT INTO Roles (Role_ID, Role_Name) VALUES
(1, 'Admin'),
(2, 'Employee'),
(3, 'Member');

SET IDENTITY_INSERT [dbo].[Rank] ON;
INSERT [dbo].[Rank] ([Rank_ID], [Rank_Name], [Discount_Percentage], [Required_Points], [PointEarningPercentage], [ColorGradient], [IconClass]) VALUES 
(1, 'Bronze', 0.00, 0, 5.00, 'linear-gradient(135deg, #804A00 0%, #B87333 50%, #CD7F32 100%)', 'fa-medal'),
(2, 'Gold', 5.00, 30000, 7.00, 'linear-gradient(135deg, #FFD700 0%, #FDB931 50%, #DAA520 100%)', 'fa-trophy'),
(3, 'Diamond', 10.00, 50000, 10.00, 'linear-gradient(135deg, #89CFF0 0%, #A0E9FF 50%, #B9F2FF 100%)', 'fa-gem'),
(4, 'Elite', 15.00, 80000, 12.00, 'linear-gradient(135deg, #1a1a1a 0%, #2C3E50 50%, #2c3e50 100%)', 'fa-star');
SET IDENTITY_INSERT [dbo].[Rank] OFF;
GO


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

INSERT INTO [dbo].[Member] ([Member_ID], [Score], [Account_ID], [Total_Points])
VALUES 
('MB001', 10000, 'AC002', 50000),
('MB002', 10000, 'AC003', 10000);
GO

-- Insert Employees
INSERT INTO Employee (Employee_ID, Account_ID) VALUES
('EM001', 'AC004'),
('EM002', 'AC005'),
('EM003', 'AC006'),
('EM004', 'AC007'),
('EM005', 'AC008'),
('EM006', 'AC009');

INSERT INTO Movie (Movie_ID, Actor, Content, Director, Duration, From_Date, Movie_Production_Company, To_Date, Movie_Name_English, Movie_Name_VN, Large_Image, Small_Image, TrailerUrl)
VALUES
('MV001', 'Cillian Murphy, Emily Blunt', 'The story of American scientist J. Robert Oppenheimer and his role in the development of the atomic bomb.', 'Christopher Nolan', 180, '2025-06-21', 'Universal Pictures', '2025-07-25', 'Oppenheimer', 'Oppenheimer', '/image/open.jpg', '/image/open.jpg', 'https://www.youtube.com/embed/uYPbbksJxIg'),
('MV002', 'Tom Holland, Zendaya', 'Peter Parker seeks help from Doctor Strange after his identity is revealed, leading to multiverse chaos.', 'Jon Watts', 148, '2025-06-20', 'Marvel Studios', '2025-07-25', 'Spider-Man: No Way Home', 'Người Nhện: Không Còn Nhà', '/image/spider.jpg', '/image/spider.jpg', 'https://www.youtube.com/embed/rt-2cxAiPJk'),
('MV003', 'Timothée Chalamet, Zendaya', 'Paul Atreides unites with the Fremen to seek revenge against the conspirators who destroyed his family.', 'Denis Villeneuve', 166, '2025-06-01', 'Legendary Pictures', '2025-07-21', 'Dune: Part Two', 'Hành Tinh Cát: Phần Hai', '/image/dune.jpg', '/image/dune.jpg', 'https://www.youtube.com/embed/Way9Dexny3w'),
('MV004', 'Margot Robbie, Ryan Gosling', 'Barbie suffers a crisis that leads her to question her world and her existence.', 'Greta Gerwig', 114, '2025-06-21', 'Warner Bros.', '2025-07-27', 'Barbie', 'Barbie', '/image/barbie.jpg', '/image/barbie.jpg', 'https://www.youtube.com/embed/pBk4NYhWNMM'),
('MV005', 'Michelle Yeoh, Ke Huy Quan', 'A woman is swept into a multiverse adventure where she must connect with different versions of herself.', 'Daniel Kwan, Daniel Scheinert', 139, '2025-06-25', 'A24', '2025-07-02', 'Everything Everywhere All at Once', 'Mọi Thứ Mọi Nơi Tất Cả Cùng Lúc', '/image/everything.jpg', '/image/everything.jpg', 'https://www.youtube.com/embed/wxN1T1uxQ2g'),
('MV006', 'Sam Worthington, Zoe Saldana', 'Jake Sully lives with his family on Pandora and must protect them from a new threat.', 'James Cameron', 192, '2025-06-27', '20th Century Studios', '2025-07-02', 'Avatar: The Way of Water', 'Avatar: Dòng Chảy Của Nước', '/image/avatar.jpg', '/image/avatar.jpg', 'https://www.youtube.com/embed/d9MyW72ELq0'),
('MV007', 'Robert Pattinson, Zoë Kravitz', 'Batman uncovers corruption in Gotham while pursuing the Riddler, a sadistic killer.', 'Matt Reeves', 176, '2025-06-28', 'Warner Bros.', '2025-08-02', 'The Batman', 'Người Dơi', '/image/batman.jpg', '/image/batman.jpg', 'https://www.youtube.com/embed/mqqft2x_Aa4'),
('MV008', 'Tom Cruise, Miles Teller', 'Pete "Maverick" Mitchell trains Top Gun graduates for a high-stakes mission.', 'Joseph Kosinski', 131, '2025-06-29', 'Paramount Pictures', '2025-08-02', 'Top Gun: Maverick', 'Phi Công Siêu Đẳng Maverick', '/image/topgun.jpg', '/image/topgun.jpg', 'https://www.youtube.com/embed/giXco2jaZ_4'),
('MV009', 'Song Kang-ho, Choi Woo-shik', 'A poor family schemes to become employed by a wealthy family and infiltrate their household.', 'Bong Joon-ho', 132, '2025-06-30', 'CJ Entertainment', '2025-08-02', 'Parasite', 'Ký Sinh Trùng', '/image/parasite.jpg', '/image/parasite.jpg', 'https://www.youtube.com/embed/5xH0HfJHsaY');

INSERT INTO Schedule (Schedule_Time) VALUES
('09:00:00'), ('09:30:00'), ('10:00:00'), ('10:30:00'), ('11:00:00'),
('11:30:00'), ('12:00:00'), ('12:30:00'), ('13:00:00'), ('13:30:00'),
('14:00:00'), ('14:30:00'), ('15:00:00'), ('15:30:00'), ('16:00:00'),
('16:30:00'), ('17:00:00'), ('17:30:00'), ('18:00:00'), ('18:30:00'),
('19:00:00'), ('19:30:00'), ('20:00:00'), ('20:30:00'), ('21:00:00'),
('21:30:00'), ('22:00:00'), ('22:30:00');

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

INSERT INTO Version (Version_ID, Version_Name, Multi) VALUES
(1, '2D', 1),
(2, '4DX', 1.5),	
(3, 'IMAX', 2);

INSERT INTO Movie_Version (Movie_ID, Version_ID) VALUES
('MV001', 1),
('MV001', 2),
('MV002', 1), 
('MV002', 2),
('MV003', 3),
('MV004', 2), 
('MV004', 3),
('MV005', 2),
('MV006', 2), 
('MV006', 3),
('MV007', 1),
('MV008', 1),
('MV009', 1), 
('MV009', 2);

INSERT INTO Cinema_Room(Cinema_Room_Name, Version_ID) VALUES
('Screen 1', 1), ('Screen 2', 1), ('Screen 3', 1), ('Screen 4', 2), ('Screen 5', 2), ('Screen 6', 2), ('Screen 7', 3);

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

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Food](
	[FoodId] [int] IDENTITY(1,1) NOT NULL,
	[Category] [varchar](50) NOT NULL,
	[Name] [varchar](255) NOT NULL,
	[Price] [decimal](18, 2) NOT NULL,
	[Description] [varchar](500) NULL,
	[Image] [varchar](255) NULL,
	[Status] [bit] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[UpdatedDate] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[FoodId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[Food] ON 

INSERT [dbo].[Food] ([FoodId], [Category], [Name], [Price], [Description], [Image], [Status], [CreatedDate], [UpdatedDate]) VALUES (1, N'food', N'Popcorn', CAST(45000.00 AS Decimal(18, 2)), N'Fresh buttered popcorn', N'/images/foods/af2a1d7e-d2ad-4b45-810b-c7390e0449d8_888adcde997a2f1fd25853e9916186b8.jpg', 1, CAST(N'2025-06-25T09:04:25.193' AS DateTime), CAST(N'2025-06-25T09:06:56.127' AS DateTime))
INSERT [dbo].[Food] ([FoodId], [Category], [Name], [Price], [Description], [Image], [Status], [CreatedDate], [UpdatedDate]) VALUES (4, N'food', N'Nachos', CAST(55000.00 AS Decimal(18, 2)), N'Cheese nachos with salsa', N'/images/foods/642a3374-88eb-4926-9542-8dd4fc765b30_RobloxScreenShot20250203_115037658.png', 1, CAST(N'2025-06-25T09:04:25.193' AS DateTime), CAST(N'2025-06-25T11:30:08.190' AS DateTime))
INSERT [dbo].[Food] ([FoodId], [Category], [Name], [Price], [Description], [Image], [Status], [CreatedDate], [UpdatedDate]) VALUES (5, N'drink', N'Pepsi', CAST(25000.00 AS Decimal(18, 2)), N'Cold Pepsi 500ml', NULL, 1, CAST(N'2025-06-25T09:04:25.193' AS DateTime), NULL)
INSERT [dbo].[Food] ([FoodId], [Category], [Name], [Price], [Description], [Image], [Status], [CreatedDate], [UpdatedDate]) VALUES (7, N'combo', N'cc1212', CAST(45000.00 AS Decimal(18, 2)), N'cc', N'/images/foods/c16079b3-f3ee-44e9-9256-abfbe3981d92_9899e932a02f459d3edfca15903b3ef8.jpg', 1, CAST(N'2025-06-25T09:08:46.057' AS DateTime), CAST(N'2025-06-25T10:08:59.063' AS DateTime))
INSERT [dbo].[Food] ([FoodId], [Category], [Name], [Price], [Description], [Image], [Status], [CreatedDate], [UpdatedDate]) VALUES (8, N'food', N'chim', CAST(87000.00 AS Decimal(18, 2)), N'hhh', N'/images/foods/0ef33541-8d5e-4aa2-b85a-0205eb81fdcc_eb2e9ccb079e26102b7427a94d4b3bc6.jpg', 1, CAST(N'2025-06-25T09:17:24.663' AS DateTime), CAST(N'2025-06-25T10:27:54.923' AS DateTime))
SET IDENTITY_INSERT [dbo].[Food] OFF
GO
ALTER TABLE [dbo].[Food] ADD  DEFAULT ((1)) FOR [Status]
GO
ALTER TABLE [dbo].[Food] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO

