# FluentAssertions to xUnit Migration Tool

A .NET tool that automatically converts FluentAssertions test assertions to their xUnit equivalents.

## Installation

```bash
dotnet tool install -g FluentAssertionsMigrator
```

## Usage

```bash
migrate-assertions <path-to-solution-file>
```

Example:
```bash
migrate-assertions C:\Projects\MySolution.sln
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
- `.Should().HaveCount()` → `Assert.Equal(count)` or `Assert.Single()`
- `.Should().Contain()` → `Assert.Contains()`
- `.Should().NotContain()` → `Assert.DoesNotContain()`

### Strings
- `.Should().StartWith()` → `Assert.StartsWith()`
- `.Should().EndWith()` → `Assert.EndsWith()`

### Type checks
- `.Should().BeOfType()` → `Assert.True(x is Type)`

### Exceptions
- `.Should().Throw<T>()` → `Assert.Throws<T>()`
- `.Should().ThrowAsync<T>()` → `Assert.ThrowsAsync<T>()`

### Numeric comparisons
- `.Should().BeGreaterThan()` → `Assert.True(x > y)`
- `.Should().BeLessThan()` → `Assert.True(x < y)`
- `.Should().BeCloseTo()` → Custom assertion
- `.Should().NotBeCloseTo()` → Custom assertion

### Date comparisons
- `.Should().BeBefore()` → `Assert.True(x < y)`
- `.Should().BeAfter()` → `Assert.True(x > y)`
- `.Should().BeOnOrBefore()` → `Assert.True(x <= y)`
- `.Should().BeOnOrAfter()` → `Assert.True(x >= y)`

## Important Note

While this tool automates a significant portion of the migration, manual review and adjustments will be needed:

1. Some complex FluentAssertions may not have direct xUnit equivalents
2. Custom assertion messages will need to be reformatted
3. Chain assertions will need to be split into multiple Assert statements
4. Some assertions might require additional null checks or type conversions

Always review the generated code and test thoroughly after migration.

## Contributing

Feel free to submit issues and pull requests but I'm not making any promises. :-) 
