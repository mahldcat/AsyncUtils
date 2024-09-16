using FluentAssertions;
using Xunit.Abstractions;

namespace AsyncUtils.Tests;

public class MultiMapTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TestAddSyncronousTest()
    {
        AsyncMultiMap<int, int> amm = new AsyncMultiMap<int, int>();
        amm.AddAsync(1, 42).GetAwaiter().GetResult();
        amm.AddAsync(2,44).GetAwaiter().GetResult();

        List<int> values= amm.GetValuesAsync(1).GetAwaiter().GetResult();

        values.Should().NotBeNull();
        values.Should().NotBeEmpty();
        values.Count.Should().Be(1);
        values[0].Should().Be(42);
    }

    [Fact]
    public async Task TestAddAsyncAsAsyncTest()
    {
        AsyncMultiMap<int, int> multiMap = new AsyncMultiMap<int, int>();
        await multiMap.AddAsync(1, 42);
        await multiMap.AddAsync(2,44);

        List<int> values= await multiMap.GetValuesAsync(1);

        values.Should().NotBeNull();
        values.Should().NotBeEmpty();
        values.Count.Should().Be(1);
        values[0].Should().Be(42);
    }

    [Fact]
    public async Task TestAsyncEnum()
    {
        AsyncMultiMap<int, int> multiMap = new AsyncMultiMap<int, int>();
        await multiMap.AddAsync(1, 42);
        await multiMap.AddAsync(1, 43);
        await multiMap.AddAsync(2,44);

        int ct = 0;
        
        await foreach (var kvp in multiMap)
        {
            ++ct;
            testOutputHelper.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");
        }

        ct.Should().Be(3);
    }
}