using CoreOne.Extensions;
using CoreOne.Results;

namespace Tests.Extensions;

public class EnumerableExtensionsTests
{
    [Test]
    public void AddOrUpdate_AddsNewItem_WhenNotExists()
    {
        var list = new List<int> { 1, 2, 3 };
        var result = list.AddOrUpdate(4);
        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result, Does.Contain(4));
    }

    [Test]
    public void AddOrUpdate_UpdatesExistingItem_WhenExists()
    {
        var list = new List<string> { "apple", "banana", "cherry" };
        var result = list.AddOrUpdate("BANANA", p => p.Equals("banana", StringComparison.OrdinalIgnoreCase));
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[1], Is.EqualTo("BANANA"));
    }

    [Test]
    public void AddOrUpdate_CreatesNewList_WhenNull()
    {
        List<int>? list = null;
        var result = list.AddOrUpdate(5);
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(5));
    }

    [Test]
    public void ExcludeNulls_RemovesNullValues()
    {
        var list = new List<string?> { "apple", null, "banana", null, "cherry" };
        var result = list.ExcludeNulls().ToList();
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result, Does.Not.Contain(null));
    }

    [Test]
    public void ExcludeNulls_ReturnsEmpty_WhenSourceIsNull()
    {
        List<string>? list = null;
        var result = list.ExcludeNulls();
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ExcludeNullOrEmpty_RemovesNullAndEmptyStrings()
    {
        var list = new List<string?> { "apple", null, "", "  ", "banana" };
        var result = list.ExcludeNullOrEmpty().ToList();
        Assert.That(result, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0], Is.EqualTo("apple"));
            Assert.That(result[1], Is.EqualTo("banana"));
        }
    }

    [Test]
    public void Each_InvokesActionForEachItem()
    {
        var list = new List<int> { 1, 2, 3 };
        var sum = 0;
        list.Each(x => sum += x);
        Assert.That(sum, Is.EqualTo(6));
    }

    [TestCase(new[] { 0, 1, 2 })]
    public void Each_WithIndex_ProvidesIndexToAction(int[] data)
    {
        var list = new List<string> { "a", "b", "c" };
        var indices = new List<int>();
        list.Each((item, index) => indices.Add(index));
        Assert.That(indices, Is.EqualTo(data));
    }

    [TestCase(new[] { 1, 2, 3 })]
    public async Task EachAsync_ProcessesItemsSequentially(int[] data)
    {
        var list = new List<int> { 1, 2, 3 };
        var results = new List<int>();
        await list.EachAsync(async x => {
            await Task.Delay(10);
            results.Add(x);
        });
        Assert.That(results, Is.EqualTo(data));
    }

    [Test]
    public void Partition_SplitsIntoChunks()
    {
        var list = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
        var partitions = list.Partition(3).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(partitions, Has.Count.EqualTo(3));
            Assert.That(partitions[0].Count(), Is.EqualTo(3));
            Assert.That(partitions[1].Count(), Is.EqualTo(3));
            Assert.That(partitions[2].Count(), Is.EqualTo(1));
        }
    }

    [Test]
    public void SelectList_MapsToNewType()
    {
        var list = new List<int> { 1, 2, 3 };
        var data = new[] { "1", "2", "3" };
        var result = list.SelectList(x => x.ToString());
        Assert.That(result, Is.EqualTo(data));
    }

    [Test]
    public void SelectArray_MapsToArray()
    {
        var list = new List<int> { 1, 2, 3 };
        var data = new[] { 2, 4, 6 };
        var result = list.SelectArray(x => x * 2);
        Assert.That(result, Is.InstanceOf<int[]>());
        Assert.That(result, Is.EqualTo(data));
    }

    [Test]
    public void ToData_CreatesDictionaryFromItems()
    {
        var list = new List<KeyValuePair<string, int>> {
            new("one", 1),
            new("two", 2),
            new("three", 3)
        };
        var data = list.ToData(kv => kv.Key, kv => kv.Value);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(data["one"], Is.EqualTo(1));
            Assert.That(data["two"], Is.EqualTo(2));
            Assert.That(data["three"], Is.EqualTo(3));
        }
    }

    [Test]
    public async Task AggregateAsync_AccumulatesValues()
    {
        var list = new List<int> { 1, 2, 3, 4 };
        var result = await list.AggregateAsync(0, async (sum, x) => {
            await Task.Delay(1);
            return sum + x;
        });
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void AggregateResult_StopsOnFailure()
    {
        var list = new List<int> { 1, 2, 3, 4 };
        var processedCount = 0;
        var result = list.AggregateResult(Result.Ok, (acc, x) => {
            processedCount++;
            return x == 3 ? Result.Fail() : Result.Ok;
        });
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(processedCount, Is.EqualTo(3));
        }
    }

    [Test]
    public async Task AggregateResultAsync_StopsOnFailure()
    {
        var list = new List<int> { 1, 2, 3, 4 };
        var processedCount = 0;
        var result = await list.AggregateResultAsync(Result.Ok, async (acc, x) => {
            processedCount++;
            await Task.Delay(1);
            return x == 2 ? Result.Fail() : Result.Ok;
        });
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(processedCount, Is.EqualTo(2));
        }
    }

    [Test]
    public void AddRange_AddsItemsToSet()
    {
        var set = new HashSet<int> { 1, 2 };
        var items = new List<int> { 3, 4, 5 };
        set.AddRange(items);
        
        Assert.That(set, Has.Count.EqualTo(5));
        Assert.That(set, Does.Contain(3).And.Contain(4).And.Contain(5));
    }

    [Test]
    public void AddRange_HandlesNullSet()
    {
        HashSet<int>? set = null;
        var items = new List<int> { 1, 2 };
        
        Assert.DoesNotThrow(() => set.AddRange(items));
    }

    [Test]
    public void AddRange_HandlesNullItems()
    {
        var set = new HashSet<int> { 1, 2 };
        
        Assert.DoesNotThrow(() => set.AddRange(null));
        Assert.That(set, Has.Count.EqualTo(2));
    }

    [Test]
    public void AddOrUpdate_WithNullModel_ReturnsUnchanged()
    {
        var list = new List<int?> { 1, 2, 3 };
        var result = list.AddOrUpdate((int?)null);
        
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void Partition_HandlesEmptyList()
    {
        var list = new List<int>();
        var result = list.Partition(3).ToList();
        
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Partition_HandlesNullList()
    {
        List<int>? list = null;
        var result = list.Partition(3).ToList();
        
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Partition_SingleItemSmallerThanPartitionSize()
    {
        var list = new List<int> { 1 };
        var result = list.Partition(5).ToList();
        
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Count(), Is.EqualTo(1));
    }

    [Test]
    public void SelectList_WithIndex_WorksCorrectly()
    {
        var list = new List<string> { "a", "b", "c" };
        var result = list.SelectList((x, i) => $"{x}{i}");
        var data = new[] { "a0", "b1", "c2" };
        Assert.That(result, Is.EqualTo( data));
    }

    [Test]
    public void SelectList_HandlesNullEnumerable()
    {
        List<int>? list = null;
        var result = list.SelectList(x => x.ToString());
        
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SelectArray_HandlesNullEnumerable()
    {
        List<int>? list = null;
        var result = list.SelectArray(x => x * 2);
        
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SelectArray_HandlesNullCallback()
    {
        var list = new List<int> { 1, 2, 3 };
        var result = list.SelectArray<int, int>(null!);
        
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ToData_WithComparer_UsesCaseInsensitiveKeys()
    {
        var list = new List<KeyValuePair<string, int>> {
            new("ABC", 1),
            new("def", 2)
        };
        var data = list.ToData(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(data["abc"], Is.EqualTo(1));
            Assert.That(data["DEF"], Is.EqualTo(2));
        }
    }

    [Test]
    public void ToData_WithItemOnly_CreatesDataWithItemAsValue()
    {
        var list = new List<string> { "one", "two", "three" };
        var data = list.ToData(x => x.ToUpper());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(data["ONE"], Is.EqualTo("one"));
            Assert.That(data["TWO"], Is.EqualTo("two"));
        }
    }

    [Test]
    public void ToData_HandlesNullEnumerable()
    {
        List<int>? list = null;
        var data = list.ToData(x => x, x => x.ToString());
        
        Assert.That(data, Is.Empty);
    }

    [Test]
    public void ToDataDictionary_CreatesDataFromItems()
    {
        var list = new List<KeyValuePair<int, string>> {
            new(1, "one"),
            new(2, "two")
        };
        var data = list.ToDataDictionary(kv => kv.Key, kv => kv.Value);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(data[1], Is.EqualTo("one"));
            Assert.That(data[2], Is.EqualTo("two"));
        }
    }

    [Test]
    public void ToDataDictionary_WithKeyOnly_ItemIsValue()
    {
        var list = new List<int> { 1, 2, 3 };
        var data = list.ToDataDictionary(x => x);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(data[1], Is.EqualTo(1));
            Assert.That(data[2], Is.EqualTo(2));
        }
    }

    [Test]
    public void ToList_WithPredicate_FiltersItems()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var result = list.ToList(x => x % 2 == 0);
        var data = new[] { 2, 4 };
        Assert.That(result, Is.EqualTo(data));
    }

    [Test]
    public void ToList_WithPredicateOnNull_ReturnsEmpty()
    {
        List<int>? list = null;
        var result = list.ToList(x => x > 0);
        
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Where_WithNullableBool_FiltersCorrectly()
    {
        var list = new List<int> { 1, 2, 3, 4 };
        var result = list.Where(x => x > 2 ? true : (bool?)null).ToList();
        var data = new[] { 3, 4 };
        Assert.That(result, Is.EqualTo(data));
    }

    [Test]
    public void Where_WithNullEnumerable_ReturnsEmpty()
    {
        List<int>? list = null;
        var result = EnumerableExtensions.Where(list, (Func<int, bool?>)(x => true)).ToList();
        
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ExcludeNulls_WithNullableStructs_RemovesNulls()
    {
        var list = new List<int?> { 1, null, 2, null, 3 };
        var result = list.ExcludeNulls().ToList();
        var data = new[] { 1, 2, 3 };
        Assert.That(result, Is.EqualTo(data));
    }

    [Test]
    public async Task EachAsync_HandlesCancellation()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var cts = new CancellationTokenSource();
        var processed = new List<int>();
        
        await list.EachAsync(async x => {
            if (x == 3) cts.Cancel();
            processed.Add(x);
            await Task.Delay(1);
        }, cts.Token);
        
        Assert.That(processed, Has.Count.LessThan(5));
    }

    [Test]
    public void Each_HandlesNullEnumerable()
    {
        List<int>? list = null;
        var sum = 0;
        
        Assert.DoesNotThrow(() => list.Each(x => sum += x));
        Assert.That(sum, Is.EqualTo(0));
    }

    [Test]
    public void Each_WithIndexOnNull_DoesNothing()
    {
        List<int>? list = null;
        var called = false;
        
        Assert.DoesNotThrow(() => list.Each((x, i) => called = true));
        Assert.That(called, Is.False);
    }

    [Test]
    public async Task AggregateAsync_WithNullEnumerable_ReturnsSeed()
    {
        List<int>? list = null;
        var result = await list.AggregateAsync(10, async (acc, x) => {
            await Task.Delay(1);
            return acc + x;
        });
        
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public async Task AggregateAsync_WithEmptyEnumerable_ReturnsSeed()
    {
        var list = new List<int>();
        var result = await list.AggregateAsync(5, async (acc, x) => {
            await Task.Delay(1);
            return acc + x;
        });
        
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void AggregateResult_WithNullEnumerable_ReturnsSeed()
    {
        List<int>? list = null;
        var result = list.AggregateResult(Result.Ok, (acc, x) => Result.Fail());
        
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void AggregateResult_ContinuesOnSuccess()
    {
        var list = new List<int> { 1, 2, 3 };
        var count = 0;
        var result = list.AggregateResult(Result.Ok, (acc, x) => {
            count++;
            return Result.Ok;
        });
        using (Assert.EnterMultipleScope())
        {
            Assert.That(count, Is.EqualTo(3));
            Assert.That(result.Success, Is.True);
        }
    }

    [Test]
    public async Task AggregateResultAsync_WithNullEnumerable_ReturnsSeed()
    {
        List<int>? list = null;
        var result = await list.AggregateResultAsync(Result.Ok, async (acc, x) => {
            await Task.Delay(1);
            return Result.Fail();
        });
        
        Assert.That(result.Success, Is.True);
    }
}