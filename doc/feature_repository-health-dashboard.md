# 🩺 CodeMedic — Repository Health Dashboard  
### *A unified, extensible system for analyzing the health of .NET repositories*

The dashboard should feel like the "front door" to CodeMedic — a single command that orchestrates multiple subsystems (including the BOM engine) and produces a holistic, actionable report.

Let’s break this down into:

1. What the dashboard should show  
2. How the BOM integrates into it  
3. The initial app architecture  
4. How the components communicate  
5. How to keep it extensible for future features  

---

# ✅ 1. What the Repository Health Dashboard Should Show

Think of the dashboard as a **summary of everything CodeMedic knows** about the repo.

### **Dashboard Sections**
- **Overall Health Score**  
- **Code Quality Summary**  
- **Architecture & Layering Summary**  
- **Dependency Health Summary**  
- **BOM Summary (integrated)**  
- **Build Performance Summary**  
- **Test Health Summary**  
- **Security & Configuration Summary**  
- **Workflow & GitHub Activity Summary**  
- **Doctor’s Orders (recommended actions)**  

Each section should be expandable in the CLI, with the BOM being one of the most important.

---

# ✅ 2. How the BOM Integrates Into the Dashboard

The BOM becomes both:

- A **standalone artifact** (exportable as JSON/Markdown)  
- A **data source** for the dashboard  

### **Dashboard Integration Examples**

#### **Dependency Health**
- “12 NuGet packages are outdated”
- “3 packages have known vulnerabilities”
- “2 packages appear abandoned”

#### **Vendor & Service Health**
- “Using Azure Blob Storage — SDK version 12.18.0”
- “Using Stripe API — last updated 2023”
- “Using Redis — vendor: StackExchange”

#### **Configuration Health**
- “App requires 14 environment variables”
- “3 secrets detected in config files”

#### **Build & Tooling Health**
- “Docker base image: mcr.microsoft.com/dotnet/aspnet:7.0”
- “global.json pins SDK 7.0.100”

#### **Architecture Health**
- “EF Core detected in domain layer (possible leak)”
- “Minimal APIs + MVC mixed in same project”

The BOM becomes the **data backbone** for several dashboard sections.

---

# ✅ 3. Initial App Architecture  
Here’s a clean, modular architecture that will scale beautifully.

```
CodeMedic
 ├── Core
 │    ├── RepoScanner
 │    ├── AnalyzerEngine
 │    ├── BOMEngine
 │    ├── MetricsEngine
 │    ├── HealthScoring
 │    └── PluginSystem
 ├── Modules
 │    ├── CodeQuality
 │    ├── Architecture
 │    ├── Dependencies
 │    ├── BuildPerformance
 │    ├── TestHealth
 │    ├── SecurityConfig
 │    └── GitWorkflow
 ├── Dashboard
 │    ├── DashboardAggregator
 │    ├── DashboardRenderer (Spectre.Console)
 │    └── DashboardModels
 ├── CLI
 │    ├── Commands
 │    ├── Options
 │    └── Output
 └── Integrations
      ├── GitHub
      ├── NuGet
      ├── AdvisoryDB
      └── CloudVendorMetadata
```

Let’s unpack the important parts.

---

# ✅ 4. Component Responsibilities

## **BOM Engine**
- Scans the repo for:
  - NuGet packages  
  - Framework features  
  - External services  
  - Vendors  
  - Config/environment requirements  
  - Build tooling  
- Produces a structured BOM model  
- Exposes BOM data to other modules  

## **Analyzer Engine**
- Runs Roslyn analyzers  
- Computes complexity, coupling, duplication  
- Feeds results into the dashboard  

## **Metrics Engine**
- Normalizes metrics across modules  
- Provides scoring inputs  

## **Health Scoring**
- Combines:
  - Code quality  
  - Architecture  
  - Dependencies  
  - Build performance  
  - Test health  
  - Security  
  - Workflow  
- Produces a single health score  

## **Dashboard Aggregator**
- Pulls data from all modules  
- Merges into a unified dashboard model  
- Applies scoring  
- Prepares summaries + recommendations  

## **Dashboard Renderer**
- Uses Spectre.Console  
- Renders:
  - Summary  
  - Sections  
  - Expandable details  
  - Color-coded severity  
  - Links to docs  

---

# ✅ 5. How the BOM and Dashboard Communicate

The BOM engine produces a **BOMModel**:

```csharp
public class BomModel
{
    public List<NuGetPackageInfo> Packages { get; set; }
    public List<FrameworkFeatureInfo> FrameworkFeatures { get; set; }
    public List<ServiceVendorInfo> ExternalServices { get; set; }
    public List<ConfigRequirementInfo> ConfigRequirements { get; set; }
    public List<BuildToolInfo> BuildTools { get; set; }
}
```

The dashboard aggregator consumes it:

```csharp
public class DashboardAggregator
{
    public DashboardModel BuildDashboard(
        BomModel bom,
        CodeQualityReport codeQuality,
        ArchitectureReport architecture,
        BuildReport build,
        TestReport tests,
        SecurityReport security,
        WorkflowReport workflow)
    {
        // Combine everything into a unified dashboard
    }
}
```

This keeps the system clean, modular, and testable.

---

# ✅ 6. Extensibility Strategy

CodeMedic should be built with **plugins** in mind.

### Plugin examples:
- Azure plugin  
- AWS plugin  
- EF Core plugin  
- ASP.NET plugin  
- Security plugin  
- Architecture rules plugin  

Each plugin can contribute:
- BOM entries  
- Dashboard sections  
- Health scoring inputs  
- Recommendations  

This makes CodeMedic future-proof.

---

# ✅ 7. MVP Scope (Practical Starting Point)

To get something working quickly:

### **Phase 1 — Core Architecture**
- CLI skeleton  
- Dashboard renderer  
- BOM engine (NuGet + basic config)  
- Code quality metrics  
- Basic health scoring  

### **Phase 2 — Expand BOM**
- Framework feature detection  
- External service detection  
- Build tooling detection  

### **Phase 3 — Dashboard Enhancements**
- Architecture analysis  
- Test health  
- Workflow analysis  
