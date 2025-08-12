# 🎬 Cinema Website FPT777 - Modern Movie Theater Management System

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![C#](https://img.shields.io/badge/C%23-239120?logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-red.svg)](https://dotnet.microsoft.com/apps/aspnet)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2019-CC2927?logo=microsoft-sql-server&logoColor=white)](https://www.microsoft.com/en-us/sql-server)
[![SignalR](https://img.shields.io/badge/SignalR-1.2.0-green.svg)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> **A comprehensive, feature-rich cinema management system built with ASP.NET Core 8.0, featuring real-time booking, admin dashboard, payment integration, and modern UI/UX.**

## 🌟 Features

### 🎭 **User Experience**
- **Modern Hero Section** with dynamic movie carousel and background transitions
- **Responsive Design** optimized for all devices (desktop, tablet, mobile)
- **Real-time Seat Selection** with SignalR for live updates
- **Interactive Movie Grid** with filtering and search capabilities
- **User Authentication** with Google OAuth and JWT support
- **Member Loyalty System** with points and rank progression

### 🎫 **Booking & Ticketing**
- **Advanced Seat Selection** with visual seat map
- **Real-time Availability** updates during booking process
- **Multiple Payment Methods** (VNPay, QR Payment, PayOS)
- **Food & Beverage** ordering during ticket booking
- **Promotion & Voucher** system with automatic discounts
- **Booking History** and ticket management
- **QR Code Generation** for tickets and payments

### 🎬 **Movie Management**
- **Comprehensive Movie Database** with cast, director, and metadata
- **Showtime Scheduling** with multiple cinema rooms
- **Movie Categories** (Now Showing, Coming Soon, etc.)
- **Trailer Integration** and movie details
- **Rating & Review** system
- **Movie Search** and filtering

### 🏢 **Admin Dashboard**
- **Real-time Analytics** with interactive charts and metrics
- **User Management** (Members, Employees, Admins)
- **Content Management** (Movies, Promotions, Food, Vouchers)
- **Booking Management** with detailed oversight
- **Financial Reports** and revenue tracking
- **System Monitoring** and performance metrics

### 🔐 **Security & Authentication**
- **Role-based Access Control** (Admin, Employee, Member, Guest)
- **JWT Token Authentication** with secure cookie handling
- **Google OAuth Integration** for seamless login
- **Payment Security Middleware** for transaction protection
- **Input Validation** and SQL injection prevention

## 🚀 Technology Stack

### **Backend**
- **ASP.NET Core 8.0** - Modern web framework
- **Entity Framework Core 9.0** - ORM for database operations
- **SQL Server** - Relational database
- **SignalR** - Real-time communication
- **JWT Bearer** - Token-based authentication
- **Serilog** - Structured logging

### **Frontend**
- **Razor Views** - Server-side templating
- **Bootstrap 5** - Responsive CSS framework
- **jQuery** - JavaScript library
- **Chart.js** - Interactive charts and graphs
- **Swiper.js** - Touch slider for mobile
- **Font Awesome** - Icon library

### **Payment Integration**
- **VNPay** - Vietnamese payment gateway
- **PayOS** - Modern payment platform
- **QR Code Generation** - Custom payment QR codes
- **VietQR** - Bank transfer integration

### **Testing & Quality**
- **xUnit** - Unit testing framework
- **Code Coverage** - Comprehensive test coverage
- **Integration Tests** - End-to-end testing
- **Custom Web Application Factory** - Test infrastructure

## 📁 Project Structure

```
CinemaWebsiteFPT777/
├── Controllers/           # MVC Controllers
│   ├── AccountController.cs
│   ├── AdminController.cs
│   ├── BookingController.cs
│   ├── MovieController.cs
│   └── ...
├── Models/                # Entity Models
│   ├── Account.cs
│   ├── Movie.cs
│   ├── CinemaRoom.cs
│   └── ...
├── Services/              # Business Logic Layer
│   ├── AccountService.cs
│   ├── BookingService.cs
│   ├── MovieService.cs
│   └── ...
├── Repository/            # Data Access Layer
│   ├── AccountRepository.cs
│   ├── MovieRepository.cs
│   └── ...
├── Views/                 # Razor Views
│   ├── Home/
│   ├── Admin/
│   ├── Booking/
│   └── ...
├── wwwroot/               # Static Assets
│   ├── css/
│   ├── js/
│   └── images/
├── Hubs/                  # SignalR Hubs
│   ├── SeatHub.cs
│   ├── DashboardHub.cs
│   └── CinemaHub.cs
├── Middleware/            # Custom Middleware
├── ViewModels/            # Data Transfer Objects
└── MovieTheater.Tests/    # Test Project
```

## 🛠️ Prerequisites

- **.NET 8.0 SDK** or later
- **SQL Server 2019** or later
- **Visual Studio 2022** or **VS Code**
- **Git** for version control

## ⚡ Quick Start

### 1. **Clone the Repository**
```bash
git clone https://github.com/quocdat2601/CinemaWebsiteFPT777.git
cd CinemaWebsiteFPT777
```

### 2. **Database Setup**
```bash
# Restore the database using the provided SQL script
sqlcmd -S (local) -i Cinama.sql
```

### 3. **Configuration**
Update `appsettings.json` with your database connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(local);uid=sa;pwd=your_password;database=MovieTheater;TrustServerCertificate=True;"
  }
}
```

### 4. **Install Dependencies**
```bash
dotnet restore
```

### 5. **Run the Application**
```bash
dotnet run
```

The application will be available at `https://localhost:7201`

## 🔧 Configuration

### **Authentication Settings**
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your_google_client_id",
      "ClientSecret": "your_google_client_secret"
    }
  }
}
```

### **Payment Configuration**
```json
{
  "VNPay": {
    "TmnCode": "your_tmn_code",
    "HashSecret": "your_hash_secret",
    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
  },
  "QRPayment": {
    "PayOSClientId": "your_payos_client_id",
    "PayOSApiKey": "your_payos_api_key"
  }
}
```

## 🧪 Testing

### **Run Tests**
```bash
dotnet test
```

### **Generate Coverage Report**
```bash
# Windows
generate-coverage.bat

