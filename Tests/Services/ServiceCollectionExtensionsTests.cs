
using CoreOne.Attributes;
using CoreOne.Extensions;
using Microsoft.Extensions.DependencyInjection;

// Public service types at namespace level so AddTypesFromAssembly (which filters p.IsPublic) can discover them
namespace Tests.Services;

public interface IScopedService { }

[Service]
public class ScopedService : IScopedService { }

public interface ISingletonService { }

[Service(ServiceLifetime.Singleton)]
public class SingletonService : ISingletonService { }

public interface IMultiInterfaceService { }
public interface ISecondaryInterface { }

[Service]
public class MultiInterfaceService : IMultiInterfaceService, ISecondaryInterface { }

public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddTypesFromAssembly_ScopedService_InterfaceAndConcreteShareScopeInstance()
    {
        var services = new ServiceCollection();
        services.AddTypesFromAssembly<ScopedService>();
        using var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var concrete = scope.ServiceProvider.GetRequiredService<ScopedService>();
        var iface = scope.ServiceProvider.GetRequiredService<IScopedService>();

        Assert.That(concrete, Is.SameAs(iface));
    }

    [Test]
    public void AddTypesFromAssembly_ScopedService_DifferentScopesYieldDifferentInstances()
    {
        var services = new ServiceCollection();
        services.AddTypesFromAssembly<ScopedService>();
        using var provider = services.BuildServiceProvider();

        object instance1, instance2;
        using (var scope1 = provider.CreateScope())
            instance1 = scope1.ServiceProvider.GetRequiredService<ScopedService>();
        using (var scope2 = provider.CreateScope())
            instance2 = scope2.ServiceProvider.GetRequiredService<ScopedService>();

        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void AddTypesFromAssembly_SingletonService_InterfaceAndConcreteShareInstance()
    {
        var services = new ServiceCollection();
        services.AddTypesFromAssembly<SingletonService>();
        using var provider = services.BuildServiceProvider();

        var concrete = provider.GetRequiredService<SingletonService>();
        var iface = provider.GetRequiredService<ISingletonService>();

        Assert.That(concrete, Is.SameAs(iface));
    }

    [Test]
    public void AddTypesFromAssembly_SingletonService_SameInstanceAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddTypesFromAssembly<SingletonService>();
        using var provider = services.BuildServiceProvider();

        ISingletonService instance1, instance2;
        using (var scope1 = provider.CreateScope())
            instance1 = scope1.ServiceProvider.GetRequiredService<ISingletonService>();
        using (var scope2 = provider.CreateScope())
            instance2 = scope2.ServiceProvider.GetRequiredService<ISingletonService>();

        Assert.That(instance1, Is.SameAs(instance2));
    }

    [Test]
    public void AddTypesFromAssembly_MultipleInterfaces_AllShareSameInstance()
    {
        var services = new ServiceCollection();
        services.AddTypesFromAssembly<MultiInterfaceService>();
        using var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var concrete = scope.ServiceProvider.GetRequiredService<MultiInterfaceService>();
        var primary = scope.ServiceProvider.GetRequiredService<IMultiInterfaceService>();
        var secondary = scope.ServiceProvider.GetRequiredService<ISecondaryInterface>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(concrete, Is.SameAs(primary));
            Assert.That(concrete, Is.SameAs(secondary));
        }
    }
}
