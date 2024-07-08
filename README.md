# AdmxCodeGen

[![NuGet Version](https://img.shields.io/nuget/v/AdmxCodeGen)](https://www.nuget.org/packages/AdmxCodeGen/) ![Build Status](https://github.com/rkttu/AdmxCodeGen/actions/workflows/dotnet.yml/badge.svg) [![GitHub Sponsors](https://img.shields.io/github/sponsors/rkttu)](https://github.com/sponsors/rkttu/)

A conversion tool that converts ADMX and ADML files to C# code

## Minimum Requirements

- Minimum required .NET runtime version: .NET 6.0 (LTS)
  - If you run this tool on a runtime later than this version, dependencies will be automatically aligned by the roll forward feature.
  - This library does not support ADM files.
- The generated assemlby and C# code requires at least .NET 6 and can only run on Windows platforms.

## How to install

1. Install the latest .NET runtime from [https://dot.net/](https://dot.net) first.
2. Run `dotnet tool install --global AdmxCodeGen` command. (Internet connection required.)
3. Run `admxcodegen --help` command to validate installation.

## How to use

### Command Line Synopsis

```
Description:
  ADMX to C# code generator

Usage:
  admxcodegen <assemblyName> <inputPath> <outputPath> [options]

Arguments:
  <assemblyName>  Output assembly name
  <inputPath>     Input directory path or ADMX file path
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
admxcodegen TestProject "./PolicyDefinitions" "./TestProject" --generate-csproj "MyProject" --generate-linqpad "MyProjectLinq" --generate-buildlog
```

### Convert ADMX file into .NET assembly

```bash
admxcodegen InternetExplorer "./PolicyDefinitions/inetres.admx" "./InternetExplorer" --generate-csproj "MyProject" --generate-linqpad "MyProjectLinq" --generate-buildlog
```

## License

This library follows Apache-2.0 license. See [LICENSE](./LICENSE) file for more information.
