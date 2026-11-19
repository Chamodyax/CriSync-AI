# CriSync — Humanitarian Crisis Aid & Emergency Coordination Platform

**Module:** SE205.3 Software Architecture — Coursework 2026  
**Architecture Style:** Clean Modular Monolith (`ReliefCoord.Domain`, `ReliefCoord.Application`, `ReliefCoord.Infrastructure`, `ReliefCoord.Api`, `ReliefCoord.Tests`)  
**Technology Stack:** C# ASP.NET Core 9.0 Web API + Entity Framework Core (SQL Server & SQLite Dual-Provider) + React 18 / TypeScript / Vite / Tailwind CSS + Leaflet / OpenStreetMap

---

## 1. Project Structure

```text
HCP Project/
├── HumanitarianCrisisPlatform.slnx
├── backend/
│   ├── ReliefCoord.Domain/           # Enterprise Entities, Enums, Domain Events
│   ├── ReliefCoord.Application/      # Use-Case Orchestration, DTOs, Interfaces, Allocation Strategies
│   ├── ReliefCoord.Infrastructure/   # EF Core DbContext, Repositories, AI/Mapping/SMS Adapters, Async Event Bus
│   ├── ReliefCoord.Api/              # REST Controllers, JWT Security, RBAC, Swagger/OpenAPI
│   └── ReliefCoord.Tests/            # xUnit Unit & Integration Test Suite
├── frontend/                         # React + TypeScript + Tailwind CSS + Leaflet Interactive Client
└── database/
    └── schema_sqlserver.sql          # Full T-SQL DDL Schema for Microsoft SQL Server
```

---

## 2. Quick-Start Installation & Execution

### Prerequisites
- **.NET SDK 9.0+**
- **Node.js 18+** and **npm**
- **SQL Server / LocalDB** *(Optional — the API automatically defaults to a zero-configuration local SQLite database `reliefcoord_platform.db` and can be switched to SQL Server via `appsettings.json`).*

### Step 1: Run Automated Unit & Integration Tests
```powershell
dotnet test backend/ReliefCoord.Tests/ReliefCoord.Tests.csproj
```

### Step 2: Start the C# ASP.NET Core Web API Backend
```powershell
cd backend/ReliefCoord.Api
dotnet run --urls "http://localhost:5080"
```
- **API Base URL:** `http://localhost:5080/api`
- **Swagger / OpenAPI UI:** `http://localhost:5080/swagger`

### Step 3: Start the React Web Client
```powershell
cd frontend
npm run dev
```
- **Web Client URL:** `http://localhost:5173`

### Switching to Microsoft SQL Server
To run against Microsoft SQL Server instead of the built-in SQLite provider:
1. Execute `database/schema_sqlserver.sql` on your SQL Server instance or let EF Core create the database automatically.
2. In `backend/ReliefCoord.Api/appsettings.json`, change `"DatabaseProvider": "Sqlite"` to `"DatabaseProvider": "SqlServer"` and update `"SqlServerConnection"` if needed.

---

## 3. Platform Roles & Seeded Credentials

Operational and public accounts use the password **`Crisync@2026`** (sign in via **Sign In**).  
Administrator accounts use **`Admin@Crisync2026`** (sign in via **Admin Login**).

