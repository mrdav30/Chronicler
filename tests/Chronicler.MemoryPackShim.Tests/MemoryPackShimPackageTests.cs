using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Xunit;

namespace Chronicler.MemoryPackShim.Tests;

public sealed class MemoryPackShimPackageTests
{
    private const string PackageId = "Chronicler.MemoryPackShim";
    private const string MemoryPackVersion = "1.21.4";

    [Fact]
    public void LeanConsumer_CompilesAnnotatedPublicTypesWithoutMemoryPackPackage()
    {
        using TestWorkspace workspace = TestWorkspace.Create();
        string version = PackShim(workspace);
        string projectDirectory = workspace.CreateDirectory("lean-consumer");

        WriteNuGetConfig(projectDirectory, workspace.PackageSource);
        WriteConsumerProject(
            projectDirectory,
            version,
            disableMemoryPack: true,
            includeRealMemoryPack: false,
            disableShim: false,
            treatWarningsAsErrors: true);
        WriteAnnotatedType(projectDirectory);

        DotNetResult result = RunDotNet("build --configuration Release --nologo", projectDirectory);

        AssertBuildSucceeded(result);
        string assets = ReadAssetsFile(projectDirectory);
        Assert.DoesNotContain("\"MemoryPack/", assets);
        Assert.DoesNotContain("\"MemoryPack.Core/", assets);

        Assembly assembly = LoadConsumerAssembly(projectDirectory);
        Type? shimType = assembly.GetType("MemoryPack.MemoryPackableAttribute", throwOnError: false);
        Assert.NotNull(shimType);
        Assert.False(shimType!.IsPublic);
    }

    [Fact]
    public void StandardConsumer_UsesRealMemoryPackAndDoesNotCompileShimSource()
    {
        using TestWorkspace workspace = TestWorkspace.Create();
        string version = PackShim(workspace);
        string projectDirectory = workspace.CreateDirectory("standard-consumer");

        WriteNuGetConfig(projectDirectory, workspace.PackageSource);
        WriteConsumerProject(
            projectDirectory,
            version,
            disableMemoryPack: false,
            includeRealMemoryPack: true,
            disableShim: false,
            treatWarningsAsErrors: true);
        WriteAnnotatedType(projectDirectory);

        DotNetResult result = RunDotNet("build --configuration Release --nologo", projectDirectory);

        AssertBuildSucceeded(result);
        Assert.Contains("MemoryPack.Core", ReadAssetsFile(projectDirectory));

        Assembly assembly = LoadConsumerAssembly(projectDirectory);
        Assert.Null(assembly.GetType("MemoryPack.MemoryPackableAttribute", throwOnError: false));
    }

