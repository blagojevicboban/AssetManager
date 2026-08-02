---
name: sqlite-efcore-schema-migration
description: Workflow rules for EF Core SQLite database migrations, legacy database column patching, process locking troubleshooting, and unit testing in ERPiSredstva.
---

# SQLite & EF Core Schema Migration Workflow (ERPiSredstva)

This skill documents the database schema management patterns, legacy SQLite database patching, and build troubleshooting for `ERPiSredstva`.

---

## 1. Safe SQLite Schema Patching (`SredstvaDbContext.cs`)

`ERPiSredstva` supports legacy SQLite database files created before EF Core migrations were introduced as well as standard databases with `__EFMigrationsHistory`.

### Rule: Always Update `EnsureExtraColumnsExist`
When adding new properties to model entities (e.g. `Sredstvo.cs`):
1. **Model**: Add the property with appropriate C# type.
2. **Database Context**: In [SredstvaDbContext.cs](file:///c:/SREDSTVA/ERPiSredstva/ERPiSredstvaData/SredstvaDbContext.cs), update `EnsureExtraColumnsExist(string dbPath)`.
3. **Execution**: `EnsureExtraColumnsExist` MUST run unconditionally in `SredstvaDbContext.Create(dbPath)` **before** `Database.Migrate()`.
4. **Column Check Pattern**:
   ```csharp
   if (!ColumnExists("Sredstva", "ColumnName"))
   {
       Exec("ALTER TABLE \"Sredstva\" ADD COLUMN \"ColumnName\" TEXT NOT NULL DEFAULT '';");
   }
   ```

---

## 2. Process File Locking & Build Failures

During `dotnet build` or `dotnet test`, the `ERPiSredstvaApp.exe` or `ERPiSredstvaData.dll` binaries may be locked by a running instance of `ERPiSredstvaApp` or `netcoredbg`.

### Remediation Command:
```powershell
powershell -Command "Stop-Process -Name ERPiSredstvaApp, netcoredbg -Force -ErrorAction SilentlyContinue"
```
Run this command whenever `dotnet build` fails with `MSB3021` or `MSB3026` file access error.

---

## 3. Unit Testing & Verification Workflow

- **Test Project**: `ERPiSredstvaData.Tests`
- **Execution Command**:
  ```powershell
  dotnet test C:\SREDSTVA\ERPiSredstva\ERPiSredstvaData.Tests\ERPiSredstvaData.Tests.csproj
  ```
- **Rule**: All calculations (accounting depreciation, start rules, pre-disposal rules, tax differences) MUST be unit tested without DB/UI dependencies.
