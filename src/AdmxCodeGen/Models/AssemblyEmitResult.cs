namespace AdmxCodeGen.Models;

internal sealed class AssemblyEmitResult
{
    public bool BuildSucceed { get; internal set; }
    public IReadOnlyList<string> Diagnostics { get; internal set; } = Array.Empty<string>();
    public string AssemblyName { get; internal set; } = string.Empty;
    public string OutputDirectoryPath { get; internal set; } = string.Empty;
    public string BuildOutputPath { get; internal set; } = string.Empty;
    public string DebugSymbolFilePath { get; internal set; } = string.Empty;
    public string XmlDocumentFilePath { get; internal set; } = string.Empty;
}
