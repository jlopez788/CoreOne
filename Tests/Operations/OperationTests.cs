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
        Assert.That(preq.GetFilters().Count(), Is.EqualTo(1));
    }
}