    [Fact]
    public void DisabledShim_DoesNotInjectSource()
    {
        using TestWorkspace workspace = TestWorkspace.Create();
        string version = PackShim(workspace);
        string projectDirectory = workspace.CreateDirectory("disabled-consumer");

        WriteNuGetConfig(projectDirectory, workspace.PackageSource);
        WriteConsumerProject(
            projectDirectory,
            version,
            disableMemoryPack: true,
            includeRealMemoryPack: false,
            disableShim: true,
            treatWarningsAsErrors: false);
        WriteAnnotatedType(projectDirectory);

        DotNetResult result = RunDotNet("build --configuration Release --nologo", projectDirectory);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("MemoryPack", result.CombinedOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Package_ContainsOnlyBuildAndSourceAssets()
    {
        using TestWorkspace workspace = TestWorkspace.Create();
        string version = PackShim(workspace);
        string packagePath = Path.Combine(workspace.PackageSource, $"{PackageId}.{version}.nupkg");

        using var archive = ZipFile.OpenRead(packagePath);
        Assert.Contains(archive.Entries, entry => entry.FullName == "buildTransitive/Chronicler.MemoryPackShim.targets");
        Assert.Contains(archive.Entries, entry => entry.FullName == "contentFiles/cs/any/MemoryPack.Disable.Shim.cs");
        Assert.DoesNotContain(archive.Entries, entry => entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
    }

    private static string PackShim(TestWorkspace workspace)
    {
        string version = "99.0.0-test" + Guid.NewGuid().ToString("N").Substring(0, 8);
        string projectPath = Path.Combine(RepositoryRoot, "src", "Chronicler.MemoryPackShim", "Chronicler.MemoryPackShim.csproj");
        DotNetResult result = RunDotNet(
            $"pack \"{projectPath}\" --configuration Release --output \"{workspace.PackageSource}\" --nologo /p:PackageVersion={version}",
            RepositoryRoot);

        AssertBuildSucceeded(result);
        Assert.True(
            File.Exists(Path.Combine(workspace.PackageSource, $"{PackageId}.{version}.nupkg")),
            result.CombinedOutput);
        return version;
    }

    private static void WriteNuGetConfig(string projectDirectory, string packageSource)
    {
        string content = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="local" value="{packageSource}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """;

        File.WriteAllText(Path.Combine(projectDirectory, "NuGet.config"), content);
    }

    private static void WriteConsumerProject(
        string projectDirectory,
        string packageVersion,
        bool disableMemoryPack,
        bool includeRealMemoryPack,
        bool disableShim,
        bool treatWarningsAsErrors)
    {
        string memoryPackReference = includeRealMemoryPack
            ? $"""<PackageReference Include="MemoryPack" Version="{MemoryPackVersion}" />"""
            : string.Empty;
        string disableShimProperty = disableShim
            ? "<ChroniclerMemoryPackShimEnabled>false</ChroniclerMemoryPackShimEnabled>"
            : string.Empty;

        string content = $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <LangVersion>11.0</LangVersion>
                <ImplicitUsings>disable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <DisableMemoryPack>{{disableMemoryPack.ToString().ToLowerInvariant()}}</DisableMemoryPack>
                <TreatWarningsAsErrors>{{treatWarningsAsErrors.ToString().ToLowerInvariant()}}</TreatWarningsAsErrors>
                {{disableShimProperty}}
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="{{PackageId}}" Version="{{packageVersion}}" PrivateAssets="all" />
                {{memoryPackReference}}
              </ItemGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(projectDirectory, "Consumer.csproj"), content);
    }

    private static void WriteAnnotatedType(string projectDirectory)
    {
        const string content = """
            using MemoryPack;

            [MemoryPackable]
            public partial class PublicAnnotatedLeanType
            {
                [MemoryPackInclude]
                [MemoryPackOrder(0)]
                [MemoryPackAllowSerialize]
                public int Value;

                [MemoryPackIgnore]
                public int RuntimeOnly => Value + 1;

                public PublicAnnotatedLeanType()
                    : this(0)
                {
                }

                [MemoryPackConstructor]
                public PublicAnnotatedLeanType(int value)
                {
                    Value = value;
                }
            }
            """;

        File.WriteAllText(Path.Combine(projectDirectory, "PublicAnnotatedLeanType.cs"), content);
    }

    private static Assembly LoadConsumerAssembly(string projectDirectory)
    {
        string assemblyPath = Path.Combine(
            projectDirectory,
            "bin",
            "Release",
            "net8.0",
            "Consumer.dll");
        return Assembly.LoadFile(assemblyPath);
    }

    private static string ReadAssetsFile(string projectDirectory)
    {
        return File.ReadAllText(Path.Combine(projectDirectory, "obj", "project.assets.json"));
    }

    private static void AssertBuildSucceeded(DotNetResult result)
    {
        Assert.True(result.ExitCode == 0, result.CombinedOutput);
    }

    private static DotNetResult RunDotNet(string arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start dotnet process.");

        var output = new StringBuilder();
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data != null)
                output.AppendLine(args.Data);
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data != null)
                output.AppendLine(args.Data);
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(milliseconds: 120000))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            throw new TimeoutException($"dotnet {arguments} timed out in {workingDirectory}.");
        }

        process.WaitForExit();
        return new DotNetResult(process.ExitCode, output.ToString());
    }

    private static string RepositoryRoot
    {
        get
        {
            string directory = AppContext.BaseDirectory;
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory, "Chronicler.slnx")))
                    return directory;

                DirectoryInfo? parent = Directory.GetParent(directory);
                directory = parent?.FullName!;
            }

            throw new InvalidOperationException("Unable to locate Chronicler repository root.");
        }
    }

    private readonly struct DotNetResult
    {
        public DotNetResult(int exitCode, string combinedOutput)
        {
            ExitCode = exitCode;
            CombinedOutput = combinedOutput;
        }

        public int ExitCode { get; }

        public string CombinedOutput { get; }
    }

    private sealed class TestWorkspace : IDisposable
    {
        private readonly string _root;

        private TestWorkspace(string root)
        {
            _root = root;
            PackageSource = Path.Combine(root, "packages");
            Directory.CreateDirectory(PackageSource);
        }

        public string PackageSource { get; }

        public static TestWorkspace Create()
        {
            string root = Path.Combine(Path.GetTempPath(), "ChroniclerMemoryPackShimTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TestWorkspace(root);
        }

        public string CreateDirectory(string name)
        {
            string path = Path.Combine(_root, name);
            Directory.CreateDirectory(path);
            return path;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_root, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
