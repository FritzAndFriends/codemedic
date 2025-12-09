# Docker Usage Guide

CodeMedic can be run inside a Docker container, providing a consistent, isolated environment across all platforms without needing to install .NET locally.

## Quick Start

### Building the Docker Image

Run the appropriate build script for your platform from the repository root:

**Windows (PowerShell):**
```powershell
.\build-docker.ps1
```

**Windows (Command Prompt):**
```cmd
build-docker.cmd
```

**Linux/macOS:**
```bash
./build-docker.sh
```

This will automatically:
- Retrieve the version from Nerd Bank Git Versioning (NBGv2)
- Build the Docker image with appropriate version tags
- Display usage examples

### Running CodeMedic in Docker

**Show help:**
```bash
docker run --rm codemedic:latest --help
```

**Run health check on current directory:**
```bash
# Windows (PowerShell)
docker run --rm -v ${PWD}:/repo codemedic:latest health --path /repo

# Windows (Command Prompt)
docker run --rm -v %cd%:/repo codemedic:latest health --path /repo

# Linux/macOS
docker run --rm -v $(pwd):/repo codemedic:latest health --path /repo
```

**Run health check with output to file:**
```bash
docker run --rm -v ${PWD}:/repo codemedic:latest health -p /repo --format markdown > health-report.md
```

**Run other commands:**
```bash
# Bill of Materials
docker run --rm -v $(pwd):/repo codemedic:latest bom --path /repo

# Vulnerability scan
docker run --rm -v $(pwd):/repo codemedic:latest vulnerabilities -p /repo --format markdown
```

## Build Script Options

All build scripts support the following options:

### PowerShell (`build-docker.ps1`)
```powershell
# Build locally (default)
.\build-docker.ps1

# Build and push to registry
.\build-docker.ps1 -Registry "ghcr.io/csharpfritz" -Push

# Build debug configuration
.\build-docker.ps1 -BuildConfiguration Debug
```

**Parameters:**
- `-Registry <string>` - Docker registry prefix (e.g., `ghcr.io/owner`, `docker.io/username`)
- `-Push` - Push the built images to the specified registry
- `-BuildConfiguration <Release|Debug>` - Build configuration (default: Release)

### Bash (`build-docker.sh`)
```bash
# Build locally (default)
./build-docker.sh

# Build and push to registry
./build-docker.sh --registry ghcr.io/csharpfritz --push

# Build debug configuration
./build-docker.sh --configuration Debug
```

**Options:**
- `-r, --registry <registry>` - Docker registry prefix
- `-p, --push` - Push images to registry after build
- `-c, --configuration <Release|Debug>` - Build configuration
- `-h, --help` - Show help message

### Command Prompt (`build-docker.cmd`)
```cmd
REM Build locally (default)
build-docker.cmd

REM Build and push to registry
build-docker.cmd --registry ghcr.io/csharpfritz --push

REM Build debug configuration
build-docker.cmd --configuration Debug
```

**Options:**
- `-r, --registry <registry>` - Docker registry prefix
- `-p, --push` - Push images to registry after build
- `-c, --configuration <Release|Debug>` - Build configuration
- `-h, --help` - Show help message

## Image Tags

The build scripts automatically create multiple tags for flexibility:

- `codemedic:x.y.z` - Full semantic version (e.g., `0.1.0`)
- `codemedic:x.y` - Major.minor version (e.g., `0.1`)
- `codemedic:latest` - Latest build
- `codemedic:x.y.z-abc1234` - Version with commit hash for tracking

## Volume Mounting

To analyze a repository, you need to mount it as a volume inside the container:

```bash
docker run --rm -v /path/to/repo:/repo codemedic:latest health /repo
```

**Important:** The path after `-v` should be:
- An absolute path to the repository on your host system
- Followed by `:/repo` (the mount point inside the container)
- Then use `/repo` as the target path in the CodeMedic command

## Common Use Cases

### Analyze Local Repository
```bash
# PowerShell
docker run --rm -v ${PWD}:/repo codemedic:latest health /repo

# Bash/Linux/macOS
docker run --rm -v $(pwd):/repo codemedic:latest health /repo
```

### Save Report to File
```bash
docker run --rm -v ${PWD}:/repo codemedic:latest health /repo --format markdown --output /repo/health.md
```

### Run in CI/CD Pipeline
```yaml
# GitHub Actions example
- name: Run CodeMedic Health Check
  run: |
    docker run --rm -v ${{ github.workspace }}:/repo codemedic:latest health /repo
```

### Interactive Container (for debugging)
```bash
docker run --rm -it --entrypoint /bin/bash codemedic:latest
```

## Publishing to Container Registries

### GitHub Container Registry (GHCR)

1. **Authenticate:**
   ```bash
   echo $GITHUB_TOKEN | docker login ghcr.io -u USERNAME --password-stdin
   ```

2. **Build and push:**
   ```powershell
   .\build-docker.ps1 -Registry "ghcr.io/csharpfritz" -Push
   ```

### Docker Hub

1. **Authenticate:**
   ```bash
   docker login
   ```

2. **Build and push:**
   ```bash
   ./build-docker.sh --registry docker.io/yourusername --push
   ```

## Troubleshooting

### Permission Issues on Linux/macOS
If you encounter permission issues accessing mounted volumes:

```bash
# Run as current user
docker run --rm --user $(id -u):$(id -g) -v $(pwd):/repo codemedic:latest health /repo
```

### Windows Path Issues
Use PowerShell or explicitly set the path:

```powershell
# PowerShell (recommended)
docker run --rm -v ${PWD}:/repo codemedic:latest health /repo

# Command Prompt
docker run --rm -v %cd%:/repo codemedic:latest health /repo
```

### Image Size
The multi-stage Dockerfile keeps the final image small by:
- Using the SDK image only for building
- Using the minimal runtime image for the final container
- Excluding unnecessary files via `.dockerignore`

## Version Information

The Docker image version is automatically synchronized with the NBGv2 version from the repository. You can verify the version:

```bash
docker run --rm codemedic:latest --version
```

## Advanced Usage

### Custom Entrypoint
Run arbitrary commands inside the container:

```bash
docker run --rm -it --entrypoint /bin/bash codemedic:latest
```

### Override Default Command
The default command is `--help`. Override it by specifying commands:

```bash
docker run --rm codemedic:latest health /repo --verbose
```

### Environment Variables
Pass environment variables for configuration:

```bash
docker run --rm -e SOME_CONFIG=value -v $(pwd):/repo codemedic:latest health /repo
```
