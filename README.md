# Hotel Room Reservation System

A comprehensive web-based hotel room reservation system built with ASP.NET Core MVC, featuring advanced booking management, user authentication, payment processing, and administrative tools.

## 🏨 Project Overview

This Hotel Room Reservation System is a full-stack web application designed to streamline hotel operations from customer reservations to administrative management. The system provides a complete solution for hotel management with modern web technologies and user-friendly interfaces.

## ✨ Key Features

### 🔐 Security Management Module
- **User Authentication & Authorization**: Secure login/logout system with role-based access control
- **Password Security**: Advanced password hashing and recovery mechanisms
- **Email Token-Based Recovery**: Secure password recovery and account assistance with resend functionality
- **Login Protection**: Account blocking and CAPTCHA integration for enhanced security
- **Multi-Factor Authentication**: SMS-based MFA using Twilio service integration
- **Email Verification**: Token-based user activation with resend capabilities

### 👥 User Management Module
- **CRUD Operations**: Complete user account management for admins and members
- **Profile Management**: Photo upload functionality and profile customization
- **Account Control**: Block/unblock user accounts with administrative oversight
- **Advanced Search & Filter**: AJAX-powered searching, sorting, filtering, and pagination
- **Webcam Integration**: Real-time photo capture functionality
- **Bulk Operations**: Excel-based import/export with batch insert, update, and delete operations
- **Selective Export**: Export specific user data based on selection criteria

### 🏠 Room Management Module
- **Room Category Management**: Complete CRUD operations for room types
- **Room Maintenance**: Comprehensive room management with availability tracking
- **Advanced Listing**: Detailed room listings with AJAX integration
- **Media Management**: Multiple photo uploads with drag-and-drop functionality
- **Batch Operations**: Multiple room add, update, replace, and delete operations
- **Interactive Features**: On-screen room movement and availability updates
- **Visual Management**: One-to-many photo relationships per room

### 📊 Report Management Module
- **Analytics Dashboard**: Comprehensive reporting with visual charts
- **Sales Analytics**: Top-selling room reports and revenue tracking
- **Activity Logs**: Detailed reservation and payment history
- **Occupancy Reports**: Room utilization and availability analytics
- **Data Export**: PDF and Excel download capabilities
- **Visual Representation**: Interactive charts for all report types
- **Compact Data Views**: Streamlined data presentation

### 📅 Reservation Management Module
- **Customer Reservations**: Self-service booking and cancellation system
- **Administrative Control**: Admin reservation management with approval workflows
- **Booking History**: Comprehensive reservation tracking and history
- **Schedule Management**: Visual reservation calendar and scheduling
- **Flexible Modifications**: Check-in/check-out date adjustments
- **Batch Operations**: Multiple reservation status updates
- **AJAX Integration**: Real-time searching, sorting, and pagination

### 💳 Payment & Billing Module
- **Multiple Payment Methods**: Integration with Stripe API and PayPal
- **Invoice Generation**: Automated PDF invoice creation and email delivery
- **Payment Approval**: Administrative payment request management
- **Billing History**: Comprehensive payment and billing logs
- **Transaction Security**: Secure payment processing with encryption

### 📝 Feedback Management Module
- **Customer Reviews**: Complete feedback CRUD operations
- **Rating System**: Star-based rating functionality
- **Media Support**: Multiple photo uploads with drag-and-drop
- **Status Management**: Feedback approval and moderation system
- **Advanced Filtering**: AJAX-powered search and filter capabilities
- **Administrative Oversight**: Feedback monitoring and response management

### 🎯 Membership & Rewards Module
- **Loyalty Program**: Points-based reward system
- **Membership Tiers**: Multiple membership plans with benefits
- **Daily Check-in**: Point accumulation through daily activities
- **Reward Exchange**: Points-to-benefits conversion system
- **Batch Management**: Multiple status updates and management
- **Price Discounts**: Member-exclusive pricing benefits

### 📞 Communication Module
- **Contact System**: Guest messaging functionality
- **Email Integration**: Administrative email response system
- **Message Management**: Comprehensive communication tracking
- **Automated Responses**: Email-based reply system

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core MVC
- **Database**: Entity Framework Core with SQL Server
- **Frontend**: HTML5, CSS3, JavaScript, AJAX
- **Authentication**: ASP.NET Core Identity
- **Payment Processing**: Stripe API, PayPal Integration
- **Communication**: Twilio SMS Service
- **File Processing**: Excel Import/Export functionality
- **PDF Generation**: Custom PDF generation for invoices and reports

##  Installation & Setup

1. **Clone the Repository**
   ```bash
   git clone https://github.com/jimsenglee/HotelReservationSystem.git
   cd HotelReservationSystem
   ```

2. **Install Dependencies**
   ```bash
   dotnet restore
   ```

3. **Database Setup**
   ```bash
   dotnet ef database update
   ```

4. **Configure App Settings**
   - Update `appsettings.json` with your database connection string
   - Configure API credentials (see `CONFIGURATION.md` for detailed setup)
   - Set up Twilio settings for SMS functionality
   - Configure Stripe and PayPal API credentials
   - Set up Gmail SMTP for email functionality
   - Configure reCAPTCHA settings

5. **Run the Application**
   ```bash
   dotnet run
   ```

## 📁 Project Structure

```
HotelRoomReservationSystem/
├── Controllers/           # MVC Controllers
├── Models/               # Data models and ViewModels
├── Views/                # Razor views
├── BLL/                  # Business Logic Layer
├── DAL/                  # Data Access Layer
├── Services/             # External service integrations
├── wwwroot/              # Static files (CSS, JS, Images)
├── Migrations/           # Entity Framework migrations
└── Properties/           # Application configuration
```

## 🔧 Key Features Implementation

- **AJAX Integration**: Real-time updates without page refresh
- **Responsive Design**: Mobile-friendly interface
- **Security**: Multi-layered security with encryption and authentication
- **File Management**: Drag-and-drop uploads with multiple file support
- **Excel Integration**: Bulk data operations with Excel import/export
- **PDF Generation**: Automated document creation
- **Email System**: Automated notifications and communication
- **Payment Gateway**: Secure payment processing
- **Reporting**: Comprehensive analytics with visual charts

## 📸 Screenshots

The system includes comprehensive interfaces for:
- User authentication and profile management
- Room booking and management
- Administrative dashboards
- Payment processing
- Reporting and analytics
- Feedback and communication systems

## 🤝 Contributing

This project was developed as a comprehensive hotel management solution. For any questions or collaboration opportunities, please feel free to reach out.

## 📧 Contact

**Email**: gimsheng.lee@gmail.com  
**GitHub**: [@jimsenglee](https://github.com/jimsenglee)

## �️ Database File

If you need the database file to run this project, please send me an email at gimsheng.lee@gmail.com and I'll provide it to you.

## �📄 License

This project is developed for educational and portfolio purposes.
