using CoreOne.Models;
using CoreOne.Results;
using System.ComponentModel.DataAnnotations;
using Moq;
using DataAnnotationsRange = System.ComponentModel.DataAnnotations.RangeAttribute;

namespace Tests.Models;

[TestFixture]
public class ModelValidationContextTests
{
    private class TestModel
    {
        [Required]
        public string? Name { get; set; }

        [DataAnnotationsRange(1, 100)]
        public int Age { get; set; }
    }

    private class NestedModel
    {
        public TestModel? Child { get; set; }
    }

    [Test]
    public void Constructor_WithNullServiceProvider_InitializesCorrectly()
    {
        var context = new ModelValidationContext(null);

        Assert.Multiple(() => {
            Assert.That(context.Store, Is.Not.Null);
            Assert.That(context.Store, Is.Empty);
            Assert.That(context.Success, Is.True);
            Assert.That(context.ResultType, Is.EqualTo(ResultType.Success));
        });
    }

    [Test]
    public void Constructor_WithServiceProvider_StoresProvider()
    {
        var services = new Mock<IServiceProvider>();
        var testModel = new TestModel();

        var context = new ModelValidationContext(services.Object);

        // ServiceProvider is protected, verify it's used by creating a ValidationContext
        var validationContext = context.CreateValidationContext(testModel);

        // Verify the ValidationContext was created (ServiceProvider is internal to ValidationContext)
        Assert.That(validationContext, Is.Not.Null);
        Assert.That(validationContext.ObjectInstance, Is.SameAs(testModel));
    }

    [Test]
    public void Success_WhenNoErrors_ReturnsTrue()
    {
        var context = new ModelValidationContext(null);

        Assert.That(context.Success, Is.True);
    }

    [Test]
    public void Success_WhenErrorsExist_ReturnsFalse()
    {
        var context = new ModelValidationContext(null);
        context.Store.Add("Name", "Name is required");

        Assert.That(context.Success, Is.False);
    }

    [Test]
    public void ResultType_WhenNoErrors_ReturnsSuccess()
    {
        var context = new ModelValidationContext(null);

        Assert.That(context.ResultType, Is.EqualTo(ResultType.Success));
    }

    [Test]
    public void ResultType_WhenErrorsExist_ReturnsFail()
    {
        var context = new ModelValidationContext(null);
        context.Store.Add("Age", "Age must be between 1 and 100");

        Assert.That(context.ResultType, Is.EqualTo(ResultType.Fail));
    }

    [Test]
    public void Store_IsNotNull()
    {
        var context = new ModelValidationContext(null);

        Assert.That(context.Store, Is.Not.Null);
    }

    [Test]
    public void Store_IsCaseInsensitive()
    {
        var context = new ModelValidationContext(null);
        context.Store.Add("Name", "Error 1");
        context.Store.Add("NAME", "Error 2");

        // Case insensitive comparison means both entries share the same key
        var nameErrors = context.Store["name"];
        Assert.That(nameErrors, Is.Not.Null);
        Assert.That(nameErrors!, Is.Not.Empty);
    }

    [Test]
    public void CreateValidationContext_WithInstance_ReturnsValidationContext()
    {
        var context = new ModelValidationContext(null);
        var model = new TestModel { Name = "Test" };

        var validationContext = context.CreateValidationContext(model);

        Assert.Multiple(() => {
            Assert.That(validationContext, Is.Not.Null);
            Assert.That(validationContext.ObjectInstance, Is.SameAs(model));
            Assert.That(validationContext.Items, Is.Not.Null);
        });
    }

    [Test]
    public void CreateValidationContext_AddsInstanceToValidatedModels()
    {
        var context = new ModelValidationContext(null);
        var model = new TestModel();

        context.CreateValidationContext(model);

        Assert.That(context.HasSeen(model), Is.True);
    }

    [Test]
    public void HasSeen_WithUnseenInstance_ReturnsFalse()
    {
        var context = new ModelValidationContext(null);
        var model = new TestModel();

        Assert.That(context.HasSeen(model), Is.False);
    }

