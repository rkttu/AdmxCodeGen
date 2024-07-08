using AdmxCodeGen.Models;
using AdmxParser;
using AdmxParser.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Scriban;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace AdmxCodeGen;

internal static partial class CodeRenderer
{
    public static readonly string BannerKey = "<BANNER>";

    public static readonly string SharedKey = "<SHARED>";

    public static async Task<AssemblyEmitResult> EmitCompiledAssemblyAsync(this AdmxDirectory admxDirectory,
        string? assemblyName,
        string outputDirectoryPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
            throw new ArgumentException("Assembly name cannot be null or white space string.", nameof(assemblyName));

        if (!Directory.Exists(outputDirectoryPath))
            Directory.CreateDirectory(outputDirectoryPath);

        var output = new AssemblyEmitResult();
        var executableReferences = await PrepareAppReferencesFromOnlineAsync(cancellationToken).ConfigureAwait(false);
        if (executableReferences.Count < 1)
            throw new DirectoryNotFoundException("Cannot obtain base directory path of .NET runtime.");

        var targetEncoding = new UTF8Encoding(false);
        var tempSourceFilePath = Path.Combine(outputDirectoryPath, $"{assemblyName}_temp.cs");

        using (var stream = File.OpenWrite(tempSourceFilePath))
        using (var writer = new StreamWriter(stream, targetEncoding))
        {
            await RenderCSharpCodesAsync(admxDirectory, assemblyName, writer, cancellationToken).ConfigureAwait(false);
        }

        var sourceText = default(SourceText);
        using (var stream = File.OpenRead(tempSourceFilePath))
        {
            sourceText = LoadCSharpCode(SourceText.From(stream), cancellationToken);
        }

        var sourceFilePath = Path.Combine(outputDirectoryPath, $"{assemblyName}.cs");
        using (var stream = File.OpenWrite(sourceFilePath))
        using (var writer = new StreamWriter(stream, targetEncoding))
        {
            sourceText.Write(writer, cancellationToken);
        }

        if (File.Exists(tempSourceFilePath))
            File.Delete(tempSourceFilePath);

        var peFilePath = Path.Combine(outputDirectoryPath, $"{assemblyName}.dll");
        var pdbFilePath = Path.Combine(outputDirectoryPath, $"{assemblyName}.pdb");
        var xmlFilePath = Path.Combine(outputDirectoryPath, $"{assemblyName}.xml");

        using var peStream = File.OpenWrite(peFilePath);
        {
            using var pdbStream = File.OpenWrite(pdbFilePath);
            using var xmlOutputStream = File.OpenWrite(xmlFilePath);

            var compileOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, cancellationToken: cancellationToken);
            var compilation = CSharpCompilation.Create(assemblyName, new[] { syntaxTree, }, executableReferences, compileOptions);

            await $"Compiling '{assemblyName}'...".OutWriteLineAsync(cancellationToken).ConfigureAwait(false);
            var emitResult = compilation.Emit(peStream, pdbStream: pdbStream, xmlDocumentationStream: xmlOutputStream, cancellationToken: cancellationToken);
            var failures = emitResult.Diagnostics.Where(x => x.Severity != DiagnosticSeverity.Hidden && x.Severity != DiagnosticSeverity.Info);
            var messages = new List<string>();

            output.BuildSucceed = emitResult.Success;

            if (failures.Any())
            {
                foreach (var diagnostic in failures)
                {
                    var lineSpan = diagnostic.Location.GetLineSpan();
                    var startLine = lineSpan.StartLinePosition.Line;
                    var lineText = sourceText.Lines[startLine].ToString();
                    messages.Add($"[ln.{startLine + 1}] {diagnostic.GetMessage()} - {lineText}");
                }
            }

            output.Diagnostics = messages.AsReadOnly();

            if (emitResult.Success)
            {
                output.AssemblyName = assemblyName;
                output.OutputDirectoryPath = outputDirectoryPath;
                output.BuildOutputPath = peFilePath;
                output.DebugSymbolFilePath = pdbFilePath;
                output.XmlDocumentFilePath = xmlFilePath;
            }
        }

