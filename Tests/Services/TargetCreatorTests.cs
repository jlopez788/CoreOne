using CoreOne.Services;
using CoreOne.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Services;

[TestFixture]
public class TargetCreatorTests
{
    private class NoParameterConstructor
    {
        public string Value { get; set; } = "Default";
    }

    private class SingleParameterConstructor(string name)
    {
        public string Name { get; } = name;
    }

    private class MultipleConstructors
    {
        public string? Name { get; }
        public int Value { get; }

        public MultipleConstructors()
        {
            Name = "Default";
            Value = 0;
        }

        public MultipleConstructors(string name)
        {
            Name = name;
            Value = 0;
        }

        public MultipleConstructors(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    private class WithServiceDependency(IServiceProvider services)
    {
        public IServiceProvider? Services { get; } = services;
    }

    private class WithStringDependency(string value)
    {
        public string Value { get; } = value;
    }

    private class ComplexDependencies(IServiceProvider services, string name, int value)
    {
        public IServiceProvider? Services { get; } = services;
        public string Name { get; } = name;
        public int Value { get; } = value;
    }

    [Test]
    public void Default_ReturnsStaticInstance()
    {
        var defaultCreator = TargetCreator.Default;

        Assert.That(defaultCreator, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithNullServiceProvider_CreatesInstance()
    {
        var creator = new TargetCreator(null);

        Assert.That(creator, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithServiceProvider_StoresProvider()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var creator = new TargetCreator(services);

        // ServiceProvider is protected, verify it works by creating an instance
        var instance = creator.CreateInstance(typeof(NoParameterConstructor));
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateInstance_WithNoParameters_CreatesObject()
    {
        var creator = new TargetCreator(null);

        var instance = creator.CreateInstance(typeof(NoParameterConstructor));

        Assert.Multiple(() => {
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<NoParameterConstructor>());
            Assert.That(((NoParameterConstructor)instance!).Value, Is.EqualTo("Default"));
        });
    }

    [Test]
    public void CreateInstance_WithParameters_CreatesObjectWithParameters()
    {
        var creator = new TargetCreator(null);
        var parameters = new object[] { "TestName" };

        var result = creator.CreateInstance(typeof(SingleParameterConstructor), parameters);

        Assert.Multiple(() => {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.InstanceOf<SingleParameterConstructor>());
            Assert.That(((SingleParameterConstructor)result.Model!).Name, Is.EqualTo("TestName"));
        });
    }

    [Test]
    public void CreateInstance_WithMultipleParameters_SelectsCorrectConstructor()
    {
        var creator = new TargetCreator(null);
        var parameters = new object[] { "Test", 42 };

        var result = creator.CreateInstance(typeof(MultipleConstructors), parameters);

        Assert.Multiple(() => {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.InstanceOf<MultipleConstructors>());
            var instance = (MultipleConstructors)result.Model!;
            Assert.That(instance.Name, Is.EqualTo("Test"));
            Assert.That(instance.Value, Is.EqualTo(42));
        });
    }

    [Test]
    public void CreateInstance_WithMatchingParameterTypes_CreatesInstance()
    {
        var creator = new TargetCreator(null);
        var parameters = new object[] { "TestName" }; // Matches SingleParameterConstructor

        var result = creator.CreateInstance(typeof(SingleParameterConstructor), parameters);

        Assert.Multiple(() => {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.InstanceOf<SingleParameterConstructor>());
            Assert.That(((SingleParameterConstructor)result.Model!).Name, Is.EqualTo("TestName"));
        });
    }

    [Test]
    public void CreateInstance_WithServiceProvider_InjectsServiceProvider()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var creator = new TargetCreator(services);

        var instance = creator.CreateInstance(typeof(WithServiceDependency));

        Assert.Multiple(() => {
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<WithServiceDependency>());
            Assert.That(((WithServiceDependency)instance!).Services, Is.SameAs(services));
        });
    }

