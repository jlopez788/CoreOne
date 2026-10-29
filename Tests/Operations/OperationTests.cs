using CoreOne;
using CoreOne.Operations;

namespace Tests.Operations;

[TestFixture]
public class OperationTests
{
    [Test]
    public void TestSerialization()
    {
        var request = new PageRequest {
            CurrentPage = 1,
            PageSize = 10
        };
        request.FilterBy("tes", "field");
        var content = Utility.Serialize(request, true);
        var preq = Utility.DeserializeObject<PageRequest>(content);
        Assert.That(preq, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(preq!.GetFilters().Count(), Is.EqualTo(1));
            Assert.That(preq.GetFilterValue("field"), Is.EqualTo("tes"));
        }
    }

    [Test]
    public void CursorRequest_AddFilterAndOrder_ResetsCursorOnlyWhenOperationSignalsChange()
    {
        var request = new CursorRequest("cursor-token-123");

        request.AddField(typeof(string), "Name");
        request.OrderBy("Name", SortDirection.Ascending, single: false);
        var cursorAfterNonResettingOps = request.Cursor;

        request.OrderBy("Name", SortDirection.Descending, single: true);
        var cursorAfterSingleOrder = request.Cursor;

        request.Cursor = "cursor-token-456";
        request.Add(new FilterBy("Alice", "Name"));
        var cursorAfterFilter = request.Cursor;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cursorAfterNonResettingOps, Is.EqualTo("cursor-token-123"));
            Assert.That(cursorAfterSingleOrder, Is.Null);
            Assert.That(cursorAfterFilter, Is.Null);
            Assert.That(request.GetSortFields().Single(), Is.TypeOf<SingleOrderBy>());
        }
    }

    [Test]
    public void ResetFilter_ApplyAndEquality_WorkAsExpected()
    {
        var reset1 = new ResetFilter("Name");
        var reset2 = new ResetFilter("name");
        var reset3 = new ResetFilter("Age");
        var operations = new List<IOperation> {
            new FilterBy("Alice", "Name"),
            new FilterBy("30", "Age"),
            new OrderBy("Name", SortDirection.Ascending)
        };

        var changed = reset1.Apply(operations);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(changed, Is.True);
            Assert.That(operations, Has.Count.EqualTo(2));
            Assert.That(operations.OfType<FilterBy>().Single().Field, Is.EqualTo("Age"));
            Assert.That(reset1, Is.EqualTo(reset2));
            Assert.That(reset1, Is.Not.EqualTo(reset3));
            Assert.That(reset1.ToString(), Is.EqualTo("Reset Filter: Name"));
        }
    }

    [Test]
    public void AvailableField_ApplyAndNullableDetection_WorkAsExpected()
    {
        var operations = new List<IOperation>();
        var field = new AvailableField(typeof(int?), "Age", "N0");

        var changed = field.Apply(operations);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(changed, Is.False);
            Assert.That(operations, Has.Count.EqualTo(1));
            Assert.That(field.Type, Is.EqualTo(typeof(int)));
            Assert.That(field.IsNullable, Is.True);
            Assert.That(field.Format, Is.EqualTo("N0"));
        }
    }
}