    [Test]
    public void HasSeen_WithSeenInstance_ReturnsTrue()
    {
        var context = new ModelValidationContext(null);
        var model = new TestModel();

        context.CreateValidationContext(model);

        Assert.That(context.HasSeen(model), Is.True);
    }

    [Test]
    public void HasSeen_WithDifferentInstances_TracksSeparately()
    {
        var context = new ModelValidationContext(null);
        var model1 = new TestModel { Name = "Test1" };
        var model2 = new TestModel { Name = "Test2" };

        context.CreateValidationContext(model1);

        Assert.Multiple(() => {
            Assert.That(context.HasSeen(model1), Is.True);
            Assert.That(context.HasSeen(model2), Is.False);
        });
    }

    [Test]
    public void HasSeen_WithSameInstanceMultipleTimes_ReturnsTrue()
    {
        var context = new ModelValidationContext(null);
        var model = new TestModel();

        context.CreateValidationContext(model);
        context.CreateValidationContext(model);

        Assert.That(context.HasSeen(model), Is.True);
    }

    [Test]
    public void ValidatedModels_UsesReferenceEquality()
    {
        var context = new ModelValidationContext(null);
        var model1 = new TestModel { Name = "Test", Age = 25 };
        var model2 = new TestModel { Name = "Test", Age = 25 }; // Same values, different instance

        context.CreateValidationContext(model1);

        Assert.Multiple(() => {
            Assert.That(context.HasSeen(model1), Is.True);
            Assert.That(context.HasSeen(model2), Is.False);
        });
    }

    [Test]
    public void Message_CanBeSetAndRetrieved()
    {
        var context = new ModelValidationContext(null) {
            Message = "Validation failed"
        };

        Assert.That(context.Message, Is.EqualTo("Validation failed"));
    }

    [Test]
    public void Message_DefaultsToNull()
    {
        var context = new ModelValidationContext(null);

        Assert.That(context.Message, Is.Null);
    }

    [Test]
    public void Store_CanStoreMultipleErrorsForSameField()
    {
        var context = new ModelValidationContext(null);

        context.Store.Add("Email", "Email is required");
        context.Store.Add("Email", "Email format is invalid");

        var emailErrors = context.Store["Email"];
        Assert.That(emailErrors, Has.Count.EqualTo(2));
    }

    [Test]
    public void Store_CanStoreErrorsForDifferentFields()
    {
        var context = new ModelValidationContext(null);

        context.Store.Add("Name", "Name is required");
        context.Store.Add("Age", "Age must be positive");
        context.Store.Add("Email", "Email is invalid");

        Assert.Multiple(() => {
            Assert.That(context.Store, Has.Count.EqualTo(3));
            Assert.That(context.Success, Is.False);
            Assert.That(context.ResultType, Is.EqualTo(ResultType.Fail));
        });
    }

    [Test]
    public void CreateValidationContext_WithNestedModel_TracksCorrectly()
    {
        var context = new ModelValidationContext(null);
        var nested = new NestedModel { Child = new TestModel { Name = "Child" } };

        context.CreateValidationContext(nested);
        context.CreateValidationContext(nested.Child!);

        Assert.Multiple(() => {
            Assert.That(context.HasSeen(nested), Is.True);
            Assert.That(context.HasSeen(nested.Child), Is.True);
        });
    }

    [Test]
    public void IResult_Implementation_ReturnsCorrectValues()
    {
        var context = new ModelValidationContext(null);
        context.Store.Add("Field", "Error");
        context.Message = "Validation errors occurred";

        IResult result = context;
        Assert.Multiple(() => {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Fail));
            Assert.That(result.Message, Is.EqualTo("Validation errors occurred"));
        });
    }

    [Test]
    public void CreateValidationContext_CreatesNewDictionaryForItems()
    {
        var context = new ModelValidationContext(null);
        var model1 = new TestModel();
        var model2 = new TestModel();

        var validationContext1 = context.CreateValidationContext(model1);
        var validationContext2 = context.CreateValidationContext(model2);

        Assert.That(validationContext1.Items, Is.Not.SameAs(validationContext2.Items));
    }
}
