# CodeMedic Documentation

Welcome to the CodeMedic documentation. This folder contains technical documentation for contributors and developers working on the CodeMedic project.

## Table of Contents

### Architecture & Design
- **[CLI Architecture and Extension Guide](cli_architecture.md)** - Overview of the CLI architecture, command routing, and how to extend the system with new commands
- **[Plugin Architecture](plugin_architecture.md)** - Comprehensive guide for developing, packaging, and integrating plugins with CodeMedic

### Features
- **[Repository Health Dashboard](feature_repository-health-dashboard.md)** - Design and implementation details for the unified health analysis system
- **[Bill of Materials (BOM)](feature_bill-of-materials.md)** - Specification for the comprehensive dependency and vendor inventory feature
- **[Vulnerability Scanning](feature_vulnerability-scanning.md)** - Security-focused NuGet package vulnerability analysis and reporting

### Scanning & Analysis
- **[NuGet Scanning Architecture](nuget_scanning_architecture.md)** - Design and implementation of NuGet package discovery, resolution, and analysis including central package management support and version mismatch detection

### Implementation Details
- **[CLI Skeleton Implementation](cli_skeleton_implementation.md)** - Details of the initial CLI implementation including command handling and console rendering
- **[CLI Skeleton Test Results](cli_skeleton_test_results.md)** - Test results and validation of the CLI skeleton functionality

---

## Document Conventions

- **Architecture documents** describe system design, patterns, and extension points
- **Feature documents** provide detailed specifications and requirements for major features
- **Implementation documents** capture technical details of specific implementations
- **Test results** document validation and testing outcomes

## Contributing to Documentation

When adding new documentation:
- Place it in this `/doc` folder
- Update this README with a link and brief description
- Follow existing document structure and formatting conventions
- Focus on information useful to contributors and developers (not end-users)

For end-user documentation, see the `/user-docs` folder instead.
