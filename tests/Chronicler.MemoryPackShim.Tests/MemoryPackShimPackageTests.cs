extern alias Shim;

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;
using ShimMemoryPack = Shim::MemoryPack;

namespace Chronicler.MemoryPackShim.Tests;

public sealed class MemoryPackShimPackageTests
{
    private const string PackageId = "Chronicler.MemoryPackShim";
    private static readonly string[] CompatibilityTypeNames =
    {
        "MemoryPack.GenerateType",
        "MemoryPack.MemoryPackableAttribute",
        "MemoryPack.MemoryPackAllowSerializeAttribute",
        "MemoryPack.MemoryPackConstructorAttribute",
        "MemoryPack.MemoryPackIgnoreAttribute",
        "MemoryPack.MemoryPackIncludeAttribute",
        "MemoryPack.MemoryPackOrderAttribute",
        "MemoryPack.SerializeLayout"
    };

    [Fact]
    public void PublicContract_MatchesMemoryPackCoreAttributes()
    {
        Assembly shimAssembly = LoadShimAssembly();
        Assembly memoryPackAssembly = typeof(MemoryPack.MemoryPackableAttribute).Assembly;

        Assert.Equal(
            CompatibilityTypeNames,
            shimAssembly.ExportedTypes.Select(type => type.FullName).OrderBy(name => name).ToArray());

        foreach (string typeName in CompatibilityTypeNames)
        {
            Type expected = memoryPackAssembly.GetType(typeName, throwOnError: true)!;
            Type actual = shimAssembly.GetType(typeName, throwOnError: true)!;

            Assert.Equal(expected.IsEnum, actual.IsEnum);
            Assert.Equal(expected.IsSealed, actual.IsSealed);
            Assert.Equal(GetConstructorSignatures(expected), GetConstructorSignatures(actual));
            Assert.Equal(GetPropertySignatures(expected), GetPropertySignatures(actual));

            if (expected.IsEnum)
            {
                Assert.Equal(Enum.GetNames(expected), Enum.GetNames(actual));
                Assert.Equal(
                    Enum.GetValues(expected).Cast<object>().Select(Convert.ToInt32),
                    Enum.GetValues(actual).Cast<object>().Select(Convert.ToInt32));
                continue;
            }

            AttributeUsageAttribute expectedUsage = expected.GetCustomAttribute<AttributeUsageAttribute>()!;
            AttributeUsageAttribute actualUsage = actual.GetCustomAttribute<AttributeUsageAttribute>()!;
            Assert.Equal(expectedUsage.ValidOn, actualUsage.ValidOn);
            Assert.Equal(expectedUsage.AllowMultiple, actualUsage.AllowMultiple);
            Assert.Equal(expectedUsage.Inherited, actualUsage.Inherited);
        }
    }

    [Theory]
    [InlineData(ShimMemoryPack.GenerateType.Object, ShimMemoryPack.SerializeLayout.Sequential)]
    [InlineData(ShimMemoryPack.GenerateType.VersionTolerant, ShimMemoryPack.SerializeLayout.Explicit)]
    [InlineData(ShimMemoryPack.GenerateType.CircularReference, ShimMemoryPack.SerializeLayout.Explicit)]
    [InlineData(ShimMemoryPack.GenerateType.Collection, ShimMemoryPack.SerializeLayout.Sequential)]
    [InlineData(ShimMemoryPack.GenerateType.NoGenerate, ShimMemoryPack.SerializeLayout.Sequential)]
    public void MemoryPackable_GenerationModeSelectsCompatibleDefaultLayout(
        ShimMemoryPack.GenerateType generateType,
        ShimMemoryPack.SerializeLayout expectedLayout)
    {
        var actual = new ShimMemoryPack.MemoryPackableAttribute(generateType);
        var expected = new MemoryPack.MemoryPackableAttribute((MemoryPack.GenerateType)generateType);

        Assert.Equal(generateType, actual.GenerateType);
        Assert.Equal(expectedLayout, actual.SerializeLayout);
        Assert.Equal((int)expected.GenerateType, (int)actual.GenerateType);
        Assert.Equal((int)expected.SerializeLayout, (int)actual.SerializeLayout);
    }

