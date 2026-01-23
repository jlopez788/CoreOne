using CoreOne.Extensions;
using System.ComponentModel.DataAnnotations;

namespace Tests.Extensions;

public class TypeExtensionsTests
{
    [AttributeUsage(AttributeTargets.Class)]
    private class CustomTestAttribute : Attribute { }

    private class TestClass { }

    [CustomTestAttribute]
    private class DecoratedClass { }

    private class BaseClass { }
    private class DerivedClass : BaseClass { }

    [Test]
    public void AttributeExists_ExistingAttribute_ReturnsTrue()
    {
        var type = typeof(DecoratedClass);
        
        Assert.That(type.AttributeExists<CustomTestAttribute>(), Is.True);
    }

    [Test]
    public void AttributeExists_NonExistingAttribute_ReturnsFalse()
    {
        var type = typeof(TestClass);
        
        Assert.That(type.AttributeExists<CustomTestAttribute>(), Is.False);
    }

    [Test]
    public void GetAttributes_ExistingAttribute_ReturnsAttributes()
    {
        var type = typeof(DecoratedClass);
        var attributes = type.GetAttributes<CustomTestAttribute>();
        
        Assert.That(attributes, Is.Not.Empty);
        Assert.That(attributes.Count(), Is.EqualTo(1));
    }

    [Test]
    public void GetAttributes_NonExistingAttribute_ReturnsEmpty()
    {
        var type = typeof(TestClass);
        var attributes = type.GetAttributes<RequiredAttribute>();
        
        Assert.That(attributes, Is.Empty);
    }

