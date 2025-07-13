using ValidationStore = CoreOne.Collections.DataList<string, string?>;

namespace CoreOne.Models;

public class ModelValidationContext(IServiceProvider? serviceProvider) : IResult
{
    public string? Message { get; set; }
    public virtual ResultType ResultType => Store.Count == 0 ? ResultType.Success : ResultType.Fail;
    public ValidationStore Store { get; } = new(StringComparer.OrdinalIgnoreCase);
    public virtual bool Success => Store.Count == 0;
    protected IServiceProvider? ServiceProvider { get; } = serviceProvider;
    protected HashSet<object> ValidatedModels { get; } = new HashSet<object>(ReferenceEqualityComparer.Default);

    public ValidationContext CreateValidationContext(object instance)
    {
        ValidatedModels.Add(instance);
        return new ValidationContext(instance, ServiceProvider, new Dictionary<object, object?>());
    }

    public bool HasSeen(object instance) => ValidatedModels.Contains(instance);
}