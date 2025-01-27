using CliWrap;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace FluentAssertionsMigrator.Tests;

public sealed class FluentAssertionsSolutionMigratorTests
{
    private readonly FluentAssertionsSolutionMigrator _solutionMigrator;
    private readonly FileInfo _solutionFile;
    private readonly ILogger<FluentAssertionsSolutionMigratorTests> _testLogger;

    public FluentAssertionsSolutionMigratorTests(ITestOutputHelper output)
    {
        _testLogger = output.ToLogger<FluentAssertionsSolutionMigratorTests>();
        _solutionMigrator = new FluentAssertionsSolutionMigrator(
            output.ToLogger<FluentAssertionsSolutionMigrator>(),
            new FluentAssertionsDocumentMigrator(
                output.ToLogger<FluentAssertionsDocumentMigrator>(),
                new FluentAssertionsSyntaxRewriterFactory(output.ToLoggerFactory())
            )
        );
        
        var originalTestSolutionFile = new FileInfo(Path.GetFullPath("../../../../testsolution/TestSolution.sln"));
        if (!originalTestSolutionFile.Exists)
        {
            throw new InvalidOperationException($"Solution file {originalTestSolutionFile} does not exist");
        }
        
        // Copy test solution to a temporary folder to support repeated test runs
        var newTestSolutionFileDirectory = new DirectoryInfo(Path.GetFullPath("./testsolution"));
        if (newTestSolutionFileDirectory.Exists)
        {
            newTestSolutionFileDirectory.Delete(true);
        }
        newTestSolutionFileDirectory.Create();
        CopyDirectory(originalTestSolutionFile.Directory!, newTestSolutionFileDirectory);
        
        _solutionFile = new FileInfo(Path.Combine(newTestSolutionFileDirectory.FullName, "TestSolution.sln"));
    }
    
    [Fact]
    public async Task MigrateTestSolution()
    {
        // Act
        await _solutionMigrator.MigrateAsync(_solutionFile);

        // Assert
        await Cli.Wrap("dotnet")
            .WithArguments("test")
            .WithWorkingDirectory(_solutionFile.Directory!.FullName)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(message => _testLogger.LogInformation("[dotnet test]: {Message}", message)))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(message => _testLogger.LogError("[dotnet test]: {Message}", message)))
            .ExecuteAsync();
    }

    // Helper method for recursive directory copying
    private static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
    {
        destination.Create();
        
        foreach (var file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(destination.FullName, file.Name), true);
        }

        foreach (var dir in source.GetDirectories())
        {
            CopyDirectory(dir, new DirectoryInfo(Path.Combine(destination.FullName, dir.Name)));
        }
    }
}
