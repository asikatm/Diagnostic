# HIS ERP – Diagnostic

Diagnostic centre software on **ASP.NET Core 10** and **SQL Server**, split into three independent solutions.

| Folder | Solution | What it is | URL (dev) |
|---|---|---|---|
| `api/` | `Diagnostic.Api.slnx` | Web API: EF Core, JWT, menu-based permissions, owns the database | http://localhost:5100/docs |
| `report/` | `Diagnostic.Report.slnx` | Report server: runs the stored procedures mapped in *Reports and Sp Mapping*, renders PDF (QuestPDF) / CSV | http://localhost:5200/docs |
| `app/` | `Diagnostic.App.slnx` | Web UI (MVC + JS), calls the API and the report server from the browser | http://localhost:5300 |

## Run

Start them in this order. Each one is a separate solution and can be opened on its own in Visual Studio.

```powershell
cd api\Diagnostic.Api;       dotnet run    # creates/migrates DiagnosticDb, stored procedures, seed data
cd report\Diagnostic.Report; dotnet run
cd app\Diagnostic.App;       dotnet run
```

Login: **admin / Admin@123**. Change the password after the first login.

Configuration lives in each project's `appsettings.json`. **Secrets are not in git.** The database password and JWT key
go in `appsettings.Development.json` (git-ignored), next to `appsettings.json` in `api/Diagnostic.Api` and `report/Diagnostic.Report`.
After cloning, copy `appsettings.Development.example.json` → `appsettings.Development.json` in both projects and fill in:
- `ConnectionStrings:DiagnosticDb` (api, report) – SQL Server connection string (server, user, password)
- `Jwt:Key` (api, report) – **must be identical in both**. Use a long random value.

On a live server, set the same values as environment variables instead
(`ConnectionStrings__DiagnosticDb`, `Jwt__Key`). `dotnet ef database update` needs `$env:ASPNETCORE_ENVIRONMENT="Development"` locally.
Other settings in `appsettings.json`:
- `Cors:Origins` (api, report) – the URL(s) where the app runs.
- `AppSettings:ApiBaseUrl / ReportBaseUrl` (app).

## Database

One database, `DiagnosticDb`, with two schemas:

- **admin** – Tenants, Departments, ServiceUnits, Users, Roles, UserRoles, Projects, Menus, MenuPermissions,
  Reports, ReportSpParameters, ReportPermissions, PaymentModes, PaymentChannels, Countries, Religions,
  AppGlobalSettings, Printers, NumberSequences
- **diag** – ParentGroups, TestGroups, TestItems, ReportParameters, Specimens, Vacutainers, Machines,
  VacuMachineMappings, ReportDeliveryTimes, OpdPackages, OpdPackageItems, LetterHeads, Doctors, DiscountSetups,
  Patients, Investigations, InvestigationDetails, InvestigationPayments, CancelRequests, SampleCollections,
  LabReports, LabResults

Conventions used by every table:
- `Id`, `IsActive`, `IsDeleted`, `CreatedBy`, `CreatedAt`, `UpdatedBy`, `UpdatedAt`. Audit columns are filled automatically.
- **Soft delete only**: a delete sets `IsDeleted = 1`, and deleted rows are hidden from all queries.
- Unique `Code` per setup table (filtered index that ignores deleted rows). Enums are stored as readable strings.
- Money is `decimal(18,2)`. Foreign keys are `RESTRICT` except the owned child rows (invoice lines, payments, results …).
- Invoice / receipt / sample / patient numbers come from `admin.NumberSequences` with a row lock
  (e.g. `INV260900001`, `MR…`, `S…`, `P26000001`); they restart every month (yearly for patients).

Scripts in `api/database/` are for DBAs, or for setting up a server without running the API:
- `01_Schema.sql` – idempotent EF migration script (all tables, keys, indexes)
- `02_ReportProcedures.sql` – `diag.usp_Rpt_*` stored procedures (also executed by the API at start-up)

Schema change: edit the entity + `DiagnosticDbContext`, then
`dotnet ef migrations add <Name> -o Data/Migrations` in `api/Diagnostic.Api`.

## Workflow

1. **Investigation Entry** – choose or register the patient, pick the **Media** (agent) if any, add tests / an OPD package,
   apply a discount, take payment → money receipt.
   - *Discount* schemes can be test wise (one test), group wise (one test group) or for all tests.
   - *Media commission* is stored per test line at billing. The rules come from **Media Commission Setup**: a test wise rule wins
     over a group wise rule, and a rule for one media wins over a rule for all media. If no rule matches, the media's default % applies.
     The base is the test price after discount, before VAT. Cancelled tests earn no commission.
     Reports: *Media Commission Summary / Detail*.
   Delivery time per test comes from *Report Delivery Time Set* (test item rule → test group rule → `Diag.DefaultDeliveryHours`).
2. **Sample Collection** – three tabs:
   - *Pending Collection*: collect one test, a whole invoice, or tests from several invoices at once (select all).
     One sample number (barcode) is made per invoice and vacutainer, from *Vacu And Machine Group Mapping*, and the sticker prints.
   - *Sample Dispatch*: send collected tubes to the lab (sample status Collected → Dispatched).
   - *Repeat Sample*: tubes the lab rejected. *Recollect* makes a new tube for the same tests, linked to the rejected one.
   **Sample Acknowledgement** (lab): scan or select tubes to acknowledge (→ Received), or reject them with a reason.
   The tests of a rejected tube become `SampleRejected` and appear under *Repeat Sample*.
3. **Result Entry** – values are flagged H / L from each *Report Parameter*'s low / high values; gender-specific ranges are supported.
   Text / radiology tests use a template text. **Save & Verify** needs the signing doctor.
4. **Lab Report Finalization** – the pathologist reviews each verified result, then **Finalizes** it (one at a time or in bulk) or
   **Returns** it to the lab with a reason (the reason shows in Result Entry). Only users with a role listed in the setting
   `Diag.FinalizeRoleCodes` (default `PATHOLOGIST`) can finalize. **Super Admin can't**, on purpose. A finalized result is locked.
   A report printed before finalization carries a "PRELIMINARY – NOT FINALIZED" watermark.
5. **Report Delivery** – only finalized reports can be printed as final and delivered (Print & Deliver, with a due-amount warning).
   **LABREPORT WITH LETTER HEAD** is for reprinting on the letter head or on a pre-printed pad.
6. **Cancel Service** → **Approve Cancel** – approval cancels the line(s), recalculates the invoice and books the refund.
   A refund can never be more than the amount paid beyond the remaining bill.
7. **Invoice List** – due collection and receipt reprints. **Dashboard** – billing, collection, due and the work queue. **Reports** – MIS reports.

## Permissions

- Every screen is a row in `admin.Menus`, identified by its `Code`. The API controllers carry `[MenuCode("…")]`.
- *Menu Permission* grants View / Add / Edit / Delete / Print per role. The HTTP verb decides which right is checked
  (GET = View, POST = Add, PUT = Edit, DELETE = Delete; printing/delivery = Print). A super admin role bypasses the checks.
- *Report Permission Management* controls which role can view / export each report on the report server.
- New report: write a `schema.usp_Name` procedure, add it in *Reports and Sp Mapping* with its parameters, then grant it to roles.
  It appears on the Reports page straight away (`Tabular` template = grid PDF + CSV).

Seed roles: Super Admin, Receptionist, Lab Technician, Pathologist, Accounts. Sample setup data covers 12 tests
(CBC, sugar, lipid, TSH, urine, X-ray, USG, ECG …), tubes, machines, doctors, one package and discounts. Edit or deactivate it as needed.
