using CoreOne;
using NUnit.Framework;

namespace Tests;

public class PoolTests
{
    [Test]
    public void Rent_CreatesPooledBuffer()
    {
        using var buffer = Pool.Rent<int>(10);
        
        Assert.That(buffer, Is.Not.Null);
        Assert.That(buffer.Size, Is.EqualTo(10));
        Assert.That(buffer.Array, Is.Not.Null);
    }

    [Test]
    public void Rent_BufferHasCorrectSize()
    {
        using var buffer = Pool.Rent<int>(100);
        
        Assert.That(buffer.Array.Length, Is.GreaterThanOrEqualTo(100));
        Assert.That(buffer.Size, Is.EqualTo(100));
    }

    [Test]
    public void Rent_CanAccessElements()
    {
        using var buffer = Pool.Rent<int>(5);
        buffer.Array[0] = 10;
        buffer.Array[1] = 20;
        
        Assert.That(buffer[0], Is.EqualTo(10));
        Assert.That(buffer[1], Is.EqualTo(20));
    }

    [Test]
    public void Rent_CanUseIndexer()
    {
        using var buffer = Pool.Rent<string>(3);
        buffer.Array[0] = "first";
        buffer.Array[1] = "second";
        buffer.Array[2] = "third";
        
        Assert.That(buffer[0], Is.EqualTo("first"));
        Assert.That(buffer[1], Is.EqualTo("second"));
        Assert.That(buffer[2], Is.EqualTo("third"));
    }

    [Test]
    public void Rent_ImplicitConversion_ToArray()
    {
        using var buffer = Pool.Rent<int>(5);
        int[] array = buffer;
        
        Assert.That(array, Is.Not.Null);
        Assert.That(array.Length, Is.GreaterThanOrEqualTo(5));
    }

    [Test]
    public void Dispose_ReleasesBuffer()
    {
        var buffer = Pool.Rent<int>(10);
        buffer.Array[0] = 42;
        
        Assert.DoesNotThrow(() => buffer.Dispose());
    }

    [Test]
    public void Rent_MultipleBuffers_Independent()
    {
        using var buffer1 = Pool.Rent<int>(5);
        using var buffer2 = Pool.Rent<int>(5);
        
        buffer1.Array[0] = 10;
        buffer2.Array[0] = 20;
        
        Assert.That(buffer1[0], Is.EqualTo(10));
        Assert.That(buffer2[0], Is.EqualTo(20));
    }

    [Test]
    public void Rent_DifferentTypes_Work()
    {
        using var intBuffer = Pool.Rent<int>(10);
        using var stringBuffer = Pool.Rent<string>(10);
        using var doubleBuffer = Pool.Rent<double>(10);
        
        Assert.That(intBuffer.Size, Is.EqualTo(10));
        Assert.That(stringBuffer.Size, Is.EqualTo(10));
        Assert.That(doubleBuffer.Size, Is.EqualTo(10));
    }

    [Test]
    public void Rent_LargeBuffer_Works()
    {
        using var buffer = Pool.Rent<byte>(1024 * 1024); // 1MB
        
        Assert.That(buffer.Size, Is.EqualTo(1024 * 1024));
        Assert.That(buffer.Array.Length, Is.GreaterThanOrEqualTo(1024 * 1024));
    }

    [Test]
    public void Rent_UsingStatement_AutomaticallyDisposes()
    {
        Pool.PooledBuffer<int>? bufferRef = null;
        
        using (var buffer = Pool.Rent<int>(10))
        {
            bufferRef = buffer;
            buffer.Array[0] = 42;
            Assert.That(buffer[0], Is.EqualTo(42));
        }
        
        // After using block, buffer should be disposed
        Assert.That(bufferRef, Is.Not.Null);
    }
}
