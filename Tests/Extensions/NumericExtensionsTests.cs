using CoreOne.Extensions;
using NUnit.Framework;

namespace Tests.Extensions;

public class NumericExtensionsTests
{
    [Test]
    public void Lerp_Float_InterpolatesCorrectly()
    {
        var start = 0f;
        var end = 10f;
        var result = start.Lerp(end, 0.5f);
        Assert.That(result, Is.EqualTo(5f).Within(0.001f));
    }

    [Test]
    public void Lerp_Float_AtZeroPercent_ReturnsStart()
    {
        var start = 5f;
        var end = 15f;
        var result = start.Lerp(end, 0f);
        Assert.That(result, Is.EqualTo(5f).Within(0.001f));
    }

    [Test]
    public void Lerp_Float_AtOneHundredPercent_ReturnsEnd()
    {
        var start = 5f;
        var end = 15f;
        var result = start.Lerp(end, 1f);
        Assert.That(result, Is.EqualTo(15f).Within(0.001f));
    }

    [Test]
    public void Lerp_Double_InterpolatesCorrectly()
    {
        var start = 0.0;
        var end = 100.0;
        var result = start.Lerp(end, 0.25f);
        Assert.That(result, Is.EqualTo(25.0).Within(0.001));
    }

    [Test]
    public void Lerp_Int_InterpolatesCorrectly()
    {
        var start = 0;
        var end = 100;
        var result = start.Lerp(end, 0.5f);
        Assert.That(result, Is.EqualTo(50));
    }

    [Test]
    public void Lerp_Int_RoundsResult()
    {
        var start = 0;
        var end = 10;
        var result = start.Lerp(end, 0.33f);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Lerp_Float_NegativeValues()
    {
        var start = -10f;
        var end = 10f;
        var result = start.Lerp(end, 0.5f);
        Assert.That(result, Is.EqualTo(0f).Within(0.001f));
    }
}
