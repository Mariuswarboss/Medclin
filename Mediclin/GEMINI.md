# Mediclin Project Overview

Mediclin is a comprehensive medical clinic management system built with .NET 8 and WPF. It features a multi-portal architecture supporting Administrators, Doctors, and Patients.

## Technology Stack
- **Framework:** .NET 8.0 (Windows)
- **UI:** WPF (Windows Presentation Foundation)
- **Architecture:** 3-layer (Data, Business, UI) with Repository and Service patterns.
- **Database:** MySQL (Direct ADO.NET wrapper)
- **Authentication:** Custom implementation with BCrypt password hashing.
- **Dependency Management:** Manual DLL management (located in the `libs/` directory).

## Project Structure
- **Mediclin.UI:** WPF application containing Views, ViewModels (MVVM), and UI-specific services.
- **Mediclin.Business:** Core business logic, Services, DTOs, and Validators.
- **Mediclin.Data:** Data access layer, Repositories, Models, and Database Context.
- **Mediclin.Tests:** Unit tests for business logic and services.
- **docs/:** Contains architectural notes, database schemas, and user manuals.
- **libs/:** External library dependencies (.dll files).

## Building and Running

### Prerequisites
- .NET 8 SDK
- MySQL Server (running on `localhost:3306`)
- PowerShell (for initialization scripts)

### Setup
1.  **Database Initialization:** Run the `initialize-db.ps1` script to create the database schema and seed initial data.
    ```powershell
    ./initialize-db.ps1
    ```
2.  **Configuration:** Update the connection string in `Mediclin.UI/appsettings.json` if necessary.

### Build & Run
- **Build:** `dotnet build Mediclin.sln`
- **Run:** `dotnet run --project Mediclin.UI/Mediclin.UI.csproj`
- **Test:** `dotnet test Mediclin.Tests/Mediclin.Tests.csproj`

## Development Conventions

### Architecture & Design
- **MVVM Pattern:** The UI layer strictly follows the MVVM pattern. ViewModels inherit from `BaseViewModel` and use `RelayCommand`.
- **Repository Pattern:** All database interactions are encapsulated in Repository classes within the `Mediclin.Data` layer.
- **Service Layer:** Business logic is orchestrated by Services in `Mediclin.Business`.
- **Manual DI:** Dependency injection is managed manually through the `ApplicationServices` class in `Mediclin.UI/Services`.

### Data Access
- The project uses a custom `DatabaseContext` wrapper for ADO.NET. It does **not** use Entity Framework Core.
- Queries are performed using raw SQL strings with parameters.
- Data models are mapped from `Dictionary<string, object>` or read directly from `MySqlDataReader`.

### Coding Style
- Use C# 12 features (Implicit Usings, Nullable Reference Types).
- Follow standard C# naming conventions (PascalCase for classes/methods, camelCase for private fields with `_` prefix).
- Business logic should be kept in Services, while Repositories handle data persistence.

## Key Files
- `Mediclin.Data/Context/DatabaseContext.cs`: Main entry point for database operations.
- `Mediclin.UI/Services/ApplicationServices.cs`: Central hub for service registration and dependency management.
- `initialize-db.ps1`: Automated environment setup script.
- `Mediclin.Data/Migrations/schema.sql`: Current database schema definition.
