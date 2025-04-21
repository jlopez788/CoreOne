namespace CoreOne.Extensions;

public static class ModelExtensions
{
    /// <summary>
    /// Validates an entire object tree
    /// </summary>
    /// <param name="model">Model to validate</param>
    /// <param name="services">Service provider</param>
    /// <param name="requireInstance">Check if validation should fail on null model</param>
    /// <returns>True if valid, otherwise false</returns>
    public static MValidationResult ValidateModel(this object? model, IServiceProvider? services, bool requireInstance)
    {
        var formContext = new ModelValidationContext(services);
        if (requireInstance && model is null)
            formContext.Store.Add("", "Model is null");
        if (model is not null)
            formContext = ValidateObject(formContext, model);
        return new MValidationResult(formContext.Store.SelectMany(p => p.Value).ExcludeNullOrEmpty());
    }

    private static ModelValidationContext ValidateObject(ModelValidationContext formContext, object? instance)
    {
        if (instance is null || formContext.HasSeen(instance))
            return formContext;

        if (instance is IEnumerable enumerable and not string)
        {
            foreach (object value in enumerable)
                formContext = ValidateObject(formContext, value);
            return formContext;
        }

        ValidationContext validation;
        var type = instance.GetType();
        if (instance is IValidatableObject validatable)
        {
            validation = formContext.CreateValidationContext(instance);
            var name = type.Name;
            var results = validatable.Validate(validation);
            formContext.Store.AddRange(results.Select(p => p?.ErrorMessage).ExcludeNullOrEmpty(), p => name);
        }

        if (type.IsValueType || type.Assembly == Types.String.Assembly)
            return formContext; // Cannot continue validating value types as reference types... EditContext validation framework doesn't like

        validation = formContext.CreateValidationContext(instance);
        var metadatas = MetaType.GetMetadatas(type, BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
        return metadatas.Aggregate(formContext, (p, meta) => ValidateProperty(p, validation, instance, meta));
    }

    private static ModelValidationContext ValidateProperty(ModelValidationContext formContext, ValidationContext validation, object instance, Metadata metadata)
    {
        if (metadata == Metadata.Empty || IsExcludedFromValidation())
            return formContext;

        object? value = metadata.GetValue(instance);
        var attributes = metadata.GetCustomAttributes<ValidationAttribute>();
        var errors = attributes.Select(p => p.GetValidationResult(value, validation))
            .Select(p => p?.ErrorMessage)
            .ExcludeNullOrEmpty()
            .ToList();

        if (errors.Count > 0)
            formContext.Store.AddRange(errors, p => metadata.Name);

        return ValidateObject(formContext, value);

        bool IsExcludedFromValidation() => metadata.GetCustomAttribute<ExcludeFromValidationAttribute>() is not null;
    }
}