| Role | Email | Password | Organisation | Access & Governance Policy |
| :--- | :--- | :--- | :--- | :--- |
| **Affected Citizen** | `citizen@crisync.org` | `Crisync@2026` | Public / Household | Self-registration supported; submit requests, upload evidence, manage family vulnerability profile |
| **Public Donor** | `publicdonor@crisync.org` | `Crisync@2026` | Public Contributor | Self-registration supported; fund relief appeals & pledge material donations |
| **Field Volunteer** | `fieldvolunteer@crisync.org` | `Crisync@2026` | Field Responder | Self-registration creates `Pending` account until verified by Volunteer Coordinator |
| **Partner Officer** | `partner@crisync.org` | `Crisync@2026` | MSF Medical Rapid Team | Submit partnership proposals (approved by Admin) & coordinate shared stock |
| **Emergency Coordinator** | `coordinator@crisync.org` | `Crisync@2026` | UN OCHA Regional Office | Declare crises, override AI recommendations (HITL), invite operational staff, broadcast alerts |
| **Case Manager** | `casemanager@crisync.org` | `Crisync@2026` | National DMC | Invited role; verify cases, register offline citizens on behalf, merge duplicates |
| **Logistics Manager** | `logistics@crisync.org` | `Crisync@2026` | Sri Lanka Red Cross | Invited role; manage inventory, execute allocation strategies, dispatch convoys |
| **Shelter Administrator** | `shelter@crisync.org` | `Crisync@2026` | National DMC | Invited role; manage shelter capacity, accessibility & family allocations |
| **Volunteer Coordinator** | `volunteer@crisync.org` | `Crisync@2026` | Sri Lanka Red Cross | Invited role; verify `Pending` volunteers & assign field rescue missions |
| **Donor / Funding Officer** | `donor@crisync.org` | `Crisync@2026` | UN OCHA Regional Office | Exclusively appointed by Global System Admin; manage funds & high-value compliance checks |
| **Organization Admin** | `orgadmin@crisync.org` | `Admin@Crisync2026` | UN OCHA Regional Office | Admin tier; manage staff accounts, approve partner requests, audit logs |
| **Global System Admin** | `admin@crisync.org` | `Admin@Crisync2026` | UN OCHA Regional Office | Top admin tier (Min 2 / Max 3 policy); exclusive Donor Officer appointment, system config & integrations |

---

## 4. Repeatable UAT & Verification Scenarios

1. **Human-in-the-Loop Governance & Audit Trail:**
   - Open **AI Priority & Forecast** while signed in as `EmergencyCoordinator` (`coordinator@crisync.org`).
   - Review AI triage on `REQ-20261116-1042`, verify or adjust urgency, enter clinical/field justification, and inspect the immutable `HumanGovernanceDecision` and `AuditEventRecord`.
2. **Duplicate Detection Without Auto-Deletion:**
   - On the main dashboard, inspect `REQ-20261116-1051`, flagged at **86% similarity** with `REQ-20261116-1042`.
   - Click **Merge Duplicate** to transition its status to `MergedAsDuplicate` while preserving both records in the database.
3. **Multi-Constraint & Shortage Contention Allocation:**
   - Open **Supplies & Dispatch**, select `REQ-20261116-1042` and `REQ-20261116-1048` competing for limited medical kits at Ratnapura MSF Depot.
   - Compare `MultiConstraintOptimal`, `ShortageProportionalContention`, and `ProximityFirst` strategies, then execute allocation & dispatch.
4. **Notification Idempotency Deduplication (Requirement #6):**
   - Open **Tab 8 (GIS Map, Resilience & Audit Logs)**.
   - Click **Send / Replay Notification Event** twice with the same Idempotency Key (`IDEMP-MANUAL-ALERT-2026`). The first call delivers via SMS; the second is blocked as `SkippedDuplicate`.
5. **External Service Failure Resilience (Requirement #7):**
   - In **Tab 8**, toggle **External AI Service**, **External Mapping Service**, or **Notification Gateway** to **Outage Simulated**.
   - Submit a new assistance request or calculate a route and observe seamless fallback to `ResilientLocalHeuristicFallback`, `LocalHaversineFallbackEngine`, and `AsyncRetryQueue`.

---

## 5. Known Limitations & Scope Boundaries
- Per coursework scope boundaries, no physical IoT sensors or hardware telemetry are used; coordinates and field updates are ingested via the web portal and OpenStreetMap integrations.
- External SMS (Twilio), Email (SendGrid), and Payment Gateway integrations operate in deterministic sandbox/adapter mode with full circuit-breaker simulation so the platform can be evaluated offline without paid third-party API keys.
