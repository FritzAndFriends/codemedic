# CodeMedic — Repository Bill of Materials (BOM)  
*A comprehensive dependency, vendor, and feature inventory for .NET repositories*

## Overview  
The **CodeMedic Bill of Materials (BOM)** provides a complete, multi‑layered inventory of all external dependencies, frameworks, services, vendors, and environmental requirements used by a .NET codebase. This goes far beyond a simple list of NuGet packages — it delivers enterprise‑grade visibility into the full ecosystem a repository relies on.

The BOM is designed to support:  
- Compliance and procurement workflows  
- Security and risk assessments  
- Architecture and dependency reviews  
- Long‑term maintainability planning  
- Vendor and service lifecycle tracking  

---

## Goals  
The BOM feature aims to:

- Identify **all external dependencies** (packages, frameworks, services, vendors)  
- Provide **links** to documentation, vendor pages, advisories, and source repositories  
- Detect **configuration and environment requirements**  
- Highlight **risk factors** (abandoned packages, vulnerable dependencies, unapproved vendors)  
- Support **drift detection** and **BOM diffs** between branches or releases  
- Produce **machine‑readable** and **human‑readable** outputs  

---

# 1. NuGet Package BOM  
A detailed inventory of all NuGet dependencies across the solution.

### Includes  
- Package name  
- Version  
- Direct vs. transitive  
- Target frameworks  
- License type  
- Vendor/maintainer  
- Repository URL  
- Last update date  
- Known vulnerabilities  
- Advisory database links  

### Value  
- Detect abandoned or risky packages  
- Identify version inconsistencies across projects  
- Provide upgrade recommendations  

---

# 2. Framework & Platform Feature BOM  
Identifies which .NET platform features and frameworks the repository uses.

### Examples  
- ASP.NET Core MVC  
- Minimal APIs  
- Entity Framework Core  
- gRPC  
- SignalR  
- System.Text.Json  
- HttpClientFactory  
- BackgroundService / HostedService  
- Azure SDKs  
- Identity / Authorization  
- Logging providers  

### Detection Methods  
- Using statements  
- Base classes  
- Attributes  
- DI registrations  
- Project references  

### Value  
- Helps teams understand architectural patterns  
- Supports modernization and migration planning  
- Enables feature‑level compliance checks  

---

# 3. External Service & Vendor BOM  
Captures all third‑party services, cloud providers, and external APIs referenced by the codebase.

### Categories  
#### Cloud Providers  
- Azure (Blob, Queue, Service Bus, Key Vault, Cosmos, etc.)  
- AWS (S3, DynamoDB, SNS, SQS, etc.)  
- GCP  

#### Third‑Party APIs  
- Stripe  
- Twilio  
- Auth0 / Okta  
- SendGrid  
- ElasticSearch  
- Redis providers  
- Payment gateways  
- Analytics SDKs  

### Detection Methods  
- NuGet package names  
- DI registrations  
- Configuration keys  
- Known client types (e.g., `BlobServiceClient`)  
- URL patterns in config files  

### BOM Output Includes  
- Vendor name  
- Service name  
- SDK package  
- API endpoints used  
- Links to documentation  
- Links to vendor status pages  
- Links to pricing pages  

### Value  
- Supports vendor management and procurement  
- Enables risk and compliance reviews  
- Helps teams track external service usage  

---

# 4. Configuration & Environment BOM  
Documents all environmental requirements for running the application.

### Includes  
- Required environment variables  
- Required secrets  
- Required connection strings  
- Required certificates  
- Required feature flags  
- Required config files  

### Additional Insights  
- Detect unused config keys  
- Detect missing config keys  
- Detect secrets committed to repo  
- Generate a “configuration contract” for the application  

### Value  
- Supports DevOps, SRE, and platform engineering  
- Reduces onboarding friction  
- Improves deployment reliability  

---

# 5. Build & Tooling BOM  
Captures all tools, SDKs, and build‑time dependencies.

### Includes  
- .NET SDK versions  
- global.json  
- MSBuild custom targets  
- Code generators (e.g., NSwag, EF migrations)  
- Docker base images  
- GitHub Actions workflows  
- Required CLI tools  

### Value  
- Provides a reproducible build environment specification  
- Helps teams identify outdated or unnecessary tooling  
- Supports CI/CD modernization  

---

# 6. Output Formats & Linking Strategy  
The BOM should be available in multiple formats for different audiences.

### Formats  
- **CLI output** (rich, color‑coded)  
- **Markdown** (for documentation)  
- **JSON** (for CI pipelines and automation)  

### Each BOM entry includes  
- Name  
- Version  
- Category  
- Vendor  
- Description  
- Link to documentation  
- Link to source repository  
- Link to security advisories  
- Link to pricing (for cloud services)  
- Link to vendor homepage  

### Value  
- Makes the BOM a living, navigable artifact  
- Enables integration with enterprise tooling  

---

# 7. Enterprise‑Grade Enhancements (Future)  

### ✅ BOM Drift Detection  
Track changes in dependencies, vendors, or services over time.

### ✅ BOM Diff Between Branches  
Identify new dependencies introduced by feature branches.

### ✅ Compliance Checks  
- License compatibility  
- Vendor approval lists  
- Security posture checks  

### ✅ Procurement Exports  
Generate vendor lists for legal or procurement teams.

---

# 8. CLI Command Concepts  

### Generate BOM  
```
codemedic bom generate
```

### Export BOM  
```
codemedic bom export --format json
codemedic bom export --format md
```

### Compare BOMs  
```
codemedic bom diff --from main --to feature/new-api
```

### Validate BOM  
```
codemedic bom validate --rules enterprise.json
```

---

# 9. Implementation Roadmap (High‑Level)  

### Phase 1 — Core BOM Engine  
- NuGet package scanning  
- Framework feature detection  
- JSON + Markdown output  

### Phase 2 — Vendor & Service Detection  
- Cloud SDK heuristics  
- Config‑based service detection  
- Vendor metadata linking  

### Phase 3 — Environment & Tooling BOM  
- Config scanning  
- Build pipeline analysis  
- Docker + GitHub Actions detection  

### Phase 4 — Enterprise Features  
- Drift detection  
- BOM diffs  
- Compliance rules  
- Procurement exports  

---

If you want, I can help you create a companion doc for the **Repository Health Dashboard**, or we can start drafting the **project plan** for implementing the BOM engine.