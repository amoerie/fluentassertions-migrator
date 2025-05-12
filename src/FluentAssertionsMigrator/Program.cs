using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

// Sources
// https://github.com/xunit/xunit/issues/3133
// https://www.meziantou.net/using-roslyn-to-analyze-and-rewrite-code-in-a-solution.htm
// https://stackoverflow.com/questions/31481251/applying-multiple-changes-to-a-solution-in-roslyn

if (args.Length == 0)
{
    Console.WriteLine("FluentAssertions to xUnit Migration Tool");
    Console.WriteLine("Usage: FluentAssertionsMigrator.exe <path-to-solution-file>");
    Console.WriteLine("Example: FluentAssertionsMigrator.exe C:\\Projects\\MySolution.sln");
    return 1;
}

var solutionFile = new FileInfo(args[0]);

if (!solutionFile.Exists)
{
    Console.WriteLine($"Error: Solution file not found at path: {solutionFile.FullName}");
    return 1;
}

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss";
    });
});

var solutionMigrator = new FluentAssertionsSolutionMigrator(
    loggerFactory.CreateLogger<FluentAssertionsSolutionMigrator>(),
    new FluentAssertionsDocumentMigrator(
        loggerFactory.CreateLogger<FluentAssertionsDocumentMigrator>(),
        new FluentAssertionsSyntaxRewriterFactory(loggerFactory)
    )
);

await solutionMigrator.MigrateAsync(solutionFile);

return 0;

public sealed class FluentAssertionsSolutionMigrator(
    ILogger<FluentAssertionsSolutionMigrator> logger,
    FluentAssertionsDocumentMigrator documentMigrator)
{
    public async Task MigrateAsync(FileInfo solutionFile)
    {
        if (!solutionFile.Exists)
        {
            throw new ArgumentException($"The specified solution file {solutionFile} does not exist.",
                nameof(solutionFile));
        }

        logger.LogInformation("Migrating solution: {SolutionFile}", solutionFile);

        // Find where the MSBuild assemblies are located on your system.
        // If you need a specific version, you can use MSBuildLocator.RegisterMSBuildPath.
        logger.LogDebug("Locating MSBuild");
        var instance = Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();
        logger.LogInformation("Located MSBuild at {MSBuildPath}", instance.MSBuildPath);

        // Note that you may need to restore the NuGet packages for the solution before opeing it with Roslyn.
        // Depending on what you want to do, dependencies may be required for a correct analysis.

        // Create a Roslyn workspace and load the solution
        logger.LogDebug("Creating Roslyn workspace and opening solution (this may take a few moments)");
        var start = Stopwatch.GetTimestamp();
        var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionFile.FullName);
        var elapsed = Stopwatch.GetElapsedTime(start);
        logger.LogInformation("Opened solution {Solution} in {Elapsed}s, found {NumberOfProjects} projects",
            solution.FilePath, elapsed.TotalSeconds, solution.ProjectIds.Count);

        var solutionProjectIds = solution.ProjectIds;
        foreach (var projectId in solutionProjectIds)
        {
            var project = solution.GetProject(projectId);
            if (project is null)
            {
                continue;
            }

            var projectDocumentIds = project.DocumentIds;

            foreach (var documentId in projectDocumentIds)
            {
                var document = project.GetDocument(documentId);
                if (document is null)
                {
                    continue;
                }

                var migratedDocument = await documentMigrator.MigrateAsync(document);

                //Persist your changes to the current project
                project = migratedDocument.Project;
            }

            //Persist the project changes to the current solution
            solution = project.Solution;
        }

        //Finally, apply all your changes to the workspace at once.
        if (workspace.TryApplyChanges(solution))
        {
            logger.LogInformation("Successfully migrated solution: {SolutionFile}", solutionFile);
        }
        else
        {
            logger.LogError("Failed to apply changes to solution: {SolutionFile}", solutionFile);
        }
    }
}

