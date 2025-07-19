USE [master]
GO
/****** Object:  Database [MovieTheater]    Script Date: 7/19/2025 5:34:45 PM ******/
CREATE DATABASE [MovieTheater]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'MovieTheater', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\MovieTheater.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'MovieTheater_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\MovieTheater_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO
ALTER DATABASE [MovieTheater] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [MovieTheater].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [MovieTheater] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [MovieTheater] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [MovieTheater] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [MovieTheater] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [MovieTheater] SET ARITHABORT OFF 
GO
ALTER DATABASE [MovieTheater] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [MovieTheater] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [MovieTheater] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [MovieTheater] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [MovieTheater] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [MovieTheater] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [MovieTheater] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [MovieTheater] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [MovieTheater] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [MovieTheater] SET  ENABLE_BROKER 
GO
ALTER DATABASE [MovieTheater] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [MovieTheater] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [MovieTheater] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [MovieTheater] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [MovieTheater] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [MovieTheater] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [MovieTheater] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [MovieTheater] SET RECOVERY FULL 
GO
ALTER DATABASE [MovieTheater] SET  MULTI_USER 
GO
ALTER DATABASE [MovieTheater] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [MovieTheater] SET DB_CHAINING OFF 
GO
ALTER DATABASE [MovieTheater] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [MovieTheater] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [MovieTheater] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [MovieTheater] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [MovieTheater] SET QUERY_STORE = ON
GO
ALTER DATABASE [MovieTheater] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [MovieTheater]
GO
/****** Object:  Table [dbo].[Account]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Account](
	[Account_ID] [varchar](10) NOT NULL,
	[Address] [varchar](255) NULL,
	[Date_Of_Birth] [date] NULL,
	[Email] [varchar](255) NULL,
	[Full_Name] [varchar](255) NULL,
	[Gender] [varchar](255) NULL,
	[Identity_Card] [varchar](255) NULL,
	[Image] [varchar](255) NULL,
	[Password] [varchar](255) NULL,
	[Phone_Number] [varchar](255) NULL,
	[Register_Date] [date] NULL,
	[Role_ID] [int] NULL,
	[STATUS] [int] NULL,
	[USERNAME] [varchar](50) NULL,
	[Rank_ID] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Account_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Cinema_Room]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Cinema_Room](
	[Cinema_Room_ID] [int] IDENTITY(1,1) NOT NULL,
	[Cinema_Room_Name] [varchar](255) NULL,
	[Seat_Width] [int] NULL,
	[Seat_Length] [int] NULL,
	[Version_ID] [int] NULL,
	[Seat_Quantity]  AS ([Seat_Width]*[Seat_Length]),
	[Status_ID] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Cinema_Room_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ConditionType]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ConditionType](
	[ConditionType_ID] [int] NOT NULL,
	[Name] [varchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ConditionType_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CoupleSeat]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CoupleSeat](
	[CoupleSeatId] [int] IDENTITY(1,1) NOT NULL,
	[FirstSeatId] [int] NOT NULL,
	[SecondSeatId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CoupleSeatId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Employee]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Employee](
	[Employee_ID] [varchar](10) NOT NULL,
	[Account_ID] [varchar](10) NULL,
PRIMARY KEY CLUSTERED 
(
	[Employee_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Food]    Script Date: 7/19/2025 5:34:45 PM ******/
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
/****** Object:  Table [dbo].[FoodInvoice]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FoodInvoice](
	[FoodInvoice_ID] [int] IDENTITY(1,1) NOT NULL,
	[Invoice_ID] [varchar](10) NOT NULL,
	[Food_ID] [int] NOT NULL,
	[Quantity] [int] NOT NULL,
	[Price] [decimal](18, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FoodInvoice_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Invoice]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Invoice](
	[Invoice_ID] [varchar](10) NOT NULL,
	[Add_Score] [int] NULL,
	[BookingDate] [datetime] NULL,
	[Status] [int] NULL,
	[Total_Money] [decimal](18, 0) NULL,
	[Use_Score] [int] NULL,
	[Seat] [varchar](30) NULL,
	[Account_ID] [varchar](10) NULL,
	[Movie_Show_Id] [int] NULL,
	[Promotion_Discount] [nvarchar](100) NULL,
	[Voucher_ID] [varchar](10) NULL,
	[RankDiscountPercentage] [decimal](5, 2) NULL,
	[Seat_IDs] [nvarchar](max) NULL,
	[Cancel] [bit] NOT NULL,
	[CancelDate] [datetime] NULL,
	[CancelBy] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[Invoice_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Member]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Member](
	[Member_ID] [varchar](10) NOT NULL,
	[Score] [int] NULL,
	[Account_ID] [varchar](10) NULL,
	[Total_Points] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Member_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Movie]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Movie](
	[Movie_ID] [varchar](10) NOT NULL,
	[Actor] [varchar](255) NULL,
	[Cinema_Room_ID] [int] NULL,
	[Content] [varchar](1000) NULL,
	[Director] [varchar](255) NULL,
	[Duration] [int] NULL,
	[From_Date] [date] NULL,
	[Movie_Production_Company] [varchar](255) NULL,
	[To_Date] [date] NULL,
	[Movie_Name_English] [varchar](255) NULL,
	[Movie_Name_VN] [varchar](255) NULL,
	[Large_Image] [varchar](255) NULL,
	[Small_Image] [varchar](255) NULL,
	[TrailerUrl] [varchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Movie_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Movie_Show]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Movie_Show](
	[Movie_Show_ID] [int] IDENTITY(1,1) NOT NULL,
	[Movie_ID] [varchar](10) NOT NULL,
	[Cinema_Room_ID] [int] NOT NULL,
	[Show_Date] [date] NOT NULL,
	[Schedule_ID] [int] NOT NULL,
	[Version_ID] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Movie_Show_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Movie_Type]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Movie_Type](
	[Movie_ID] [varchar](10) NOT NULL,
	[Type_ID] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Movie_ID] ASC,
	[Type_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Movie_Version]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Movie_Version](
	[Movie_ID] [varchar](10) NOT NULL,
	[Version_ID] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Movie_ID] ASC,
	[Version_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Promotion]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Promotion](
	[Promotion_ID] [int] NOT NULL,
	[Detail] [varchar](255) NULL,
	[Discount_Level] [int] NULL,
	[End_Time] [datetime] NULL,
	[Image] [varchar](255) NULL,
	[Start_Time] [datetime] NULL,
	[Title] [varchar](255) NULL,
	[Is_Active] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Promotion_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PromotionCondition]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PromotionCondition](
	[Condition_ID] [int] IDENTITY(1,1) NOT NULL,
	[Promotion_ID] [int] NULL,
	[ConditionType_ID] [int] NULL,
	[Target_Entity] [varchar](50) NULL,
	[Target_Field] [varchar](50) NULL,
	[Operator] [varchar](10) NULL,
	[Target_Value] [varchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Condition_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Rank]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Rank](
	[Rank_ID] [int] IDENTITY(1,1) NOT NULL,
	[Rank_Name] [varchar](50) NULL,
	[Discount_Percentage] [decimal](5, 2) NULL,
	[Required_Points] [int] NULL,
	[PointEarningPercentage] [decimal](5, 2) NOT NULL,
	[ColorGradient] [nvarchar](200) NOT NULL,
	[IconClass] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_Rank] PRIMARY KEY CLUSTERED 
(
	[Rank_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Roles]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Roles](
	[Role_ID] [int] NOT NULL,
	[Role_Name] [varchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Role_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Schedule]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Schedule](
	[Schedule_ID] [int] IDENTITY(1,1) NOT NULL,
	[Schedule_Time] [time](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Schedule_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Schedule_Seat]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Schedule_Seat](
	[Schedule_Seat_ID] [int] IDENTITY(1,1) NOT NULL,
	[Movie_Show_ID] [int] NULL,
	[Invoice_ID] [varchar](10) NULL,
	[Seat_ID] [int] NULL,
	[Seat_Status_ID] [int] NULL,
	[HoldUntil] [datetime] NULL,
	[HoldBy] [nvarchar](100) NULL,
	[Booked_Price] [decimal](18, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[Schedule_Seat_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Seat]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Seat](
	[Seat_ID] [int] IDENTITY(1,1) NOT NULL,
	[Cinema_Room_ID] [int] NULL,
	[Seat_Column] [varchar](5) NULL,
	[Seat_Row] [int] NULL,
	[Seat_Status_ID] [int] NULL,
	[Seat_Type_ID] [int] NULL,
	[SeatName] [varchar](5) NULL,
PRIMARY KEY CLUSTERED 
(
	[Seat_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Seat_Status]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Seat_Status](
	[Seat_Status_ID] [int] IDENTITY(1,1) NOT NULL,
	[Status_Name] [varchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[Seat_Status_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Seat_Type]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Seat_Type](
	[Seat_Type_ID] [int] IDENTITY(1,1) NOT NULL,
	[Type_Name] [varchar](50) NULL,
	[Price_Percent] [decimal](18, 0) NOT NULL,
	[ColorHex] [varchar](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Seat_Type_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Status]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Status](
	[Status_ID] [int] IDENTITY(1,1) NOT NULL,
	[Status_Name] [varchar](10) NULL,
PRIMARY KEY CLUSTERED 
(
	[Status_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Type]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Type](
	[Type_ID] [int] NOT NULL,
	[Type_Name] [varchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Type_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Version]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Version](
	[Version_ID] [int] NOT NULL,
	[Version_Name] [varchar](255) NULL,
	[Multi] [decimal](18, 0) NULL,
PRIMARY KEY CLUSTERED 
(
	[Version_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Voucher]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Voucher](
	[Voucher_ID] [varchar](10) NOT NULL,
	[Account_ID] [varchar](10) NOT NULL,
	[Code] [nvarchar](20) NOT NULL,
	[Value] [decimal](18, 2) NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[ExpiryDate] [datetime] NOT NULL,
	[IsUsed] [bit] NULL,
	[Image] [varchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Voucher_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Wishlist]    Script Date: 7/19/2025 5:34:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Wishlist](
	[Account_ID] [varchar](10) NOT NULL,
	[Movie_ID] [varchar](10) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Account_ID] ASC,
	[Movie_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
INSERT [dbo].[Account] ([Account_ID], [Address], [Date_Of_Birth], [Email], [Full_Name], [Gender], [Identity_Card], [Image], [Password], [Phone_Number], [Register_Date], [Role_ID], [STATUS], [USERNAME], [Rank_ID]) VALUES (N'AC001', N'123 Main St', CAST(N'2000-01-15' AS Date), N'admin@gmail.com', N'Admin', N'Female', N'123456789', N'/image/profile.jpg', N'1', N'0123456789', CAST(N'2023-01-01' AS Date), 1, 1, N'admin', NULL)
INSERT [dbo].[Account] ([Account_ID], [Address], [Date_Of_Birth], [Email], [Full_Name], [Gender], [Identity_Card], [Image], [Password], [Phone_Number], [Register_Date], [Role_ID], [STATUS], [USERNAME], [Rank_ID]) VALUES (N'AC002', N'789 Oak St', CAST(N'2002-11-10' AS Date), N'member@gmail.com', N'Quang', N'male', N'192837465', N'/images/avatars/6219c76c-d0fa-4958-aac2-8add2a2f0795_Screenshot 2025-06-06 085155.png', N'1', N'0111222333', CAST(N'2025-07-09' AS Date), 3, 1, N'member', 4)
INSERT [dbo].[Account] ([Account_ID], [Address], [Date_Of_Birth], [Email], [Full_Name], [Gender], [Identity_Card], [Image], [Password], [Phone_Number], [Register_Date], [Role_ID], [STATUS], [USERNAME], [Rank_ID]) VALUES (N'AC003', N'789 Oak St', CAST(N'2002-11-10' AS Date), N'member2@gmail.com', N'Member', N'Female', N'132837465', N'/image/profile.jpg', N'1', N'0111222333', CAST(N'2023-01-10' AS Date), 3, 0, N'member2', NULL)
INSERT [dbo].[Account] ([Account_ID], [Address], [Date_Of_Birth], [Email], [Full_Name], [Gender], [Identity_Card], [Image], [Password], [Phone_Number], [Register_Date], [Role_ID], [STATUS], [USERNAME], [Rank_ID]) VALUES (N'AC004', N'123 Street', CAST(N'1999-01-01' AS Date), N'minh.nguyen@example.com', N'Nguyen Hoang Minh', N'Male', N'111111111', N'/image/profile.jpg', N'1', N'0900000001', CAST(N'2023-01-01' AS Date), 2, 1, N'minhnguyen', NULL)
INSERT [dbo].[Account] ([Account_ID], [Address], [Date_Of_Birth], [Email], [Full_Name], [Gender], [Identity_Card], [Image], [Password], [Phone_Number], [Register_Date], [Role_ID], [STATUS], [USERNAME], [Rank_ID]) VALUES (N'AC005', N'123 Street', CAST(N'1999-01-01' AS Date), N'tue.phan@example.com', N'Phan Do Gia Tue', N'Male', N'111111112', N'/image/profile.jpg', N'1', N'0900000002', CAST(N'2023-01-01' AS Date), 2, 1, N'tuephan', NULL)
INSERT [dbo].[Account] ([Account_ID], [Address], [Date_Of_Birth], [Email], [Full_Name], [Gender], [Identity_Card], [Image], [Password], [Phone_Number], [Register_Date], [Role_ID], [STATUS], [USERNAME], [Rank_ID]) VALUES (N'AC006', N'123 Street', CAST(N'1999-01-01' AS Date), N'bao.nguyen@example.com', N'Nguyen Gia Bao', N'Male', N'111111113', N'/image/profile.jpg', N'1', N'0900000003', CAST(N'2023-01-01' AS Date), 2, 1, N'baonguyen', NULL)
INSERT [dbo].[Account] ([Account_ID], [Address], [Date_Of_Birth], [Email], [Full_Name], [Gender], [Identity_Card], [Image], [Password], [Phone_Number], [Register_Date], [Role_ID], [STATUS], [USERNAME], [Rank_ID]) VALUES (N'AC007', N'123 Street', CAST(N'1999-01-01' AS Date), N'quang.nguyen@example.com', N'Nguyen Quang Duy Quang', N'Male', N'111111114', N'/image/profile.jpg', N'1', N'0900000004', CAST(N'2023-01-01' AS Date), 2, 1, N'quangnguyen', NULL)
INSERT [dbo].[Account] ([Account_ID], [Address], [Date_Of_Birth], [Email], [Full_Name], [Gender], [Identity_Card], [Image], [Password], [Phone_Number], [Register_Date], [Role_ID], [STATUS], [USERNAME], [Rank_ID]) VALUES (N'AC008', N'123 Street', CAST(N'1999-01-01' AS Date), N'dat.nguyen@example.com', N'Nguyen Le Quoc Dat', N'Male', N'111111115', N'/image/profile.jpg', N'1', N'0900000005', CAST(N'2023-01-01' AS Date), 2, 1, N'datnguyen', NULL)
INSERT [dbo].[Account] ([Account_ID], [Address], [Date_Of_Birth], [Email], [Full_Name], [Gender], [Identity_Card], [Image], [Password], [Phone_Number], [Register_Date], [Role_ID], [STATUS], [USERNAME], [Rank_ID]) VALUES (N'AC009', N'123 Street', CAST(N'1999-01-01' AS Date), N'dat.thai@example.com', N'Thai Cong Dat', N'Male', N'111111116', N'/image/profile.jpg', N'1', N'0900000006', CAST(N'2023-01-01' AS Date), 2, 1, N'datthai', NULL)
INSERT [dbo].[Account] ([Account_ID], [Address], [Date_Of_Birth], [Email], [Full_Name], [Gender], [Identity_Card], [Image], [Password], [Phone_Number], [Register_Date], [Role_ID], [STATUS], [USERNAME], [Rank_ID]) VALUES (N'AC010', NULL, NULL, N'duyq099@gmail.com', N'Duy Quang', NULL, NULL, N'/image/profile.jpg', NULL, NULL, CAST(N'2025-07-02' AS Date), 3, 1, N'duyq099@gmail.com', 1)
INSERT [dbo].[Account] ([Account_ID], [Address], [Date_Of_Birth], [Email], [Full_Name], [Gender], [Identity_Card], [Image], [Password], [Phone_Number], [Register_Date], [Role_ID], [STATUS], [USERNAME], [Rank_ID]) VALUES (N'AC011', N'0 S? 494, Tang Nhon Phú A, Qu?n 9, Thành ph? H? Chí Minh, Vi?t Nam', CAST(N'2025-05-04' AS Date), N'thaidat011003@gmail.com', N'Dat Thai Công', N'male', N'1', N'/image/profile.jpg', N'AQAAAAIAAYagAAAAEBOiavkOw4eOAVFBkuds4yCWkDbg6xaLdLWGcHAboB/LFf4Anjt7XHXVgS2ALt3FHQ==', N'0937250343', CAST(N'2025-07-14' AS Date), 3, 1, N'trew234', 1)
INSERT [dbo].[Account] ([Account_ID], [Address], [Date_Of_Birth], [Email], [Full_Name], [Gender], [Identity_Card], [Image], [Password], [Phone_Number], [Register_Date], [Role_ID], [STATUS], [USERNAME], [Rank_ID]) VALUES (N'AC012', N'Demo ', CAST(N'2000-01-01' AS Date), N'member8@demo.com', N'Member8', N'male', N'0123456798', N'/image/profile.jpg', N'AQAAAAIAAYagAAAAEGRHV9K8MFw/2sEIt7kFG6fj0Pxh+JtytQTOpgp7WcMu7/yG0NTE6FJwwCo7LGhllg==', N'0123456789', CAST(N'2025-07-16' AS Date), 3, 1, N'member8', 1)
INSERT [dbo].[Account] ([Account_ID], [Address], [Date_Of_Birth], [Email], [Full_Name], [Gender], [Identity_Card], [Image], [Password], [Phone_Number], [Register_Date], [Role_ID], [STATUS], [USERNAME], [Rank_ID]) VALUES (N'AC013', N'Gay', CAST(N'2025-07-19' AS Date), N'quanglikecookie@gmail.com', N'Tecookie', N'unknown', N'0', N'/image/profile.jpg', NULL, N'0', CAST(N'2025-07-19' AS Date), 3, 1, N'quanglikecookie@gmail.com', 1)
GO
SET IDENTITY_INSERT [dbo].[Cinema_Room] ON 

INSERT [dbo].[Cinema_Room] ([Cinema_Room_ID], [Cinema_Room_Name], [Seat_Width], [Seat_Length], [Version_ID], [Status_ID]) VALUES (1, N'Screen 1', 5, 5, 1, NULL)
INSERT [dbo].[Cinema_Room] ([Cinema_Room_ID], [Cinema_Room_Name], [Seat_Width], [Seat_Length], [Version_ID], [Status_ID]) VALUES (2, N'Screen 2', 5, 5, 1, NULL)
INSERT [dbo].[Cinema_Room] ([Cinema_Room_ID], [Cinema_Room_Name], [Seat_Width], [Seat_Length], [Version_ID], [Status_ID]) VALUES (3, N'Screen 3', 10, 10, 1, NULL)
INSERT [dbo].[Cinema_Room] ([Cinema_Room_ID], [Cinema_Room_Name], [Seat_Width], [Seat_Length], [Version_ID], [Status_ID]) VALUES (4, N'Screen 4', 5, 5, 2, NULL)
INSERT [dbo].[Cinema_Room] ([Cinema_Room_ID], [Cinema_Room_Name], [Seat_Width], [Seat_Length], [Version_ID], [Status_ID]) VALUES (5, N'Screen 5', 6, 6, 2, NULL)
INSERT [dbo].[Cinema_Room] ([Cinema_Room_ID], [Cinema_Room_Name], [Seat_Width], [Seat_Length], [Version_ID], [Status_ID]) VALUES (6, N'Screen 6', NULL, NULL, 2, NULL)
INSERT [dbo].[Cinema_Room] ([Cinema_Room_ID], [Cinema_Room_Name], [Seat_Width], [Seat_Length], [Version_ID], [Status_ID]) VALUES (7, N'Screen 7', NULL, NULL, 3, NULL)
SET IDENTITY_INSERT [dbo].[Cinema_Room] OFF
GO
INSERT [dbo].[ConditionType] ([ConditionType_ID], [Name]) VALUES (1, N'Comparison')
INSERT [dbo].[ConditionType] ([ConditionType_ID], [Name]) VALUES (2, N'Selection')
GO
INSERT [dbo].[Employee] ([Employee_ID], [Account_ID]) VALUES (N'EM001', N'AC004')
INSERT [dbo].[Employee] ([Employee_ID], [Account_ID]) VALUES (N'EM002', N'AC005')
INSERT [dbo].[Employee] ([Employee_ID], [Account_ID]) VALUES (N'EM003', N'AC006')
INSERT [dbo].[Employee] ([Employee_ID], [Account_ID]) VALUES (N'EM004', N'AC007')
INSERT [dbo].[Employee] ([Employee_ID], [Account_ID]) VALUES (N'EM005', N'AC008')
INSERT [dbo].[Employee] ([Employee_ID], [Account_ID]) VALUES (N'EM006', N'AC009')
GO
SET IDENTITY_INSERT [dbo].[Food] ON 

INSERT [dbo].[Food] ([FoodId], [Category], [Name], [Price], [Description], [Image], [Status], [CreatedDate], [UpdatedDate]) VALUES (1, N'food', N'Popcorn', CAST(45000.00 AS Decimal(18, 2)), N'Fresh buttered popcorn', N'/images/foods/af2a1d7e-d2ad-4b45-810b-c7390e0449d8_888adcde997a2f1fd25853e9916186b8.jpg', 1, CAST(N'2025-06-25T09:04:25.193' AS DateTime), CAST(N'2025-06-25T09:06:56.127' AS DateTime))
INSERT [dbo].[Food] ([FoodId], [Category], [Name], [Price], [Description], [Image], [Status], [CreatedDate], [UpdatedDate]) VALUES (4, N'food', N'Nachos', CAST(55000.00 AS Decimal(18, 2)), N'Cheese nachos with salsa', N'/images/foods/642a3374-88eb-4926-9542-8dd4fc765b30_RobloxScreenShot20250203_115037658.png', 1, CAST(N'2025-06-25T09:04:25.193' AS DateTime), CAST(N'2025-06-25T11:30:08.190' AS DateTime))
INSERT [dbo].[Food] ([FoodId], [Category], [Name], [Price], [Description], [Image], [Status], [CreatedDate], [UpdatedDate]) VALUES (5, N'drink', N'Pepsi', CAST(25000.00 AS Decimal(18, 2)), N'Cold Pepsi 500ml', NULL, 1, CAST(N'2025-06-25T09:04:25.193' AS DateTime), NULL)
INSERT [dbo].[Food] ([FoodId], [Category], [Name], [Price], [Description], [Image], [Status], [CreatedDate], [UpdatedDate]) VALUES (7, N'combo', N'cc1212', CAST(45000.00 AS Decimal(18, 2)), N'cc', N'/images/foods/c16079b3-f3ee-44e9-9256-abfbe3981d92_9899e932a02f459d3edfca15903b3ef8.jpg', 1, CAST(N'2025-06-25T09:08:46.057' AS DateTime), CAST(N'2025-06-25T10:08:59.063' AS DateTime))
INSERT [dbo].[Food] ([FoodId], [Category], [Name], [Price], [Description], [Image], [Status], [CreatedDate], [UpdatedDate]) VALUES (8, N'food', N'chim', CAST(87000.00 AS Decimal(18, 2)), N'hhh', N'/images/foods/0ef33541-8d5e-4aa2-b85a-0205eb81fdcc_eb2e9ccb079e26102b7427a94d4b3bc6.jpg', 1, CAST(N'2025-06-25T09:17:24.663' AS DateTime), CAST(N'2025-06-25T10:27:54.923' AS DateTime))
INSERT [dbo].[Food] ([FoodId], [Category], [Name], [Price], [Description], [Image], [Status], [CreatedDate], [UpdatedDate]) VALUES (9, N'combo', N'1 BAP + 1 NUOC', CAST(65000.00 AS Decimal(18, 2)), N'BAP SIZE L , NUOC 450ML', N'/images/foods/0f60a958-758b-448b-9157-bef3109544fe_Screenshot 2025-06-06 085155.png', 1, CAST(N'2025-07-02T15:47:49.617' AS DateTime), NULL)
SET IDENTITY_INSERT [dbo].[Food] OFF
GO
SET IDENTITY_INSERT [dbo].[FoodInvoice] ON 

INSERT [dbo].[FoodInvoice] ([FoodInvoice_ID], [Invoice_ID], [Food_ID], [Quantity], [Price]) VALUES (1, N'INV063', 1, 1, CAST(45000.00 AS Decimal(18, 2)))
INSERT [dbo].[FoodInvoice] ([FoodInvoice_ID], [Invoice_ID], [Food_ID], [Quantity], [Price]) VALUES (2, N'INV070', 7, 1, CAST(45000.00 AS Decimal(18, 2)))
INSERT [dbo].[FoodInvoice] ([FoodInvoice_ID], [Invoice_ID], [Food_ID], [Quantity], [Price]) VALUES (3, N'INV071', 1, 3, CAST(45000.00 AS Decimal(18, 2)))
INSERT [dbo].[FoodInvoice] ([FoodInvoice_ID], [Invoice_ID], [Food_ID], [Quantity], [Price]) VALUES (4, N'INV072', 1, 1, CAST(45000.00 AS Decimal(18, 2)))
INSERT [dbo].[FoodInvoice] ([FoodInvoice_ID], [Invoice_ID], [Food_ID], [Quantity], [Price]) VALUES (5, N'INV073', 1, 1, CAST(45000.00 AS Decimal(18, 2)))
INSERT [dbo].[FoodInvoice] ([FoodInvoice_ID], [Invoice_ID], [Food_ID], [Quantity], [Price]) VALUES (6, N'INV073', 7, 1, CAST(45000.00 AS Decimal(18, 2)))
INSERT [dbo].[FoodInvoice] ([FoodInvoice_ID], [Invoice_ID], [Food_ID], [Quantity], [Price]) VALUES (7, N'INV075', 1, 1, CAST(45000.00 AS Decimal(18, 2)))
INSERT [dbo].[FoodInvoice] ([FoodInvoice_ID], [Invoice_ID], [Food_ID], [Quantity], [Price]) VALUES (8, N'INV075', 8, 1, CAST(87000.00 AS Decimal(18, 2)))
INSERT [dbo].[FoodInvoice] ([FoodInvoice_ID], [Invoice_ID], [Food_ID], [Quantity], [Price]) VALUES (9, N'INV075', 9, 5, CAST(65000.00 AS Decimal(18, 2)))
INSERT [dbo].[FoodInvoice] ([FoodInvoice_ID], [Invoice_ID], [Food_ID], [Quantity], [Price]) VALUES (10, N'INV076', 1, 1, CAST(45000.00 AS Decimal(18, 2)))
SET IDENTITY_INSERT [dbo].[FoodInvoice] OFF
GO
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV001', 14, CAST(N'2025-07-01T22:58:03.197' AS DateTime), 0, CAST(138400 AS Decimal(18, 0)), 20, N'D3, E3, E2, D2', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV002', 0, CAST(N'2025-07-01T23:29:36.673' AS DateTime), 0, CAST(138400 AS Decimal(18, 0)), 0, N'D4, E4, E5, D5', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV003', 10, CAST(N'2025-07-01T23:39:36.500' AS DateTime), 0, CAST(147200 AS Decimal(18, 0)), 20, N'D4,E4,E5,D5', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV004', 10, CAST(N'2025-07-01T23:39:52.240' AS DateTime), 0, CAST(147200 AS Decimal(18, 0)), 20, N'D4,E4,E5,D5', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV005', 12, CAST(N'2025-07-01T23:44:52.707' AS DateTime), 0, CAST(167200 AS Decimal(18, 0)), 0, N'D4,E4,E5,D5', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV006', 12, CAST(N'2025-07-01T23:45:06.430' AS DateTime), 0, CAST(167200 AS Decimal(18, 0)), 0, N'D4,E4,E5,D5', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV007', 10, CAST(N'2025-07-01T23:45:35.963' AS DateTime), 0, CAST(147200 AS Decimal(18, 0)), 20, N'D4,E4,E5,D5', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV008', 10, CAST(N'2025-07-01T23:51:39.273' AS DateTime), 0, CAST(147200 AS Decimal(18, 0)), 20, N'D4,E4,E5,D5', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV009', 7, CAST(N'2025-07-01T23:56:32.080' AS DateTime), 0, CAST(101600 AS Decimal(18, 0)), 20, N'B5,A5,A4,B4', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV010', 4, CAST(N'2025-07-02T00:20:57.393' AS DateTime), 0, CAST(63600 AS Decimal(18, 0)), 20, N'D3,E3', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV011', 4, CAST(N'2025-07-02T07:59:10.047' AS DateTime), 0, CAST(60800 AS Decimal(18, 0)), 0, N'B5,A5', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV012', 6, CAST(N'2025-07-02T08:01:13.877' AS DateTime), 0, CAST(83600 AS Decimal(18, 0)), 0, N'D5, E5', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV013', 0, CAST(N'2025-07-02T08:12:14.967' AS DateTime), 0, CAST(101600 AS Decimal(18, 0)), 0, N'B4, A4, A3, B3', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV014', 6, CAST(N'2025-07-02T08:58:49.713' AS DateTime), 0, CAST(83600 AS Decimal(18, 0)), 0, N'D5,E5', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV015', 6, CAST(N'2025-07-02T09:05:59.643' AS DateTime), 0, CAST(83600 AS Decimal(18, 0)), 0, N'D4,E4', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV016', 4, CAST(N'2025-07-02T09:10:17.777' AS DateTime), 0, CAST(60800 AS Decimal(18, 0)), 0, N'A4,B4', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV017', 3, CAST(N'2025-07-02T09:29:55.560' AS DateTime), 0, CAST(40800 AS Decimal(18, 0)), 20, N'B3,A3', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV018', 3, CAST(N'2025-07-02T09:30:07.007' AS DateTime), 0, CAST(40800 AS Decimal(18, 0)), 20, N'B3,A3', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV019', 6, CAST(N'2025-07-02T09:41:41.977' AS DateTime), 0, CAST(83600 AS Decimal(18, 0)), 0, N'D3,E3', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV020', 6, CAST(N'2025-07-02T09:41:46.323' AS DateTime), 0, CAST(83600 AS Decimal(18, 0)), 0, N'D3,E3', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV021', 4, CAST(N'2025-07-02T09:41:51.383' AS DateTime), 0, CAST(63600 AS Decimal(18, 0)), 20, N'D3,E3', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV022', 4, CAST(N'2025-07-02T09:41:56.263' AS DateTime), 0, CAST(63600 AS Decimal(18, 0)), 20, N'D3,E3', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV023', 8, CAST(N'2025-07-02T09:50:34.257' AS DateTime), 0, CAST(79200 AS Decimal(18, 0)), 0, N'D2,E2', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV024', 4, CAST(N'2025-07-02T09:56:54.420' AS DateTime), 0, CAST(37600 AS Decimal(18, 0)), 20, N'B2,A2', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV025', 4, CAST(N'2025-07-02T09:57:39.390' AS DateTime), 0, CAST(37600 AS Decimal(18, 0)), 20, N'A1,B1', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV026', 6, CAST(N'2025-07-02T10:02:34.837' AS DateTime), 0, CAST(59200 AS Decimal(18, 0)), 20, N'D1,E1', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV027', 6, CAST(N'2025-07-02T10:06:09.263' AS DateTime), 0, CAST(57600 AS Decimal(18, 0)), 0, N'B1,A1', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV028', 8, CAST(N'2025-07-02T10:13:10.057' AS DateTime), 0, CAST(79200 AS Decimal(18, 0)), 0, N'D1,E1', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV029', 14, CAST(N'2025-07-02T10:21:32.353' AS DateTime), 0, CAST(136800 AS Decimal(18, 0)), 0, N'A1,B1,E1,D1', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV030', 6, CAST(N'2025-07-02T10:31:52.577' AS DateTime), 0, CAST(57600 AS Decimal(18, 0)), 0, N'A2,B2', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV031', 8, CAST(N'2025-07-02T10:35:22.157' AS DateTime), 0, CAST(79200 AS Decimal(18, 0)), 0, N'D2,E2', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV032', 8, CAST(N'2025-07-02T10:35:26.780' AS DateTime), 0, CAST(79200 AS Decimal(18, 0)), 0, N'D2,E2', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV033', 7, CAST(N'2025-07-02T11:20:49.503' AS DateTime), 0, CAST(72000 AS Decimal(18, 0)), 0, N'B2,A2', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV034', 15, CAST(N'2025-07-02T11:23:08.693' AS DateTime), 0, CAST(151200 AS Decimal(18, 0)), 0, N'A3,B3,D2,E2', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV035', 15, CAST(N'2025-07-02T11:23:29.047' AS DateTime), 0, CAST(151200 AS Decimal(18, 0)), 0, N'A3,B3,D2,E2', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV036', 16, CAST(N'2025-07-02T11:34:20.530' AS DateTime), 0, CAST(235600 AS Decimal(18, 0)), 0, N'A4,B4,D4,E4,B3,A3', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV037', 16, CAST(N'2025-07-02T11:38:44.053' AS DateTime), 0, CAST(228000 AS Decimal(18, 0)), 0, N'A4,A5,B4,B5,D4,D5', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV038', 16, CAST(N'2025-07-02T12:18:02.823' AS DateTime), 0, CAST(228000 AS Decimal(18, 0)), 0, N'E8,F8,I8,J8,D8,C8', N'AC002', 3, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV039', 22, CAST(N'2025-07-02T12:19:31.977' AS DateTime), 0, CAST(216000 AS Decimal(18, 0)), 0, N'C5,D5,E5,E4,A5,A4', N'AC002', 4, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV040', 22, CAST(N'2025-07-02T12:19:58.313' AS DateTime), 0, CAST(216000 AS Decimal(18, 0)), 0, N'A2,A1,C1,D1,E1,E2', N'AC002', 4, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV041', 19, CAST(N'2025-07-02T12:21:27.103' AS DateTime), 1, CAST(194400 AS Decimal(18, 0)), 0, N'A1,B1,C1,E1,F1,G1', N'AC002', 3, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV042', 22, CAST(N'2025-07-02T12:22:51.910' AS DateTime), 1, CAST(216000 AS Decimal(18, 0)), 0, N'C4,B4,D4,E4,G3,F3', N'AC002', 3, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV043', 23, CAST(N'2025-07-02T12:23:45.770' AS DateTime), 0, CAST(230400 AS Decimal(18, 0)), 0, N'G8,D3,F6,H5,J6,C7', N'AC002', 3, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), NULL, 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV044', 16, CAST(N'2025-07-02T14:35:17.767' AS DateTime), 0, CAST(228000 AS Decimal(18, 0)), 0, N'C3, C2, C5, D5, B2, B3', N'AC002', 5, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), N'434,433,436,442,427,428', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV045', 22, CAST(N'2025-07-02T14:43:50.670' AS DateTime), 0, CAST(216000 AS Decimal(18, 0)), 0, N'A3, A2, A4, D4, D3, D2', N'AC002', 5, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), N'422,421,423,441,440,439', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV046', 22, CAST(N'2025-07-02T14:44:04.153' AS DateTime), 0, CAST(216000 AS Decimal(18, 0)), 0, N'A3, A2, A4, D4, D3, D2', N'AC002', 5, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), N'422,421,423,441,440,439', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV047', 17, CAST(N'2025-07-02T14:58:50.750' AS DateTime), 0, CAST(172800 AS Decimal(18, 0)), 0, N'F5, F4, F3, F2, E2', N'AC002', 5, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), N'454,453,452,451,445', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV048', 17, CAST(N'2025-07-02T15:05:41.703' AS DateTime), 0, CAST(172800 AS Decimal(18, 0)), 0, N'A1, B1, C1, D1, F1, C4', N'AC002', 5, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), N'420,426,432,438,450,435', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV049', 17, CAST(N'2025-07-02T15:05:53.983' AS DateTime), 0, CAST(172800 AS Decimal(18, 0)), 0, N'A1, B1, C1, D1, F1, C4', N'AC002', 5, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), N'420,426,432,438,450,435', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV050', 5, CAST(N'2025-07-02T15:33:46.933' AS DateTime), 0, CAST(76000 AS Decimal(18, 0)), 0, N'A3, B3', N'AC002', 2, N'20', NULL, CAST(5.00 AS Decimal(5, 2)), N'3,8', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV051', 1, CAST(N'2025-07-02T15:43:43.913' AS DateTime), 0, CAST(11200 AS Decimal(18, 0)), 20, N'G2, H2', N'AC002', 3, N'20', N'VC016', CAST(10.00 AS Decimal(5, 2)), N'267,277', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV052', 4, CAST(N'2025-07-09T08:36:11.567' AS DateTime), 0, CAST(43200 AS Decimal(18, 0)), 0, N'E5', N'AC002', 2, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), N'25', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV053', 6, CAST(N'2025-07-09T08:36:31.040' AS DateTime), 0, CAST(64800 AS Decimal(18, 0)), 0, N'C6, D6', N'AC002', 3, N'20', NULL, CAST(10.00 AS Decimal(5, 2)), N'231,241', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV054', 8, CAST(N'2025-07-10T18:59:13.437' AS DateTime), 0, CAST(68000 AS Decimal(18, 0)), 0, N'A3, B3', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'3,8', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV055', 9, CAST(N'2025-07-10T19:01:45.497' AS DateTime), 0, CAST(74800 AS Decimal(18, 0)), 0, N'E4, D2', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'24,17', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV056', 10, CAST(N'2025-07-10T20:45:30.713' AS DateTime), 0, CAST(81600 AS Decimal(18, 0)), 0, N'E4, E2', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'24,22', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV057', 12, CAST(N'2025-07-12T12:01:25.677' AS DateTime), 0, CAST(102000 AS Decimal(18, 0)), 0, N'B3, A3, D2', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'8,3,17', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV058', 12, CAST(N'2025-07-12T12:17:46.963' AS DateTime), 0, CAST(102000 AS Decimal(18, 0)), 0, N'A3, B2, D2', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'3,7,17', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV059', 8, CAST(N'2025-07-14T08:18:48.127' AS DateTime), 0, CAST(68000 AS Decimal(18, 0)), 0, N'A2, B2', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'2,7', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV060', 8, CAST(N'2025-07-14T08:26:27.070' AS DateTime), 0, CAST(68000 AS Decimal(18, 0)), 0, N'A2, E5', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'2,25', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV061', 9, CAST(N'2025-07-14T08:29:14.057' AS DateTime), 0, CAST(74800 AS Decimal(18, 0)), 0, N'B2, D2', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'7,17', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV062', 9, CAST(N'2025-07-14T08:39:46.247' AS DateTime), 0, CAST(74800 AS Decimal(18, 0)), 0, N'B2, D2', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'7,17', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV063', 9, CAST(N'2025-07-14T16:36:10.867' AS DateTime), 0, CAST(34000 AS Decimal(18, 0)), 0, N'E10', N'AC002', 3, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'255', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV064', 9, CAST(N'2025-07-14T16:36:23.203' AS DateTime), 0, CAST(34000 AS Decimal(18, 0)), 0, N'E10', N'AC002', 3, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'255', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV065', 11, CAST(N'2025-07-14T18:20:31.897' AS DateTime), 0, CAST(95200 AS Decimal(18, 0)), 0, N'A5, A4, B4', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'5,4,9', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV066', 8, CAST(N'2025-07-16T08:55:27.870' AS DateTime), 1, CAST(68000 AS Decimal(18, 0)), 0, N'A5, B4', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'5,9', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV067', 8, CAST(N'2025-07-16T08:55:34.893' AS DateTime), 1, CAST(68000 AS Decimal(18, 0)), 0, N'A5, B4', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'5,9', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV068', 8, CAST(N'2025-07-16T08:55:36.700' AS DateTime), 1, CAST(68000 AS Decimal(18, 0)), 0, N'A5, B4', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'5,9', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV069', 8, CAST(N'2025-07-16T08:55:43.973' AS DateTime), 1, CAST(68000 AS Decimal(18, 0)), 0, N'A5, B4', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'5,9', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV070', 9, CAST(N'2025-07-16T09:12:55.687' AS DateTime), 1, CAST(81600 AS Decimal(18, 0)), 0, N'E2, E5', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'22,25', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV071', 8, CAST(N'2025-07-16T09:16:09.057' AS DateTime), 1, CAST(74800 AS Decimal(18, 0)), 0, N'D5, E4', N'AC002', 2, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'20,24', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV072', 17, CAST(N'2025-07-16T15:13:11.340' AS DateTime), 0, CAST(149600 AS Decimal(18, 0)), 0, N'H9, H10, I9, I10', N'AC002', 3, N'20', NULL, CAST(15.00 AS Decimal(5, 2)), N'284,285,294,295', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV073', 8, CAST(N'2025-07-16T15:18:45.863' AS DateTime), 1, CAST(74800 AS Decimal(18, 0)), 0, N'B5, D4', N'AC002', 2, N'20', N'VC042', CAST(15.00 AS Decimal(5, 2)), N'10,19', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV074', 1, CAST(N'2025-07-19T13:01:00.567' AS DateTime), 1, CAST(128000 AS Decimal(18, 0)), 0, N'A6, B5', N'AC009', 5, N'{"seat":20.0,"food":[]}', NULL, CAST(0.00 AS Decimal(5, 2)), N'425,430', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV075', 3, CAST(N'2025-07-19T13:38:33.910' AS DateTime), 1, CAST(261000 AS Decimal(18, 0)), 0, N'F4', N'AC013', 5, N'{"seat":20.0,"food":[]}', NULL, CAST(0.00 AS Decimal(5, 2)), N'453', 1, CAST(N'2025-07-19T13:40:51.437' AS DateTime), N'Admin')
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV076', 13, CAST(N'2025-07-19T13:45:34.443' AS DateTime), 1, CAST(153800 AS Decimal(18, 0)), 0, N'F5, F6', N'AC002', 5, N'{"seat":20.0,"food":[]}', NULL, CAST(15.00 AS Decimal(5, 2)), N'454,455', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV077', 2, CAST(N'2025-07-19T17:02:33.910' AS DateTime), 0, CAST(224000 AS Decimal(18, 0)), 0, N'A3, A4, A5', N'AC001', 5, N'{"seat":20.0,"food":[]}', NULL, CAST(0.00 AS Decimal(5, 2)), N'422,423,424', 0, NULL, NULL)
INSERT [dbo].[Invoice] ([Invoice_ID], [Add_Score], [BookingDate], [Status], [Total_Money], [Use_Score], [Seat], [Account_ID], [Movie_Show_Id], [Promotion_Discount], [Voucher_ID], [RankDiscountPercentage], [Seat_IDs], [Cancel], [CancelDate], [CancelBy]) VALUES (N'INV078', 2, CAST(N'2025-07-19T17:04:26.563' AS DateTime), 1, CAST(208000 AS Decimal(18, 0)), 0, N'D4, D6, E2', N'AC001', 5, N'{"seat":20.0,"food":[]}', NULL, CAST(0.00 AS Decimal(5, 2)), N'441,443,445', 0, NULL, NULL)
GO
INSERT [dbo].[Member] ([Member_ID], [Score], [Account_ID], [Total_Points]) VALUES (N'MB001', 10010, N'AC002', 99840)
INSERT [dbo].[Member] ([Member_ID], [Score], [Account_ID], [Total_Points]) VALUES (N'MB002', 10000, N'AC003', 10000)
INSERT [dbo].[Member] ([Member_ID], [Score], [Account_ID], [Total_Points]) VALUES (N'MB003', 0, N'AC010', 0)
INSERT [dbo].[Member] ([Member_ID], [Score], [Account_ID], [Total_Points]) VALUES (N'MB004', 0, N'AC011', 0)
INSERT [dbo].[Member] ([Member_ID], [Score], [Account_ID], [Total_Points]) VALUES (N'MB005', 0, N'AC012', 0)
INSERT [dbo].[Member] ([Member_ID], [Score], [Account_ID], [Total_Points]) VALUES (N'MB006', 0, N'AC013', 0)
GO
INSERT [dbo].[Movie] ([Movie_ID], [Actor], [Cinema_Room_ID], [Content], [Director], [Duration], [From_Date], [Movie_Production_Company], [To_Date], [Movie_Name_English], [Movie_Name_VN], [Large_Image], [Small_Image], [TrailerUrl]) VALUES (N'MV001', N'Cillian Murphy, Emily Blunt', NULL, N'The story of American scientist J. Robert Oppenheimer and his role in the development of the atomic bomb.', N'Christopher Nolan', 190, CAST(N'2025-06-21' AS Date), N'Universal Pictures', CAST(N'2025-07-25' AS Date), N'Oppenheimer', N'Oppenheimer', N'/image/li-open.jpg', N'/image/open.jpg', N'https://www.youtube.com/embed/uYPbbksJxIg')
INSERT [dbo].[Movie] ([Movie_ID], [Actor], [Cinema_Room_ID], [Content], [Director], [Duration], [From_Date], [Movie_Production_Company], [To_Date], [Movie_Name_English], [Movie_Name_VN], [Large_Image], [Small_Image], [TrailerUrl]) VALUES (N'MV002', N'Tom Holland, Zendaya', NULL, N'Peter Parker seeks help from Doctor Strange after his identity is revealed, leading to multiverse chaos.', N'Jon Watts', 148, CAST(N'2025-06-20' AS Date), N'Marvel Studios', CAST(N'2025-07-25' AS Date), N'Spider-Man: No Way Home', N'Ngu?i Nh?n: Không Còn Nhà', N'/image/spider.jpg', N'/image/spider.jpg', N'https://www.youtube.com/embed/rt-2cxAiPJk')
INSERT [dbo].[Movie] ([Movie_ID], [Actor], [Cinema_Room_ID], [Content], [Director], [Duration], [From_Date], [Movie_Production_Company], [To_Date], [Movie_Name_English], [Movie_Name_VN], [Large_Image], [Small_Image], [TrailerUrl]) VALUES (N'MV003', N'Timothée Chalamet, Zendaya', NULL, N'Paul Atreides unites with the Fremen to seek revenge against the conspirators who destroyed his family.', N'Denis Villeneuve', 166, CAST(N'2025-06-01' AS Date), N'Legendary Pictures', CAST(N'2025-07-21' AS Date), N'Dune: Part Two', N'Hành Tinh Cát: Ph?n Hai', N'/image/dune.jpg', N'/image/dune.jpg', N'https://www.youtube.com/embed/Way9Dexny3w')
INSERT [dbo].[Movie] ([Movie_ID], [Actor], [Cinema_Room_ID], [Content], [Director], [Duration], [From_Date], [Movie_Production_Company], [To_Date], [Movie_Name_English], [Movie_Name_VN], [Large_Image], [Small_Image], [TrailerUrl]) VALUES (N'MV004', N'Margot Robbie, Ryan Gosling', NULL, N'Barbie suffers a crisis that leads her to question her world and her existence.', N'Greta Gerwig', 114, CAST(N'2025-06-21' AS Date), N'Warner Bros.', CAST(N'2025-07-27' AS Date), N'Barbie', N'Barbie', N'/image/barbie.jpg', N'/image/barbie.jpg', N'https://www.youtube.com/embed/pBk4NYhWNMM')
INSERT [dbo].[Movie] ([Movie_ID], [Actor], [Cinema_Room_ID], [Content], [Director], [Duration], [From_Date], [Movie_Production_Company], [To_Date], [Movie_Name_English], [Movie_Name_VN], [Large_Image], [Small_Image], [TrailerUrl]) VALUES (N'MV005', N'Michelle Yeoh, Ke Huy Quan', NULL, N'A woman is swept into a multiverse adventure where she must connect with different versions of herself.', N'Daniel Kwan, Daniel Scheinert', 139, CAST(N'2025-06-25' AS Date), N'A24', CAST(N'2025-07-02' AS Date), N'Everything Everywhere All at Once', N'M?i Th? M?i Noi T?t C? Cùng Lúc', N'/image/everything.jpg', N'/image/everything.jpg', N'https://www.youtube.com/embed/wxN1T1uxQ2g')
INSERT [dbo].[Movie] ([Movie_ID], [Actor], [Cinema_Room_ID], [Content], [Director], [Duration], [From_Date], [Movie_Production_Company], [To_Date], [Movie_Name_English], [Movie_Name_VN], [Large_Image], [Small_Image], [TrailerUrl]) VALUES (N'MV006', N'Sam Worthington, Zoe Saldana', NULL, N'Jake Sully lives with his family on Pandora and must protect them from a new threat.', N'James Cameron', 192, CAST(N'2025-06-27' AS Date), N'20th Century Studios', CAST(N'2025-07-02' AS Date), N'Avatar: The Way of Water', N'Avatar: Dòng Ch?y C?a Nu?c', N'/image/avatar.jpg', N'/image/avatar.jpg', N'https://www.youtube.com/embed/d9MyW72ELq0')
INSERT [dbo].[Movie] ([Movie_ID], [Actor], [Cinema_Room_ID], [Content], [Director], [Duration], [From_Date], [Movie_Production_Company], [To_Date], [Movie_Name_English], [Movie_Name_VN], [Large_Image], [Small_Image], [TrailerUrl]) VALUES (N'MV007', N'Robert Pattinson, Zoë Kravitz', NULL, N'Batman uncovers corruption in Gotham while pursuing the Riddler, a sadistic killer.', N'Matt Reeves', 176, CAST(N'2025-06-28' AS Date), N'Warner Bros.', CAST(N'2025-08-02' AS Date), N'The Batman', N'Ngu?i Doi', N'/image/batman.jpg', N'/image/batman.jpg', N'https://www.youtube.com/embed/mqqft2x_Aa4')
INSERT [dbo].[Movie] ([Movie_ID], [Actor], [Cinema_Room_ID], [Content], [Director], [Duration], [From_Date], [Movie_Production_Company], [To_Date], [Movie_Name_English], [Movie_Name_VN], [Large_Image], [Small_Image], [TrailerUrl]) VALUES (N'MV008', N'Tom Cruise, Miles Teller', NULL, N'Pete "Maverick" Mitchell trains Top Gun graduates for a high-stakes mission.', N'Joseph Kosinski', 131, CAST(N'2025-06-29' AS Date), N'Paramount Pictures', CAST(N'2025-08-02' AS Date), N'Top Gun: Maverick', N'Phi Công Siêu Ð?ng Maverick', N'/image/li-topgun.jpg', N'/image/topgun.jpg', N'https://www.youtube.com/embed/giXco2jaZ_4')
INSERT [dbo].[Movie] ([Movie_ID], [Actor], [Cinema_Room_ID], [Content], [Director], [Duration], [From_Date], [Movie_Production_Company], [To_Date], [Movie_Name_English], [Movie_Name_VN], [Large_Image], [Small_Image], [TrailerUrl]) VALUES (N'MV009', N'Song Kang-ho, Choi Woo-shik', NULL, N'A poor family schemes to become employed by a wealthy family and infiltrate their household.', N'Bong Joon-ho', 132, CAST(N'2025-06-30' AS Date), N'CJ Entertainment', CAST(N'2025-08-02' AS Date), N'Parasite', N'Ký Sinh Trùng', N'/image/parasite.jpg', N'/image/parasite.jpg', N'https://www.youtube.com/embed/5xH0HfJHsaY')
GO
SET IDENTITY_INSERT [dbo].[Movie_Show] ON 

INSERT [dbo].[Movie_Show] ([Movie_Show_ID], [Movie_ID], [Cinema_Room_ID], [Show_Date], [Schedule_ID], [Version_ID]) VALUES (2, N'MV001', 1, CAST(N'2025-07-16' AS Date), 3, 1)
INSERT [dbo].[Movie_Show] ([Movie_Show_ID], [Movie_ID], [Cinema_Room_ID], [Show_Date], [Schedule_ID], [Version_ID]) VALUES (3, N'MV002', 3, CAST(N'2025-07-02' AS Date), 1, 1)
INSERT [dbo].[Movie_Show] ([Movie_Show_ID], [Movie_ID], [Cinema_Room_ID], [Show_Date], [Schedule_ID], [Version_ID]) VALUES (4, N'MV004', 4, CAST(N'2025-07-02' AS Date), 3, 2)
INSERT [dbo].[Movie_Show] ([Movie_Show_ID], [Movie_ID], [Cinema_Room_ID], [Show_Date], [Schedule_ID], [Version_ID]) VALUES (5, N'MV009', 5, CAST(N'2025-08-02' AS Date), 28, 2)
SET IDENTITY_INSERT [dbo].[Movie_Show] OFF
GO
INSERT [dbo].[Movie_Type] ([Movie_ID], [Type_ID]) VALUES (N'MV001', 1)
INSERT [dbo].[Movie_Type] ([Movie_ID], [Type_ID]) VALUES (N'MV001', 2)
INSERT [dbo].[Movie_Type] ([Movie_ID], [Type_ID]) VALUES (N'MV002', 3)
INSERT [dbo].[Movie_Type] ([Movie_ID], [Type_ID]) VALUES (N'MV002', 4)
INSERT [dbo].[Movie_Type] ([Movie_ID], [Type_ID]) VALUES (N'MV003', 5)
INSERT [dbo].[Movie_Type] ([Movie_ID], [Type_ID]) VALUES (N'MV004', 6)
INSERT [dbo].[Movie_Type] ([Movie_ID], [Type_ID]) VALUES (N'MV004', 7)
INSERT [dbo].[Movie_Type] ([Movie_ID], [Type_ID]) VALUES (N'MV005', 8)
INSERT [dbo].[Movie_Type] ([Movie_ID], [Type_ID]) VALUES (N'MV006', 9)
INSERT [dbo].[Movie_Type] ([Movie_ID], [Type_ID]) VALUES (N'MV006', 10)
INSERT [dbo].[Movie_Type] ([Movie_ID], [Type_ID]) VALUES (N'MV007', 11)
INSERT [dbo].[Movie_Type] ([Movie_ID], [Type_ID]) VALUES (N'MV008', 12)
INSERT [dbo].[Movie_Type] ([Movie_ID], [Type_ID]) VALUES (N'MV009', 1)
INSERT [dbo].[Movie_Type] ([Movie_ID], [Type_ID]) VALUES (N'MV009', 5)
GO
INSERT [dbo].[Movie_Version] ([Movie_ID], [Version_ID]) VALUES (N'MV001', 1)
INSERT [dbo].[Movie_Version] ([Movie_ID], [Version_ID]) VALUES (N'MV001', 2)
INSERT [dbo].[Movie_Version] ([Movie_ID], [Version_ID]) VALUES (N'MV002', 1)
INSERT [dbo].[Movie_Version] ([Movie_ID], [Version_ID]) VALUES (N'MV002', 2)
INSERT [dbo].[Movie_Version] ([Movie_ID], [Version_ID]) VALUES (N'MV003', 3)
INSERT [dbo].[Movie_Version] ([Movie_ID], [Version_ID]) VALUES (N'MV004', 2)
INSERT [dbo].[Movie_Version] ([Movie_ID], [Version_ID]) VALUES (N'MV004', 3)
INSERT [dbo].[Movie_Version] ([Movie_ID], [Version_ID]) VALUES (N'MV005', 2)
INSERT [dbo].[Movie_Version] ([Movie_ID], [Version_ID]) VALUES (N'MV006', 2)
INSERT [dbo].[Movie_Version] ([Movie_ID], [Version_ID]) VALUES (N'MV006', 3)
INSERT [dbo].[Movie_Version] ([Movie_ID], [Version_ID]) VALUES (N'MV007', 1)
INSERT [dbo].[Movie_Version] ([Movie_ID], [Version_ID]) VALUES (N'MV008', 1)
INSERT [dbo].[Movie_Version] ([Movie_ID], [Version_ID]) VALUES (N'MV009', 1)
INSERT [dbo].[Movie_Version] ([Movie_ID], [Version_ID]) VALUES (N'MV009', 2)
GO
INSERT [dbo].[Promotion] ([Promotion_ID], [Detail], [Discount_Level], [End_Time], [Image], [Start_Time], [Title], [Is_Active]) VALUES (1, N'Get 20% off on your first order!', 20, CAST(N'2025-12-31T00:00:00.000' AS DateTime), N'/images/promotions/dfda08723fbb4b91b859a0f7d27867a7.png', CAST(N'2025-05-01T00:00:00.000' AS DateTime), N'First Time Promo', 1)
INSERT [dbo].[Promotion] ([Promotion_ID], [Detail], [Discount_Level], [End_Time], [Image], [Start_Time], [Title], [Is_Active]) VALUES (3, N'Weekend', 20, CAST(N'2025-08-15T16:22:06.337' AS DateTime), N'/images/promotions/46dabde9e53e46da867f7090f5ffd94e.png', CAST(N'2025-07-16T16:22:06.337' AS DateTime), N'Promotion', 1)
GO
SET IDENTITY_INSERT [dbo].[PromotionCondition] ON 

INSERT [dbo].[PromotionCondition] ([Condition_ID], [Promotion_ID], [ConditionType_ID], [Target_Entity], [Target_Field], [Operator], [Target_Value]) VALUES (1, 1, 1, N'User', N'OrderCount', N'=', N'0')
INSERT [dbo].[PromotionCondition] ([Condition_ID], [Promotion_ID], [ConditionType_ID], [Target_Entity], [Target_Field], [Operator], [Target_Value]) VALUES (2, NULL, 1, N'Order', N'Seat_Count', N'>=', N'3')
SET IDENTITY_INSERT [dbo].[PromotionCondition] OFF
GO
SET IDENTITY_INSERT [dbo].[Rank] ON 

INSERT [dbo].[Rank] ([Rank_ID], [Rank_Name], [Discount_Percentage], [Required_Points], [PointEarningPercentage], [ColorGradient], [IconClass]) VALUES (1, N'Bronze', CAST(0.00 AS Decimal(5, 2)), 0, CAST(5.00 AS Decimal(5, 2)), N'linear-gradient(135deg, #804A00 0%, #B87333 50%, #CD7F32 100%)', N'fas fa-medal')
INSERT [dbo].[Rank] ([Rank_ID], [Rank_Name], [Discount_Percentage], [Required_Points], [PointEarningPercentage], [ColorGradient], [IconClass]) VALUES (2, N'Gold', CAST(5.00 AS Decimal(5, 2)), 30000, CAST(7.00 AS Decimal(5, 2)), N'linear-gradient(135deg, #FFD700 0%, #FDB931 50%, #DAA520 100%)', N'fas fa-trophy')
INSERT [dbo].[Rank] ([Rank_ID], [Rank_Name], [Discount_Percentage], [Required_Points], [PointEarningPercentage], [ColorGradient], [IconClass]) VALUES (3, N'Diamond', CAST(10.00 AS Decimal(5, 2)), 50000, CAST(10.00 AS Decimal(5, 2)), N'linear-gradient(135deg, #89CFF0 0%, #A0E9FF 50%, #B9F2FF 100%)', N'fas fa-gem')
INSERT [dbo].[Rank] ([Rank_ID], [Rank_Name], [Discount_Percentage], [Required_Points], [PointEarningPercentage], [ColorGradient], [IconClass]) VALUES (4, N'Elite', CAST(15.00 AS Decimal(5, 2)), 80000, CAST(12.00 AS Decimal(5, 2)), N'linear-gradient(135deg, #1a1a1a 0%, #2C3E50 50%, #2c3e50 100%)', N'fas fa-star')
SET IDENTITY_INSERT [dbo].[Rank] OFF
GO
INSERT [dbo].[Roles] ([Role_ID], [Role_Name]) VALUES (1, N'Admin')
INSERT [dbo].[Roles] ([Role_ID], [Role_Name]) VALUES (2, N'Employee')
INSERT [dbo].[Roles] ([Role_ID], [Role_Name]) VALUES (3, N'Member')
GO
SET IDENTITY_INSERT [dbo].[Schedule] ON 

INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (1, CAST(N'09:00:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (2, CAST(N'09:30:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (3, CAST(N'10:00:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (4, CAST(N'10:30:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (5, CAST(N'11:00:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (6, CAST(N'11:30:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (7, CAST(N'12:00:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (8, CAST(N'12:30:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (9, CAST(N'13:00:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (10, CAST(N'13:30:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (11, CAST(N'14:00:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (12, CAST(N'14:30:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (13, CAST(N'15:00:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (14, CAST(N'15:30:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (15, CAST(N'16:00:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (16, CAST(N'16:30:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (17, CAST(N'17:00:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (18, CAST(N'17:30:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (19, CAST(N'18:00:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (20, CAST(N'18:30:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (21, CAST(N'19:00:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (22, CAST(N'19:30:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (23, CAST(N'20:00:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (24, CAST(N'20:30:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (25, CAST(N'21:00:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (26, CAST(N'21:30:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (27, CAST(N'22:00:00' AS Time))
INSERT [dbo].[Schedule] ([Schedule_ID], [Schedule_Time]) VALUES (28, CAST(N'22:30:00' AS Time))
SET IDENTITY_INSERT [dbo].[Schedule] OFF
GO
SET IDENTITY_INSERT [dbo].[Schedule_Seat] ON 

INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (1, 2, N'INV001', 18, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (2, 2, N'INV001', 23, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (3, 2, N'INV001', 22, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (4, 2, N'INV001', 17, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (5, 2, N'INV073', 19, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (6, 2, N'INV002', 24, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (7, 2, N'INV002', 25, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (8, 2, N'INV002', 20, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (9, 2, N'INV008', 19, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (10, 2, N'INV008', 24, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (11, 2, N'INV008', 25, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (12, 2, N'INV008', 20, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (13, 2, N'INV073', 10, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (14, 2, N'INV009', 5, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (15, 2, N'INV009', 4, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (16, 2, N'INV009', 9, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (17, 2, N'INV010', 18, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (18, 2, N'INV010', 23, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (19, 2, N'INV011', 10, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (20, 2, N'INV011', 5, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (21, 2, N'INV012', 20, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (22, 2, N'INV012', 25, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (23, 2, N'INV013', 9, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (24, 2, N'INV013', 4, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (25, 2, N'INV013', 3, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (26, 2, N'INV013', 8, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (27, 2, N'INV014', 20, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (28, 2, N'INV014', 25, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (29, 2, N'INV015', 19, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (30, 2, N'INV015', 24, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (31, 2, N'INV016', 4, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (32, 2, N'INV016', 9, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (33, 2, N'INV018', 8, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (34, 2, N'INV018', 3, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (35, 2, N'INV023', 17, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (36, 2, N'INV023', 22, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (37, 2, N'INV024', 7, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (38, 2, N'INV024', 2, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (39, 2, N'INV025', 1, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (40, 2, N'INV025', 6, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (41, 2, N'INV026', 16, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (42, 2, N'INV026', 21, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (43, 2, N'INV030', 2, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (44, 2, N'INV030', 7, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (45, 2, N'INV032', 17, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (46, 2, N'INV032', 22, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (47, 2, N'INV033', 7, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (48, 2, N'INV033', 2, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (49, 2, N'INV035', 3, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (50, 2, N'INV035', 8, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (51, 2, N'INV035', 17, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (52, 2, N'INV035', 22, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (53, 2, N'INV036', 4, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (54, 2, N'INV036', 9, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (55, 2, N'INV036', 19, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (56, 2, N'INV036', 24, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (57, 2, N'INV036', 8, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (58, 2, N'INV036', 3, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (59, 2, N'INV037', 4, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (60, 2, N'INV037', 5, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (61, 2, N'INV037', 9, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (62, 2, N'INV037', 10, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (63, 2, N'INV037', 19, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (64, 2, N'INV037', 20, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (65, 3, N'INV038', 253, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (66, 3, N'INV038', 263, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (67, 3, N'INV038', 293, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (68, 3, N'INV038', 303, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (69, 3, N'INV038', 243, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (70, 3, N'INV038', 233, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (71, 4, N'INV039', 409, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (72, 4, N'INV039', 414, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (73, 4, N'INV039', 419, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (74, 4, N'INV039', 418, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (75, 4, N'INV039', 399, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (76, 4, N'INV039', 398, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (77, 4, N'INV040', 396, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (78, 4, N'INV040', 395, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (79, 4, N'INV040', 405, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (80, 4, N'INV040', 410, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (81, 4, N'INV040', 415, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (82, 4, N'INV040', 416, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (83, 3, N'INV041', 206, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (84, 3, N'INV041', 216, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (85, 3, N'INV041', 226, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (86, 3, N'INV041', 246, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (87, 3, N'INV041', 256, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (88, 3, N'INV041', 266, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (89, 3, N'INV042', 229, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (90, 3, N'INV042', 219, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (91, 3, N'INV042', 239, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (92, 3, N'INV042', 249, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (93, 3, N'INV042', 268, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (94, 3, N'INV042', 258, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (95, 3, N'INV043', 273, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (96, 3, N'INV043', 238, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (97, 3, N'INV043', 261, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (98, 3, N'INV043', 280, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (99, 3, N'INV043', 301, 1, NULL, NULL, NULL)
GO
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (100, 3, N'INV043', 232, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (101, 5, N'INV044', 434, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (102, 5, N'INV044', 433, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (103, 5, N'INV044', 436, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (104, 5, N'INV044', 442, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (105, 5, N'INV044', 427, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (106, 5, N'INV044', 428, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (107, 5, N'INV044', 13, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (108, 5, N'INV045', 422, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (109, 5, N'INV045', 421, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (110, 5, N'INV045', 423, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (111, 5, N'INV045', 441, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (112, 5, N'INV045', 440, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (113, 5, N'INV045', 439, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (114, 5, N'INV046', 422, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (115, 5, N'INV046', 421, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (116, 5, N'INV046', 423, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (117, 5, N'INV046', 441, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (118, 5, N'INV046', 440, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (119, 5, N'INV046', 439, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (120, 5, N'INV046', 3, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (121, 5, N'INV047', 454, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (122, 5, N'INV047', 453, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (123, 5, N'INV047', 452, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (124, 5, N'INV047', 451, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (125, 5, N'INV047', 445, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (126, 5, N'INV048', 420, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (127, 5, N'INV048', 426, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (128, 5, N'INV048', 432, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (129, 5, N'INV048', 438, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (130, 5, N'INV048', 450, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (131, 5, N'INV048', 435, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (132, 5, N'INV049', 420, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (133, 5, N'INV049', 426, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (134, 5, N'INV049', 432, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (135, 5, N'INV049', 438, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (136, 5, N'INV049', 450, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (137, 5, N'INV049', 435, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (138, 5, N'INV049', 1, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (139, 2, N'INV050', 3, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (140, 2, N'INV050', 8, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (141, 3, N'INV051', 267, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (142, 3, N'INV051', 277, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (143, 2, N'INV052', 25, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (144, 3, N'INV053', 231, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (145, 2, N'INV056', 24, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (146, 2, N'INV057', 8, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (147, 2, N'INV058', 3, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (148, 2, N'INV059', 2, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (149, 2, N'INV060', 2, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (150, 2, N'INV061', 7, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (151, 2, N'INV062', 7, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (152, 3, N'INV063', 255, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (153, 3, N'INV064', 255, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (154, 2, N'INV065', 5, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (155, 2, N'INV065', 4, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (156, 2, N'INV065', 9, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (157, 2, N'INV066', 5, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (158, 2, N'INV066', 9, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (159, 2, N'INV067', 5, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (160, 2, N'INV067', 9, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (161, 2, N'INV068', 5, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (162, 2, N'INV068', 9, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (163, 2, N'INV069', 5, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (164, 2, N'INV069', 9, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (165, 2, N'INV070', 22, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (166, 2, N'INV070', 25, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (167, 2, N'INV071', 20, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (168, 2, N'INV071', 24, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (169, 3, N'INV072', 284, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (170, 3, N'INV072', 285, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (171, 3, N'INV072', 294, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (172, 3, N'INV072', 295, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (173, 5, N'INV074', 425, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (174, 5, N'INV074', 430, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (175, 5, N'INV075', 453, 1, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (176, 5, N'INV076', 454, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (177, 5, N'INV076', 455, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (178, 5, N'INV077', 422, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (179, 5, N'INV077', 423, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (180, 5, N'INV077', 424, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (181, 5, N'INV078', 441, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (182, 5, N'INV078', 443, 2, NULL, NULL, NULL)
INSERT [dbo].[Schedule_Seat] ([Schedule_Seat_ID], [Movie_Show_ID], [Invoice_ID], [Seat_ID], [Seat_Status_ID], [HoldUntil], [HoldBy], [Booked_Price]) VALUES (183, 5, N'INV078', 445, 2, NULL, NULL, NULL)
SET IDENTITY_INSERT [dbo].[Schedule_Seat] OFF
GO
SET IDENTITY_INSERT [dbo].[Seat] ON 

INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (1, 1, N'1', 1, 1, 1, N'A1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (2, 1, N'2', 1, 1, 1, N'A2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (3, 1, N'3', 1, 1, 1, N'A3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (4, 1, N'4', 1, 1, 1, N'A4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (5, 1, N'5', 1, 1, 1, N'A5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (6, 1, N'1', 2, 1, 3, N'B1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (7, 1, N'2', 2, 1, 3, N'B2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (8, 1, N'3', 2, 1, 3, N'B3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (9, 1, N'4', 2, 1, 3, N'B4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (10, 1, N'5', 2, 1, 3, N'B5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (11, 1, N'1', 3, 1, 4, N'C1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (12, 1, N'2', 3, 1, 4, N'C2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (13, 1, N'3', 3, 1, 4, N'C3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (14, 1, N'4', 3, 1, 4, N'C4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (15, 1, N'5', 3, 1, 4, N'C5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (16, 1, N'1', 4, 1, 2, N'D1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (17, 1, N'2', 4, 1, 2, N'D2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (18, 1, N'3', 4, 1, 2, N'D3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (19, 1, N'4', 4, 1, 2, N'D4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (20, 1, N'5', 4, 1, 2, N'D5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (21, 1, N'1', 5, 1, 3, N'E1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (22, 1, N'2', 5, 1, 3, N'E2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (23, 1, N'3', 5, 1, 3, N'E3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (24, 1, N'4', 5, 1, 3, N'E4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (25, 1, N'5', 5, 1, 3, N'E5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (206, 3, N'1', 1, 1, 1, N'A1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (207, 3, N'2', 1, 1, 1, N'A2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (208, 3, N'3', 1, 1, 4, N'A3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (209, 3, N'4', 1, 1, 1, N'A4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (210, 3, N'5', 1, 1, 1, N'A5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (211, 3, N'6', 1, 1, 1, N'A6')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (212, 3, N'7', 1, 1, 1, N'A7')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (213, 3, N'8', 1, 1, 4, N'A8')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (214, 3, N'9', 1, 1, 1, N'A9')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (215, 3, N'10', 1, 1, 1, N'A10')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (216, 3, N'1', 2, 1, 1, N'B1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (217, 3, N'2', 2, 1, 1, N'B2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (218, 3, N'3', 2, 1, 4, N'B3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (219, 3, N'4', 2, 1, 1, N'B4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (220, 3, N'5', 2, 1, 1, N'B5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (221, 3, N'6', 2, 1, 1, N'B6')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (222, 3, N'7', 2, 1, 1, N'B7')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (223, 3, N'8', 2, 1, 4, N'B8')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (224, 3, N'9', 2, 1, 1, N'B9')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (225, 3, N'10', 2, 1, 1, N'B10')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (226, 3, N'1', 3, 1, 1, N'C1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (227, 3, N'2', 3, 1, 1, N'C2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (228, 3, N'3', 3, 1, 4, N'C3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (229, 3, N'4', 3, 1, 1, N'C4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (230, 3, N'5', 3, 1, 1, N'C5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (231, 3, N'6', 3, 1, 1, N'C6')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (232, 3, N'7', 3, 1, 1, N'C7')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (233, 3, N'8', 3, 1, 4, N'C8')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (234, 3, N'9', 3, 1, 1, N'C9')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (235, 3, N'10', 3, 1, 1, N'C10')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (236, 3, N'1', 4, 1, 2, N'D1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (237, 3, N'2', 4, 1, 2, N'D2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (238, 3, N'3', 4, 1, 4, N'D3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (239, 3, N'4', 4, 1, 2, N'D4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (240, 3, N'5', 4, 1, 2, N'D5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (241, 3, N'6', 4, 1, 2, N'D6')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (242, 3, N'7', 4, 1, 2, N'D7')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (243, 3, N'8', 4, 1, 4, N'D8')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (244, 3, N'9', 4, 1, 2, N'D9')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (245, 3, N'10', 4, 1, 2, N'D10')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (246, 3, N'1', 5, 1, 2, N'E1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (247, 3, N'2', 5, 1, 2, N'E2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (248, 3, N'3', 5, 1, 4, N'E3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (249, 3, N'4', 5, 1, 2, N'E4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (250, 3, N'5', 5, 1, 2, N'E5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (251, 3, N'6', 5, 1, 2, N'E6')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (252, 3, N'7', 5, 1, 2, N'E7')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (253, 3, N'8', 5, 1, 4, N'E8')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (254, 3, N'9', 5, 1, 2, N'E9')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (255, 3, N'10', 5, 1, 2, N'E10')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (256, 3, N'1', 6, 1, 4, N'F1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (257, 3, N'2', 6, 1, 4, N'F2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (258, 3, N'3', 6, 1, 4, N'F3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (259, 3, N'4', 6, 1, 2, N'F4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (260, 3, N'5', 6, 1, 2, N'F5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (261, 3, N'6', 6, 1, 2, N'F6')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (262, 3, N'7', 6, 1, 2, N'F7')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (263, 3, N'8', 6, 1, 4, N'F8')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (264, 3, N'9', 6, 1, 4, N'F9')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (265, 3, N'10', 6, 1, 4, N'F10')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (266, 3, N'1', 7, 1, 2, N'G1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (267, 3, N'2', 7, 1, 2, N'G2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (268, 3, N'3', 7, 1, 4, N'G3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (269, 3, N'4', 7, 1, 2, N'G4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (270, 3, N'5', 7, 1, 2, N'G5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (271, 3, N'6', 7, 1, 2, N'G6')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (272, 3, N'7', 7, 1, 2, N'G7')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (273, 3, N'8', 7, 1, 4, N'G8')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (274, 3, N'9', 7, 1, 2, N'G9')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (275, 3, N'10', 7, 1, 2, N'G10')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (276, 3, N'1', 8, 1, 2, N'H1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (277, 3, N'2', 8, 1, 2, N'H2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (278, 3, N'3', 8, 1, 4, N'H3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (279, 3, N'4', 8, 1, 2, N'H4')
GO
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (280, 3, N'5', 8, 1, 2, N'H5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (281, 3, N'6', 8, 1, 2, N'H6')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (282, 3, N'7', 8, 1, 2, N'H7')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (283, 3, N'8', 8, 1, 4, N'H8')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (284, 3, N'9', 8, 1, 2, N'H9')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (285, 3, N'10', 8, 1, 2, N'H10')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (286, 3, N'1', 9, 1, 3, N'I1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (287, 3, N'2', 9, 1, 3, N'I2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (288, 3, N'3', 9, 1, 4, N'I3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (289, 3, N'4', 9, 1, 3, N'I4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (290, 3, N'5', 9, 1, 3, N'I5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (291, 3, N'6', 9, 1, 3, N'I6')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (292, 3, N'7', 9, 1, 3, N'I7')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (293, 3, N'8', 9, 1, 4, N'I8')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (294, 3, N'9', 9, 1, 3, N'I9')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (295, 3, N'10', 9, 1, 3, N'I10')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (296, 3, N'1', 10, 1, 3, N'J1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (297, 3, N'2', 10, 1, 3, N'J2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (298, 3, N'3', 10, 1, 4, N'J3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (299, 3, N'4', 10, 1, 3, N'J4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (300, 3, N'5', 10, 1, 3, N'J5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (301, 3, N'6', 10, 1, 3, N'J6')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (302, 3, N'7', 10, 1, 3, N'J7')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (303, 3, N'8', 10, 1, 4, N'J8')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (304, 3, N'9', 10, 1, 3, N'J9')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (305, 3, N'10', 10, 1, 3, N'J10')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (370, 2, N'1', 1, 1, 1, N'A1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (371, 2, N'2', 1, 1, 1, N'A2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (372, 2, N'3', 1, 1, 1, N'A3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (373, 2, N'4', 1, 1, 1, N'A4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (374, 2, N'5', 1, 1, 1, N'A5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (375, 2, N'1', 2, 1, 1, N'B1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (376, 2, N'2', 2, 1, 1, N'B2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (377, 2, N'3', 2, 1, 1, N'B3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (378, 2, N'4', 2, 1, 1, N'B4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (379, 2, N'5', 2, 1, 1, N'B5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (380, 2, N'1', 3, 1, 1, N'C1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (381, 2, N'2', 3, 1, 1, N'C2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (382, 2, N'3', 3, 1, 1, N'C3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (383, 2, N'4', 3, 1, 1, N'C4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (384, 2, N'5', 3, 1, 1, N'C5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (385, 2, N'1', 4, 1, 1, N'D1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (386, 2, N'2', 4, 1, 1, N'D2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (387, 2, N'3', 4, 1, 1, N'D3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (388, 2, N'4', 4, 1, 1, N'D4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (389, 2, N'5', 4, 1, 1, N'D5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (390, 2, N'1', 5, 1, 1, N'E1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (391, 2, N'2', 5, 1, 1, N'E2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (392, 2, N'3', 5, 1, 1, N'E3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (393, 2, N'4', 5, 1, 1, N'E4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (394, 2, N'5', 5, 1, 1, N'E5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (395, 4, N'1', 1, 1, 1, N'A1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (396, 4, N'2', 1, 1, 1, N'A2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (397, 4, N'3', 1, 1, 1, N'A3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (398, 4, N'4', 1, 1, 1, N'A4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (399, 4, N'5', 1, 1, 1, N'A5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (400, 4, N'1', 2, 1, 4, N'B1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (401, 4, N'2', 2, 1, 4, N'B2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (402, 4, N'3', 2, 1, 4, N'B3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (403, 4, N'4', 2, 1, 4, N'B4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (404, 4, N'5', 2, 1, 4, N'B5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (405, 4, N'1', 3, 1, 2, N'C1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (406, 4, N'2', 3, 1, 2, N'C2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (407, 4, N'3', 3, 1, 2, N'C3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (408, 4, N'4', 3, 1, 2, N'C4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (409, 4, N'5', 3, 1, 2, N'C5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (410, 4, N'1', 4, 1, 2, N'D1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (411, 4, N'2', 4, 1, 2, N'D2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (412, 4, N'3', 4, 1, 2, N'D3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (413, 4, N'4', 4, 1, 2, N'D4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (414, 4, N'5', 4, 1, 2, N'D5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (415, 4, N'1', 5, 1, 3, N'E1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (416, 4, N'2', 5, 1, 3, N'E2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (417, 4, N'3', 5, 1, 3, N'E3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (418, 4, N'4', 5, 1, 3, N'E4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (419, 4, N'5', 5, 1, 3, N'E5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (420, 5, N'1', 1, 1, 1, N'A1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (421, 5, N'2', 1, 1, 2, N'A2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (422, 5, N'3', 1, 1, 3, N'A3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (423, 5, N'4', 1, 1, 1, N'A4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (424, 5, N'5', 1, 1, 1, N'A5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (425, 5, N'6', 1, 1, 1, N'A6')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (426, 5, N'1', 2, 1, 1, N'B1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (427, 5, N'2', 2, 1, 2, N'B2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (428, 5, N'3', 2, 1, 3, N'B3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (429, 5, N'4', 2, 1, 1, N'B4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (430, 5, N'5', 2, 1, 1, N'B5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (431, 5, N'6', 2, 1, 1, N'B6')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (432, 5, N'1', 3, 1, 1, N'C1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (433, 5, N'2', 3, 1, 2, N'C2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (434, 5, N'3', 3, 1, 3, N'C3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (435, 5, N'4', 3, 1, 1, N'C4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (436, 5, N'5', 3, 1, 1, N'C5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (437, 5, N'6', 3, 1, 1, N'C6')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (438, 5, N'1', 4, 1, 1, N'D1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (439, 5, N'2', 4, 1, 2, N'D2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (440, 5, N'3', 4, 1, 3, N'D3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (441, 5, N'4', 4, 1, 1, N'D4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (442, 5, N'5', 4, 1, 1, N'D5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (443, 5, N'6', 4, 1, 1, N'D6')
GO
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (444, 5, N'1', 5, 1, 4, N'E1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (445, 5, N'2', 5, 1, 2, N'E2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (446, 5, N'3', 5, 1, 4, N'E3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (447, 5, N'4', 5, 1, 4, N'E4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (448, 5, N'5', 5, 1, 4, N'E5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (449, 5, N'6', 5, 1, 4, N'E6')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (450, 5, N'1', 6, 1, 1, N'F1')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (451, 5, N'2', 6, 1, 2, N'F2')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (452, 5, N'3', 6, 1, 3, N'F3')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (453, 5, N'4', 6, 1, 1, N'F4')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (454, 5, N'5', 6, 1, 1, N'F5')
INSERT [dbo].[Seat] ([Seat_ID], [Cinema_Room_ID], [Seat_Column], [Seat_Row], [Seat_Status_ID], [Seat_Type_ID], [SeatName]) VALUES (455, 5, N'6', 6, 1, 1, N'F6')
SET IDENTITY_INSERT [dbo].[Seat] OFF
GO
SET IDENTITY_INSERT [dbo].[Seat_Status] ON 

INSERT [dbo].[Seat_Status] ([Seat_Status_ID], [Status_Name]) VALUES (1, N'Available')
INSERT [dbo].[Seat_Status] ([Seat_Status_ID], [Status_Name]) VALUES (2, N'Booked')
INSERT [dbo].[Seat_Status] ([Seat_Status_ID], [Status_Name]) VALUES (3, N'Held')
SET IDENTITY_INSERT [dbo].[Seat_Status] OFF
GO
SET IDENTITY_INSERT [dbo].[Seat_Type] ON 

INSERT [dbo].[Seat_Type] ([Seat_Type_ID], [Type_Name], [Price_Percent], [ColorHex]) VALUES (1, N'Normal', CAST(40000 AS Decimal(18, 0)), N'#cccbc8')
INSERT [dbo].[Seat_Type] ([Seat_Type_ID], [Type_Name], [Price_Percent], [ColorHex]) VALUES (2, N'VIP', CAST(50000 AS Decimal(18, 0)), N'#fa7a7a')
INSERT [dbo].[Seat_Type] ([Seat_Type_ID], [Type_Name], [Price_Percent], [ColorHex]) VALUES (3, N'Couple', CAST(60000 AS Decimal(18, 0)), N'#ffa1f1')
INSERT [dbo].[Seat_Type] ([Seat_Type_ID], [Type_Name], [Price_Percent], [ColorHex]) VALUES (4, N'Disabled', CAST(0 AS Decimal(18, 0)), N'#2F2F2F')
SET IDENTITY_INSERT [dbo].[Seat_Type] OFF
GO
SET IDENTITY_INSERT [dbo].[Status] ON 

INSERT [dbo].[Status] ([Status_ID], [Status_Name]) VALUES (1, N'Active')
INSERT [dbo].[Status] ([Status_ID], [Status_Name]) VALUES (2, N'Deleted')
INSERT [dbo].[Status] ([Status_ID], [Status_Name]) VALUES (3, N'Hidden')
SET IDENTITY_INSERT [dbo].[Status] OFF
GO
INSERT [dbo].[Type] ([Type_ID], [Type_Name]) VALUES (1, N'Action')
INSERT [dbo].[Type] ([Type_ID], [Type_Name]) VALUES (2, N'Comedy')
INSERT [dbo].[Type] ([Type_ID], [Type_Name]) VALUES (3, N'Romance')
INSERT [dbo].[Type] ([Type_ID], [Type_Name]) VALUES (4, N'Drama')
INSERT [dbo].[Type] ([Type_ID], [Type_Name]) VALUES (5, N'Sci-Fi')
INSERT [dbo].[Type] ([Type_ID], [Type_Name]) VALUES (6, N'War')
INSERT [dbo].[Type] ([Type_ID], [Type_Name]) VALUES (7, N'Wuxia')
INSERT [dbo].[Type] ([Type_ID], [Type_Name]) VALUES (8, N'Music')
INSERT [dbo].[Type] ([Type_ID], [Type_Name]) VALUES (9, N'Horror')
INSERT [dbo].[Type] ([Type_ID], [Type_Name]) VALUES (10, N'Adventure')
INSERT [dbo].[Type] ([Type_ID], [Type_Name]) VALUES (11, N'Psychology 18+')
INSERT [dbo].[Type] ([Type_ID], [Type_Name]) VALUES (12, N'Animation')
GO
INSERT [dbo].[Version] ([Version_ID], [Version_Name], [Multi]) VALUES (1, N'2D', CAST(1 AS Decimal(18, 0)))
INSERT [dbo].[Version] ([Version_ID], [Version_Name], [Multi]) VALUES (2, N'4DX', CAST(2 AS Decimal(18, 0)))
INSERT [dbo].[Version] ([Version_ID], [Version_Name], [Multi]) VALUES (3, N'IMAX', CAST(2 AS Decimal(18, 0)))
GO
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC002', N'AC002', N'REFUND', CAST(101600.00 AS Decimal(18, 2)), CAST(N'2025-07-02T00:25:03.187' AS DateTime), CAST(N'2025-08-01T00:25:03.187' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC003', N'AC002', N'REFUND', CAST(147200.00 AS Decimal(18, 2)), CAST(N'2025-07-02T07:58:42.520' AS DateTime), CAST(N'2025-08-01T07:58:42.520' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC004', N'AC002', N'REFUND', CAST(101600.00 AS Decimal(18, 2)), CAST(N'2025-07-02T08:30:26.733' AS DateTime), CAST(N'2025-08-01T08:30:26.733' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC005', N'AC002', N'REFUND', CAST(83600.00 AS Decimal(18, 2)), CAST(N'2025-07-02T08:32:01.080' AS DateTime), CAST(N'2025-08-01T08:32:01.080' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC006', N'AC002', N'REFUND', CAST(59200.00 AS Decimal(18, 2)), CAST(N'2025-07-02T10:05:43.943' AS DateTime), CAST(N'2025-08-01T10:05:43.943' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC007', N'AC002', N'REFUND', CAST(37600.00 AS Decimal(18, 2)), CAST(N'2025-07-02T10:05:59.423' AS DateTime), CAST(N'2025-08-01T10:05:59.423' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC008', N'AC002', N'REFUND', CAST(57600.00 AS Decimal(18, 2)), CAST(N'2025-07-02T10:07:10.727' AS DateTime), CAST(N'2025-08-01T10:07:10.727' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC009', N'AC002', N'REFUND', CAST(79200.00 AS Decimal(18, 2)), CAST(N'2025-07-02T10:30:38.560' AS DateTime), CAST(N'2025-08-01T10:30:38.560' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC010', N'AC002', N'REFUND', CAST(136800.00 AS Decimal(18, 2)), CAST(N'2025-07-02T10:30:43.203' AS DateTime), CAST(N'2025-08-01T10:30:43.203' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC011', N'AC002', N'REFUND', CAST(37600.00 AS Decimal(18, 2)), CAST(N'2025-07-02T10:31:38.197' AS DateTime), CAST(N'2025-08-01T10:31:38.197' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC012', N'AC002', N'REFUND', CAST(57600.00 AS Decimal(18, 2)), CAST(N'2025-07-02T10:34:51.207' AS DateTime), CAST(N'2025-08-01T10:34:51.207' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC013', N'AC002', N'REFUND', CAST(79200.00 AS Decimal(18, 2)), CAST(N'2025-07-02T10:35:11.857' AS DateTime), CAST(N'2025-08-01T10:35:11.857' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC014', N'AC002', N'REFUND', CAST(79200.00 AS Decimal(18, 2)), CAST(N'2025-07-02T11:22:45.480' AS DateTime), CAST(N'2025-08-01T11:22:45.480' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC015', N'AC002', N'REFUND', CAST(63600.00 AS Decimal(18, 2)), CAST(N'2025-07-02T11:22:50.890' AS DateTime), CAST(N'2025-08-01T11:22:50.890' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC016', N'AC002', N'REFUND', CAST(40800.00 AS Decimal(18, 2)), CAST(N'2025-07-02T11:22:55.153' AS DateTime), CAST(N'2025-08-01T11:22:55.153' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC017', N'AC002', N'REFUND', CAST(151200.00 AS Decimal(18, 2)), CAST(N'2025-07-02T11:33:22.110' AS DateTime), CAST(N'2025-08-01T11:33:22.110' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC018', N'AC002', N'REFUND', CAST(72000.00 AS Decimal(18, 2)), CAST(N'2025-07-02T11:33:25.950' AS DateTime), CAST(N'2025-08-01T11:33:25.950' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC019', N'AC002', N'REFUND', CAST(60800.00 AS Decimal(18, 2)), CAST(N'2025-07-02T11:33:31.403' AS DateTime), CAST(N'2025-08-01T11:33:31.403' AS DateTime), 1, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC020', N'AC002', N'REFUND', CAST(83600.00 AS Decimal(18, 2)), CAST(N'2025-07-02T11:33:36.537' AS DateTime), CAST(N'2025-08-01T11:33:36.537' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC021', N'AC002', N'REFUND', CAST(83600.00 AS Decimal(18, 2)), CAST(N'2025-07-02T11:33:40.883' AS DateTime), CAST(N'2025-08-01T11:33:40.883' AS DateTime), 1, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC022', N'AC002', N'REFUND', CAST(60800.00 AS Decimal(18, 2)), CAST(N'2025-07-02T11:33:45.983' AS DateTime), CAST(N'2025-08-01T11:33:45.983' AS DateTime), 1, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC023', N'AC002', N'REFUND', CAST(235600.00 AS Decimal(18, 2)), CAST(N'2025-07-02T11:36:42.310' AS DateTime), CAST(N'2025-08-01T11:36:42.310' AS DateTime), 1, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC024', N'AC002', N'EXPIRY', CAST(20000000.00 AS Decimal(18, 2)), CAST(N'2025-06-01T14:38:57.717' AS DateTime), CAST(N'2025-06-02T14:38:57.717' AS DateTime), 0, N'/images/vouchers/voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC025', N'AC002', N'REFUND', CAST(11200.00 AS Decimal(18, 2)), CAST(N'2025-07-02T15:45:15.440' AS DateTime), CAST(N'2025-08-01T15:45:15.440' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC026', N'AC002', N'REFUND', CAST(43200.00 AS Decimal(18, 2)), CAST(N'2025-07-09T08:36:19.217' AS DateTime), CAST(N'2025-08-08T08:36:19.217' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC027', N'AC002', N'REFUND', CAST(64800.00 AS Decimal(18, 2)), CAST(N'2025-07-09T14:49:45.983' AS DateTime), CAST(N'2025-08-08T14:49:45.987' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC028', N'AC002', N'REFUND', CAST(76000.00 AS Decimal(18, 2)), CAST(N'2025-07-09T14:50:03.553' AS DateTime), CAST(N'2025-08-08T14:50:03.553' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC029', N'AC002', N'REFUND', CAST(172800.00 AS Decimal(18, 2)), CAST(N'2025-07-09T14:51:41.683' AS DateTime), CAST(N'2025-08-08T14:51:41.683' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC030', N'AC002', N'REFUND', CAST(68000.00 AS Decimal(18, 2)), CAST(N'2025-07-14T08:24:58.977' AS DateTime), CAST(N'2025-08-13T08:24:58.977' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC031', N'AC002', N'REFUND', CAST(74800.00 AS Decimal(18, 2)), CAST(N'2025-07-14T08:39:14.803' AS DateTime), CAST(N'2025-08-13T08:39:14.803' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC032', N'AC002', N'REFUND', CAST(74800.00 AS Decimal(18, 2)), CAST(N'2025-07-14T09:39:14.630' AS DateTime), CAST(N'2025-08-13T09:39:14.630' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC033', N'AC002', N'REFUND', CAST(68000.00 AS Decimal(18, 2)), CAST(N'2025-07-14T09:47:27.590' AS DateTime), CAST(N'2025-08-13T09:47:27.590' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC034', N'AC002', N'REFUND', CAST(102000.00 AS Decimal(18, 2)), CAST(N'2025-07-14T09:52:16.303' AS DateTime), CAST(N'2025-08-13T09:52:16.303' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC035', N'AC002', N'REFUND', CAST(102000.00 AS Decimal(18, 2)), CAST(N'2025-07-14T09:54:56.843' AS DateTime), CAST(N'2025-08-13T09:54:56.843' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC036', N'AC002', N'REFUND', CAST(172800.00 AS Decimal(18, 2)), CAST(N'2025-07-14T10:01:55.497' AS DateTime), CAST(N'2025-08-13T10:01:55.497' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC037', N'AC002', N'REFUND', CAST(172800.00 AS Decimal(18, 2)), CAST(N'2025-07-14T10:03:08.423' AS DateTime), CAST(N'2025-08-13T10:03:08.423' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC038', N'AC002', N'REFUND', CAST(216000.00 AS Decimal(18, 2)), CAST(N'2025-07-14T10:03:55.760' AS DateTime), CAST(N'2025-08-13T10:03:55.760' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC039', N'AC002', N'REFUND', CAST(228000.00 AS Decimal(18, 2)), CAST(N'2025-07-14T10:07:45.757' AS DateTime), CAST(N'2025-08-13T10:07:45.757' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC040', N'AC002', N'REFUND', CAST(216000.00 AS Decimal(18, 2)), CAST(N'2025-07-14T10:18:10.540' AS DateTime), CAST(N'2025-08-13T10:18:10.540' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC041', N'AC002', N'REFUND', CAST(228000.00 AS Decimal(18, 2)), CAST(N'2025-07-14T11:01:18.510' AS DateTime), CAST(N'2025-08-13T11:01:18.510' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC042', N'AC002', N'REFUND', CAST(228000.00 AS Decimal(18, 2)), CAST(N'2025-07-14T14:23:51.337' AS DateTime), CAST(N'2025-08-13T14:23:51.337' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC043', N'AC002', N'REFUND', CAST(81600.00 AS Decimal(18, 2)), CAST(N'2025-07-14T14:28:24.553' AS DateTime), CAST(N'2025-08-13T14:28:24.553' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC044', N'AC002', N'REFUND', CAST(34000.00 AS Decimal(18, 2)), CAST(N'2025-07-14T16:53:24.267' AS DateTime), CAST(N'2025-08-13T16:53:24.267' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC045', N'AC002', N'REFUND', CAST(95200.00 AS Decimal(18, 2)), CAST(N'2025-07-14T19:27:37.477' AS DateTime), CAST(N'2025-08-13T19:27:37.477' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC046', N'AC002', N'REFUND', CAST(34000.00 AS Decimal(18, 2)), CAST(N'2025-07-14T19:31:12.370' AS DateTime), CAST(N'2025-08-13T19:31:12.370' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC047', N'AC002', N'REFUND', CAST(216000.00 AS Decimal(18, 2)), CAST(N'2025-07-14T19:37:46.420' AS DateTime), CAST(N'2025-08-13T19:37:46.420' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC048', N'AC002', N'REFUND', CAST(230400.00 AS Decimal(18, 2)), CAST(N'2025-07-14T19:41:29.717' AS DateTime), CAST(N'2025-08-13T19:41:29.717' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC049', N'AC002', N'REFUND', CAST(216000.00 AS Decimal(18, 2)), CAST(N'2025-07-14T19:44:10.183' AS DateTime), CAST(N'2025-08-13T19:44:10.183' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC050', N'AC002', N'REFUND', CAST(149600.00 AS Decimal(18, 2)), CAST(N'2025-07-16T15:13:49.820' AS DateTime), CAST(N'2025-08-15T15:13:49.820' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
INSERT [dbo].[Voucher] ([Voucher_ID], [Account_ID], [Code], [Value], [CreatedDate], [ExpiryDate], [IsUsed], [Image]) VALUES (N'VC051', N'AC013', N'REFUND-INV075', CAST(261000.00 AS Decimal(18, 2)), CAST(N'2025-07-19T13:40:51.567' AS DateTime), CAST(N'2025-08-18T13:40:51.567' AS DateTime), 0, N'/images/vouchers/refund-voucher.jpg')
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Account__A9D10534B0A8E429]    Script Date: 7/19/2025 5:34:46 PM ******/
ALTER TABLE [dbo].[Account] ADD UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__Account__B15BE12EC989212B]    Script Date: 7/19/2025 5:34:46 PM ******/
ALTER TABLE [dbo].[Account] ADD UNIQUE NONCLUSTERED 
(
	[USERNAME] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_CoupleSeat_First]    Script Date: 7/19/2025 5:34:46 PM ******/