    [Theory]
    [InlineData(ShimMemoryPack.SerializeLayout.Sequential)]
    [InlineData(ShimMemoryPack.SerializeLayout.Explicit)]
    public void MemoryPackable_LayoutOnlyUsesObjectGeneration(ShimMemoryPack.SerializeLayout layout)
    {
        var actual = new ShimMemoryPack.MemoryPackableAttribute(layout);
        var expected = new MemoryPack.MemoryPackableAttribute((MemoryPack.SerializeLayout)layout);

        Assert.Equal(ShimMemoryPack.GenerateType.Object, actual.GenerateType);
        Assert.Equal(layout, actual.SerializeLayout);
        Assert.Equal((int)expected.GenerateType, (int)actual.GenerateType);
        Assert.Equal((int)expected.SerializeLayout, (int)actual.SerializeLayout);
    }

    [Theory]
    [InlineData(ShimMemoryPack.GenerateType.Object, ShimMemoryPack.SerializeLayout.Explicit)]
    [InlineData(ShimMemoryPack.GenerateType.VersionTolerant, ShimMemoryPack.SerializeLayout.Sequential)]
    public void MemoryPackable_ExplicitLayoutOverridesGenerationDefault(
        ShimMemoryPack.GenerateType generateType,
        ShimMemoryPack.SerializeLayout layout)
    {
        var actual = new ShimMemoryPack.MemoryPackableAttribute(generateType, layout);
        var expected = new MemoryPack.MemoryPackableAttribute(
            (MemoryPack.GenerateType)generateType,
            (MemoryPack.SerializeLayout)layout);

        Assert.Equal(generateType, actual.GenerateType);
        Assert.Equal(layout, actual.SerializeLayout);
        Assert.Equal((int)expected.GenerateType, (int)actual.GenerateType);
        Assert.Equal((int)expected.SerializeLayout, (int)actual.SerializeLayout);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(0)]
    [InlineData(17)]
    [InlineData(int.MaxValue)]
    public void MemoryPackOrder_PreservesDeclaredOrder(int order)
    {
        var actual = new ShimMemoryPack.MemoryPackOrderAttribute(order);
        var expected = new MemoryPack.MemoryPackOrderAttribute(order);

        Assert.Equal(order, actual.Order);
        Assert.Equal(expected.Order, actual.Order);
    }

    [Fact]
    public void LeanConsumer_CompilesAnnotatedPublicTypesWithoutMemoryPackPackage()
    {
        using TestWorkspace workspace = TestWorkspace.Create();
        string version = PackShim(workspace);
        string projectDirectory = workspace.CreateDirectory("lean-consumer");

        WriteNuGetConfig(projectDirectory, workspace.PackageSource);
        WriteConsumerProject(projectDirectory, version);
        WriteAnnotatedType(projectDirectory);

        DotNetResult result = RunDotNet("build --configuration Release --nologo", projectDirectory);

        AssertBuildSucceeded(result);
        string assets = ReadAssetsFile(projectDirectory);
        Assert.DoesNotContain("\"MemoryPack/", assets);
        Assert.DoesNotContain("\"MemoryPack.Core/", assets);
        Assert.Contains($"\"{PackageId.ToLowerInvariant()}/{version}\"", assets);

        Assembly assembly = LoadConsumerAssembly(projectDirectory);
        Assert.Null(assembly.GetType("MemoryPack.MemoryPackableAttribute", throwOnError: false));

        Assembly shimAssembly = LoadShimAssembly();
        Type? shimType = shimAssembly.GetType("MemoryPack.MemoryPackableAttribute", throwOnError: false);
        Assert.NotNull(shimType);
        Assert.True(shimType!.IsPublic);
    }

    [Fact]
    public void FriendAssemblyConsumer_UsesOneSharedAttributeIdentity()
    {
        using TestWorkspace workspace = TestWorkspace.Create();
        string version = PackShim(workspace);
        string libraryDirectory = workspace.CreateDirectory("friend-library");
        string consumerDirectory = workspace.CreateDirectory("friend-consumer");

        WriteNuGetConfig(libraryDirectory, workspace.PackageSource);
        WriteFriendLibraryProject(libraryDirectory, version);
        WriteAnnotatedType(libraryDirectory);
        File.WriteAllText(
            Path.Combine(libraryDirectory, "AssemblyInfo.cs"),
            """
            using System.Runtime.CompilerServices;

            [assembly: InternalsVisibleTo("FriendConsumer")]
            """);

        DotNetResult libraryResult = RunDotNet(
            "build --configuration Release --nologo",
            libraryDirectory);
        AssertBuildSucceeded(libraryResult);

        WriteNuGetConfig(consumerDirectory, workspace.PackageSource);
        WriteFriendConsumerProject(
            consumerDirectory,
            libraryDirectory,
            version);
        WriteAnnotatedType(consumerDirectory);

        DotNetResult consumerResult = RunDotNet(
            "build --configuration Release --nologo",
            consumerDirectory);

        AssertBuildSucceeded(consumerResult);
    }

