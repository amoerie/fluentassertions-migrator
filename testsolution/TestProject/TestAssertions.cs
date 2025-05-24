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
}
