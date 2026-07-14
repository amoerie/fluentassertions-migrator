using FluentAssertions;

namespace TestProject;

public class TestAssertions
{
    /* Equality */
    [Fact]
    public void Be()
    {
        var actual = 42;
        actual.Should().Be(42);
    }

    [Fact]
    public void NotBe()
    {
        var actual = 42;
        actual.Should().NotBe(43);
    }

    [Fact]
    public void BeSameAs()
    {
        var obj = new object();
        var same = obj;
        same.Should().BeSameAs(obj);
    }
    
    [Fact]
    public void NotBeSameAs()
    {
        var obj1 = new object();
        var obj2 = new object();
        obj1.Should().NotBeSameAs(obj2);
    }
    
    [Fact]
    public void BeEquivalentTo()
    {
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 1, 2, 3 };
        list1.Should().BeEquivalentTo(list2);
    }

    /* Boolean checks */
    [Fact]
    public void BeTrue()
    {
        var result = true;
        result.Should().BeTrue();
    }

    [Fact]
    public void BeFalse()
    {
        var result = false;
        result.Should().BeFalse();
    }

    /* Null checks */
    [Fact]
    public void BeNull()
    {
        string? value = null;
        value.Should().BeNull();
    }

    [Fact]
    public void NotBeNull()
    {
        string value = "test";
        value.Should().NotBeNull();
    }

    /* Collections */
    [Fact]
    public void BeEmpty()
    {
        var list = new List<int>();
        list.Should().BeEmpty();
    }
    
    [Fact]
    public void NotBeEmpty()
    {
        var list = new List<int> { 1 };
        list.Should().NotBeEmpty();
    }

    [Fact]
    public void HaveCount()
    {
        var list = new List<int> { 1, 2, 3 };
        list.Should().HaveCount(3);
    }

    [Fact]
    public void Contain()
    {
        var list = new List<int> { 1, 2, 3 };
        list.Should().Contain(2);
    }
    
    [Fact]
    public void ContainSingle()
    {
        var list = new List<int> { 1};
        list.Should().ContainSingle();
    }
    
    [Fact]
    public void NotContain()
    {
        var list = new List<int> { 1, 2, 3 };
        list.Should().NotContain(4);
    }
    
    [Fact]
    public void BeNullOrEmpty()
    {
        string? value = null;
        value.Should().BeNullOrEmpty();
    }

    [Fact]
    public void NotBeNullOrEmpty()
    {
        string value = "test";
        value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void BeOneOf()
    {
        var value = 2;
        var validValues = new[] { 1, 2, 3 };
        value.Should().BeOneOf(validValues);
    }

    [Fact]
    public void ContainEquivalentOf()
    {
        var value = "TestExample";
        value.Should().ContainEquivalentOf("TEST");
    }

    [Fact]
    public void NotContainEquivalentOf()
    {
        var value = "TestExample";
        value.Should().NotContainEquivalentOf("OTHER");
    }

    /* Strings */
    [Fact]
    public void StartWith()
    {
        var text = "Hello World";
        text.Should().StartWith("Hello");
    }

    [Fact]
    public void EndWith()
    {
        var text = "Hello World";
        text.Should().EndWith("World");
    }

    /* Type checks */
    [Fact]
    public void BeOfType()
    {
        var obj = "test";
        obj.Should().BeOfType<string>();
    }
    
    [Fact]
    public void NotBeOfType()
    {
        var obj = "test";
        obj.Should().NotBeOfType<int>();
    }

    /* Exceptions */
    [Fact]
    public void Throw()
    {
        Action action = () => throw new InvalidOperationException();
        action.Should().Throw<InvalidOperationException>();
    }
    
    [Fact]
    public void NotThrow()
    {
        Action action = () => { };
        action.Should().NotThrow();
    }

    [Fact]
    public async Task ThrowAsync()
    {
        Func<Task> action = () => Task.FromException(new InvalidOperationException());
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task NotThrowAsync()
    {
        Func<Task> action = async () => await Task.CompletedTask;
        await action.Should().NotThrowAsync();
    }

    /* Numeric comparison */
    [Fact]
    public void BeGreaterThan()
    {
        var number = 10;
        number.Should().BeGreaterThan(5);
    }

    [Fact]
    public void BeLessThan()
    {
        var number = 5;
        number.Should().BeLessThan(10);
    }
    
    [Fact]
    public void BeCloseTo()
    {
        var value = 10;
        value.Should().BeCloseTo(11, 1);
    }

    [Fact]
    public void NotBeCloseTo()
    {
        var value = 10;
        value.Should().NotBeCloseTo(12, 1);
    }

    /* Date comparisons */
    [Fact]
    public void BeBefore()
    {
        var earlier = DateTime.Now.AddDays(-1);
        var later = DateTime.Now;
        earlier.Should().BeBefore(later);
    }

    [Fact]
    public void BeAfter()
    {
        var earlier = DateTime.Now.AddDays(-1);
        var later = DateTime.Now;
        later.Should().BeAfter(earlier);
    }

    [Fact]
    public void NotBeBefore()
    {
        var date1 = DateTime.Now;
        var date2 = date1.AddDays(-1);
        date1.Should().NotBeBefore(date2);
    }

    [Fact]
    public void NotBeAfter()
    {
        var date1 = DateTime.Now;
        var date2 = date1.AddDays(1);
        date1.Should().NotBeAfter(date2);
    }

    [Fact]
    public void BeOnOrBefore()
    {
        var date1 = DateTime.Now;
        var date2 = date1;
        date1.Should().BeOnOrBefore(date2);
    }

    [Fact]
    public void BeOnOrAfter()
    {
        var date1 = DateTime.Now;
        var date2 = date1;
        date1.Should().BeOnOrAfter(date2);
    }

    /* Numeric comparison (inclusive) */
    [Fact]
    public void BeGreaterThanOrEqualTo()
    {
        var number = 10;
        number.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void BeLessThanOrEqualTo()
    {
        var number = 10;
        number.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public void BePositive()
    {
        var number = 5;
        number.Should().BePositive();
    }

    [Fact]
    public void BeNegative()
    {
        var number = -5;
        number.Should().BeNegative();
    }

    /* Dictionaries */
    [Fact]
    public void ContainKey()
    {
        var dictionary = new Dictionary<string, int> { ["a"] = 1 };
        dictionary.Should().ContainKey("a");
    }

    [Fact]
    public void NotContainKey()
    {
        var dictionary = new Dictionary<string, int> { ["a"] = 1 };
        dictionary.Should().NotContainKey("b");
    }

    /* Regex */
    [Fact]
    public void MatchRegex()
    {
        var text = "abc123";
        text.Should().MatchRegex("^[a-z]+[0-9]+$");
    }

    [Fact]
    public void NotMatchRegex()
    {
        var text = "abc123";
        text.Should().NotMatchRegex("^[0-9]+$");
    }

    /* Collections */
    [Fact]
    public void OnlyContain()
    {
        var list = new List<int> { 2, 4, 6 };
        list.Should().OnlyContain(x => x % 2 == 0);
    }

    [Fact]
    public void Equal()
    {
        var list = new List<int> { 1, 2, 3 };
        list.Should().Equal(new List<int> { 1, 2, 3 });
    }

    [Fact]
    public void NotEqual()
    {
        var list = new List<int> { 1, 2, 3 };
        list.Should().NotEqual(new List<int> { 3, 2, 1 });
    }

    [Fact]
    public void HaveCountGreaterThan()
    {
        var list = new List<int> { 1, 2, 3 };
        list.Should().HaveCountGreaterThan(2);
    }

    [Fact]
    public void HaveCountGreaterThanOrEqualTo()
    {
        var list = new List<int> { 1, 2, 3 };
        list.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    /* Strings */
    [Fact]
    public void HaveLength()
    {
        var text = "hello";
        text.Should().HaveLength(5);
    }

    [Fact]
    public void BeNullOrWhiteSpace()
    {
        var text = "   ";
        text.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public void NotBeNullOrWhiteSpace()
    {
        var text = "hello";
        text.Should().NotBeNullOrWhiteSpace();
    }

    /* Type checks */
    [Fact]
    public void BeAssignableTo()
    {
        var list = new List<int> { 1, 2, 3 };
        list.Should().BeAssignableTo<IEnumerable<int>>();
    }

    /* Nested-generic type arguments (regression tests for the <.+> regex fix) */
    [Fact]
    public void BeOfTypeWithNestedGeneric()
    {
        object obj = new List<int> { 1, 2, 3 };
        obj.Should().BeOfType<List<int>>();
    }

    [Fact]
    public void NotBeOfTypeWithNestedGeneric()
    {
        object obj = new List<int> { 1, 2, 3 };
        obj.Should().NotBeOfType<Dictionary<string, int>>();
    }

    [Fact]
    public void BeAssignableToWithNestedGeneric()
    {
        var dictionary = new Dictionary<string, List<int>>();
        dictionary.Should().BeAssignableTo<IDictionary<string, List<int>>>();
    }

    [Fact]
    public void ThrowWithNestedGeneric()
    {
        Action action = () => throw new CustomException<List<int>>();
        action.Should().Throw<CustomException<List<int>>>();
    }

    /* Invoking / Awaiting (produce the "act" delegate for throw assertions) */
    [Fact]
    public void InvokingThrows()
    {
        var subject = new ThrowingSubject();
        subject.Invoking(s => s.ThrowInvalidOperation()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void InvokingDoesNotThrow()
    {
        var subject = new ThrowingSubject();
        subject.Invoking(s => s.DoNothing()).Should().NotThrow();
    }

    [Fact]
    public async Task AwaitingThrows()
    {
        var subject = new ThrowingSubject();
        await subject.Awaiting(s => s.ThrowInvalidOperationAsync()).Should().ThrowAsync<InvalidOperationException>();
    }

    /* Exception detail chains */
    [Fact]
    public void ThrowWithMessage()
    {
        Action action = () => throw new InvalidOperationException("something went wrong");
        action.Should().Throw<InvalidOperationException>().WithMessage("*went wrong*");
    }

    [Fact]
    public void ThrowWithParameterName()
    {
        Action action = () => throw new ArgumentNullException("myParam");
        action.Should().Throw<ArgumentNullException>().WithParameterName("myParam");
    }

    /* Multi-line fluent chains (the .Should() and the assertion sit on separate lines) */
    [Fact]
    public void MultiLineBe()
    {
        var actual = 42;
        actual
            .Should()
            .Be(42);
    }

    [Fact]
    public void MultiLineNotThrow()
    {
        var subject = new ThrowingSubject();
        subject
            .Invoking(s => s.DoNothing())
            .Should()
            .NotThrow();
    }

    [Fact]
    public void MultiLineThrowWithMessage()
    {
        Action action = () => throw new InvalidOperationException("boom happened");
        action
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*boom*");
    }

    [Fact]
    public async Task AwaitingAsyncThrows()
    {
        var subject = new ThrowingSubject();
        await subject.Awaiting(async s => await s.ThrowInvalidOperationAsync()).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void ContainSingleWhich()
    {
        var list = new List<int> { 42 };
        Assert.Equal(42, list.Should().ContainSingle().Which);
    }
}

public sealed class ThrowingSubject
{
    public void ThrowInvalidOperation() => throw new InvalidOperationException();

    public void DoNothing() { }

    public Task ThrowInvalidOperationAsync() => throw new InvalidOperationException();
}

public sealed class CustomException<T> : Exception;
