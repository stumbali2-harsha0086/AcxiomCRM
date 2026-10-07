# AcxiomCRM — Production-Grade CRM Web Application

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-blue?logo=dotnet)](https://asp.net/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.0-green)](https://docs.microsoft.com/ef/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**AcxiomCRM** is a web-based Customer Relationship Management (CRM) platform built strictly according to the official specification document `AcxiomCRM_Complete_Project_Assignment.pdf`. 

The system demonstrates the complete customer-sales lifecycle—from lead capture and follow-up management to lead conversion, opportunity stage tracking, interactive pipeline analytics, audit logging, role-based security, and secured REST APIs.

---

##  Architecture & Project Structure

The solution follows a strict **Layered Architecture** adhering to clean architecture principles:

```
AcxiomCRM/
├── AcxiomCRM.Domain/          # Core Domain Entities, Enums & Data Annotations
├── AcxiomCRM.Data/            # DbContext (EF Core + Identity), Relationships & Seed Initializer
├── AcxiomCRM.Services/        # Business Logic Services & Server-Side Role Authorization Scoping
├── AcxiomCRM.Web/             # ASP.NET Core MVC, Razor Views, REST API Controllers, Bootstrap 5 UI & Chart.js
└── AcxiomCRM.Tests/           # Automated xUnit Acceptance & Validation Test Suite
```

---

##  Seeded Demo Credentials

On first run, the system automatically initializes the database schema (`AcxiomCRM.db`) and seeds initial roles and accounts:

| Role | Email | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@acxiomcrm.com` | `Admin@123` | Full application administration, Users, Roles, Audit Logs, CRM records, System management & Reports |
| **Manager** | `manager@acxiomcrm.com` | `Manager@123` | Team pipeline, Customers, Leads, Opportunities, Follow-Ups, Team performance & Reports |
| **Sales Executive** | `sales@acxiomcrm.com` | `Sales@123` | Restricted strictly to assigned/owned CRM records |

---

##  Features & Module Implementation

### 1. Security & Authentication (ASP.NET Core Identity)
- **Password Policy**: Enforces minimum 8 characters, uppercase, lowercase, digit, and special characters.
- **Account Lockout**: 5 failed login attempts trigger an automatic 15-minute account lockout.
- **Anti-Forgery Protection**: State-changing MVC forms are protected with `[ValidateAntiForgeryToken]`.
- **Sensitive Data Protection**: Passwords, password hashes, reset tokens, and secrets are never exposed in UI, DTOs, logs, or API responses.

### 2. Executive Dashboard & Analytics
- **8 KPI Cards**: Total Customers, Total Leads, Open Leads, Total Opportunities, Open Opportunities, Won Opportunities, Lost Opportunities, Total Pipeline Value.
- **Date Filters**: Today, This Week, This Month, Custom Range.
- **Chart.js Visualizations**:
  - *Lead Status Breakdown* (Doughnut Chart)
  - *Opportunity Pipeline Value* (Bar Chart)
  - *Monthly Revenue Outcome* (Line Chart)

### 3. Customer & Lead Management
- **Customer CRUD**: Unique email & phone enforcement, duplicate customer prevention, contact profile details, customer history & related activities.
- **Lead CRUD**: Source tracking, priority, status progression (`New` $\rightarrow$ `Contacted` $\rightarrow$ `Qualified` $\rightarrow$ `Unqualified` $\rightarrow$ `Converted` $\rightarrow$ `Lost`).
- **Lead-to-Customer Workflow**: 1-click conversion from Lead to active Customer and Opportunity deal, updating lead status and generating audit records.

### 4. Opportunity & Sales Pipeline Management
- **Kanban Sales Pipeline**: Stage movement across `Qualification` $\rightarrow$ `Proposal` $\rightarrow$ `Negotiation` $\rightarrow$ `Won` / `Lost`.
- **Business Validations**:
  - `Amount > 0` (Rejects non-positive amounts with *"Opportunity Amount must be greater than 0."*)
  - `Probability` (Rejects values outside 0..100 with *"Probability must be between 0 and 100."*)
  - `Expected Close Date` (Rejects past dates for active deals with *"Expected Close Date cannot be in the past."*)
- **Weighted Forecast**: Calculated as $\text{Amount} \times \text{Probability} / 100$.

### 5. Follow-Up & Activity Management
- **Follow-Up Scheduling**: Planned, Completed, Missed, Overdue tracking with date validation (`FollowUpDate >= today`).
- **Activity Logging**: Tracks Calls, Meetings, Emails, and Tasks linked to Customers and Leads.

### 6. REST API Controllers (`/api/*`)
- Secured REST API endpoints exposing DTOs (no EF entities/passwords exposed):
  - `POST /api/auth/login`, `POST /api/auth/logout`
  - `GET/POST /api/customers`, `GET/PUT/DELETE /api/customers/{id}`
  - `GET/POST /api/leads`
  - `GET/POST /api/opportunities`
  - `GET/POST /api/followups`
  - `GET /api/reports/pipeline`

### 7. Scoped Business Reports
- 8 interactive reports: Customer Report, Lead Report, Follow-Up Report, Opportunity Report, Pipeline Report (Stage/Owner grouping), Sales Conversion Report, User Activity Report, and Audit Report.

---

##  Automated Test Verification

Run automated xUnit acceptance tests to verify all business rules:

```powershell
dotnet test AcxiomCRM.Tests/AcxiomCRM.Tests.csproj
```

**Test Output:**
```
Passed! - Failed: 0, Passed: 7, Skipped: 0, Total: 7, Duration: 1 s
```

---

##  Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Running the Application locally

1. Clone the repository:
   ```bash
   git clone https://github.com/YOUR-USERNAME/AcxiomCRM.git
   cd AcxiomCRM/AcxiomCRM.Web
   ```

2. Build & Run the project:
   ```bash
   dotnet run
   ```

3. Open your browser and navigate to **`http://localhost:5168`**. Log in using any of the seeded demo credentials.

---

## 📄 License
This project is licensed under the MIT License.