public sealed class FluentAssertionsDocumentMigrator(
    ILogger<FluentAssertionsDocumentMigrator> logger,
    FluentAssertionsSyntaxRewriterFactory syntaxRewriterFactory)
{
    public async Task<Document> MigrateAsync(Document document)
    {
        logger.LogTrace(">> Migrating: {Document}", document.FilePath);

        var originalRoot = await document.GetSyntaxRootAsync();

        // Remove FluentAssertions using directive
        var rootWithoutUsingFluentAssertions = originalRoot?.RemoveNodes(
            originalRoot.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Where(u => u.Name?.ToString().StartsWith("FluentAssertions") == true),
            SyntaxRemoveOptions.KeepNoTrivia);

        var lazySemanticModel = new Lazy<Task<SemanticModel?>>(async () => await document.GetSemanticModelAsync());
        var syntaxRewriter = syntaxRewriterFactory.Create(lazySemanticModel);
        var migratedRoot = syntaxRewriter.Visit(rootWithoutUsingFluentAssertions);

        if (migratedRoot is null || migratedRoot.IsEquivalentTo(originalRoot))
        {
            logger.LogTrace(">> No changes: {Document}", document.FilePath);
            return document;
        }

        logger.LogDebug(">> Migrated: {Document}", document.FilePath);
        return document.WithSyntaxRoot(migratedRoot);
    }
}

public sealed class FluentAssertionsSyntaxRewriterFactory(ILoggerFactory loggerFactory)
{
    public FluentAssertionsSyntaxRewriter Create(Lazy<Task<SemanticModel?>> semanticModel)
    {
        return new FluentAssertionsSyntaxRewriter(
            loggerFactory.CreateLogger<FluentAssertionsSyntaxRewriter>(),
            semanticModel);
    }
}

