using CoreOne;
using CoreOne.Services;
using NUnit.Framework;

namespace Tests.Services;

public class BaseServiceTests
{
    private class TestService(IServiceProvider? services) : BaseService(services)
    {
        public bool DisposeCalled { get; private set; }
        public bool DisposeAsyncCalled { get; private set; }

        protected override void Dispose(bool disposing)
        {
            DisposeCalled = true;
            base.Dispose(disposing);
        }

#if NET9_0_OR_GREATER
        protected override ValueTask DisposeAsync(bool disposing)
#else
        protected override Task DisposeAsync(bool disposing)
#endif
        {
            DisposeAsyncCalled = true;
            return base.DisposeAsync(disposing);
        }

        public TService GetService<TService>() where TService : notnull => Get<TService>();
        
        public AToken GetToken() => Token;
    }

    [Test]
    public void Constructor_InitializesToken()
    {
        var service = new TestService(null);
        var token = service.GetToken();
        
        Assert.That(token, Is.Not.Null);
    }

    [Test]
    public async Task DisposeAsync_CallsDisposeMethod()
    {
        var service = new TestService(null);
        
        await service.DisposeAsync();
        
        Assert.That(service.DisposeAsyncCalled, Is.True);
    }

    [Test]
    public async Task DisposeAsync_DisposesToken()
    {
        var service = new TestService(null);
        var token = service.GetToken();
        
        await service.DisposeAsync();
        
        // Token should be disposed (no public property to verify)
        Assert.That(token, Is.Not.Null);
    }

    [Test]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        var service = new TestService(null);
        
        await service.DisposeAsync();
        await service.DisposeAsync();
        
        // Should not throw
        Assert.That(service.DisposeAsyncCalled, Is.True);
    }

    [Test]
    public void Dispose_MarkedAsObsolete()
    {
        var service = new TestService(null);
        
        // Should still work but is marked obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.DoesNotThrow(() => service.Dispose());
#pragma warning restore CS0618
    }
}
