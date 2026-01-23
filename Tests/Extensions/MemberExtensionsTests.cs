using CoreOne.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Tests.Extensions;

public class MemberExtensionsTests
{
    [Required]
    [StringLength(50)]
    public string TestProperty { get; set; } = "";

    public string NoAttributeProperty { get; set; } = "";

    [Test]
    public void AttributeExists_ExistingAttribute_ReturnsTrue()
    {
        var prop = typeof(MemberExtensionsTests).GetProperty(nameof(TestProperty));
        
        Assert.That(prop.AttributeExists<RequiredAttribute>(), Is.True);
    }

    [Test]
    public void AttributeExists_NonExistingAttribute_ReturnsFalse()
    {
        var prop = typeof(MemberExtensionsTests).GetProperty(nameof(NoAttributeProperty));
        
        Assert.That(prop.AttributeExists<RequiredAttribute>(), Is.False);
    }

    [Test]
    public void AttributeExists_NullMember_ReturnsFalse()
    {
        MemberInfo? member = null;
        
        Assert.That(member.AttributeExists<RequiredAttribute>(), Is.False);
    }

    [Test]
    public void GetAttribute_ExistingAttribute_ReturnsAttribute()
    {
        var prop = typeof(MemberExtensionsTests).GetProperty(nameof(TestProperty));
        
        var attribute = prop.GetAttribute<RequiredAttribute>();
        
        Assert.That(attribute, Is.Not.Null);
    }

    [Test]
    public void GetAttribute_NonExistingAttribute_ReturnsNull()
    {
        var prop = typeof(MemberExtensionsTests).GetProperty(nameof(NoAttributeProperty));
        
        var attribute = prop.GetAttribute<RequiredAttribute>();
        
        Assert.That(attribute, Is.Null);
    }

    [Test]
    public void GetAttribute_NullMember_ReturnsNull()
    {
        MemberInfo? member = null;
        
        var attribute = member.GetAttribute<RequiredAttribute>();
        
        Assert.That(attribute, Is.Null);
    }

    [Test]
    public void GetAttributes_ExistingAttributes_ReturnsAll()
    {
        var prop = typeof(MemberExtensionsTests).GetProperty(nameof(TestProperty));
        
        var attributes = prop.GetAttributes<Attribute>().ToList();
        
        Assert.That(attributes, Has.Count.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void GetAttributes_NonExistingAttributes_ReturnsEmpty()
    {
        var prop = typeof(MemberExtensionsTests).GetProperty(nameof(NoAttributeProperty));
        
        var attributes = prop.GetAttributes<RequiredAttribute>().ToList();
        
        Assert.That(attributes, Is.Empty);
    }

    [Test]
    public void GetAttributes_NullMember_ReturnsEmpty()
    {
        MemberInfo? member = null;
        
        var attributes = member.GetAttributes<RequiredAttribute>().ToList();
        
        Assert.That(attributes, Is.Empty);
    }

    [Test]
    public void TryGetAttribute_ExistingAttribute_ReturnsTrueAndAttribute()
    {
        var prop = typeof(MemberExtensionsTests).GetProperty(nameof(TestProperty))!;
        
        var success = prop.TryGetAttribute<RequiredAttribute>(out var attribute);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(attribute, Is.Not.Null);
        }
    }

    [Test]
    public void TryGetAttribute_NonExistingAttribute_ReturnsFalse()
    {
        var prop = typeof(MemberExtensionsTests).GetProperty(nameof(NoAttributeProperty))!;
        
        var success = prop.TryGetAttribute<RequiredAttribute>(out var attribute);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.False);
            Assert.That(attribute, Is.Null);
        }
    }

    [Test]
    public void TryGetAttribute_WithInherit_FindsInheritedAttributes()
    {
        var prop = typeof(MemberExtensionsTests).GetProperty(nameof(TestProperty))!;

        var success = prop.TryGetAttribute<Attribute>(out _, inherit: true);
        
        Assert.That(success, Is.True);
    }

    [Test]
    public void GetAttribute_StringLengthAttribute_ReturnsCorrectType()
    {
        var prop = typeof(MemberExtensionsTests).GetProperty(nameof(TestProperty));
        
        var attribute = prop.GetAttribute<StringLengthAttribute>();
        
        Assert.That(attribute, Is.Not.Null);
        Assert.That(attribute!.MaximumLength, Is.EqualTo(50));
    }
}
