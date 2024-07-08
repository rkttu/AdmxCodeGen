using AdmxParser;

namespace AdmxCodeGen.Test;

public class CodeGenTestBasics
{
    [Fact]
    public async Task CodeGenTest()
    {
        var admxDirectory = new AdmxDirectory(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "PolicyDefinitions"));
        await admxDirectory.LoadAsync().ConfigureAwait(false);

        var outputDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "AdmxgenResult");
        var emitResult = await admxDirectory.EmitCompiledAssemblyAsync(
            "TestProejct",
            outputDirectoryPath)
            .ConfigureAwait(false);

        Assert.NotNull(emitResult);
        Assert.True(emitResult.BuildSucceed);
        Assert.Empty(emitResult.Diagnostics);

        var fi = new FileInfo(emitResult.BuildOutputPath);
        Assert.True(fi.Exists);
        Assert.True(fi.Length > 0L);

        fi = new FileInfo(emitResult.DebugSymbolFilePath);
        Assert.True(fi.Exists);
        Assert.True(fi.Length > 0L);

        fi = new FileInfo(emitResult.XmlDocumentFilePath);
        Assert.True(fi.Exists);
        Assert.True(fi.Length > 0L);
    }
}
