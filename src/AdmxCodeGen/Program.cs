using AdmxCodeGen.Models;
using AdmxParser;
using System.CommandLine;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace AdmxCodeGen;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        Console.CancelKeyPress += (s, e) =>
        {
            Console.Out.WriteLine("Canceling...");
            cts.Cancel();
            e.Cancel = true;
        };

        var assemblyName = new Argument<string>("assemblyName", description: "Output assembly name");
        var inputPath = new Argument<FileSystemInfo>("inputPath", description: "Input directory path or ADMX file path");
        var outputPath = new Argument<FileInfo>("outputPath", description: "Output file path");

        var generateCsproj = new Option<string>("--generate-csproj", description: "Generate SDK style .csproj file");
        var generateLinqPad = new Option<string>("--generate-linqpad", description: "Generate LinqPad script file");
        var generateBuildLog = new Option<bool>("--generate-buildlog", () => true, description: "Generate build log file");

        var rootCommand = new RootCommand(description: "ADMX to C# code generator");
        rootCommand.Name = "admxcodegen";

        rootCommand.AddArgument(assemblyName);
        rootCommand.AddArgument(inputPath);
        rootCommand.AddArgument(outputPath);

        rootCommand.AddOption(generateCsproj);
        rootCommand.AddOption(generateBuildLog);
        rootCommand.AddOption(generateLinqPad);

        rootCommand.SetHandler(async (
            assemlbyNameValue, inputPathValue, outputPathValue,
            generateCsprojPathValue, generateBuildLogValue, generateLinqPadValue) =>
        {
            try
            {
                AssemblyEmitResult? emitResult = default;

                if (inputPathValue is DirectoryInfo dirInfo)
                {
                    await $"Loading ADMX files from '{inputPathValue.FullName}' directory...".OutWriteLineAsync(cancellationToken).ConfigureAwait(false);

                    var admxDirectory = new AdmxDirectory(inputPathValue.FullName);
                    await admxDirectory.LoadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                    emitResult = await admxDirectory.EmitCompiledAssemblyAsync(
                        assemlbyNameValue,
                        outputPathValue.FullName,
                        cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
                else if (inputPathValue is FileInfo fileInfo)
                {
                    await $"Loading '{inputPathValue.FullName}' ADMX file...".OutWriteLineAsync(cancellationToken).ConfigureAwait(false);

                    var admxContent = new AdmxContent(inputPathValue.FullName);
                    await admxContent.LoadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                    emitResult = await admxContent.EmitCompiledAssemblyAsync(
                        assemlbyNameValue,
                        outputPathValue.FullName,
                        cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                    throw new InvalidOperationException("Invalid input path.");

                if (emitResult == null)
                    throw new InvalidOperationException("Failed to load ADMX content.");

                await $"Build {(emitResult.BuildSucceed ? "succeed" : "failed")}.".OutWriteLineAsync(cancellationToken).ConfigureAwait(false);

                if (generateBuildLogValue)
                {
                    await "Generating build log file...".OutWriteLineAsync(cancellationToken).ConfigureAwait(false);
                    await emitResult.GenerateBuildLog(
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                if (!emitResult.BuildSucceed)
                {
                    await "Build failed with one or more errors: ".ErrorWriteLineAsync(cancellationToken).ConfigureAwait(false);
                    foreach (var eachDiagnostic in emitResult.Diagnostics)
                        await $"* {eachDiagnostic.ToString()}".ErrorWriteLineAsync(cancellationToken).ConfigureAwait(false);
                    Environment.ExitCode = 1;
                    return;
                }

                if (!string.IsNullOrWhiteSpace(generateCsprojPathValue))
                {
                    await "Generating SDK style .csproj file...".OutWriteLineAsync(cancellationToken).ConfigureAwait(false);
                    await emitResult.GenerateSdkStyleProject(
                        generateLinqPadValue, cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(generateLinqPadValue))
                {
                    await "Generating LinqPad script file...".OutWriteLineAsync(cancellationToken).ConfigureAwait(false);
                    await emitResult.GenerateLinqPadScript(
                        generateLinqPadValue, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            catch (AggregateException aggreagateException)
            {
                Environment.ExitCode = 1;
                ExceptionDispatchInfo.Capture(aggreagateException).Throw();
            }
            catch (TargetInvocationException targetInvocationException)
            {
                Environment.ExitCode = 1;
                ExceptionDispatchInfo.Capture(targetInvocationException.InnerException!).Throw();
            }
            catch (Exception thrownException)
            {
                Environment.ExitCode = 1;
                await thrownException.ToString().ErrorWriteLineAsync(cancellationToken).ConfigureAwait(false);
            }
        },
        assemblyName, inputPath, outputPath, generateCsproj, generateBuildLog, generateLinqPad);

        return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
    }

    public static async Task OutWriteLineAsync(this string? s, CancellationToken cancellationToken = default)
        => await Console.Out.WriteLineAsync((s ?? string.Empty).AsMemory(), cancellationToken).ConfigureAwait(false);

    public static async Task ErrorWriteLineAsync(this string? s, CancellationToken cancellationToken = default)
        => await Console.Error.WriteLineAsync((s ?? string.Empty).AsMemory(), cancellationToken).ConfigureAwait(false);
}
