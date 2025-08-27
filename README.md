# FluentAssertions to xUnit Migration Tool

A .NET tool that automatically converts FluentAssertions test assertions to their xUnit equivalents.
Regardless of how we feel about the upcoming license changes in the next major version of FluentAssertions, I imagine many of us will find ourselves in the position of having to strip out FluentAssertions of our codebases. 
I know I will. This tool tries to automate that process as much as possible. 

## Based on the following sources

- https://github.com/xunit/xunit/issues/3133
- https://www.meziantou.net/using-roslyn-to-analyze-and-rewrite-code-in-a-solution.htm
- https://stackoverflow.com/questions/31481251/applying-multiple-changes-to-a-solution-in-rosly
- ... 


## Installation

```bash
dotnet tool install -g FluentAssertionsMigrator
```

### Building the Tool yourself

```bash
cd C:\...\fluentassertions-migrator\src\
dotnet pack
dotnet tool install -g FluentAssertionsMigrator
```

## Usage

**Make a backup of your solution (or use version control like a sane person) before you run this tool**

**Before you run this tool, be sure to restore the nuget packages of your solution. This tool uses semantic analysis in some places that will not work without these packages.**

```bash
migrate-fluentassertions <path-to-solution-file>
```

Example:
```bash
migrate-fluentassertions C:\Projects\MySolution.sln
```

## What it does

This tool analyzes your test files and converts FluentAssertions API calls to their xUnit equivalents. It handles many common assertion patterns including:

### Equality
- `.Should().Be()` → `Assert.Equal()`
- `.Should().NotBe()` → `Assert.NotEqual()`
- `.Should().BeSameAs()` → `Assert.Same()`
- `.Should().NotBeSameAs()` → `Assert.NotSame()`
- `.Should().BeEquivalentTo()` → `Assert.Equivalent()`

### Boolean checks
- `.Should().BeTrue()` → `Assert.True()`
- `.Should().BeFalse()` → `Assert.False()`

### Null checks
- `.Should().BeNull()` → `Assert.Null()`
- `.Should().NotBeNull()` → `Assert.NotNull()`

### Collections
- `.Should().BeEmpty()` → `Assert.Empty()`
- `.Should().NotBeEmpty()` → `Assert.NotEmpty()`
- `.Should().HaveCount()` → `Assert.Equal(count)` or `Assert.Single()` or `Assert.Empty()`
- `.Should().Contain()` → `Assert.Contains()`
- `.Should().NotContain()` → `Assert.DoesNotContain()`
- `.Should().ContainSingle()` → `Assert.Single()`
- `.Should().BeNullOrEmpty()` → `Assert.False(?.Any() ?? false)`
- `.Should().NotBeNullOrEmpty()` → `Assert.True(?.Any())`
- `.Should().BeOneOf()` → `Assert.Contains()`
- `.Should().ContainEquivalentOf()` → `Assert.Contains()`
- `.Should().NotContainEquivalentOf()` → `Assert.DoesNotContain()`

### Strings
- `.Should().StartWith()` → `Assert.StartsWith()`
- `.Should().EndWith()` → `Assert.EndsWith()`
- `.Should().ContainAll()` → `Assert.All(strings, s => Assert.Contains(s, x))`

### Type checks
- `.Should().BeOfType()` → `Assert.True(x is Type)`
- `.Should().NotBeOfType()` → `Assert.False(x is Type)`

### Exceptions
- `.Should().Throw()` → `Assert.Throws()`
- `.Should().NotThrow()` → `Assert.Nulll(Record.Exception())`
- `.Should().Throw<T>()` → `Assert.Throws<T>()`
- `.Should().NotThrow<T>()` → `Assert.IsType<T>(Record.Exception())`
- `.Should().ThrowAsync()` → `Assert.ThrowsAsync()`
- `.Should().NotThrowAsync()` → `Assert.Null(Record.ExceptionAsync())`
- `.Should().ThrowAsync<T>()` → `Assert.ThrowsAsync<T>()`
- `.Should().NotThrowAsync<T>()` → `Assert.IsNotType<T>(Record.ExceptionAsync())`

### Numeric comparisons
- `.Should().BeGreaterThan()` → `Assert.True(x > y)`
- `.Should().BeLessThan()` → `Assert.True(x < y)`
- `.Should().BeCloseTo()` → `Assert.True(x > (expected - precision) && x < (expected + precision))`
- `.Should().NotBeCloseTo()` → `Assert.False(x > (expected - precision) && x < (expected + precision))`

### Date comparisons
- `.Should().BeBefore()` → `Assert.True(x < y)`
- `.Should().NotBeBefore()` → `Assert.False(x < y)`
- `.Should().BeAfter()` → `Assert.True(x > y)`
- `.Should().NotBeAfter()` → `Assert.False(x > y)`
- `.Should().BeOnOrBefore()` → `Assert.True(x <= y)`
- `.Should().NotBeOnOrBefore()` → `Assert.False(x <= y)`
- `.Should().BeOnOrAfter()` → `Assert.True(x >= y)`
- `.Should().NotBeOnOrAfter()` → `Assert.False(x >= y)`

## Important Note

While this tool automates a significant portion of the migration, manual review and adjustments will be needed:

1. Some complex FluentAssertions may not have direct xUnit equivalents
2. Custom assertion messages will need to be reformatted
3. Chain assertions (using .And) will need to be split into multiple Assert statements
4. Some assertions might require additional null checks or type conversions

Always review the generated code and test thoroughly after migration.

## Contributing

Feel free to submit issues and pull requests but I'm not making any promises. :-) 

## Is this free?

Yes, it is! 
If you really like this and want to give something in return, donations can be made here: https://github.com/sponsors/amoerie?frequency=one-time. Thanks!
