# Transaction Inquiry

A complete ASP.NET Core MVC web application for managing transaction inquiries, built with PostgreSQL, JWT authentication, and a comprehensive user management system.

## Features

### Authentication & Security
- **Dual Authentication**: Cookie-based auth + JWT Bearer tokens
- **Force Password Change**: Users created by admin get a default password and MUST change it before accessing the dashboard
- **BCrypt Password Hashing**: Secure password storage
- **TripleDES Encryption**: Database connection string encryption using the Cryptor class
- **Audit Logging**: Every significant action is logged with account, action, description, IP address, and timestamp

### Dashboard Tabs
1. **Dashboard** - Overview with user statistics and quick navigation
2. **SWGLOBAL** - Switch global transaction data module
3. **Settlement Reports** - Settlement report generation
4. **Scripts** - System script management
5. **Manual Settlements** - Manual settlement processing
6. **Exception Reports** - Transaction exception reporting
7. **Users** - Full user management (CRUD, enable/disable, delete, view)
8. **Audit** - Complete audit log trail

### User Management
- Create users with first name, last name, email, account type, and privileges
- View user details in a detailed modal
- Edit user information
- Enable/disable user accounts
- Delete users permanently
- Users created with default password (hashed with BCrypt)

### Database Schema

#### transactionInquiryUsers
| Column | Type | Constraints |
|--------|------|-------------|
| id | BIGSERIAL | PRIMARY KEY |
| firstName | VARCHAR(100) | NOT NULL |
| lastName | VARCHAR(100) | NOT NULL |
| email | VARCHAR(255) | NOT NULL, UNIQUE |
| password | VARCHAR(255) | NOT NULL (BCrypt hashed) |
| accountStatus | VARCHAR(50) | NOT NULL (enabled/disabled) |
| accountType | VARCHAR(50) | NOT NULL (Admin/User) |
| privileges | TEXT | Optional |
| lastLoginIP | VARCHAR(45) | Optional |
| created_at | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP |

#### auditLogs
| Column | Type | Constraints |
|--------|------|-------------|
| id | BIGSERIAL | PRIMARY KEY |
| account | VARCHAR(255) | NOT NULL |
| action | VARCHAR(100) | NOT NULL |
| description | TEXT | Optional |
| ipAddress | VARCHAR(45) | Optional |
| time | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP |

## Technology Stack

- **.NET 8.0** - Web framework
- **PostgreSQL** - Primary database (via Npgsql.EntityFrameworkCore.PostgreSQL)
- **Entity Framework Core 8** - ORM
- **BCrypt.Net-Next** - Password hashing
- **JWT Bearer Authentication** - Token-based auth
- **Cookie Authentication** - Session-based auth
- **TripleDES Cryptor** - Connection string encryption

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- PostgreSQL 14+ (local or remote)
- Visual Studio 2022+ or VS Code

### Setup Steps

1. **Clone/navigate to the project directory:**
   ```
   cd transactioninquiry
   ```

2. **Configure the connection string in `appsettings.json`:**
   
   The default connection string is encrypted. Generate your own encrypted connection string:
   
   Create a small C# program or use the Cryptor class to encrypt your PostgreSQL connection string:
   ```csharp
   string connStr = "Host=localhost;Port=5432;Database=transactioninquiry;Username=postgres;Password=yourpassword";
   string encrypted = Cryptor.Encrypt(connStr, true);
   ```
   
   Then replace the `DefaultConnection` and `AppConnection` values in `appsettings.json` with the encrypted string.

3. **Run the application:**
   ```
   dotnet run
   ```
   
   Or open the `.slnx` file in Visual Studio and press F5.

4. **Default admin credentials:**
   - Email: `admin@transactioninquiry.com`
   - Password: `P@ssw0rd123!`
   
   On first login, you'll be forced to change the password.

### Database Migration

The project includes pre-built migrations. On first run, the database will be auto-migrated and an admin user will be seeded.

If you need to recreate migrations:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Encrypting Your Connection String

Use the Cryptor class to encrypt your PostgreSQL connection string:

```csharp
using transactioninquiry.Classes.Utils;

// Encrypt
string myConnStr = "Host=localhost;Port=5432;Database=transactioninquiry;Username=postgres;Password=yourpassword";
string encrypted = Cryptor.Encrypt(myConnStr, true);
Console.WriteLine(encrypted); // Copy this to appsettings.json

// Decrypt (the app does this automatically)
string decrypted = Cryptor.Decrypt(encrypted, true);
```

## Architecture

```
transactioninquiry/
├── Classes/
│   └── Utils/
│       ├── Cryptor.cs           # TripleDES encryption/decryption
│       ├── JwtService.cs        # JWT token generation & validation
│       ├── AuditLogger.cs       # Async audit logging helper
│       └── GlobalFunctions.cs   # Reusable user/audit CRUD operations
├── Controllers/
│   ├── AuthController.cs        # Login, logout, password change
│   ├── DashboardController.cs   # All dashboard views & APIs
│   └── HomeController.cs        # Home pages
├── Data/
│   └── AppDbContext.cs          # EF Core DbContext (PostgreSQL)
├── Models/
│   ├── TransactionInquiryUser.cs
│   ├── AuditLog.cs
│   └── ViewModels/
│       ├── CreateUserRequest.cs
│       ├── UpdateStatusRequest.cs
│       └── JwtLoginRequest.cs
├── Views/
│   ├── Auth/
│   │   ├── Login.cshtml
│   │   └── UpdatePassword.cshtml
│   ├── Dashboard/
│   │   ├── Index.cshtml
│   │   ├── Swglobal.cshtml
│   │   ├── SettlementReports.cshtml
│   │   ├── Scripts.cshtml
│   │   ├── ManualSettlements.cshtml
│   │   ├── ExceptionReports.cshtml
│   │   ├── Users.cshtml
│   │   └── Audit.cshtml
│   └── Shared/
│       ├── _DashboardLayout.cshtml
│       ├── _Layout.cshtml
│       └── Error.cshtml
├── Migrations/                   # EF Core migrations
├── wwwroot/
│   ├── css/
│   │   └── site.css             # Complete design system
│   └── js/
│       └── site.js              # Utilities
├── appsettings.json
├── appsettings.Development.json
└── transactioninquiry.slnx      # Visual Studio solution
```

## API Endpoints

### Authentication
- `GET /Auth/Login` - Login page
- `POST /Auth/Login` - Authenticate and set JWT cookie
- `GET /Auth/UpdatePassword` - Force password change page
- `POST /Auth/UpdatePassword` - Submit new password
- `GET /Auth/Logout` - Sign out and clear cookies

### Dashboard Views
- `GET /Dashboard` - Main dashboard
- `GET /Dashboard/Swglobal` - SWGLOBAL module
- `GET /Dashboard/SettlementReports` - Settlement reports
- `GET /Dashboard/Scripts` - Scripts management
- `GET /Dashboard/ManualSettlements` - Manual settlements
- `GET /Dashboard/ExceptionReports` - Exception reports
- `GET /Dashboard/Users` - User management
- `GET /Dashboard/Audit` - Audit logs

### User Management APIs
- `GET /Dashboard/Users/GetAll` - List all users
- `POST /Dashboard/Users/Create` - Create new user (admin only)
- `POST /Dashboard/Users/Update/{id}` - Update user
- `POST /Dashboard/Users/Delete/{id}` - Delete user
- `POST /Dashboard/Users/UpdateStatus/{id}` - Enable/disable user

### Audit APIs
- `GET /Dashboard/Audit/GetAll` - List all audit logs

## License

Internal use only.