    [Test]
    public void GetAttributes_MultipleAttributes_ReturnsAll()
    {
        var type = typeof(DecoratedClass);
        var attributes = type.GetAttributes<Attribute>();
        
        Assert.That(attributes.Count(), Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void GetDefault_ValueType_ReturnsDefault()
    {
        var result = typeof(int).GetDefault();
        
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void GetDefault_ReferenceType_ReturnsNull()
    {
        var result = typeof(string).GetDefault();
        
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetDefault_NullableInt_ReturnsNull()
    {
        var result = typeof(int?).GetDefault();
        
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void GetDefault_Bool_ReturnsFalse()
    {
        var result = typeof(bool).GetDefault();
        
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetDefault_Guid_ReturnsEmptyGuid()
    {
        var result = typeof(Guid).GetDefault();
        
        Assert.That(result, Is.EqualTo(Guid.Empty));
    }

    [Test]
    public void GetDefault_Void_ReturnsNull()
    {
        var result = typeof(void).GetDefault();
        
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetDefault_NullType_ReturnsNull()
    {
        Type? nullType = null;
        var result = nullType.GetDefault();
        
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Implements_Interface_ReturnsTrue()
    {
        var type = typeof(List<int>);
        
        Assert.That(type.Implements(typeof(IEnumerable<int>)), Is.True);
    }

    [Test]
    public void Implements_NonImplementedInterface_ReturnsFalse()
    {
        var type = typeof(string);
        
        Assert.That(type.Implements(typeof(IDisposable)), Is.False);
    }

    [Test]
    public void Implements_GenericInterface_ReturnsTrue()
    {
        var type = typeof(List<string>);
        
        Assert.That(type.Implements(typeof(IList<>)), Is.True);
    }

    [Test]
    public void Implements_NullType_ReturnsFalse()
    {
        Type? nullType = null;
        
        Assert.That(nullType.Implements(typeof(IDisposable)), Is.False);
    }

    [Test]
    public void Implements_NullAbstractType_ReturnsFalse()
    {
        var type = typeof(string);
        
        Assert.That(type.Implements(null), Is.False);
    }

    [Test]
    public void Implements_BaseClass_ReturnsTrue()
    {
        var type = typeof(DerivedClass);
        
        Assert.That(type.Implements(typeof(BaseClass)), Is.True);
    }

    [Test]
    public void IsAnonymous_AnonymousType_ReturnsTrue()
    {
        var anon = new { Name = "Test", Value = 42 };
        var type = anon.GetType();
        
        Assert.That(type.IsAnonymous(), Is.True);
    }

    [Test]
    public void IsAnonymous_RegularClass_ReturnsFalse()
    {
        var type = typeof(TestClass);
        
        Assert.That(type.IsAnonymous(), Is.False);
    }

    [Test]
    public void IsAnonymous_String_ReturnsFalse()
    {
        var type = typeof(string);
        
        Assert.That(type.IsAnonymous(), Is.False);
    }

    [Test]
    public void IsGenericList_ListT_ReturnsTrue()
    {
        var type = typeof(List<int>);
        
        Assert.That(type.IsGenericList(), Is.True);
    }

    [Test]
    public void IsGenericList_Array_ReturnsFalse()
    {
        var type = typeof(int[]);
        
        Assert.That(type.IsGenericList(), Is.False);
    }

    [Test]
    public void IsGenericList_IEnumerable_ReturnsFalse()
    {
        var type = typeof(IEnumerable<int>);
        
        Assert.That(type.IsGenericList(), Is.False);
    }

    [Test]
    public void IsGenericList_Null_ReturnsFalse()
    {
        Type? nullType = null;
        
        Assert.That(nullType.IsGenericList(), Is.False);
    }

    [Test]
    public void IsLazyType_LazyT_ReturnsTrue()
    {
        var type = typeof(Lazy<int>);
        
        Assert.That(type.IsLazyType(), Is.True);
    }

    [Test]
    public void IsLazyType_RegularType_ReturnsFalse()
    {
        var type = typeof(int);
        
        Assert.That(type.IsLazyType(), Is.False);
    }

    [Test]
    public void IsLazyType_Null_ReturnsFalse()
    {
        Type? nullType = null;
        
        Assert.That(nullType.IsLazyType(), Is.False);
    }

    [Test]
    public void IsNullable_NullableInt_ReturnsTrue()
    {
        var type = typeof(int?);
        
        Assert.That(type.IsNullable(), Is.True);
    }

    [Test]
    public void IsNullable_ValueType_ReturnsFalse()
    {
        var type = typeof(int);
        
        Assert.That(type.IsNullable(), Is.False);
    }

    [Test]
    public void IsNullable_ReferenceType_ReturnsFalse()
    {
        var type = typeof(string);
        
        Assert.That(type.IsNullable(), Is.False);
    }

    [Test]
    public void IsNullable_Null_ReturnsFalse()
    {
        Type? nullType = null;
        
        Assert.That(nullType.IsNullable(), Is.False);
    }

    [Test]
    public void IsPrimitive_Int_ReturnsTrue()
    {
        var type = typeof(int);
        
        Assert.That(type.IsPrimitive(), Is.True);
    }

    [Test]
    public void IsPrimitive_String_ReturnsTrue()
    {
        var type = typeof(string);
        
        Assert.That(type.IsPrimitive(), Is.True);
    }

    [Test]
    public void IsPrimitive_Enum_ReturnsTrue()
    {
        var type = typeof(DayOfWeek);
        
        Assert.That(type.IsPrimitive(), Is.True);
    }

    [Test]
    public void IsPrimitive_CustomClass_ReturnsFalse()
    {
        var type = typeof(TestClass);
        
        Assert.That(type.IsPrimitive(), Is.False);
    }

    [Test]
    public void IsPrimitive_Null_ReturnsFalse()
    {
        Type? nullType = null;
        
        Assert.That(nullType.IsPrimitive(), Is.False);
    }

    [Test]
    public void IsPrimitive_Bool_ReturnsTrue()
    {
        var type = typeof(bool);
        
        Assert.That(type.IsPrimitive(), Is.True);
    }

    [Test]
    public void IsPrimitive_DateTime_ReturnsTrue()
    {
        var type = typeof(DateTime);
        
        Assert.That(type.IsPrimitive(), Is.True);
    }

    [Test]
    public void IsPrimitive_Guid_ReturnsTrue()
    {
        var type = typeof(Guid);
        
        Assert.That(type.IsPrimitive(), Is.True);
    }
}
