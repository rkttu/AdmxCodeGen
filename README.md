# AdmxCodeGen

[![NuGet Version](https://img.shields.io/nuget/v/AdmxCodeGen)](https://www.nuget.org/packages/AdmxCodeGen/) ![Build Status](https://github.com/rkttu/AdmxCodeGen/actions/workflows/dotnet.yml/badge.svg) [![GitHub Sponsors](https://img.shields.io/github/sponsors/rkttu)](https://github.com/sponsors/rkttu/)

A conversion tool that converts ADMX and ADML files to C# code

## Minimum Requirements

- Requires a platform with .NET Standard 2.0
  - This library does not support ADM files.
  - Supported .NET Version: .NET 8.0 (LTS), .NET 6.0 (LTS)
- The generated C# code can only run on Windows platforms.

## How to use

### Command Line Synopsis

```
Description:
  ADMX to C# code generator

Usage:
  admxcodegen <assemblyName> <inputPath> <outputPath> [options]

Arguments:
  <assemblyName>  Output assembly name
  <inputPath>     Input directory path
  <outputPath>    Output file path

Options:
  --generate-csproj <generate-csproj>    Generate SDK style .csproj file
  --generate-buildlog                    Generate build log file [default: True]
  --generate-linqpad <generate-linqpad>  Generate LinqPad script file
  --version                              Show version information
  -?, -h, --help                         Show help and usage information
```

### Convert ADMX directories into .NET assembly

```bash
dotnet run --framework net8.0 -- TestProject "./PolicyDefinitions" "./TestProject" --generate-csproj "MyProject" --generate-linqpad "MyProjectLinq" --generate-buildlog
```

## License

This library follows Apache-2.0 license. See [LICENSE](./LICENSE) file for more information.
