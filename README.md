# Event Booking System

## Description

The Event Booking System is a web application designed to facilitate the booking and management of events. It provides users with the ability to browse, book, and manage event reservations seamlessly.

## Features

-   User authentication and authorization
-   Event browsing and booking
-   Booking and reservation management
-   Admin panel for event management

## DEMO Images

### User Side

![Home Page](Images/Home%20Page.png)
![Home page after Login](Images/Home%20Page%20Logged%20in.png)
![My Bookings Page](Images/My%20Bookings.png)
![Congratulations Page](Images/Congratulation%20Page.png)
![Sample Ticket](Images/ticket-tech_conference_2024.jpg)

### Admin Side

![Admin panel](Images/Admin%20Panel.png)

## Frontend Information

-   **Technologies Used**: Razor Pages (CSHTML), CSS, JavaScript
-   **Port**: Served by the backend at https://localhost:5025

## Backend Information

-   **Technologies Used**: ASP.NET Core, Entity Framework Core, SQL Server
-   **Port**: https://localhost:5025
-   **Database**: SQL Server. The database is created, migrated and filled with sample data automatically on first start

## Technologies Used

-   ASP.NET Core 9.0
-   Entity Framework Core
-   SQL Server
-   BCrypt.Net for password hashing
-   JWT for authentication
-   BouncyCastle for cryptographic operations

### NuGet Packages

-   BCrypt.Net-Next 4.0.3
-   Microsoft.AspNetCore.Authentication.JwtBearer 9.0.4
-   Microsoft.EntityFrameworkCore.SqlServer 9.0.2
-   Microsoft.EntityFrameworkCore.Tools 9.0.2
-   Microsoft.Extensions.Configuration 9.0.2
-   Microsoft.Extensions.Configuration.Json 9.0.2
-   Microsoft.VisualStudio.Web.CodeGeneration.Design 9.0.0
-   Portable.BouncyCastle 1.9.0

## How to Run

### Option 1: Visual Studio / `dotnet run` (Windows)

Requirements: [.NET 9 SDK](https://dotnet.microsoft.com/download) and SQL Server **LocalDB** (installed with Visual Studio).

```bash
git clone https://github.com/TIGER779/AreebTechnologyTask.git
cd AreebTechnologyTask/AreebTechnologyTask
dotnet run
```

Open https://localhost:5025. On the first start the app creates the `Event_Booking_System` database,
applies the migrations and adds sample data. No SQL scripts or manual steps are needed.

#### Using your own SQL Server instead of LocalDB

Set your connection string with [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
(stored on your PC only, never committed):

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Server=.\SQLEXPRESS;Database=Event_Booking_System;Trusted_Connection=True;TrustServerCertificate=True"
```

An existing database created by the migrations keeps its data; sample data is only added to an empty database.

### Sample accounts

| Role  | Email               | Password   |
|-------|---------------------|------------|
| Admin | `admin@example.com` | `admin123` |
| User  | `john@example.com`  | `john123`  |
| User  | `jane@example.com`  | `jane123`  |

The sample accounts use `example.com`, a domain reserved for examples, so no email can reach a real person.
Register with your own email address to receive emails yourself.

### Database changes (developers)

After changing a model, add a migration; it is applied automatically on the next start:

```bash
dotnet tool install --global dotnet-ef   # once
dotnet ef migrations add <Name>
```

`database.sql` is no longer needed. It is kept only as a reference for manual setups.
