# Configuration File Usage

CodeMedic supports running multiple analyses across one or more repositories using configuration files in **JSON** or **YAML** format.

## Quick Start

```powershell
# Run with default configuration (YAML)
.\run-config.ps1

# Run with a specific configuration file
.\run-config.ps1 my-config.json

# On Linux/macOS
./run-config.sh my-config.yaml
```

## Configuration File Format

### JSON Format

```json
{
  "global": {
    "format": "markdown",
    "output-dir": "./reports"
  },
  "repositories": [
    {
      "name": "MyProject",
      "path": "./src",
      "commands": ["health", "bom", "vulnerabilities"]
    }
  ]
}
```

### YAML Format

```yaml
global:
  format: markdown
  output-dir: ./reports

repositories:
  - name: MyProject
    path: ./src
    commands:
      - health
      - bom
      - vulnerabilities
```

## Configuration Options

### Global Section

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `format` | string | `"markdown"` | Output format for reports. Currently only `"markdown"` is supported. |
| `output-dir` | string | `"."` | Directory where report files will be written. Will be created if it doesn't exist. |

### Repository Section

Each repository in the `repositories` array has the following properties:

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `name` | string | Yes | A friendly name for the repository. Used in report file names. |
| `path` | string | Yes | Path to the repository directory (absolute or relative). |
| `commands` | array | Yes | List of CodeMedic commands to run against this repository. |

### Available Commands

- `health` - Repository health dashboard analysis
- `bom` - Bill of materials report
- `vulnerabilities` - Vulnerability scanning report

## Output Files

For each repository and command combination, CodeMedic creates a markdown file:

```
{output-dir}/{repository-name}_{command}.md
```

**Example:**
- `reports/MyProject_health.md`
- `reports/MyProject_bom.md`
- `reports/MyProject_vulnerabilities.md`

## Multi-Repository Example

Analyze multiple projects in one run:

```yaml
global:
  format: markdown
  output-dir: ./all-reports

repositories:
  - name: Backend
    path: ./services/api
    commands:
      - health
      - bom
      - vulnerabilities

  - name: Frontend
    path: ./apps/web
    commands:
      - health

  - name: Shared
    path: ./packages/shared
    commands:
      - bom
```

This will generate:
- `all-reports/Backend_health.md`
- `all-reports/Backend_bom.md`
- `all-reports/Backend_vulnerabilities.md`
- `all-reports/Frontend_health.md`
- `all-reports/Shared_bom.md`

## Command Line Usage

```bash
# Using the config command directly
codemedic config <path-to-config-file>

# Examples
codemedic config ./config.json
codemedic config ./config.yaml
codemedic config ../another-repo/codemedic-config.yml
```

## Error Handling

- If a configuration file is not found, CodeMedic exits with an error
- If a repository path doesn't exist, the command continues but may produce incomplete reports
- If an unknown command is specified, it is skipped and processing continues with remaining commands
- Invalid JSON or YAML syntax will cause the configuration to fail to load

## Tips and Best Practices

1. **Use Relative Paths**: Keep configuration files in your repository root and use relative paths to subdirectories.

2. **Version Control**: Check configuration files into version control so team members can run consistent analyses.

3. **CI/CD Integration**: Use configuration files in CI/CD pipelines to generate reports automatically:
   ```yaml
   # GitHub Actions example
   - name: Run CodeMedic Analysis
     run: |
       dotnet tool install -g codemedic
       codemedic config .codemedic-ci.yaml
   ```

4. **Separate Configurations**: Create different configuration files for different scenarios:
   - `config-full.yaml` - Complete analysis with all commands
   - `config-quick.yaml` - Fast health checks only
   - `config-ci.yaml` - Optimized for CI/CD pipelines

5. **Output Organization**: Use descriptive output directory names:
   - `./reports/$(date +%Y%m%d)` - Date-stamped reports
   - `./reports/nightly` - Scheduled analysis results
   - `./reports/release` - Pre-release validation

## Sample Configurations

See the root directory for example configurations:
- `sample-config.json` - Basic single-repository configuration (JSON)
- `sample-config.yaml` - Basic single-repository configuration (YAML)
- `sample-config-multi-repo.json` - Multi-repository configuration (JSON)
- `sample-config-multi-repo.yaml` - Multi-repository configuration (YAML)