    [Fact]
    public void Package_ContainsOnlyCompiledCompatibilityAssets()
    {
        using TestWorkspace workspace = TestWorkspace.Create();
        string version = PackShim(workspace);
        string packagePath = Path.Combine(workspace.PackageSource, $"{PackageId}.{version}.nupkg");

        using var archive = ZipFile.OpenRead(packagePath);
        Assert.Contains(archive.Entries, entry =>
            entry.FullName == "lib/net8.0/Chronicler.MemoryPackShim.dll");
        Assert.Contains(archive.Entries, entry =>
            entry.FullName == "lib/netstandard2.1/Chronicler.MemoryPackShim.dll");
        Assert.DoesNotContain(archive.Entries, entry =>
            entry.FullName.StartsWith("build", StringComparison.OrdinalIgnoreCase)
            || entry.FullName.StartsWith("contentFiles", StringComparison.OrdinalIgnoreCase));
    }

    private static string PackShim(TestWorkspace workspace)
    {
        string version = "99.0.0-test" + Guid.NewGuid().ToString("N").Substring(0, 8);
        string projectPath = Path.Combine(RepositoryRoot, "src", "Chronicler.MemoryPackShim", "Chronicler.MemoryPackShim.csproj");
        DotNetResult result = RunDotNet(
            $"build \"{projectPath}\" --configuration Release --nologo /p:PackageVersion={version} /p:PackageOutputPath=\"{workspace.PackageSource}\"",
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
        string packageVersion)
    {
        string content = $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <LangVersion>11.0</LangVersion>
                <ImplicitUsings>disable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="{{PackageId}}" Version="{{packageVersion}}" />
              </ItemGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(projectDirectory, "Consumer.csproj"), content);
    }

    private static void WriteFriendLibraryProject(
        string projectDirectory,
        string packageVersion)
    {
        string content = $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <AssemblyName>FriendLibrary</AssemblyName>
                <LangVersion>11.0</LangVersion>
                <ImplicitUsings>disable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <DisableMemoryPack>true</DisableMemoryPack>
                <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="{{PackageId}}" Version="{{packageVersion}}" />
              </ItemGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(projectDirectory, "FriendLibrary.csproj"), content);
    }

    private static void WriteFriendConsumerProject(
        string projectDirectory,
        string libraryDirectory,
        string packageVersion)
    {
        string libraryProject = Path.Combine(
            libraryDirectory,
            "FriendLibrary.csproj");
        string content = $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <AssemblyName>FriendConsumer</AssemblyName>
                <LangVersion>11.0</LangVersion>
                <ImplicitUsings>disable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <DisableMemoryPack>true</DisableMemoryPack>
                <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="{{libraryProject}}" />
                <PackageReference Include="{{PackageId}}" Version="{{packageVersion}}" />
              </ItemGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(projectDirectory, "FriendConsumer.csproj"), content);
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

    private static Assembly LoadShimAssembly()
    {
        return typeof(ShimMemoryPack.MemoryPackableAttribute).Assembly;
    }

    private static string[] GetConstructorSignatures(Type type)
    {
        return type.GetConstructors()
            .Select(constructor => string.Join(
                ",",
                constructor.GetParameters().Select(parameter =>
                    $"{parameter.ParameterType.FullName}:{parameter.Name}:{parameter.IsOptional}:{parameter.DefaultValue}")))
            .OrderBy(signature => signature)
            .ToArray();
    }

    private static string[] GetPropertySignatures(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property =>
                $"{property.PropertyType.FullName}:{property.Name}:{property.CanRead}:{property.CanWrite}")
            .OrderBy(signature => signature)
            .ToArray();
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
