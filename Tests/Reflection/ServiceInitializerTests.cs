using CoreOne.Attributes;
using CoreOne.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Reflection;

public class ServiceInitializerTests
{
    public interface ITestService
    {
        string Name { get; }
    }

    public class TestServiceImpl : ITestService
    {
        public string Name => "Test";
    }

    private class TestClass
    {
        [Service]
        public ITestService? TestService { get; set; }

        [Service(Optional = true)]
        public ILogger? OptionalLogger { get; set; }

        public string? NoAttributeProperty { get; set; }
    }

    private class TestClassWithBackingField
    {
        [Service]
        public ITestService? TestService { get; set; }
    }

    private class TestClassWithDefaultService
    {
        [Service(DefaultServiceType = typeof(TestServiceImpl))]
        public ITestService? TestService { get; set; }
    }

    [Test]
    public void Initialize_NullServices_DoesNotThrow()
    {
        var instance = new TestClass();
        
        Assert.DoesNotThrow(() => ServiceInitializer.Initialize(instance, null));
    }

    [Test]
    public void Initialize_InjectsService()
    {
        var services = new ServiceCollection()
            .AddSingleton<ITestService, TestServiceImpl>()
            .BuildServiceProvider();
        var instance = new TestClass();
        
        ServiceInitializer.Initialize(instance, services);
        
        Assert.That(instance.TestService, Is.Not.Null);
        Assert.That(instance.TestService!.Name, Is.EqualTo("Test"));
    }

    [Test]
    public void Initialize_OptionalService_SetsNullWhenNotRegistered()
    {
        var services = new ServiceCollection()
            .AddSingleton<ITestService, TestServiceImpl>()
            .BuildServiceProvider();
        var instance = new TestClass();
        
        ServiceInitializer.Initialize(instance, services);
        
        Assert.That(instance.OptionalLogger, Is.Null);
    }

    [Test]
    public void Initialize_IgnoresPropertiesWithoutAttribute()
    {
        var services = new ServiceCollection()
            .AddSingleton<ITestService, TestServiceImpl>()
            .BuildServiceProvider();
        var instance = new TestClass { NoAttributeProperty = "Original" };
        
        ServiceInitializer.Initialize(instance, services);
        
        Assert.That(instance.NoAttributeProperty, Is.EqualTo("Original"));
    }

    [Test]
    public void Initialize_WithBackingField_SetsValue()
    {
        var services = new ServiceCollection()
            .AddSingleton<ITestService, TestServiceImpl>()
            .BuildServiceProvider();
        var instance = new TestClassWithBackingField();
        
        ServiceInitializer.Initialize(instance, services);
        
        Assert.That(instance.TestService, Is.Not.Null);
    }

    [Test]
    public void Initialize_WithDefaultServiceType_UsesDefault()
    {
        var services = new ServiceCollection()
            .AddSingleton<TestServiceImpl>()
            .BuildServiceProvider();
        var instance = new TestClassWithDefaultService();
        
        ServiceInitializer.Initialize(instance, services);
        
        Assert.That(instance.TestService, Is.Not.Null);
    }

    [Test]
    public void Initialize_MultipleProperties_InjectsAll()
    {
        var mockLogger = new Mock<ILogger>();
        var services = new ServiceCollection()
            .AddSingleton<ITestService, TestServiceImpl>()
            .AddSingleton<ILogger>(mockLogger.Object)
            .BuildServiceProvider();
        var instance = new TestClass();
        
        ServiceInitializer.Initialize(instance, services);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(instance.TestService, Is.Not.Null);
            Assert.That(instance.OptionalLogger, Is.Not.Null);
        }
    }

    [Test]
    public void Initialize_ServiceNotRegistered_LeavesPropertyNull()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var instance = new TestClass();
        
        ServiceInitializer.Initialize(instance, services);
        
        Assert.That(instance.TestService, Is.Null);
    }

    [Test]
    public void Initialize_EmptyServiceProvider_DoesNotThrow()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var instance = new TestClass();
        
        Assert.DoesNotThrow(() => ServiceInitializer.Initialize(instance, services));
    }
}
