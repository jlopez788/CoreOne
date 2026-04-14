using CoreOne.Cryptography;

namespace Tests.Services;

[TestFixture]
public class CryptServiceTests
{
    private readonly CryptKey _key = new("myPasswordSalt");
    private CypherService _service;

    [SetUp]
    public void InitializeService()
    {
        _service = new CypherService(_key);
    }

    [Test]
    public void TestEncryptDecrypt()
    {
        var originalText = "Hello, World!";
        var encrypted = _service.Encrypt(originalText);
        var decrypted = _service.Decrypt(encrypted);
        Assert.That(decrypted.Model, Is.EqualTo(originalText));
    }

    [Test]
    public void TestExpiration()
    {
        var originalText = "Hello, World!";
        var encrypted = _service.Encrypt(originalText, DateTime.UtcNow.AddMinutes(-15));
        var decrypted = _service.Decrypt(encrypted);

        Assert.Multiple(() => {
            Assert.That(decrypted.Success, Is.False);
            Assert.That(decrypted.StatusCode, Is.EqualTo(DecryptionStatus.Expired));
        });
    }

    [Test]
    public void TestFutureExpireContent()
    {
        var originalText = "Hello, World!";
        var encrypted = _service.Encrypt(originalText, DateTime.UtcNow.AddMinutes(15));
        var decrypted = _service.Decrypt(encrypted);
        Assert.Multiple(() => {
            Assert.That(decrypted.Success, Is.True);
            Assert.That(decrypted.Model, Is.EqualTo(originalText));
        });
    }

    [Test]
    public void TestModifiedContent()
    {
        var originalText = "Hello, World!";
        var encrypted = _service.Encrypt(originalText);
        // Modify the encrypted content
        var modifiedEncrypted = encrypted + "tampered";
        var decrypted = _service.Decrypt(modifiedEncrypted);
        Assert.Multiple(() => {
            Assert.That(decrypted.Success, Is.False);
            Assert.That(decrypted.StatusCode, Is.EqualTo(DecryptionStatus.InvalidData));
        });
    }
    [Test]
    public void TestModifiedContent2()
    {
        var originalText = "Hello, World!";
        var encrypted = _service.Encrypt(originalText);
        // Modify the encrypted content
        var modifiedEncrypted = "ab" + encrypted;
        var decrypted = _service.Decrypt(modifiedEncrypted);
        Assert.Multiple(() => {
            Assert.That(decrypted.Success, Is.False);
            Assert.That(decrypted.StatusCode, Is.EqualTo(DecryptionStatus.InvalidData));
        });
    }
}