# Manual
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"MovieTheater.Tests/TestResults/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

## 📊 Key Features in Detail

### **Real-time Seat Booking**
- Live seat availability updates using SignalR
- Visual seat map with different seat types
- Couple seat booking support
- Real-time conflict resolution

### **Advanced Payment System**
- Multiple payment gateways integration
- Secure payment processing
- Automatic invoice generation
- Payment verification and confirmation

### **Member Management**
- Point accumulation system
- Rank progression (Bronze, Silver, Gold, Platinum)
- Automatic rank upgrades
- Member benefits and discounts

### **Admin Dashboard**
- Real-time revenue tracking
- User activity monitoring
- System performance metrics
- Comprehensive reporting tools

## 🌐 API Documentation

Comprehensive API documentation is available in the `Document/API_DOC.pdf` file, covering all endpoints, request/response formats, and authentication requirements.

## 📚 Additional Documentation

- **Deployment Guide** - `Document/DEPLOYMENT_GUIDE.pdf`
- **Software User Manual** - `Document/Software User Manual Template.pdf`
- **Project Report** - `Document/TEAM 3 - FINAL PROJECT.pdf`

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Team

**Team 3 - FPT University**
- **Project Lead**: [Your Name]
- **Backend Development**: [Team Members]
- **Frontend Development**: [Team Members]
- **Database Design**: [Team Members]
- **Testing & QA**: [Team Members]

## 🙏 Acknowledgments

- **FPT University** for providing the platform and resources
- **Microsoft** for ASP.NET Core and development tools
- **Open Source Community** for various libraries and frameworks
- **Team Members** for their dedication and hard work

## 📞 Support

For support and questions:
- **Email**: [quocdat2601@example.com]
- **GitHub Issues**: [Create an issue](https://github.com/quocdat2601/CinemaWebsiteFPT777/issues)
- **Documentation**: Check the `Document/` folder for detailed guides

---

<div align="center">

**⭐ Star this repository if you find it helpful! ⭐**

**🎬 Built with ❤️ by Team 3 - FPT University 🎬**

</div> 
