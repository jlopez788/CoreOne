using CoreOne;
using CoreOne.Results;

namespace Tests;

public class TypesTests
{
    private enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    [Test]
    public void Parse_Int_ReturnsSuccess()
    {
        var result = Types.Parse<int>("42");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(42));
        }
    }

    [Test]
    public void Parse_Bool_ReturnsSuccess()
    {
        var result = Types.Parse<bool>("true");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.True);
        }
    }

    [Test]
    public void Parse_DateTime_ReturnsSuccess()
    {
        var result = Types.Parse<DateTime>("12/25/2024");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model.Year, Is.EqualTo(2024));
            Assert.That(result.Model.Month, Is.EqualTo(12));
            Assert.That(result.Model.Day, Is.EqualTo(25));
        }
    }

    [Test]
    public void Parse_Guid_ReturnsSuccess()
    {
        var guid = Guid.NewGuid();
        var result = Types.Parse<Guid>(guid.ToString());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(guid));
        }
    }

    [Test]
    public void Parse_Decimal_ReturnsSuccess()
    {
        var result = Types.Parse<decimal>("123.45");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(123.45m));
        }
    }

    [Test]
    public void Parse_Double_ReturnsSuccess()
    {
        var result = Types.Parse<double>("123.45");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(123.45));
        }
    }

    [Test]
    public void Parse_Float_ReturnsSuccess()
    {
        var result = Types.Parse<float>("123.45");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(123.45f).Within(0.01));
        }
    }

    [Test]
    public void Parse_InvalidValue_ReturnsFail()
    {
        var result = Types.Parse<int>("not a number");
        
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void Parse_NullValue_ReturnsFail()
    {
        var result = Types.Parse<int>(null);
        
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void Parse_AlreadyCorrectType_ReturnsSuccess()
    {
        var result = Types.Parse<int>(42);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(42));
        }
    }

    [Test]
    public void ParseEnum_ValidValue_ReturnsSuccess()
    {
        var result = Types.ParseEnum<TestEnum>("Value1");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(TestEnum.Value1));
        }
    }

    [Test]
    public void ParseEnum_InvalidValue_ReturnsFail()
    {
        var result = Types.ParseEnum<TestEnum>("InvalidValue");
        
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void ParseEnum_NullValue_ReturnsFail()
    {
        var result = Types.ParseEnum<TestEnum>(null);
        
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void ParseEnum_CaseInsensitive_ReturnsSuccess()
    {
        var result = Types.ParseEnum<TestEnum>("value2");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(TestEnum.Value2));
        }
    }

    [Test]
    public void ParseEnum_AlreadyCorrectType_ReturnsSuccess()
    {
        var result = Types.ParseEnum<TestEnum>(TestEnum.Value3);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(TestEnum.Value3));
        }
    }

    [Test]
    public void Parse_WithType_Int_ReturnsSuccess()
    {
        var result = Types.Parse(typeof(int), "42");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(42));
        }
    }

    [Test]
    public void Parse_WithType_Enum_ReturnsSuccess()
    {
        var result = Types.Parse(typeof(TestEnum), "Value1");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(TestEnum.Value1));
        }
    }

    [Test]
    public void Parse_WithType_NullType_ReturnsFail()
    {
        var result = Types.Parse(null, "42");
        
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void Parse_WithType_NullValue_ReturnsFail()
    {
        var result = Types.Parse(typeof(int), null);
        
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void TryParse_ValidInt_ReturnsTrue()
    {
        var success = Types.TryParse<int>("42", out var value);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(value, Is.EqualTo(42));
        }
    }

    [Test]
    public void TryParse_InvalidInt_ReturnsFalse()
    {
        var success = Types.TryParse<int>("not a number", out _);
        
        Assert.That(success, Is.False);
    }

    [Test]
    public void TryParse_NullValue_ReturnsFalse()
    {
        var success = Types.TryParse<int>(null, out _);
        
        Assert.That(success, Is.False);
    }

    [Test]
    public void TryParseEnum_ValidValue_ReturnsTrue()
    {
        var success = Types.TryParseEnum<TestEnum>("Value1", out var value);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(value, Is.EqualTo(TestEnum.Value1));
        }
    }

    [Test]
    public void TryParseEnum_InvalidValue_ReturnsFalse()
    {
        var success = Types.TryParseEnum<TestEnum>("InvalidValue", out _);
        
        Assert.That(success, Is.False);
    }

    [Test]
    public void TryParseEnum_NullValue_ReturnsFalse()
    {
        var success = Types.TryParseEnum<TestEnum>(null, out _);
        
        Assert.That(success, Is.False);
    }

    [Test]
    public void TryParseEnum_CaseInsensitive_ReturnsTrue()
    {
        var success = Types.TryParseEnum<TestEnum>("value2", out var value);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(value, Is.EqualTo(TestEnum.Value2));
        }
    }

    [Test]
    public void TryParseEnum_WithType_ValidValue_ReturnsTrue()
    {
        var success = Types.TryParseEnum<TestEnum>("Value3", out var value);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(value, Is.EqualTo(TestEnum.Value3));
        }
    }

    [Test]
    public void IsNumberType_Int_ReturnsTrue()
    {
        Assert.That(Types.IsNumberType(typeof(int)), Is.True);
    }

    [Test]
    public void IsNumberType_Double_ReturnsTrue()
    {
        Assert.That(Types.IsNumberType(typeof(double)), Is.True);
    }

    [Test]
    public void IsNumberType_Decimal_ReturnsTrue()
    {
        Assert.That(Types.IsNumberType(typeof(decimal)), Is.True);
    }

    [Test]
    public void IsNumberType_String_ReturnsFalse()
    {
        Assert.That(Types.IsNumberType(typeof(string)), Is.False);
    }

    [Test]
    public void IsNumberType_Null_ReturnsFalse()
    {
        Assert.That(Types.IsNumberType(null), Is.False);
    }

    [Test]
    public void IsNumberType_NullableInt_ReturnsTrue()
    {
        Assert.That(Types.IsNumberType(typeof(int?)), Is.True);
    }

    [Test]
    public void IsNumberType_AllNumericTypes_ReturnsTrue()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Types.IsNumberType(typeof(long)), Is.True);
            Assert.That(Types.IsNumberType(typeof(short)), Is.True);
            Assert.That(Types.IsNumberType(typeof(byte)), Is.True);
            Assert.That(Types.IsNumberType(typeof(sbyte)), Is.True);
            Assert.That(Types.IsNumberType(typeof(ulong)), Is.True);
            Assert.That(Types.IsNumberType(typeof(ushort)), Is.True);
            Assert.That(Types.IsNumberType(typeof(uint)), Is.True);
            Assert.That(Types.IsNumberType(typeof(float)), Is.True);
        }
    }

    [Test]
    public void GetBasicType_NullableInt_ReturnsInt()
    {
        var result = Types.GetBasicType(typeof(int?));
        
        Assert.That(result, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void GetBasicType_RegularType_ReturnsSameType()
    {
        var result = Types.GetBasicType(typeof(int));
        
        Assert.That(result, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void GetBasicType_NullableGuid_ReturnsGuid()
    {
        var result = Types.GetBasicType(typeof(Guid?));
        
        Assert.That(result, Is.EqualTo(typeof(Guid)));
    }

    [Test]
    public void StaticTypeConstants_AreCorrect()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Types.Void, Is.EqualTo(typeof(void)));
            Assert.That(Types.Object, Is.EqualTo(typeof(object)));
            Assert.That(Types.String, Is.EqualTo(typeof(string)));
            Assert.That(Types.Int, Is.EqualTo(typeof(int)));
            Assert.That(Types.Bool, Is.EqualTo(typeof(bool)));
            Assert.That(Types.Guid, Is.EqualTo(typeof(Guid)));
            Assert.That(Types.DateTime, Is.EqualTo(typeof(DateTime)));
        }
    }

    [Test]
    public void TryParseBoolean_TrueVariants_ReturnsTrue()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Types.TryParse<bool>("1", out var v1) && v1, Is.True);
            Assert.That(Types.TryParse<bool>("true", out var v2) && v2, Is.True);
            Assert.That(Types.TryParse<bool>("yes", out var v3) && v3, Is.True);
            Assert.That(Types.TryParse<bool>("t", out var v4) && v4, Is.True);
            Assert.That(Types.TryParse<bool>("y", out var v5) && v5, Is.True);
        }
    }

    [Test]
    public void TryParseBoolean_FalseVariants_ReturnsFalse()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Types.TryParse<bool>("0", out var v1) && !v1, Is.True);
            Assert.That(Types.TryParse<bool>("false", out var v2) && !v2, Is.True);
            Assert.That(Types.TryParse<bool>("no", out var v3) && !v3, Is.True);
            Assert.That(Types.TryParse<bool>("n", out var v4) && !v4, Is.True);
            Assert.That(Types.TryParse<bool>("f", out var v5) && !v5, Is.True);
        }
    }

    [Test]
    public void TryParseDateTime_VariousFormats_ReturnsTrue()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Types.TryParse<DateTime>("12/25/2024", out _), Is.True);

            Assert.That(Types.TryParse<DateTime>("12-25-2024", out _), Is.True);

            Assert.That(Types.TryParse<DateTime>("12.25.2024", out _), Is.True);
        }
    }

    [Test]
    public void Parse_Long_ReturnsSuccess()
    {
        var result = Types.Parse<long>("9223372036854775807");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(long.MaxValue));
        }
    }

    [Test]
    public void Parse_Short_ReturnsSuccess()
    {
        var result = Types.Parse<short>("32767");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(short.MaxValue));
        }
    }

    [Test]
    public void Parse_Byte_ReturnsSuccess()
    {
        var result = Types.Parse<byte>("255");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(byte.MaxValue));
        }
    }

    [Test]
    public void Parse_TimeSpan_ReturnsSuccess()
    {
        var result = Types.Parse<TimeSpan>("01:30:00");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model.Hours, Is.EqualTo(1));
            Assert.That(result.Model.Minutes, Is.EqualTo(30));
        }
    }

    [Test]
    public void DotNetTypes_ContainsExpectedTypes()
    {
        var dotNetTypes = Types.DotNetTypes;
        
        using (Assert.EnterMultipleScope())
        {
            Assert.That(dotNetTypes, Does.Contain(typeof(int)));
            Assert.That(dotNetTypes, Does.Contain(typeof(bool)));
            Assert.That(dotNetTypes, Does.Contain(typeof(string)));
            Assert.That(dotNetTypes, Does.Contain(typeof(DateTime)));
            Assert.That(dotNetTypes, Does.Contain(typeof(Guid)));
        }
    }
}
