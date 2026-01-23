using CoreOne.Extensions;
using CoreOne.Models;
using System.ComponentModel.DataAnnotations;
using DataAnnotationsRange = System.ComponentModel.DataAnnotations.RangeAttribute;

namespace Tests.Extensions;

public class ModelExtensionsTests
{
    private class ValidModel
    {
        [Required]
        public string Name { get; set; } = "Test";

        [DataAnnotationsRange(1, 100)]
        public int Age { get; set; } = 25;
    }

    private class InvalidModel
    {
        [Required]
        public string? Name { get; set; }

        [DataAnnotationsRange(1, 100)]
        public int Age { get; set; } = 150;
    }

    private class NestedModel
    {
        [Required]
        public string? ParentName { get; set; }

        public ValidModel? Child { get; set; }
    }

    private class CollectionModel
    {
        public List<ValidModel> Items { get; set; } = [];
    }

    private class ValidatableModel : IValidatableObject
    {
        public string? Name { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Name))
                yield return new ValidationResult("Name is required", [nameof(Name)]);
        }
    }

    [Test]
    public void ValidateModel_ValidObject_ReturnsSuccess()
    {
        var model = new ValidModel();
        
        var result = model.ValidateModel(null, false);
        
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateModel_InvalidObject_ReturnsFail()
    {
        var model = new InvalidModel();
        
        var result = model.ValidateModel(null, false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessages, Is.Not.Empty);
        }
    }

    [Test]
    public void ValidateModel_NullObjectRequireInstance_ReturnsFail()
    {
        object? model = null;
        
        var result = model.ValidateModel(null, true);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessages!.Any(e => e.Contains("null")), Is.True);
        }
    }

    [Test]
    public void ValidateModel_NullObjectNoRequireInstance_ReturnsSuccess()
    {
        object? model = null;
        
        var result = model.ValidateModel(null, false);
        
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateModel_NestedObject_ValidatesChildren()
    {
        var model = new NestedModel
        {
            ParentName = "Parent",
            Child = new ValidModel()
        };
        
        var result = model.ValidateModel(null, false);
        
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateModel_NestedInvalidObject_ReturnsErrors()
    {
        var model = new NestedModel
        {
            ParentName = null,
            Child = new ValidModel()
        };
        
        var result = model.ValidateModel(null, false);
        
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateModel_Collection_ValidatesAllItems()
    {
        var model = new CollectionModel
        {
            Items =
            [
                new(),
                new()
            ]
        };
        
        var result = model.ValidateModel(null, false);
        
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateModel_IValidatableObject_CallsValidate()
    {
        var model = new ValidatableModel { Name = null };
        
        var result = model.ValidateModel(null, false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessages!.Any(e => e.Contains("Name")), Is.True);
        }
    }

    [Test]
    public void ValidateModel_IValidatableObjectValid_ReturnsSuccess()
    {
        var model = new ValidatableModel { Name = "Test" };
        
        var result = model.ValidateModel(null, false);
        
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateModel_MultipleErrors_ReturnsAllErrors()
    {
        var model = new InvalidModel { Name = null, Age = 150 };
        
        var result = model.ValidateModel(null, false);
        
        Assert.That(result.ErrorMessages!, Has.Count.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void ValidateModel_EmptyCollection_ReturnsSuccess()
    {
        var model = new CollectionModel();
        
        var result = model.ValidateModel(null, false);
        
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateModel_StringValue_DoesNotThrow()
    {
        var model = "test string";
        
        var result = model.ValidateModel(null, false);
        
        Assert.That(result.IsValid, Is.True);
    }
}
