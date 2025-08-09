# Benday.CommandsFramework Development Instructions

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Project Overview
Benday.CommandsFramework is a .NET library for building command-line interface (CLI) utilities. It provides command line argument parsing, named command definition, argument validation, and support for string, boolean, int, and datetime arguments. The framework supports both synchronous and asynchronous commands.

## Working Effectively

### Prerequisites and Setup
- Install .NET 9.0 SDK: `curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0`
- Install .NET 8.0 SDK: `curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0`
- Add to PATH: `export PATH="$HOME/.dotnet:$PATH"`
- Verify installation: `dotnet --list-sdks` (should show both 8.0.x and 9.0.x versions)
- Verify runtimes: `dotnet --list-runtimes` (should show Microsoft.NETCore.App for both versions)

### Build and Test Process
**CRITICAL**: NEVER CANCEL builds or tests. Set timeouts to 60+ minutes for builds and 30+ minutes for tests.

1. **Restore dependencies** (takes ~12 seconds):
   ```bash
   dotnet restore
   ```
   - Timeout: Set to 60 seconds minimum
   - NEVER CANCEL: Wait for completion even if it appears slow

2. **Build the solution** (takes ~10 seconds):
   ```bash
   dotnet build --no-restore
   ```
   - Timeout: Set to 60 seconds minimum  
   - NEVER CANCEL: Build process may appear to pause but will complete
   - Builds both .NET 8.0 and 9.0 targets
   - Expect 6-10 nullable reference warnings (these are normal)

3. **Run tests** (takes ~3 seconds):
   ```bash
   dotnet test --no-build --verbosity normal
   ```
   - Timeout: Set to 30 seconds minimum
   - NEVER CANCEL: All 144 tests should pass
   - Tests run on .NET 9.0 target framework

### Validation and Testing Scenarios
After making changes, ALWAYS validate functionality by testing the CLI application:

1. **Basic CLI validation**:
   ```bash
   dotnet run --project test/Benday.CommandsFramework.Samples --no-build -- --help
   ```
   - Should display list of available commands
   - Application name: "Sample Tool using Commands Framework"

2. **Command help validation**:
   ```bash
   dotnet run --project test/Benday.CommandsFramework.Samples --no-build -- command1 --help
   ```
   - Should show command usage with required and optional parameters

3. **End-to-end command execution**:
   ```bash
   dotnet run --project test/Benday.CommandsFramework.Samples --no-build -- command1 /arg1:test /isawesome:true /count:5 /dateofthingy:2024-01-01
   ```
   - Should output "** SUCCESS **" followed by parameter values
   - Validates argument parsing, type conversion, and command execution

4. **Additional command validation**:
   ```bash
   dotnet run --project test/Benday.CommandsFramework.Samples --no-build -- datetimetest --help
   dotnet run --project test/Benday.CommandsFramework.Samples --no-build -- get-configuration --help
   ```

### Package Generation
The main library automatically generates NuGet packages during build:
- Target frameworks: .NET 8.0 and 9.0
- Package output: `src/Benday.CommandsFramework/bin/Debug/Benday.CommandsFramework.*.nupkg`

## Repository Structure

### Key Projects
- **src/Benday.CommandsFramework/**: Main framework library (.NET 8.0 + 9.0)
- **test/Benday.CommandsFramework.Samples/**: Sample CLI application (.NET 8.0)
- **test/Benday.CommandsFramework.Tests/**: Unit tests (.NET 9.0) - 144 tests total

### Important Files
- `Benday.CommandsFramework.sln`: Solution file containing all projects
- `.github/workflows/dotnet.yml`: CI/CD pipeline configuration
- `docfx_project/`: Documentation generation (requires DocFX installation)
- `generate-docfx-docs.sh`: Generate API documentation
- `serve-docfx-docs.sh`: Serve documentation locally

### Command Samples Location
Sample command implementations in `test/Benday.CommandsFramework.Samples/`:
- `SampleCommand1.cs`: Basic command with multiple argument types
- `SampleAsyncCommand.cs`: Asynchronous command example
- `DateTimeTestCommand.cs`: DateTime argument validation
- `SampleCommandWithDefaultValues.cs`: Commands with default parameters

## CI/CD Integration
The GitHub Actions workflow (.github/workflows/dotnet.yml) runs:
1. `dotnet restore`
2. `dotnet build --no-restore` 
3. `dotnet test --no-build --verbosity normal`

Always ensure your changes pass this pipeline by running the same commands locally.

## Common Issues and Solutions

### .NET SDK Version Problems
- Error: "The current .NET SDK does not support targeting .NET 9.0"
- Solution: Install both .NET 8.0 and 9.0 SDKs using the commands above

### Missing Runtime Error
- Error: "You must install or update .NET to run this application"
- Solution: Ensure both .NET 8.0 and 9.0 runtimes are installed (included with SDKs)

### DocFX Documentation
- DocFX is not pre-installed and requires separate installation
- Scripts `generate-docfx-docs.sh` and `serve-docfx-docs.sh` will fail without DocFX
- Documentation generation is optional for most development tasks

## Quick Reference Commands

```bash
# Complete build and test sequence
export PATH="$HOME/.dotnet:$PATH"
dotnet restore                           # ~12 seconds
dotnet build --no-restore              # ~10 seconds  
dotnet test --no-build --verbosity normal  # ~3 seconds

# Test CLI functionality
dotnet run --project test/Benday.CommandsFramework.Samples --no-build -- --help
dotnet run --project test/Benday.CommandsFramework.Samples --no-build -- command1 /arg1:test /isawesome:true /count:5 /dateofthingy:2024-01-01

# Check .NET installation
dotnet --version
dotnet --list-sdks
dotnet --list-runtimes
```

## Development Best Practices
- Always run the complete validation sequence after changes
- Test both framework library changes and CLI functionality
- Expect and ignore nullable reference warnings during build
- Use the sample commands to validate argument parsing changes
- Test with different data types (string, boolean, int, datetime)
- Verify both .NET 8.0 and 9.0 compatibility when making changes