        return output;
    }

    public static async Task<AssemblyEmitResult> EmitCompiledAssemblyAsync(this AdmxContent admxContent,
        string? assemblyName,
        string outputDirectoryPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
            throw new ArgumentException("Assembly name cannot be null or white space string.", nameof(assemblyName));

        if (!Directory.Exists(outputDirectoryPath))
            Directory.CreateDirectory(outputDirectoryPath);

        var output = new AssemblyEmitResult();
        var executableReferences = await PrepareAppReferencesFromOnlineAsync(cancellationToken).ConfigureAwait(false);
        if (executableReferences.Count < 1)
            throw new DirectoryNotFoundException("Cannot obtain base directory path of .NET runtime.");

        var targetEncoding = new UTF8Encoding(false);
        var tempSourceFilePath = Path.Combine(outputDirectoryPath, $"{assemblyName}_temp.cs");

        using (var stream = File.OpenWrite(tempSourceFilePath))
        using (var writer = new StreamWriter(stream, targetEncoding))
        {
            await RenderCSharpCodesAsync(admxContent, assemblyName, writer, cancellationToken).ConfigureAwait(false);
        }

        var sourceText = default(SourceText);
        using (var stream = File.OpenRead(tempSourceFilePath))
        {
            sourceText = LoadCSharpCode(SourceText.From(stream), cancellationToken);
        }

        var sourceFilePath = Path.Combine(outputDirectoryPath, $"{assemblyName}.cs");
        using (var stream = File.OpenWrite(sourceFilePath))
        using (var writer = new StreamWriter(stream, targetEncoding))
        {
            sourceText.Write(writer, cancellationToken);
        }

        if (File.Exists(tempSourceFilePath))
            File.Delete(tempSourceFilePath);

        var peFilePath = Path.Combine(outputDirectoryPath, $"{assemblyName}.dll");
        var pdbFilePath = Path.Combine(outputDirectoryPath, $"{assemblyName}.pdb");
        var xmlFilePath = Path.Combine(outputDirectoryPath, $"{assemblyName}.xml");

        using var peStream = File.OpenWrite(peFilePath);
        {
            using var pdbStream = File.OpenWrite(pdbFilePath);
            using var xmlOutputStream = File.OpenWrite(xmlFilePath);

            var compileOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, cancellationToken: cancellationToken);
            var compilation = CSharpCompilation.Create(assemblyName, new[] { syntaxTree, }, executableReferences, compileOptions);

            await $"Compiling '{assemblyName}'...".OutWriteLineAsync(cancellationToken).ConfigureAwait(false);
            var emitResult = compilation.Emit(peStream, pdbStream: pdbStream, xmlDocumentationStream: xmlOutputStream, cancellationToken: cancellationToken);
            var failures = emitResult.Diagnostics.Where(x => x.Severity != DiagnosticSeverity.Hidden && x.Severity != DiagnosticSeverity.Info);
            var messages = new List<string>();

            output.BuildSucceed = emitResult.Success;

            if (failures.Any())
            {
                foreach (var diagnostic in failures)
                {
                    var lineSpan = diagnostic.Location.GetLineSpan();
                    var startLine = lineSpan.StartLinePosition.Line;
                    var lineText = sourceText.Lines[startLine].ToString();
                    messages.Add($"[ln.{startLine + 1}] {diagnostic.GetMessage()} - {lineText}");
                }
            }

            output.Diagnostics = messages.AsReadOnly();

            if (emitResult.Success)
            {
                output.AssemblyName = assemblyName;
                output.OutputDirectoryPath = outputDirectoryPath;
                output.BuildOutputPath = peFilePath;
                output.DebugSymbolFilePath = pdbFilePath;
                output.XmlDocumentFilePath = xmlFilePath;
            }
        }

        return output;
    }

    private static async Task RenderCSharpCodesAsync(this AdmxDirectory admxDirectory, string assemblyName, TextWriter writer, CancellationToken cancellationToken = default)
    {
        if (admxDirectory == null)
            throw new ArgumentNullException(nameof(admxDirectory));
        if (!admxDirectory.Loaded)
            throw new ArgumentException("Please load the directory model first before rendering.", nameof(admxDirectory));
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));

        var models = admxDirectory.ParseModels();

        await writer.WriteLineAsync(
            Templates.GetFileHeaderBanner().AsMemory(),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await writer.WriteLineAsync("#pragma warning disable CS0219, CS8019");
        await writer.WriteLineAsync($"namespace {assemblyName} {{");

        var policyTemplate = Template.Parse(Templates.GetPolicyRenderingTemplate());
        foreach (var eachPolicy in models)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            await writer.WriteLineAsync(policyTemplate.Render(eachPolicy.ToTemplateContext(additionalModel: new
            {
                UsingReferences = Templates.GetUsingReferences(),
            })).AsMemory(), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        var supplementTemplate = Template.Parse(Templates.GetSupplementRenderingTemplates());
        await writer.WriteLineAsync(supplementTemplate.Render(new
        {
            UsingReferences = Templates.GetUsingReferences(),
            Body = string.Join(Environment.NewLine, new string[]
            {
                Templates.GetBaseModels(),
                Templates.GetBaseInterfaces(),
                Templates.GetGroupPolicyObject(),
                Templates.GetGroupPolicyMethods(),
                Templates.GetInteropCodes(),
                Templates.GetHelperCodes(),
            }),
        }.ToTemplateContext()).AsMemory(), cancellationToken: cancellationToken).ConfigureAwait(false);

        await writer.WriteLineAsync($"}}");
    }

    private static async Task RenderCSharpCodesAsync(this AdmxContent admxContent, string assemblyName, TextWriter writer, CancellationToken cancellationToken = default)
    {
        if (admxContent == null)
            throw new ArgumentNullException(nameof(admxContent));
        if (!admxContent.Loaded)
            throw new ArgumentException("Please load the content model first before rendering.", nameof(admxContent));
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));

        var model = admxContent.ParseModel();

        await writer.WriteLineAsync(
            Templates.GetFileHeaderBanner().AsMemory(),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await writer.WriteLineAsync("#pragma warning disable CS0219, CS8019");
        await writer.WriteLineAsync($"namespace {assemblyName} {{");

        var policyTemplate = Template.Parse(Templates.GetPolicyRenderingTemplate());
        if (cancellationToken.IsCancellationRequested)
            throw new OperationCanceledException();

        await writer.WriteLineAsync(policyTemplate.Render(model.ToTemplateContext(additionalModel: new
        {
            UsingReferences = Templates.GetUsingReferences(),
        })).AsMemory(), cancellationToken: cancellationToken).ConfigureAwait(false);

        var supplementTemplate = Template.Parse(Templates.GetSupplementRenderingTemplates());
        await writer.WriteLineAsync(supplementTemplate.Render(new
        {
            UsingReferences = Templates.GetUsingReferences(),
            Body = string.Join(Environment.NewLine, new string[]
            {
                Templates.GetBaseModels(),
                Templates.GetBaseInterfaces(),
                Templates.GetGroupPolicyObject(),
                Templates.GetGroupPolicyMethods(),
                Templates.GetInteropCodes(),
                Templates.GetHelperCodes(),
            }),
        }.ToTemplateContext()).AsMemory(), cancellationToken: cancellationToken).ConfigureAwait(false);

        await writer.WriteLineAsync($"}}");

    }

    private static SourceText LoadCSharpCode(SourceText sourceText, CancellationToken cancellationToken)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            sourceText,
            cancellationToken: cancellationToken);
        return syntaxTree
            .GetRoot(cancellationToken)
            .NormalizeWhitespace()
            .SyntaxTree
            .GetText(cancellationToken);
    }

    public static async Task GenerateSdkStyleProject(
        this AssemblyEmitResult emitResult, string projectName,
        CancellationToken cancellationToken = default)
    {
        if (emitResult == null)
            throw new ArgumentException("Emit result cannot be null reference.", nameof(emitResult));
        if (!emitResult.BuildSucceed)
            throw new InvalidOperationException("Cannot generate project file from failed build result.");
        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException("Project name cannot be null or white space string.", nameof(projectName));

        var assemblyName = emitResult.AssemblyName;
        if (string.IsNullOrWhiteSpace(assemblyName))
            throw new ArgumentException("Assembly name cannot be null or white space string.", nameof(assemblyName));

        if (string.Equals(assemblyName, projectName, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Project name and assembly name cannot be same.", nameof(projectName));

        var outputDirectoryPath = emitResult.OutputDirectoryPath;
        if (!Directory.Exists(outputDirectoryPath))
            throw new DirectoryNotFoundException($"'{outputDirectoryPath}' is not exists.");

        var targetEncoding = new UTF8Encoding(false);

        var outputFilePath = Path.Combine(outputDirectoryPath, $"{projectName}.csproj");
        await File.WriteAllTextAsync(outputFilePath, $$"""
			<Project Sdk="Microsoft.NET.Sdk">
				<PropertyGroup>
					<OutputType>Exe</OutputType>
					<TargetFramework>net6.0-windows7.0</TargetFramework>
					<ApplicationManifest>app.manifest</ApplicationManifest>
				</PropertyGroup>
				<ItemGroup>
					<Compile Remove="*.cs" />
					<Compile Include="Program.cs" />
				</ItemGroup>
				<ItemGroup>
					<None Remove="{{assemblyName}}.dll" />
					<None Remove="{{assemblyName}}.linq" />
					<None Remove="{{assemblyName}}.pdb" />
					<None Remove="{{assemblyName}}.xml" />
					<None Remove="{{assemblyName}}.log" />
				</ItemGroup>
				<ItemGroup>
					<Reference Include="{{assemblyName}}">
						<HintPath>{{assemblyName}}.dll</HintPath>
					</Reference>
				</ItemGroup>
			</Project>
			""", targetEncoding, cancellationToken).ConfigureAwait(false);

        outputFilePath = Path.Combine(outputDirectoryPath, $"app.manifest");
        await File.WriteAllTextAsync(outputFilePath, $$"""
			<?xml version="1.0" encoding="utf-8"?>
			<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
				<assemblyIdentity version="1.0.0.0" name="{{projectName}}.app"/>
				<trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
					<security>
						<requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
							<requestedExecutionLevel level="highestAvailable" uiAccess="false" />
						</requestedPrivileges>
					</security>
				</trustInfo>
				<compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
					<application>
						<supportedOS Id="{e2011457-1546-43c5-a5fe-008deee3d3f0}" />
						<supportedOS Id="{35138b9a-5d96-4fbd-8e2d-a2440225f93a}" />
						<supportedOS Id="{4a2f28e3-53b9-4441-ba9c-d69d4a4a6e38}" />
						<supportedOS Id="{1f676c76-80e1-4239-95bb-83d0f6d0da78}" />
						<supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
					</application>
				</compatibility>
			</assembly>
			""", targetEncoding, cancellationToken).ConfigureAwait(false);

        outputFilePath = Path.Combine(outputDirectoryPath, $"Program.cs");
        await File.WriteAllTextAsync(outputFilePath, $$"""
			using System;
				
			internal static class Program
			{
				[STAThread]
				private static void Main()
				{
					/* Test Your Code Here */
				}
			}
			""", targetEncoding, cancellationToken).ConfigureAwait(false);

        var projectItemGuid = Guid.NewGuid();
        var projectGuid = Guid.NewGuid();

        outputFilePath = Path.Combine(outputDirectoryPath, $"{assemblyName}.sln");
        await File.WriteAllTextAsync(outputFilePath, $$"""
			
			Microsoft Visual Studio Solution File, Format Version 12.00
			# Visual Studio Version 17
			VisualStudioVersion = 17.10.35004.147
			MinimumVisualStudioVersion = 10.0.40219.1
			Project("{{{projectItemGuid}}}") = "{{projectName}}", "{{projectName}}.csproj", "{{{projectGuid}}}"
			EndProject
			Global
				GlobalSection(SolutionConfigurationPlatforms) = preSolution
					Debug|Any CPU = Debug|Any CPU
					Release|Any CPU = Release|Any CPU
				EndGlobalSection
				GlobalSection(ProjectConfigurationPlatforms) = postSolution
					{{{projectGuid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
					{{{projectGuid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU
					{{{projectGuid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU
					{{{projectGuid}}}.Release|Any CPU.Build.0 = Release|Any CPU
				EndGlobalSection
				GlobalSection(SolutionProperties) = preSolution
					HideSolutionNode = FALSE
				EndGlobalSection
				GlobalSection(ExtensibilityGlobals) = postSolution
					SolutionGuid = {2CE9EC88-7072-4B5F-A472-BB3C282180A7}
				EndGlobalSection
			EndGlobal
			""", targetEncoding, cancellationToken).ConfigureAwait(false);
    }

    public static async Task GenerateBuildLog(
        this AssemblyEmitResult emitResult, CancellationToken cancellationToken = default)
    {
        if (emitResult == null)
            throw new ArgumentException("Emit result cannot be null reference.", nameof(emitResult));
        if (!emitResult.BuildSucceed)
            throw new InvalidOperationException("Cannot generate project file from failed build result.");

        var assemblyName = emitResult.AssemblyName;
        if (string.IsNullOrWhiteSpace(assemblyName))
            throw new ArgumentException("Assembly name cannot be null or white space string.", nameof(assemblyName));

        var outputDirectoryPath = emitResult.OutputDirectoryPath;
        if (!Directory.Exists(outputDirectoryPath))
            throw new DirectoryNotFoundException($"'{outputDirectoryPath}' is not exists.");

        var targetEncoding = new UTF8Encoding(false);
        var outputFilePath = Path.Combine(outputDirectoryPath, $"{assemblyName}.log");
        await File.WriteAllLinesAsync(outputFilePath, emitResult.Diagnostics, targetEncoding, cancellationToken).ConfigureAwait(false);
    }

    public static async Task GenerateLinqPadScript(
        this AssemblyEmitResult emitResult, string linqPadScriptFileName,
        CancellationToken cancellationToken = default)
    {
        if (emitResult == null)
            throw new ArgumentException("Emit result cannot be null reference.", nameof(emitResult));
        if (!emitResult.BuildSucceed)
            throw new InvalidOperationException("Cannot generate project file from failed build result.");

        var assemblyName = emitResult.AssemblyName;
        if (string.IsNullOrWhiteSpace(assemblyName))
            throw new ArgumentException("Assembly name cannot be null or white space string.", nameof(assemblyName));

        var outputDirectoryPath = emitResult.OutputDirectoryPath;
        if (!Directory.Exists(outputDirectoryPath))
            throw new DirectoryNotFoundException($"'{outputDirectoryPath}' is not exists.");

        var targetEncoding = new UTF8Encoding(false);

        var outputFilePath = Path.Combine(outputDirectoryPath, $"{linqPadScriptFileName}");
        await File.WriteAllTextAsync(outputFilePath, $$"""
			<Query Kind="Statements">
				<Reference Relative="{{assemblyName}}.dll">{{assemblyName}}.dll</Reference>
				<IncludeUncapsulator>false</IncludeUncapsulator>
			</Query>

			internal static class Program
			{
				[STAThread]
				private static void Main()
				{
					/* Test Your Code Here */
				}
			}
			""", targetEncoding, cancellationToken).ConfigureAwait(false);
    }

    private static FrameworkType GetFrameworkType()
    {
        var fxDescription = RuntimeInformation.FrameworkDescription;
        if (string.IsNullOrWhiteSpace(fxDescription))
            return default;
        if (fxDescription.StartsWith(".NET Native", StringComparison.OrdinalIgnoreCase))
            return FrameworkType.Native;
        if (fxDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase))
            return FrameworkType.Framework;
        if (fxDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase))
            return FrameworkType.Core;
        if (fxDescription.StartsWith(".NET", StringComparison.OrdinalIgnoreCase))
            return FrameworkType.Modern;
        return default;
    }

    private static Version GetRuntimeVersion()
        => Environment.Version;

    private static Uri GetAppRefPackageUri(Version? runtimeVersion = default)
    {
        if (runtimeVersion == null)
            runtimeVersion = GetRuntimeVersion();

        return new Uri(
            $"https://www.nuget.org/api/v2/package/Microsoft.NETCore.App.Ref/{runtimeVersion.ToString()}",
            UriKind.Absolute);
    }

    private static async Task<IReadOnlyList<MetadataReference>> PrepareAppReferencesFromOnlineAsync(CancellationToken cancellationToken = default)
    {
        var frameworkType = GetFrameworkType();
        if (frameworkType != FrameworkType.Modern &&
            frameworkType != FrameworkType.Core)
            throw new PlatformNotSupportedException("Current runtime is not compatible with this library.");

        var appDataDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AdmxGen");
        if (!Directory.Exists(appDataDirectoryPath))
            Directory.CreateDirectory(appDataDirectoryPath);

        var appRefPackageFileInfo = new FileInfo(Path.Combine(
            appDataDirectoryPath, $"AppRef_{GetRuntimeVersion()}_Package.zip"));

        if (!appRefPackageFileInfo.Exists || appRefPackageFileInfo.Length < 1L)
        {
            var appRefPackageUri = GetAppRefPackageUri();
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(appRefPackageUri);
            using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var localStream = appRefPackageFileInfo.OpenWrite();
            await responseStream.CopyToAsync(localStream, cancellationToken).ConfigureAwait(false);
        }

        using var zipFile = ZipFile.OpenRead(appRefPackageFileInfo.FullName);
        var referenceMetadatas = new List<MetadataReference>();

        foreach (var referenceFiles in zipFile.Entries.Where(x => x.FullName.StartsWith("ref/", StringComparison.Ordinal) && x.Name.EndsWith(".dll", StringComparison.Ordinal)))
        {
            using var entryFileStream = referenceFiles.Open();
            using var memStream = new MemoryStream();
            await entryFileStream.CopyToAsync(memStream, cancellationToken).ConfigureAwait(false);
            var metadata = MetadataReference.CreateFromImage(memStream.ToArray());
            referenceMetadatas.Add(metadata);
        }

        return referenceMetadatas.AsReadOnly();
    }
}