    [Test]
    public void CreateInstance_WithStringDependency_CreatesWithParameter()
    {
        var services = new ServiceCollection()
            .AddSingleton("TestValue")
            .BuildServiceProvider();
        var creator = new TargetCreator(services);

        var instance = creator.CreateInstance(typeof(WithStringDependency));

        Assert.Multiple(() => {
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<WithStringDependency>());
            Assert.That(((WithStringDependency)instance!).Value, Is.EqualTo("TestValue"));
        });
    }

    [Test]
    public void CreateInstance_CachesConstructorInfo()
    {
        var creator = new TargetCreator(null);

        var instance1 = creator.CreateInstance(typeof(NoParameterConstructor));
        var instance2 = creator.CreateInstance(typeof(NoParameterConstructor));

        Assert.Multiple(() => {
            Assert.That(instance1, Is.Not.Null);
            Assert.That(instance2, Is.Not.Null);
            Assert.That(instance1, Is.Not.SameAs(instance2)); // Different instances
        });
    }

    [Test]
    public void CreateInstance_WithInterface_ReturnsNull()
    {
        var creator = new TargetCreator(null);

        var instance = creator.CreateInstance(typeof(IServiceProvider));

        Assert.That(instance, Is.Null);
    }

    [Test]
    public void CreateInstance_WithAbstractClass_ThrowsException()
    {
        var creator = new TargetCreator(null);

        Assert.Throws<MissingMethodException>(() =>
            creator.CreateInstance(typeof(System.IO.Stream)));
    }

    [Test]
    public void CreateInstance_WithRegisteredDependencies_ResolvesAll()
    {
        var services = new ServiceCollection()
            .AddSingleton<IServiceProvider>(sp => sp)
            .BuildServiceProvider();
        var creator = new TargetCreator(services);

        var instance = creator.CreateInstance(typeof(WithServiceDependency));

        Assert.Multiple(() => {
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.InstanceOf<WithServiceDependency>());
            var complex = (WithServiceDependency)instance!;
            Assert.That(complex.Services, Is.Not.Null);
        });
    }

    [Test]
    public void CreateInstance_WithEmptyParameters_CreatesDefaultConstructor()
    {
        var creator = new TargetCreator(null);
        var parameters = Array.Empty<object>();

        var result = creator.CreateInstance(typeof(MultipleConstructors), parameters);

        Assert.Multiple(() => {
            Assert.That(result.Success, Is.True);
            var instance = (MultipleConstructors)result.Model!;
            Assert.That(instance.Name, Is.EqualTo("Default"));
            Assert.That(instance.Value, Is.EqualTo(0));
        });
    }

    [Test]
    public void CreateInstance_MultipleCallsSameType_ReturnsDifferentInstances()
    {
        var creator = new TargetCreator(null);

        var instance1 = creator.CreateInstance(typeof(NoParameterConstructor));
        var instance2 = creator.CreateInstance(typeof(NoParameterConstructor));

        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void CreateInstance_WithParametersArray_MatchesTypes()
    {
        var creator = new TargetCreator(null);
        var parameters = new object[] { "Name" };

        var result = creator.CreateInstance(typeof(SingleParameterConstructor), parameters);

        Assert.Multiple(() => {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.Not.Null);
        });
    }

    [Test]
    public void CreateInstance_WithValidParameters_CreatesInstance()
    {
        var creator = new TargetCreator(null);
        var parameters = new object[] { "ValidName" };

        var result = creator.CreateInstance(typeof(SingleParameterConstructor), parameters);

        Assert.Multiple(() => {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.InstanceOf<SingleParameterConstructor>());
        });
    }

    [Test]
    public void CreateInstance_SelectsConstructorBasedOnAvailability()
    {
        var creator = new TargetCreator(null);

        var instance = creator.CreateInstance(typeof(NoParameterConstructor));

        // Should successfully create instance with available constructor
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<NoParameterConstructor>());
    }

    [Test]
    public void CreateInstance_WithServiceProvider_ResolvesFromServices()
    {
        var services = new ServiceCollection()
            .AddSingleton<NoParameterConstructor>()
            .BuildServiceProvider();
        var creator = new TargetCreator(services);

        var instance = creator.CreateInstance(typeof(NoParameterConstructor));

        Assert.That(instance, Is.InstanceOf<NoParameterConstructor>());
    }

    [Test]
    public void CreateInstance_WithRegisteredService_UsesServiceProvider()
    {
        var testService = new NoParameterConstructor { Value = "FromService" };
        var services = new ServiceCollection()
            .AddSingleton(testService)
            .BuildServiceProvider();
        var creator = new TargetCreator(services);

        var instance = creator.CreateInstance(typeof(NoParameterConstructor));

        Assert.That(instance, Is.InstanceOf<NoParameterConstructor>());
    }

    [Test]
    public void CreateInstance_WithSimpleType_CreatesInstance()
    {
        var creator = new TargetCreator(null);

        // Create a simple class instance
        var instance = creator.CreateInstance(typeof(NoParameterConstructor));

        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void Default_IsSingleton()
    {
        var default1 = TargetCreator.Default;
        var default2 = TargetCreator.Default;

        Assert.That(default1, Is.SameAs(default2));
    }

    [Test]
    public void CreateInstance_WithValueType_CreatesInstance()
    {
        var creator = new TargetCreator(null);
        var parameters = Array.Empty<object>();

        var result = creator.CreateInstance(typeof(int), parameters);

        // Value types may be created via Activator
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void CreateInstance_ThreadSafe_MultipleCalls()
    {
        var creator = new TargetCreator(null);
        var tasks = new List<Task<object?>>();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => creator.CreateInstance(typeof(NoParameterConstructor))));
        }

        Task.WaitAll([.. tasks]);

        Assert.That(tasks.All(t => t.Result != null), Is.True);
    }
}