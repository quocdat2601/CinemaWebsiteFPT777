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
    Cinema_Room_ID INT,
    Content VARCHAR(1000),
    Duration INT,
    From_Date DATE,
    Movie_Production_Company VARCHAR(255),
    To_Date DATE,
    Movie_Name_English VARCHAR(255),
    Large_Image VARCHAR(255),
    Small_Image VARCHAR(255),
	TrailerUrl VARCHAR(255)
);

CREATE TABLE Person (
    Person_ID INT PRIMARY KEY IDENTITY(1,1),
    Name VARCHAR(255) NOT NULL,
    Date_Of_Birth DATE,
    Nationality VARCHAR(100),
    Gender BIT,
    Image VARCHAR(255),
    IsDirector BIT DEFAULT 0,
    Description VARCHAR(1000)
);

CREATE TABLE Movie_Person (
    Movie_ID VARCHAR(10),
    Person_ID INT,
    PRIMARY KEY (Movie_ID, Person_ID),
    FOREIGN KEY (Movie_ID) REFERENCES Movie(Movie_ID) ON DELETE CASCADE,
    FOREIGN KEY (Person_ID) REFERENCES Person(Person_ID) ON DELETE CASCADE
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
    Version_ID INT PRIMARY KEY IDENTITY(1,1),
    Version_Name VARCHAR(255),
	Multi DECIMAL(10,2)
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

CREATE TABLE Status (
    Status_ID INT PRIMARY KEY IDENTITY(1,1),
    Status_Name VARCHAR(10),
);

CREATE TABLE Cinema_Room (
    Cinema_Room_ID INT PRIMARY KEY IDENTITY(1,1),
    Cinema_Room_Name VARCHAR(255),
    Seat_Width INT,
    Seat_Length INT,
	Version_ID INT,
    Status_ID INT DEFAULT 1,
    FOREIGN KEY (Status_ID) REFERENCES Status(Status_ID),
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

CREATE TABLE Invoice (
    Invoice_ID VARCHAR(10) PRIMARY KEY,
    Add_Score INT,
    BookingDate DATETIME,
    Status INT,
    Total_Money DECIMAL,
    Use_Score INT,
    Seat VARCHAR(30),
    Seat_IDs NVARCHAR(MAX) NULL,
    Account_ID VARCHAR(10),
    Movie_Show_Id INT, 
    Promotion_Discount NVARCHAR(1000) DEFAULT '0',
    Voucher_ID VARCHAR(10) NULL,
    Cancel BIT NOT NULL DEFAULT 0,         -- true/false, mặc định là false (chưa hủy)
    CancelDate DATETIME NULL,              -- ngày hủy, cho phép null
    CancelBy NVARCHAR(50) NULL,            -- người thực hiện hủy, cho phép null
	RankDiscountPercentage DECIMAL(5,2) NULL,
    CONSTRAINT FK_Invoice_Account FOREIGN KEY (Account_ID) REFERENCES Account(Account_ID),
	FOREIGN KEY (Movie_Show_ID) REFERENCES Movie_Show(Movie_Show_ID),
	FOREIGN KEY (Voucher_ID) REFERENCES Voucher(Voucher_ID)
);

CREATE TABLE Schedule_Seat (
	Schedule_Seat_ID INT PRIMARY KEY IDENTITY(1,1),
    Movie_Show_ID INT,
	Invoice_ID VARCHAR(10),
    Seat_ID INT,
    Seat_Status_ID INT,
	HoldUntil DATETIME NULL,
    HoldBy NVARCHAR(100) NULL,
    Booked_Price DECIMAL(18,2) NULL,
    FOREIGN KEY (Movie_Show_ID) REFERENCES Movie_Show(Movie_Show_ID) ON DELETE CASCADE,
    FOREIGN KEY (Seat_ID) REFERENCES Seat(Seat_ID),
	FOREIGN KEY (Invoice_ID) REFERENCES Invoice(Invoice_ID),
    FOREIGN KEY (Seat_Status_ID) REFERENCES Seat_Status(Seat_Status_ID),
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

INSERT INTO Movie (Movie_ID, Content, Duration, From_Date, Movie_Production_Company, To_Date, Movie_Name_English, Large_Image, Small_Image, TrailerUrl)
VALUES
('MV001', 'A gripping biographical drama chronicling the life of American theoretical physicist J. Robert Oppenheimer, exploring his moral struggles, political entanglements, and pivotal role in developing the atomic bomb during World War II.', 180, '2025-06-21', 'Universal Pictures', '2025-07-25', 'Oppenheimer', '/image/li-open.jpg', '/image/open.jpg', 'https://www.youtube.com/embed/uYPbbksJxIg'),

('MV002', 'Peter Parker, exposed as Spider-Man, seeks help from Doctor Strange to undo the chaos caused by his unmasked identity—triggering a rift in the multiverse that unleashes familiar foes and unexpected consequences.', 148, '2025-06-20', 'Marvel Studios', '2025-07-25', 'Spider-Man: No Way Home', '/image/li-spiderman.jpg', '/image/spider.jpg', 'https://www.youtube.com/embed/rt-2cxAiPJk'),

('MV003', 'The epic continuation of Paul Atreides’ journey as he joins the desert-dwelling Fremen and leads a bold revolt against those responsible for his family’s downfall. As destiny and prophecy collide, Paul must confront not only external threats but his own fate.', 166, '2025-06-01', 'Legendary Pictures', '2025-07-21', 'Dune: Part Two', '/image/li-dune.jpg', '/image/dune.jpg', 'https://www.youtube.com/embed/Way9Dexny3w'),

('MV004', 'In an imaginative and vibrant world, Barbie begins to question her perfect existence, leading her on a thought-provoking journey into the real world where she must grapple with identity, self-worth, and deeper meaning.', 114, '2025-06-21', 'Warner Bros.', '2025-07-27', 'Barbie', '/image/li-barbie.jpg', '/image/barbie.jpg', 'https://www.youtube.com/embed/pBk4NYhWNMM'),

('MV005', 'An eccentric sci-fi comedy where a disillusioned woman is pulled into an interdimensional war. She must navigate bizarre parallel lives to reconnect with her family, harness hidden strengths, and ultimately save all timelines from collapse.', 139, '2025-06-25', 'A24', '2025-07-02', 'Everything Everywhere All at Once', '/image/li-everything.jpg', '/image/everything.jpg', 'https://www.youtube.com/embed/wxN1T1uxQ2g'),

('MV006', 'Years after the events of Avatar, Jake Sully lives peacefully among the Na’vi on Pandora. But when an old enemy returns with dangerous new forces, Jake must fight to protect his family and their homeland in a spectacular battle for survival.', 192, '2025-06-27', '20th Century Studios', '2025-07-02', 'Avatar: The Way of Water', '/image/li-ava.jpg', '/image/avatar.jpg', 'https://www.youtube.com/embed/d9MyW72ELq0'),

('MV007', 'Gotham City’s dark and gritty underbelly is revealed as Batman faces a cryptic serial killer known as the Riddler. As the mystery unfolds, he uncovers corruption and secrets that shake the foundations of the city—and his own legacy.', 176, '2025-06-28', 'Warner Bros.', '2025-08-02', 'The Batman', '/image/li-batman.jpg', '/image/batman.jpg', 'https://www.youtube.com/embed/mqqft2x_Aa4'),

('MV008', 'Decades after his first mission, Pete "Maverick" Mitchell returns to train a new generation of Top Gun pilots for a nearly impossible task. As danger looms, Maverick confronts ghosts from his past in a heart-pounding aerial adventure.', 131, '2025-06-29', 'Paramount Pictures', '2025-08-02', 'Top Gun: Maverick', '/image/li-topgun.jpg', '/image/topgun.jpg', 'https://www.youtube.com/embed/giXco2jaZ_4'),

('MV009', 'A darkly comedic thriller where a cunning lower-class family schemes its way into employment within a wealthy household. What begins as petty deceit spirals into an unsettling clash of social classes, culminating in shocking consequences.', 132, '2025-06-30', 'CJ Entertainment', '2025-08-02', 'Parasite', '/image/li-parasite.jpg', '/image/parasite.jpg', 'https://www.youtube.com/embed/5xH0HfJHsaY');

INSERT INTO Person (Name, Date_Of_Birth, Nationality, Gender, Image, IsDirector, Description)
VALUES
('Cillian Murphy', '1976-05-25', 'Irish', 0, '/image/people/cillian.jpg', 0, 'Cillian Murphy is an Irish actor, widely recognized for his intense and transformative performances that bring a profound psychological depth to his characters. He has garnered significant praise for his leading roles, particularly as the brooding anti-hero Thomas Shelby in the hit television series "Peaky Blinders" and for his compelling portrayal of J. Robert Oppenheimer in Christopher Nolan''s biographical thriller "Oppenheimer." Murphy is known for his dedication to his craft, often immersing himself deeply in his roles, and maintains a relatively private personal life, preferring to focus on his work and family away from the public eye.'),
('Christopher Nolan', '1970-07-30', 'British-American', 0, '/image/people/nolan.jpg', 1, 'Christopher Nolan is a highly influential British-American filmmaker, celebrated for his intricate narratives, ambitious visual style, and thought-provoking themes that often explore concepts of time, memory, and identity. He has directed a string of iconic films, including the mind-bending "Inception," the epic space odyssey "Interstellar," the World War II drama "Dunkirk," and the historical drama "Oppenheimer." Nolan is renowned for his commitment to practical effects over CGI and his preference for shooting on film, making him a distinctive voice in contemporary cinema who champions the immersive theatrical experience.'),
('Tom Holland', '1996-06-01', 'British', 0, '/image/people/holland.jpg', 0, 'Tom Holland is a popular British actor, best known globally for his iconic portrayal of Peter Parker / Spider-Man in the Marvel Cinematic Universe (MCU). His energetic, relatable, and youthful depiction of the web-slinger has resonated with audiences worldwide. Beyond his superhero role, Holland has also demonstrated his versatility in dramas and other genres. He is known for his charismatic public persona, engaging extensively with his fans, and his impressive background in dance and gymnastics, which often aids his demanding action roles.'),
('Zendaya', '1996-09-01', 'American', 1, '/image/people/zendaya.jpg', 0, 'Zendaya is a prominent American actress and singer who has risen to international fame for her captivating performances, trendsetting style, and influential advocacy. She has received significant praise for her role as Rue Bennett in the HBO drama series "Euphoria," and has played significant roles in major film franchises, including MJ in the "Spider-Man" series within the MCU and Chani in Denis Villeneuve''s epic science fiction film "Dune." Zendaya is also recognized for using her platform to speak out on important social issues, including voter registration and racial justice.'),
('Greta Gerwig', '1983-08-04', 'American', 1, '/image/people/greta.jpg', 1, 'Greta Gerwig is a highly regarded American filmmaker and actress, celebrated for her distinctive voice and nuanced storytelling that often centers on female experiences and coming-of-age narratives. She initially gained recognition for her work in mumblecore films before transitioning to critical acclaim as a director. Gerwig''s directorial efforts, such as the compelling coming-of-age drama "Lady Bird" and the beautifully crafted adaptation of "Little Women," have earned her widespread praise. Her work on the immensely popular, record-breaking film "Barbie" further solidified her reputation as a groundbreaking and commercially successful director, known for blending sharp humor with poignant social commentary.'),
('Denis Villeneuve', '1967-10-03', 'Canadian', 0, '/image/people/denis.jpg', 1, 'Denis Villeneuve is a masterful Canadian director known for his visually stunning, meticulously crafted, and often intellectually profound science fiction and thriller films. His signature style often involves slow-burn narratives, atmospheric cinematography, and deep thematic exploration. Villeneuve''s impressive filmography includes the critically acclaimed alien invasion drama "Arrival," the visually breathtaking sequel "Blade Runner 2049," and the ambitious and immersive adaptation of Frank Herbert''s "Dune," which showcases his incredible world-building abilities. He is widely praised for his distinctive cinematic vision and ability to deliver compelling, thought-provoking experiences on a grand scale.'),
('Michelle Yeoh', '1962-08-06', 'Malaysian', 1, '/image/people/yeoh.jpg', 0, 'Michelle Yeoh is an internationally acclaimed Malaysian actress, renowned for her martial arts prowess, captivating screen presence, and powerful dramatic performances. With a long-standing presence in cinema, she has starred in iconic films such as Ang Lee''s wuxia masterpiece "Crouching Tiger, Hidden Dragon," the James Bond film "Tomorrow Never Dies," and the mind-bending multiverse adventure "Everything Everywhere All at Once." Yeoh is celebrated for her versatility, her groundbreaking contributions to representation in Hollywood, and her continued advocacy for diversity in film.'),
('Stephanie Hsu', '1990-11-25', 'American', 0, '/image/people/hsu.jpg', 0, 'Stephanie Hsu is a talented American actress who has recently garnered significant attention for her breakout roles, demonstrating remarkable range and comedic timing. She received widespread critical acclaim for her scene-stealing dual performance as Joy Wang and the nihilistic Jobu Tupaki in the Daniels'' multiverse hit "Everything Everywhere All at Once." Prior to her film success, Hsu was also recognized for her recurring role as Mei Lin in the popular Amazon Prime Video series "The Marvelous Mrs. Maisel," showcasing her ability to effortlessly transition between different genres and character types.'),
('James Cameron', '1954-08-16', 'Canadian', 0, '/image/people/cameron.jpg', 1, 'James Cameron is a legendary Canadian filmmaker and innovator, often dubbed the "blockbuster king" for his groundbreaking work in science fiction and epic productions that push the boundaries of cinematic technology. He is responsible for directing some of the highest-grossing films of all time, including the romantic disaster epic "Titanic" and the visually revolutionary science fiction film "Avatar" and its sequels. Beyond his directing, he is known for his deep-sea exploration, environmental activism, and passion for technological advancement, constantly seeking to innovate both on and off screen.'),
('Zoe Saldana', '1978-06-19', 'American', 1, '/image/people/zoe.jpg', 0, 'Zoe Saldana is a prominent American actress, widely recognized for her roles in some of the highest-grossing film franchises in cinematic history, making her one of the most bankable stars in Hollywood. She is a key figure in major science fiction epics, having portrayed Neytiri in James Cameron''s "Avatar" and its sequels, and Gamora in the Marvel Cinematic Universe''s "Guardians of the Galaxy" series and "Avengers" films. Saldana is celebrated for her ability to embody complex characters within fantastical worlds and her commitment to physical roles, consistently delivering compelling performances across a variety of genres. She also actively champions diverse representation in media.'),
('Paul Dano', '1984-06-19', 'American', 0, '/image/people/dano.jpg', 0, 'Paul Dano is a highly respected American actor known for his intense, often quirky, and deeply committed performances that bring a unique psychological depth to his characters. He has received widespread critical acclaim for his memorable roles, including his powerful dual performance as the fervent preacher Eli Sunday and his meek brother Paul in Paul Thomas Anderson''s "There Will Be Blood," and his chilling, unsettling portrayal of The Riddler in Matt Reeves''s "The Batman." Dano is celebrated for his unique screen presence, his meticulous approach to character development, and his consistent choice of challenging and thought-provoking projects, often showcasing a quiet intensity.'),
('Matt Reeves', '1966-04-27', 'American', 0, '/image/people/reeves.jpg', 1, 'Matt Reeves is a notable American director and screenwriter, acclaimed for his ability to craft compelling and atmospheric genre films with a strong emphasis on character. He gained widespread recognition for directing the found-footage monster film "Cloverfield" and for his work on the critically lauded "Planet of the Apes" trilogy, particularly "Dawn of the Planet of the Apes" and "War for the Planet of the Apes," which showcased his skill in balancing action with emotional depth and sophisticated motion-capture performances. Reeves further cemented his reputation with the noir-inspired superhero film "The Batman," praised for its gritty realism and psychological intensity, demonstrating his talent for reinventing established franchises with a distinctive directorial voice.'),
('Bong Joon-ho', '1969-09-14', 'South Korean', 0, '/image/people/bong.jpg', 1, 'Bong Joon-ho is a celebrated South Korean film director and screenwriter, globally recognized for his masterful blend of genres, incisive social commentary, and darkly humorous storytelling that often critiques class disparity. He achieved international acclaim and made history with his film "Parasite," which brought global attention to Korean cinema. His diverse filmography also includes the critically praised monster film "The Host," the dystopian thriller "Snowpiercer," and the crime drama "Memories of Murder," all showcasing his unique vision, meticulous planning, and ability to craft compelling narratives that resonate deeply with audiences worldwide.'),
('Song Kang-ho', '1967-01-17', 'South Korean', 0, '/image/people/song.jpg', 0, 'Song Kang-ho is a legendary South Korean actor, widely considered one of the finest and most versatile actors of his generation, known for his subtle yet powerful performances. He is a frequent and iconic collaborator with director Bong Joon-ho, having delivered unforgettable performances in many of Bong''s films, including the lead role as the patriarch in "Parasite," the detective in the acclaimed "Memories of Murder," and the lead in "The Host." Song is celebrated for his incredible range, his nuanced portrayals of complex, often ordinary characters, and his ability to convey a wide spectrum of emotions with remarkable authenticity, making him a cornerstone of modern Korean cinema.')
;

INSERT INTO Movie_Person (Movie_ID, Person_ID)
VALUES
('MV001', 1), -- Cillian Murphy in Oppenheimer
('MV001', 2), -- Christopher Nolan as director
('MV002', 3), -- Tom Holland in Spider-Man
('MV002', 4), -- Zendaya in Spider-Man
('MV003', 4), -- Zendaya in Dune
('MV004', 5), -- Greta Gerwig as director for Barbie
('MV003', 6), -- Denis Villeneuve
('MV005', 7), -- Michelle Yeoh
('MV005', 8), -- Stephanie Hsu
('MV006', 9), -- James Cameron
('MV006', 10), -- Zoe Saldana
('MV007', 11), -- Paul Dano
('MV007', 12), -- Matt Reeves
('MV009', 13), -- Bong Joon-ho
('MV009', 14); -- Song Kang-ho

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

INSERT INTO Version (Version_Name, Multi) VALUES
('2D', 1),
('4DX', 1.5),	
('IMAX', 2.0);

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

INSERT INTO Status (Status_Name) VALUES
('Active'), ('Deleted'), ('Hidden');

INSERT INTO Cinema_Room (Cinema_Room_Name, Seat_Width, Seat_Length, Version_ID) VALUES
('Screen 1', 10, 20, 1),
('Screen 2', 10, 20, 1),
('Screen 3', 10, 20, 1),
('Screen 4', 12, 22, 2),
('Screen 5', 12, 22, 2),
('Screen 6', 12, 22, 2),
('Screen 7', 14, 24, 3);

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

CREATE TABLE FoodInvoice (
    FoodInvoice_ID INT IDENTITY(1,1) PRIMARY KEY,
    Invoice_ID VARCHAR(10) NOT NULL,
    Food_ID INT NOT NULL,
    Quantity INT NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_FoodInvoice_Invoice FOREIGN KEY (Invoice_ID) REFERENCES Invoice(Invoice_ID),
    CONSTRAINT FK_FoodInvoice_Food FOREIGN KEY (Food_ID) REFERENCES Food(FoodId)
);

-- Add indexes for better performance
CREATE INDEX IX_FoodInvoice_InvoiceID ON FoodInvoice(Invoice_ID);
CREATE INDEX IX_FoodInvoice_FoodID ON FoodInvoice(Food_ID);
