# NuGetInspector Test Coverage

This document outlines the test coverage for the `NuGetInspector` class.

## Test Structure

All tests follow the **Given-When-Then** syntax for clarity:
- **Given**: Setup and preconditions
- **When**: Action being tested
- **Then**: Expected outcomes and assertions

## Test Categories

### Constructor Tests (2 tests)
- ✅ Validates successful initialization with valid root path
- ✅ Confirms physical file system is used when no mock is provided

### RefreshCentralPackageVersionFiles Tests (3 tests)
- ✅ Finds Directory.Packages.props files during refresh
- ✅ Handles file system exceptions gracefully
- ✅ Verifies multiple refresh calls work correctly

### ReadPackageReferences Tests (8 tests)
- ✅ Reads packages with direct version attributes
- ✅ Reads packages with version elements
- ✅ Handles Update attribute on PackageReference
- ✅ Resolves versions from central package management (Directory.Packages.props)
- ✅ Returns "unknown" version when no version is found
- ✅ Returns empty list for projects without packages
- ✅ Uses closest Directory.Packages.props in nested hierarchies
- ✅ Supports VersionOverride attribute in central management

### ExtractTransitiveDependencies Tests (6 tests)
- ✅ Extracts transitive dependencies from packages.lock.json
- ✅ Falls back to project.assets.json when lock file doesn't exist
- ✅ Excludes project references from transitive dependencies
- ✅ Returns empty list when no lock or assets file exists
- ✅ Excludes direct dependencies from transitive list
- ✅ Correctly identifies source packages for transitive dependencies

## Test Statistics

- **Total Tests**: 17
- **Passing**: 17 ✅
- **Failed**: 0
- **Skipped**: 0
- **Duration**: ~5.2s

## Mocking Strategy

All tests use **Moq** to mock the `IFileSystem` interface, ensuring:
- No file system I/O during tests
- Fast test execution
- Predictable test behavior
- Cross-platform compatibility

## Coverage Areas

### ✅ Covered
- Constructor initialization
- Central package version resolution
- Package reference reading with various configurations
- Transitive dependency extraction
- Project reference filtering
- Error handling for missing files
- Nested Directory.Packages.props resolution

### 🔄 Future Enhancements
- RestorePackagesAsync tests (requires process mocking)
- More complex dependency graph scenarios
- Performance tests for large repositories
- Integration tests with real file system