public sealed partial class FluentAssertionsSyntaxRewriter(
    ILogger<FluentAssertionsSyntaxRewriter> logger,
    Lazy<Task<SemanticModel?>> lazySemanticModel)
    : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitAwaitExpression(AwaitExpressionSyntax node)
    {
        if (TryResolveActualValueFromAwaitExpression(node, out var invocation, out var actualValueExpression))
        {
            return HandleAssertionExpression(node, invocation, actualValueExpression);
        }

        return base.VisitAwaitExpression(node);
    }

    public override SyntaxNode? VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
    {
        if (TryResolveActualValueFromConditionalAccessExpression(node, out var invocation,
                out var actualValueExpression))
        {
            return HandleAssertionExpression(node, invocation, actualValueExpression);
        }

        return base.VisitConditionalAccessExpression(node);
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (TryResolveActualValueFromShouldInvocationExpression(node, out var actualValueExpression))
        {
            return HandleAssertionExpression(node, node, actualValueExpression);
        }

        return base.VisitInvocationExpression(node);
    }

    private SyntaxNode? HandleAssertionExpression(ExpressionSyntax node,
        InvocationExpressionSyntax shouldInvocationExpression, ExpressionSyntax actualValueExpression)
    {
        var shouldInvocationExpressionAsString = shouldInvocationExpression.Expression.ToString();

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BePositive"))
        {
            logger.LogTrace("Rewriting .Should().BePositive() in {Node}", node);
            return CreateAssertExpression($"Assert.True({actualValueExpression} > 0)", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeNegative"))
        {
            logger.LogTrace("Rewriting .Should().BeNegative() in {Node}", node);
            return CreateAssertExpression($"Assert.True({actualValueExpression} < 0)", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().Be"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().Be() in {Node}", node);
            if (expectedValue.ToString() == "1" && actualValueExpression.ToString().EndsWith(".Count"))
            {
                var valueNode = actualValueExpression.ChildNodes().FirstOrDefault();
                return CreateAssertExpression($"Assert.Single({valueNode})", node);
            }
            return CreateAssertExpression($"Assert.Equal({expectedValue}, {actualValueExpression})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().NotBe"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().NotBe() in {Node}", node);
            return CreateAssertExpression($"Assert.NotEqual({expectedValue}, {actualValueExpression})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeSameAs"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().BeSameAs() in {Node}", node);
            return CreateAssertExpression($"Assert.Same({expectedValue}, {actualValueExpression})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().NotBeSameAs"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().NotBeSameAs() in {Node}", node);
            return CreateAssertExpression($"Assert.NotSame({expectedValue}, {actualValueExpression})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeTrue"))
        {
            logger.LogTrace("Rewriting .Should().BeTrue() in {Node}", node);
            return CreateAssertExpression($"Assert.True({actualValueExpression})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeFalse"))
        {
            logger.LogTrace("Rewriting .Should().BeFalse() in {Node}", node);
            return CreateAssertExpression($"Assert.False({actualValueExpression})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeNull"))
        {
            logger.LogTrace("Rewriting .Should().BeNull() in {Node}", node);
            return CreateAssertExpression($"Assert.Null({actualValueExpression})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().NotBeNull"))
        {
            logger.LogTrace("Rewriting .Should().NotBeNull() in {Node}", node);
            return CreateAssertExpression($"Assert.NotNull({actualValueExpression})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeEmpty"))
        {
            logger.LogTrace("Rewriting .Should().BeEmpty() in {Node}", node);
            if (IsNullable(actualValueExpression) == false)
            {
                return CreateAssertExpression($"Assert.Empty({actualValueExpression})", node);
            }
            // xUnit's Assert.Empty does not handle null well, so add a fallback for that
            return CreateAssertExpression($"Assert.Empty({actualValueExpression} ?? [])", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().NotBeEmpty"))
        {
            logger.LogTrace("Rewriting .Should().NotBeEmpty() in {Node}", node);
            var assertCode = $"Assert.NotEmpty({actualValueExpression})";
            return CreateAssertExpression(assertCode, node);
        }

        if (ThrowRegex().IsMatch(shouldInvocationExpressionAsString))
        {
            var exceptionType = GetGenericTypeArgument(shouldInvocationExpression) ?? GetTypeOfArgument(shouldInvocationExpression);
            logger.LogTrace("Rewriting .Should().Throw() in {Node}", node);
            if (exceptionType is not null)
            {
                return CreateAssertExpression($"Assert.Throws<{exceptionType}>({actualValueExpression})", node);
            }
            return CreateAssertExpression($"Assert.Throws({actualValueExpression})", node);
        }

        if (NotThrowRegex().IsMatch(shouldInvocationExpressionAsString))
        {
            var exceptionType = GetGenericTypeArgument(shouldInvocationExpression) ?? GetTypeOfArgument(shouldInvocationExpression);
            logger.LogTrace("Rewriting .Should().NotThrow() in {Node}", node);
            if (exceptionType is not null)
            {
                return CreateAssertExpression($"Assert.IsNotType<{exceptionType}>(Record.Exception({actualValueExpression}))", node);
            }

            return CreateAssertExpression($"Assert.Null(Record.Exception({actualValueExpression}))", node);
        }

        if (ThrowAsyncRegex().IsMatch(shouldInvocationExpressionAsString))
        {
            logger.LogTrace("Rewriting .Should().ThrowAsync() in {Node}", node);

            if (GetGenericTypeArgument(shouldInvocationExpression) is { } genericType)
            {
                return CreateAssertExpression($"await Assert.ThrowsAsync<{genericType}>({actualValueExpression})", node);
            }
            if (GetTypeOfArgument(shouldInvocationExpression) is { } typeofArgument)
            {
                return CreateAssertExpression($"await Assert.ThrowsAsync({typeofArgument}, {actualValueExpression})", node);
            }

            return CreateAssertExpression($"await Assert.ThrowsAsync({actualValueExpression})", node);
        }

        if (NotThrowAsyncRegex().IsMatch(shouldInvocationExpressionAsString))
        {
            logger.LogTrace("Rewriting .Should().NotThrowAsync() in {Node}", node);

            if (GetGenericTypeArgument(shouldInvocationExpression) is { } genericType)
            {
                return CreateAssertExpression($"Assert.IsNotType<{genericType}>(await Record.ExceptionAsync({actualValueExpression}))", node);
            }

            if (GetTypeOfArgument(shouldInvocationExpression) is { } typeofArgument)
            {
                return CreateAssertExpression($"Assert.IsNotType({typeofArgument}, await Record.ExceptionAsync({actualValueExpression}))", node);
            }

            return CreateAssertExpression($"Assert.Null(await Record.ExceptionAsync({actualValueExpression}))", node);
        }

        if (BeOfTypeRegex().IsMatch(shouldInvocationExpressionAsString))
        {
            logger.LogTrace("Rewriting .Should().BeOfType() in {Node}", node);
            var typeofArgument = GetGenericTypeArgument(shouldInvocationExpression) ?? GetTypeOfArgument(shouldInvocationExpression);
            return CreateAssertExpression($"Assert.IsType<{typeofArgument}>({actualValueExpression})", node);
        }

        if (NotBeOfTypeRegex().IsMatch(shouldInvocationExpressionAsString))
        {
            logger.LogTrace("Rewriting .Should().NotBeOfType() in {Node}", node);
            var typeofArgument = GetGenericTypeArgument(shouldInvocationExpression) ?? GetTypeOfArgument(shouldInvocationExpression);
            return CreateAssertExpression($"Assert.IsNotType<{typeofArgument}>({actualValueExpression})", node);
        }

        if (BeAssignableToRegex().IsMatch(shouldInvocationExpressionAsString))
        {
            logger.LogTrace("Rewriting .Should().BeAssignableTo() in {Node}", node);
            var typeofArgument = GetGenericTypeArgument(shouldInvocationExpression) ?? GetTypeOfArgument(shouldInvocationExpression);
            return CreateAssertExpression($"Assert.IsType<{typeofArgument}>({actualValueExpression})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeEquivalentTo"))
        {
            ExpressionSyntax expectedValue =
                shouldInvocationExpression.ArgumentList.Arguments.FirstOrDefault()?.Expression
                ?? SyntaxFactory.IdentifierName("<missing>");
            logger.LogTrace("Rewriting .Should().BeEquivalentTo() in {Node}", node);
            return CreateAssertExpression($"Assert.Equivalent({expectedValue}, {actualValueExpression})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().Contain"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().Contain() in {Node}", node);

            // xUnit API:
            // Assert.Contains(someElement, someCollection)
            // OR
            // Assert.Contains(someCollection, x => somePredicate)
            // In FluentAssertions this uses the same Should().Contain(..) method
            // so we need to resolve the type of the expected value to know which xUnit overload we need
            if (IsLambdaExpression(expectedValue) == true)
            {
                return CreateAssertExpression($"Assert.Contains({actualValueExpression}, {expectedValue})", node);
            }

            return CreateAssertExpression($"Assert.Contains({expectedValue}, {actualValueExpression})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().NotContain"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().NotContain() in {Node}", node);

            // xUnit API:
            // Assert.DoesNotContain(someElement, someCollection)
            // OR
            // Assert.DoesNotContain(someCollection, x => somePredicate)
            // In FluentAssertions this uses the same Should().NotContain(..) method
            // so we need to resolve the type of the expected value to know which xUnit overload we need
            if (IsLambdaExpression(expectedValue) == true)
            {
                return CreateAssertExpression($"Assert.DoesNotContain({actualValueExpression}, {expectedValue})", node);
            }

            return CreateAssertExpression($"Assert.DoesNotContain({expectedValue}, {actualValueExpression})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().HaveCount"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().HaveCount() in {Node}", node);

            // Special case, checking if count is 0
            if (expectedValue.ToString() == "0")
            {
                if (IsNullable(actualValueExpression) == false)
                {
                    return CreateAssertExpression($"Assert.Empty({actualValueExpression})", node);
                }
                // xUnit's Assert.Empty does not handle null well, so add a fallback for that
                return CreateAssertExpression($"Assert.Empty({actualValueExpression} ?? [])", node);
            }

            // Special case, checking if count is 1
            if (expectedValue.ToString() == "1")
            {
                if (IsNullable(actualValueExpression) == false)
                {
                    return CreateAssertExpression($"Assert.Single({actualValueExpression})", node);
                }
                // xUnit's Assert.Single does not handle null well, so add a fallback for that
                return CreateAssertExpression($"Assert.Single({actualValueExpression} ?? [])", node);
            }

            if (IsArray(actualValueExpression) == true)
            {
                if (IsNullable(actualValueExpression) == false)
                {
                    return CreateAssertExpression($"Assert.Equal({expectedValue}, ({actualValueExpression}).Length)", node);
                }
                return CreateAssertExpression($"Assert.Equal({expectedValue}, ({actualValueExpression})?.Length)", node);
            }
            if (IsCollection(actualValueExpression) == true)
            {
                if (IsNullable(actualValueExpression) == false)
                {
                    return CreateAssertExpression($"Assert.Equal({expectedValue}, ({actualValueExpression}).Count)", node);
                }
                return CreateAssertExpression($"Assert.Equal({expectedValue}, ({actualValueExpression})?.Count)", node);
            }
            if (IsNullable(actualValueExpression) == false)
            {
                return CreateAssertExpression($"Assert.Equal({expectedValue}, ({actualValueExpression}).Count())", node);
            }
            return CreateAssertExpression($"Assert.Equal({expectedValue}, ({actualValueExpression})?.Count())", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().StartWith"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().StartWith() in {Node}", node);
            return CreateAssertExpression($"Assert.StartsWith({expectedValue}, {actualValueExpression})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().EndWith"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().EndWith() in {Node}", node);
            return CreateAssertExpression($"Assert.EndsWith({expectedValue}, {actualValueExpression})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeGreaterOrEqualTo"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().BeGreaterOrEqualTo() in {Node}", node);
            return CreateAssertExpression($"Assert.True({actualValueExpression} >= {expectedValue})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeGreaterThan"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().BeGreaterThan() in {Node}", node);
            return CreateAssertExpression($"Assert.True({actualValueExpression} > ({expectedValue}))", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeLessThan"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().BeLessThan() in {Node}", node);
            return CreateAssertExpression($"Assert.True({actualValueExpression} < ({expectedValue}))", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeBefore"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().BeBefore() in {Node}", node);
            return CreateAssertExpression($"Assert.True({actualValueExpression} < ({expectedValue}))", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().NotBeBefore"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().NotBeBefore() in {Node}", node);
            return CreateAssertExpression($"Assert.False({actualValueExpression} < ({expectedValue}))", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeOnOrBefore"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().BeOnOrBefore() in {Node}", node);
            return CreateAssertExpression($"Assert.True({actualValueExpression} <= ({expectedValue}))", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().NotBeOnOrBefore"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().NotBeOnOrBefore() in {Node}", node);
            return CreateAssertExpression($"Assert.False({actualValueExpression} <= ({expectedValue}))", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeAfter"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().BeAfter() in {Node}", node);
            return CreateAssertExpression($"Assert.True({actualValueExpression} > ({expectedValue}))", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().NotBeAfter"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().NotBeAfter() in {Node}", node);
            return CreateAssertExpression($"Assert.False({actualValueExpression} > ({expectedValue}))", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeOnOrAfter"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().BeOnOrAfter() in {Node}", node);
            return CreateAssertExpression($"Assert.True({actualValueExpression} >= ({expectedValue}))", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().NotBeOnOrAfter"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().NotBeOnOrAfter() in {Node}", node);
            return CreateAssertExpression($"Assert.False({actualValueExpression} >= ({expectedValue}))", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeNullOrEmpty"))
        {
            logger.LogTrace("Rewriting .Should().BeNullOrEmpty() in {Node}", node);
            return CreateAssertExpression($"Assert.False({actualValueExpression}?.Any() ?? false)", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().NotBeNullOrEmpty"))
        {
            logger.LogTrace("Rewriting .Should().BeNullOrEmpty() in {Node}", node);
            if (IsNullable(actualValueExpression) == false)
            {
                return CreateAssertExpression($"Assert.True({actualValueExpression}.Any())", node);
            }
            return CreateAssertExpression($"Assert.True({actualValueExpression}?.Any())", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeCloseTo"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            var precision = shouldInvocationExpression.ArgumentList.Arguments[1].Expression;
            logger.LogTrace("Rewriting .Should().BeCloseTo() in {Node}", node);
            return CreateAssertExpression(
                $"Assert.True({actualValueExpression} >= ({expectedValue} - {precision}) && {actualValueExpression} <= ({expectedValue} + {precision}))", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().NotBeCloseTo"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            var precision = shouldInvocationExpression.ArgumentList.Arguments[1].Expression;
            logger.LogTrace("Rewriting .Should().NotBeCloseTo() in {Node}", node);
            return CreateAssertExpression(
                $"Assert.False({actualValueExpression} >= ({expectedValue} - {precision}) && {actualValueExpression} <= ({expectedValue} + {precision}))", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().BeOneOf"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().BeOneOf() in {Node}", node);
            return CreateAssertExpression($"Assert.Contains({actualValueExpression}, {expectedValue})", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().ContainAll"))
        {
            // Get all the arguments passed to ContainAll
            var expectedValues = shouldInvocationExpression.ArgumentList.Arguments
                .Select(arg => arg.Expression)
                .ToList();

            // Get the target string being tested (e.g. "abc")
            var targetString = actualValueExpression;

            // Create a collection expression with all the arguments
            var collectionExpression = SyntaxFactory.CollectionExpression(
                SyntaxFactory.SeparatedList<CollectionElementSyntax>(
                    expectedValues.SelectMany(value => new SyntaxNodeOrToken[] {
                            SyntaxFactory.ExpressionElement(value),
                            SyntaxFactory.Token(SyntaxKind.CommaToken)
                        })
                        .Take(expectedValues.Count * 2 - 1) // Remove the last comma
                        .ToArray()
                )
            );

            // Create the lambda for Assert.All
            var lambdaParameter = SyntaxFactory.Parameter(
                SyntaxFactory.Identifier("substring"));

            var containsExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Assert"),
                    SyntaxFactory.IdentifierName("Contains")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[] {
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("substring")),
                            SyntaxFactory.Argument(targetString)
                        })));

            var lambda = SyntaxFactory.SimpleLambdaExpression(
                lambdaParameter,
                containsExpression);

            // Create the full Assert.All expression
            var assertAllExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Assert"),
                    SyntaxFactory.IdentifierName("All")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList([
                            SyntaxFactory.Argument(collectionExpression),
                            SyntaxFactory.Argument(lambda)
                        ])));

            logger.LogTrace("Rewriting .Should().ContainAll() in {Node}", node);
            return CreateAssertExpression(assertAllExpression, node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().ContainEquivalentOf"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().ContainEquivalentOf() in {Node}", node);
            return CreateAssertExpression($"Assert.Contains({expectedValue}, {actualValueExpression}, StringComparison.OrdinalIgnoreCase)", node);
        }

        if (shouldInvocationExpressionAsString.EndsWith(".Should().NotContainEquivalentOf"))
        {
            var expectedValue = shouldInvocationExpression.ArgumentList.Arguments[0].Expression;
            logger.LogTrace("Rewriting .Should().NotContainEquivalentOf() in {Node}", node);
            return CreateAssertExpression($"Assert.DoesNotContain({expectedValue}, {actualValueExpression}, StringComparison.OrdinalIgnoreCase)", node);
        }

        return base.VisitInvocationExpression(shouldInvocationExpression);
    }

    // Tries to resolve the part before .Should() when conditional access is used
    // e.g. myObject?.MyProperty.Should().Be(...)
    private static bool TryResolveActualValueFromConditionalAccessExpression(
        ConditionalAccessExpressionSyntax conditionalAccess,
        [NotNullWhen(true)] out InvocationExpressionSyntax? shouldInvocation,
        [NotNullWhen(true)] out ExpressionSyntax? actualValueExpression)
    {
        // Dig all the way down to the deepest WhenNotNull conditional access
        List<ConditionalAccessExpressionSyntax> conditionalAccesses = [conditionalAccess];
        ConditionalAccessExpressionSyntax current = conditionalAccess;
        while (current.WhenNotNull is ConditionalAccessExpressionSyntax nestedConditionalAccess)
        {
            conditionalAccesses.Add(nestedConditionalAccess);
            current = nestedConditionalAccess;
        }

        // Take the deepest WhenNotNull and try to resolve the actual value expression from it
        var deepestConditionalAccess = conditionalAccesses[^1];
        if (deepestConditionalAccess.WhenNotNull is InvocationExpressionSyntax possibleShouldInvocationExpression
            && TryResolveActualValueFromShouldInvocationExpression(possibleShouldInvocationExpression,
                out var finalActualValueExpression))
        {
            var newConditionalAccess = deepestConditionalAccess.WithWhenNotNull(finalActualValueExpression);

            if (conditionalAccesses.Count > 1)
            {
                for (var i = conditionalAccesses.Count - 2; i >= 0; i--)
                {
                    newConditionalAccess = conditionalAccesses[i].WithWhenNotNull(newConditionalAccess);
                }
            }

            shouldInvocation = possibleShouldInvocationExpression;
            actualValueExpression = newConditionalAccess;
            return true;
        }

        shouldInvocation = null;
        actualValueExpression = null;
        return false;
    }

    // Tries to resolve the part before .Should() when await is used
    // e.g. await someAction.Should().ThrowAsync(...)
    private static bool TryResolveActualValueFromAwaitExpression(
        AwaitExpressionSyntax awaitExpression,
        [NotNullWhen(true)] out InvocationExpressionSyntax? shouldInvocation,
        [NotNullWhen(true)] out ExpressionSyntax? actualValueExpression)
    {
        if (awaitExpression.Expression is InvocationExpressionSyntax invocationExpression
            && TryResolveActualValueFromShouldInvocationExpression(invocationExpression, out var innerActualValueExpression))
        {
            actualValueExpression = innerActualValueExpression;
            shouldInvocation = invocationExpression;
            return true;
        }

        actualValueExpression = null;
        shouldInvocation = null;
        return false;
    }

    // Tries to resolve the part before .Should()
    // e.g. myVariable.Should().Be(...)
    private static bool TryResolveActualValueFromShouldInvocationExpression(
        InvocationExpressionSyntax possibleShouldInvocationExpression,
        [NotNullWhen(true)] out ExpressionSyntax? actualValueExpression)
    {
        if (possibleShouldInvocationExpression.Expression is MemberAccessExpressionSyntax
            {
                Expression: InvocationExpressionSyntax innerInvocation
            }
            && innerInvocation.Expression.ToString().EndsWith(".Should"))
        {
            if (innerInvocation.Expression is MemberAccessExpressionSyntax shouldMemberAccess)
            {
                actualValueExpression = shouldMemberAccess.Expression;
                return true;
            }
        }

        actualValueExpression = null;
        return false;
    }

    private TypeSyntax? GetGenericTypeArgument(InvocationExpressionSyntax shouldInvocationExpression, int index = 0)
    {
        var typeArguments = shouldInvocationExpression
            .DescendantNodes()
            .OfType<GenericNameSyntax>()
            .FirstOrDefault()
            ?.TypeArgumentList
            .Arguments;

        if (typeArguments is null || typeArguments.Value.Count <= index)
        {
            return null;
        }

        return typeArguments.Value[index];
    }

    private TypeSyntax? GetTypeOfArgument(InvocationExpressionSyntax shouldInvocationExpression, int index = 0)
    {
        var arguments = shouldInvocationExpression.ArgumentList.Arguments;
        if (arguments.Count <= index)
        {
            return null;
        }

        return arguments[index].Expression is TypeOfExpressionSyntax typeOfExpression
            ? typeOfExpression.Type
            : null;
    }

    private static ExpressionSyntax CreateAssertExpression(string assertCode, ExpressionSyntax originalNode)
    {
        return SyntaxFactory.ParseExpression(assertCode)
            .WithLeadingTrivia(originalNode.GetLeadingTrivia())
            .WithTrailingTrivia(originalNode.GetTrailingTrivia());
    }

    private static ExpressionSyntax CreateAssertExpression(ExpressionSyntax assertCode, ExpressionSyntax originalNode)
    {
        return assertCode
            .WithLeadingTrivia(originalNode.GetLeadingTrivia())
            .WithTrailingTrivia(originalNode.GetTrailingTrivia());
    }

    private bool? IsLambdaExpression(ExpressionSyntax expression)
    {
        try
        {
            var semanticModel = lazySemanticModel.Value.GetAwaiter().GetResult();
            var typeInfo = semanticModel.GetTypeInfo(expression);
            var type = typeInfo.Type ?? typeInfo.ConvertedType;
            var typeFullName = type?.ToString();
            if (typeFullName is null)
            {
                return null;
            }

            return typeFullName.StartsWith("System.Predicate")
                   || typeFullName.StartsWith("System.Func")
                   || typeFullName.StartsWith("System.Action")
                   || typeFullName.StartsWith("System.Linq.Expressions");
        }
        catch
        {
            return null;
        }
    }

    private bool? IsCollection(ExpressionSyntax expression)
    {
        try
        {
            var semanticModel = lazySemanticModel.Value.GetAwaiter().GetResult();
            var typeInfo = semanticModel.GetTypeInfo(expression);
            var type = typeInfo.Type ?? typeInfo.ConvertedType;
            if (type is null)
            {
                return null;
            }

            // Check if type implements ICollection<T>
            return type.ToString()!.StartsWith("System.Collections.Generic.ICollection<")
                   || type.AllInterfaces.Any(i =>
                       i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_ICollection_T ||
                       i.ToString()?.StartsWith("System.Collections.Generic.ICollection<") == true);
        }
        catch
        {
            return null;
        }
    }

    private bool? IsArray(ExpressionSyntax expression)
    {
        try
        {
            var semanticModel = lazySemanticModel.Value.GetAwaiter().GetResult();
            var typeInfo = semanticModel.GetTypeInfo(expression);
            var type = typeInfo.Type ?? typeInfo.ConvertedType;
            if (type == null)
            {
                return null;
            }

            return type.TypeKind == TypeKind.Array;
        }
        catch
        {
            return null;
        }
    }

    private bool? IsNullable(ExpressionSyntax expression)
    {
        try
        {
            if (expression is ConditionalAccessExpressionSyntax)
            {
                return true;
            }

            var semanticModel = lazySemanticModel.Value.GetAwaiter().GetResult();
            var typeInfo = semanticModel.GetTypeInfo(expression);
            var type = typeInfo.Type;

            if (type == null)
            {
                return null;
            }

            // Check for nullable value types (Nullable<T>)
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return true;
            }

            // Check for nullable reference types
            return type.NullableAnnotation == NullableAnnotation.Annotated;
        }
        catch
        {
            return null;
        }
    }

    [GeneratedRegex(@"\.Should\(\)\.Throw(?:<[^>]+>)?$")]
    private static partial Regex ThrowRegex();

    [GeneratedRegex(@"\.Should\(\)\.NotThrow(?:<[^>]+>)?$")]
    private static partial Regex NotThrowRegex();

    [GeneratedRegex(@"\.Should\(\)\.ThrowAsync(?:<[^>]+>)?$")]
    private static partial Regex ThrowAsyncRegex();

    [GeneratedRegex(@"\.Should\(\)\.NotThrowAsync(?:<[^>]+>)?$")]
    private static partial Regex NotThrowAsyncRegex();

    [GeneratedRegex(@"\.Should\(\)\.BeOfType(?:<[^>]+>)?$")]
    private static partial Regex BeOfTypeRegex();

    [GeneratedRegex(@"\.Should\(\)\.NotBeOfType(?:<[^>]+>)?$")]
    private static partial Regex NotBeOfTypeRegex();

    [GeneratedRegex(@"\.Should\(\)\.BeAssignableTo(?:<[^>]+>)?")]
    private static partial Regex BeAssignableToRegex();
}