ALTER TABLE [dbo].[CoupleSeat] ADD  CONSTRAINT [UQ_CoupleSeat_First] UNIQUE NONCLUSTERED 
(
	[FirstSeatId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_CoupleSeat_Second]    Script Date: 7/19/2025 5:34:46 PM ******/
ALTER TABLE [dbo].[CoupleSeat] ADD  CONSTRAINT [UQ_CoupleSeat_Second] UNIQUE NONCLUSTERED 
(
	[SecondSeatId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_FoodInvoice_FoodID]    Script Date: 7/19/2025 5:34:46 PM ******/
CREATE NONCLUSTERED INDEX [IX_FoodInvoice_FoodID] ON [dbo].[FoodInvoice]
(
	[Food_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_FoodInvoice_InvoiceID]    Script Date: 7/19/2025 5:34:46 PM ******/
CREATE NONCLUSTERED INDEX [IX_FoodInvoice_InvoiceID] ON [dbo].[FoodInvoice]
(
	[Invoice_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_Rank_RankName]    Script Date: 7/19/2025 5:34:46 PM ******/
ALTER TABLE [dbo].[Rank] ADD  CONSTRAINT [UQ_Rank_RankName] UNIQUE NONCLUSTERED 
(
	[Rank_Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Cinema_Room] ADD  CONSTRAINT [DF_CinemaRoom_Status]  DEFAULT ((1)) FOR [Status_ID]
GO
ALTER TABLE [dbo].[Food] ADD  DEFAULT ((1)) FOR [Status]
GO
ALTER TABLE [dbo].[Food] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Invoice] ADD  CONSTRAINT [DF_Invoice_Promotion_Discount]  DEFAULT ('0') FOR [Promotion_Discount]
GO
ALTER TABLE [dbo].[Invoice] ADD  DEFAULT ((0)) FOR [Cancel]
GO
ALTER TABLE [dbo].[Member] ADD  DEFAULT ((0)) FOR [Total_Points]
GO
ALTER TABLE [dbo].[Promotion] ADD  DEFAULT ((1)) FOR [Is_Active]
GO
ALTER TABLE [dbo].[Rank] ADD  DEFAULT ((0)) FOR [PointEarningPercentage]
GO
ALTER TABLE [dbo].[Rank] ADD  DEFAULT ('linear-gradient(135deg, #4e54c8 0%, #6c63ff 50%, #8f94fb 100%)') FOR [ColorGradient]
GO
ALTER TABLE [dbo].[Rank] ADD  DEFAULT ('fa-crown') FOR [IconClass]
GO
ALTER TABLE [dbo].[Seat_Type] ADD  DEFAULT ((100)) FOR [Price_Percent]
GO
ALTER TABLE [dbo].[Seat_Type] ADD  DEFAULT ('#FFFFFF') FOR [ColorHex]
GO
ALTER TABLE [dbo].[Voucher] ADD  DEFAULT ((0)) FOR [IsUsed]
GO
ALTER TABLE [dbo].[Account]  WITH CHECK ADD  CONSTRAINT [FK_Account_Rank] FOREIGN KEY([Rank_ID])
REFERENCES [dbo].[Rank] ([Rank_ID])
GO
ALTER TABLE [dbo].[Account] CHECK CONSTRAINT [FK_Account_Rank]
GO
ALTER TABLE [dbo].[Account]  WITH CHECK ADD  CONSTRAINT [FK_Account_Role] FOREIGN KEY([Role_ID])
REFERENCES [dbo].[Roles] ([Role_ID])
GO
ALTER TABLE [dbo].[Account] CHECK CONSTRAINT [FK_Account_Role]
GO
ALTER TABLE [dbo].[Cinema_Room]  WITH CHECK ADD FOREIGN KEY([Version_ID])
REFERENCES [dbo].[Version] ([Version_ID])
GO
ALTER TABLE [dbo].[Cinema_Room]  WITH CHECK ADD  CONSTRAINT [FK_CinemaRoom_Status] FOREIGN KEY([Status_ID])
REFERENCES [dbo].[Status] ([Status_ID])
GO
ALTER TABLE [dbo].[Cinema_Room] CHECK CONSTRAINT [FK_CinemaRoom_Status]
GO
ALTER TABLE [dbo].[CoupleSeat]  WITH CHECK ADD FOREIGN KEY([FirstSeatId])
REFERENCES [dbo].[Seat] ([Seat_ID])
GO
ALTER TABLE [dbo].[CoupleSeat]  WITH CHECK ADD FOREIGN KEY([SecondSeatId])
REFERENCES [dbo].[Seat] ([Seat_ID])
GO
ALTER TABLE [dbo].[Employee]  WITH CHECK ADD  CONSTRAINT [FK_Employee_Account] FOREIGN KEY([Account_ID])
REFERENCES [dbo].[Account] ([Account_ID])
GO
ALTER TABLE [dbo].[Employee] CHECK CONSTRAINT [FK_Employee_Account]
GO
ALTER TABLE [dbo].[FoodInvoice]  WITH CHECK ADD  CONSTRAINT [FK_FoodInvoice_Food] FOREIGN KEY([Food_ID])
REFERENCES [dbo].[Food] ([FoodId])
GO
ALTER TABLE [dbo].[FoodInvoice] CHECK CONSTRAINT [FK_FoodInvoice_Food]
GO
ALTER TABLE [dbo].[FoodInvoice]  WITH CHECK ADD  CONSTRAINT [FK_FoodInvoice_Invoice] FOREIGN KEY([Invoice_ID])
REFERENCES [dbo].[Invoice] ([Invoice_ID])
GO
ALTER TABLE [dbo].[FoodInvoice] CHECK CONSTRAINT [FK_FoodInvoice_Invoice]
GO
ALTER TABLE [dbo].[Invoice]  WITH CHECK ADD FOREIGN KEY([Movie_Show_Id])
REFERENCES [dbo].[Movie_Show] ([Movie_Show_ID])
GO
ALTER TABLE [dbo].[Invoice]  WITH CHECK ADD FOREIGN KEY([Voucher_ID])
REFERENCES [dbo].[Voucher] ([Voucher_ID])
GO
ALTER TABLE [dbo].[Invoice]  WITH CHECK ADD  CONSTRAINT [FK_Invoice_Account] FOREIGN KEY([Account_ID])
REFERENCES [dbo].[Account] ([Account_ID])
GO
ALTER TABLE [dbo].[Invoice] CHECK CONSTRAINT [FK_Invoice_Account]
GO
ALTER TABLE [dbo].[Member]  WITH CHECK ADD  CONSTRAINT [FK_Member_Account] FOREIGN KEY([Account_ID])
REFERENCES [dbo].[Account] ([Account_ID])
GO
ALTER TABLE [dbo].[Member] CHECK CONSTRAINT [FK_Member_Account]
GO
ALTER TABLE [dbo].[Movie_Show]  WITH CHECK ADD FOREIGN KEY([Cinema_Room_ID])
REFERENCES [dbo].[Cinema_Room] ([Cinema_Room_ID])
GO
ALTER TABLE [dbo].[Movie_Show]  WITH CHECK ADD FOREIGN KEY([Movie_ID])
REFERENCES [dbo].[Movie] ([Movie_ID])
GO
ALTER TABLE [dbo].[Movie_Show]  WITH CHECK ADD FOREIGN KEY([Schedule_ID])
REFERENCES [dbo].[Schedule] ([Schedule_ID])
GO
ALTER TABLE [dbo].[Movie_Show]  WITH CHECK ADD FOREIGN KEY([Version_ID])
REFERENCES [dbo].[Version] ([Version_ID])
GO
ALTER TABLE [dbo].[Movie_Type]  WITH CHECK ADD FOREIGN KEY([Movie_ID])
REFERENCES [dbo].[Movie] ([Movie_ID])
GO
ALTER TABLE [dbo].[Movie_Type]  WITH CHECK ADD FOREIGN KEY([Type_ID])
REFERENCES [dbo].[Type] ([Type_ID])
GO
ALTER TABLE [dbo].[Movie_Version]  WITH CHECK ADD FOREIGN KEY([Movie_ID])
REFERENCES [dbo].[Movie] ([Movie_ID])
GO
ALTER TABLE [dbo].[Movie_Version]  WITH CHECK ADD FOREIGN KEY([Version_ID])
REFERENCES [dbo].[Version] ([Version_ID])
GO
ALTER TABLE [dbo].[PromotionCondition]  WITH CHECK ADD FOREIGN KEY([ConditionType_ID])
REFERENCES [dbo].[ConditionType] ([ConditionType_ID])
GO
ALTER TABLE [dbo].[PromotionCondition]  WITH CHECK ADD FOREIGN KEY([Promotion_ID])
REFERENCES [dbo].[Promotion] ([Promotion_ID])
GO
ALTER TABLE [dbo].[Schedule_Seat]  WITH CHECK ADD FOREIGN KEY([Invoice_ID])
REFERENCES [dbo].[Invoice] ([Invoice_ID])
GO
ALTER TABLE [dbo].[Schedule_Seat]  WITH CHECK ADD FOREIGN KEY([Movie_Show_ID])
REFERENCES [dbo].[Movie_Show] ([Movie_Show_ID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Schedule_Seat]  WITH CHECK ADD FOREIGN KEY([Seat_ID])
REFERENCES [dbo].[Seat] ([Seat_ID])
GO
ALTER TABLE [dbo].[Schedule_Seat]  WITH CHECK ADD FOREIGN KEY([Seat_Status_ID])
REFERENCES [dbo].[Seat_Status] ([Seat_Status_ID])
GO
ALTER TABLE [dbo].[Seat]  WITH CHECK ADD FOREIGN KEY([Cinema_Room_ID])
REFERENCES [dbo].[Cinema_Room] ([Cinema_Room_ID])
GO
ALTER TABLE [dbo].[Seat]  WITH CHECK ADD FOREIGN KEY([Seat_Status_ID])
REFERENCES [dbo].[Seat_Status] ([Seat_Status_ID])
GO
ALTER TABLE [dbo].[Seat]  WITH CHECK ADD FOREIGN KEY([Seat_Type_ID])
REFERENCES [dbo].[Seat_Type] ([Seat_Type_ID])
GO
ALTER TABLE [dbo].[Voucher]  WITH CHECK ADD  CONSTRAINT [FK_Voucher_Account] FOREIGN KEY([Account_ID])
REFERENCES [dbo].[Account] ([Account_ID])
GO
ALTER TABLE [dbo].[Voucher] CHECK CONSTRAINT [FK_Voucher_Account]
GO
ALTER TABLE [dbo].[Wishlist]  WITH CHECK ADD FOREIGN KEY([Account_ID])
REFERENCES [dbo].[Account] ([Account_ID])
GO
ALTER TABLE [dbo].[Wishlist]  WITH CHECK ADD FOREIGN KEY([Movie_ID])
REFERENCES [dbo].[Movie] ([Movie_ID])
GO
ALTER TABLE [dbo].[CoupleSeat]  WITH CHECK ADD CHECK  (([FirstSeatId]<[SecondSeatId]))
GO
ALTER TABLE [dbo].[CoupleSeat]  WITH CHECK ADD CHECK  (([FirstSeatId]<>[SecondSeatId]))
GO
ALTER TABLE [dbo].[Member]  WITH CHECK ADD  CONSTRAINT [CK_Member_TotalPoints] CHECK  (([Total_Points]>=isnull([Score],(0))))
GO
ALTER TABLE [dbo].[Member] CHECK CONSTRAINT [CK_Member_TotalPoints]
GO
USE [master]
GO
ALTER DATABASE [MovieTheater] SET  READ_WRITE 
GO
