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
    CONSTRAINT FK_Account_Role FOREIGN KEY (Role_ID) REFERENCES Roles(Role_ID)
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

INSERT INTO Roles (Role_ID, Role_Name) VALUES
(1, 'Admin'),
(2, 'Employee'),
(3, 'Member');

INSERT INTO Account (Account_ID, Address, Date_Of_Birth, Email, Full_Name, Gender, Identity_Card, Image, Password, Phone_Number, Register_Date, Role_ID, STATUS, USERNAME) VALUES
('A004', '123 Main St', '2000-01-15', 'admin@gmail.com', 'Admin', 'Female', '123456789', 'admin.jpg', '1', '0123456789', '2023-01-01', 1, 1, 'admin'),
('A005', '456 Elm St', '1995-06-25', 'employee@gmail.com', 'Employee', 'Male', '987654321', 'bob.jpg', '1', '0987654321', '2023-01-05', 2, 1, 'employee'),
('A006', '789 Oak St', '2002-11-10', 'member@gmail.com', 'Member', 'Female', '192837465', 'carol.jpg', '1', '0111222333', '2023-01-10', 3, 1, 'member');

INSERT INTO Employee (Employee_ID, Account_ID) VALUES
('E001', 'A005');

INSERT INTO Member (Member_ID, Score, Account_ID) VALUES
('M001', 120, 'A006');

INSERT INTO Show_Dates (Show_Date_ID, Show_Date, Date_Name) VALUES
(1, '2025-06-01', 'Sunday Premiere'),
(2, '2025-06-02', 'Monday Matinee');


INSERT INTO Movie (Movie_ID, Actor, Cinema_Room_ID, Content, Director, Duration, From_Date, Movie_Production_Company, To_Date, Version, Movie_Name_English, Movie_Name_VN, Large_Image, Small_Image) 
VALUES
('M001', 'Actor A, Actor B', 1, 'Italian Brainrot', 'Director X', 120, '2025-06-01', 'Xaml Studios', '2025-06-30', '2D', 'Tralalero Tralala', 'Tralalero Tralala', '/image/shark.jpg', '/image/shark.jpg'),
('M002', 'Actor A, Actor B', 1, 'Italian Brainrot', 'Director X', 120, '2025-06-01', 'Xaml Studios', '2025-06-30', '2D', 'Bombardiro Crocodilo', 'Bombardiro Crocodilo', '/image/crocodile.jpg', '/image/crocodile.jpg'),
('M003', 'Actor A, Actor B', 1, 'Italian Brainrot', 'Director X', 120, '2025-06-01', 'Xaml Studios', '2025-06-30', '2D', 'Tung Tung Sahur', 'Tung Tung Sahur', '/image/tung.jpg', '/image/tung.jpg'),
('M004', 'Actor A, Actor B', 1, 'Italian Brainrot', 'Director X', 120, '2025-06-01', 'Xaml Studios', '2025-06-30', '2D', 'Tralalero Tralala', 'Tralalero Tralala', '/image/shark.jpg', '/image/shark.jpg'),
('M005', 'Actor A, Actor B', 1, 'Italian Brainrot', 'Director X', 120, '2025-06-01', 'Xaml Studios', '2025-06-30', '2D', 'Bombardiro Crocodilo', 'Bombardiro Crocodilo', '/image/crocodile.jpg', '/image/crocodile.jpg'),
('M006', 'Actor A, Actor B', 1, 'Italian Brainrot', 'Director X', 120, '2025-06-01', 'Xaml Studios', '2025-06-30', '2D', 'Tung Tung Sahur', 'Tung Tung Sahur', '/image/tung.jpg', '/image/tung.jpg'),
('M007', 'Actor A, Actor B', 1, 'Italian Brainrot', 'Director X', 120, '2025-06-01', 'Xaml Studios', '2025-06-30', '2D', 'Tralalero Tralala', 'Tralalero Tralala', '/image/shark.jpg', '/image/shark.jpg'),
('M008', 'Actor A, Actor B', 1, 'Italian Brainrot', 'Director X', 120, '2025-06-01', 'Xaml Studios', '2025-06-30', '2D', 'Bombardiro Crocodilo', 'Bombardiro Crocodilo', '/image/crocodile.jpg', '/image/crocodile.jpg'),
('M009', 'Actor A, Actor B', 1, 'Italian Brainrot', 'Director X', 120, '2025-06-01', 'Xaml Studios', '2025-06-30', '2D', 'Tung Tung Sahur', 'Tung Tung Sahur', '/image/tung.jpg', '/image/tung.jpg'),
('M0010', 'Actor A, Actor B', 1, 'Italian Brainrot', 'Director X', 120, '2025-06-01', 'Xaml Studios', '2025-06-30', '2D', 'Tralalero Tralala', 'Tralalero Tralala', '/image/shark.jpg', '/image/shark.jpg'),
('M0011', 'Actor A, Actor B', 1, 'Italian Brainrot', 'Director X', 120, '2025-06-01', 'Xaml Studios', '2025-06-30', '2D', 'Bombardiro Crocodilo', 'Bombardiro Crocodilo', '/image/crocodile.jpg', '/image/crocodile.jpg'),
('M0012', 'Actor A, Actor B', 1, 'Italian Brainrot', 'Director X', 120, '2025-06-01', 'Xaml Studios', '2025-06-30', '2D', 'Tung Tung Sahur', 'Tung Tung Sahur', '/image/tung.jpg', '/image/tung.jpg');

INSERT INTO Movie_Date (Movie_ID, Show_Date_ID) VALUES
('M001', 1),
('M001', 2);

INSERT INTO Schedule (Schedule_ID, Schedule_Time) VALUES
(1, '10:00'),
(2, '14:00'),
(3, '18:00');

INSERT INTO Movie_Schedule (Movie_ID, Schedule_ID) VALUES
('M001', 1),
('M001', 2);

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
('M001', 1),
('M001', 2);

INSERT INTO Cinema_Room (Cinema_Room_ID, Cinema_Room_Name, Seat_Quantity) VALUES
(1, 'Main Hall', 100),
(2, 'FPT University', 100),
(3, 'F-Town', 100);

INSERT INTO Seat (Seat_ID, Cinema_Room_ID, Seat_Column, Seat_Row, Seat_Status, Seat_Type) VALUES
(1, 1, 'A', 1, 0, 1),
(2, 1, 'A', 2, 0, 1),
(3, 1, 'B', 1, 1, 2); -- 1 for booked, 2 for VIP

INSERT INTO Promotion (Promotion_ID, Detail, Discount_Level, End_Time, Image, Start_Time, Title, Is_Active) VALUES
(1, 'Giảm giá năm mới', 20, '2025-12-31 23:59:59', 'newyear.jpg', '2025-12-01 00:00:00', 'Khuyến mãi Năm Mới', 1),
(2, 'Ưu đãi cuối tuần', 15, '2025-06-30 22:00:00', 'weekend.jpg', '2025-06-01 08:00:00', 'Khuyến mãi Cuối Tuần', 0);
