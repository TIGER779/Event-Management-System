# EventHub

An event booking system built with ASP.NET Core.

## Description

EventHub is a web application designed to facilitate the booking and management of events. It provides users with the ability to browse, book, and manage event reservations seamlessly.

## Features

-   User authentication and authorization
-   Event browsing and booking
-   Booking and reservation management
-   Admin panel for event management
-   Booking confirmation email with a PDF ticket, a QR code and a calendar (.ics) file
-   Signed ticket codes and a "Check ticket" page that the QR code opens (rejects fake or cancelled tickets)

## DEMO Images

### User Side

![Home Page](Images/Home%20Page.png)
![Home page after Login](Images/Home%20Page%20Logged%20in.png)
![Congratulations Page](Images/Congratulation%20Page.png)
![My Bookings Page](Images/My%20Bookings.png)
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
-   MailKit for sending emails, QuestPDF for PDF tickets, QRCoder for QR codes
-   Docker and Docker Compose

### NuGet Packages

-   BCrypt.Net-Next 4.0.3
-   MailKit 4.18.2
-   QuestPDF 2026.9.1 (Community license)
-   QRCoder 1.8.1
-   Microsoft.AspNetCore.Authentication.JwtBearer 9.0.4
-   Microsoft.EntityFrameworkCore.SqlServer 9.0.2
-   Microsoft.EntityFrameworkCore.Tools 9.0.2
-   Microsoft.Extensions.Configuration 9.0.2
-   Microsoft.Extensions.Configuration.Json 9.0.2
-   Microsoft.VisualStudio.Web.CodeGeneration.Design 9.0.0
-   Portable.BouncyCastle 1.9.0

## How to Run

### Option 1 (recommended): Docker, on Windows, macOS or Linux

Requirements: [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine with Compose) and about 2 GB of free RAM.
Nothing else is needed: no .NET SDK and no SQL Server.

```bash
git clone https://github.com/TIGER779/Event-Management-System.git
cd Event-Management-System/AreebTechnologyTask
cp .env.example .env          # then open .env and set your own DB_PASSWORD and JWT_KEY
docker compose up --build
```

| What | Where |
|------|-------|
| Website | http://localhost:5025 |
| Email inbox (every email the app sends) | http://localhost:8025 |

`docker compose up` starts three containers:

-   **web**: the ASP.NET Core app, built by the `Dockerfile` and running as a non-root user
-   **db**: SQL Server 2022 (Express). The app waits until it is healthy, then creates the database and sample data
-   **mail**: [Mailpit](https://mailpit.axllent.org/), a fake mail server that catches all emails so none reach a real address

The first start downloads the SQL Server image (about 1.5 GB) and takes a few minutes; later starts take seconds.
Data is kept in Docker volumes. `docker compose down` stops everything, and `docker compose down -v` also deletes the database.

> On Apple-silicon Macs (M1-M4), SQL Server runs through Docker's Intel emulation (Rosetta): it works, but starts more slowly.
> The container serves plain HTTP for local use only. A real deployment must put HTTPS in front of it and
> remove `Auth__CookieRequiresHttps: "false"` from `docker-compose.yml`.

### Option 2: Visual Studio / `dotnet run` (Windows)

Requirements: [.NET 9 SDK](https://dotnet.microsoft.com/download) and SQL Server **LocalDB** (installed with Visual Studio).

```bash
git clone https://github.com/TIGER779/Event-Management-System.git
cd Event-Management-System/AreebTechnologyTask
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

#### JWT signing key

The key that signs login tokens is a secret, so it is not in `appsettings.json`. In Development the app
uses a temporary random key if none is set (logins end when the app restarts). To keep logins across
restarts, set your own key once:

```powershell
dotnet user-secrets set "Jwt:Key" "$([Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64)))"
```

Outside Development the app does not start without `Jwt:Key` (or the `Jwt__Key` environment variable).

### Sample accounts

| Role  | Email               | Password   |
|-------|---------------------|------------|
| Admin | `admin@example.com` | `admin123` |
| User  | `john@example.com`  | `john123`  |
| User  | `jane@example.com`  | `jane123`  |

The sample accounts use `example.com`, a domain reserved for examples, so no email can reach a real person.
Register with your own email address to receive emails yourself.

### Booking confirmation emails

After a booking, the app emails the user:

-   the event details and picture, and a **QR code** to show at the entrance
-   a **PDF ticket** (`ticket-TKT-000123-XXXXXXXXXX.pdf`)
-   a **calendar file** (`add-to-calendar.ics`) for Google, Outlook or Apple Calendar

The email is sent in the background, so booking stays fast and still succeeds if the mail server is down.
In **My Bookings**, users can download the PDF ticket or have it emailed again.

The ticket ID ends with a signature (HMAC-SHA256) that only the server can create. The QR code opens
`/Tickets/Verify?code=...`, which shows **Valid ticket**, or **Not valid** for fake, edited or cancelled tickets.
Ticket signatures are derived from `Jwt:Key`, or from `Tickets:SigningKey` if you set one; changing that key invalidates
existing tickets.

Where emails go is set in the `Email` section of `appsettings.json`:

| Setup | How to read the emails |
|-------|------------------------|
| Docker (`docker compose up`) | Mailpit inbox at http://localhost:8025 (already configured) |
| Docker with real emails | Fill in the `SMTP_*` and `EMAIL_FROM` lines in `.env`, then `docker compose up -d` |
| `dotnet run` (default) | Start only Mailpit with `docker compose up -d mail`, then open http://localhost:8025 |
| `dotnet run` without Docker | `dotnet user-secrets set "Email:Mode" "PickupDirectory"`: emails are saved as `.eml` files in `App_Data/emails` |

To send **real** emails, for example with Gmail and an [App Password](https://support.google.com/accounts/answer/185833),
keep the credentials in user-secrets:

```bash
dotnet user-secrets set "Email:Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Email:Smtp:Port" "587"
dotnet user-secrets set "Email:Smtp:Security" "StartTls"
dotnet user-secrets set "Email:Smtp:Username" "you@gmail.com"
dotnet user-secrets set "Email:Smtp:Password" "<16-character app password>"
dotnet user-secrets set "Email:FromAddress" "you@gmail.com"
```

### Database changes (developers)

After changing a model, add a migration; it is applied automatically on the next start:

```bash
dotnet tool install --global dotnet-ef   # once
dotnet ef migrations add <Name>
```

`database.sql` is no longer needed. It is kept only as a reference for manual setups.
