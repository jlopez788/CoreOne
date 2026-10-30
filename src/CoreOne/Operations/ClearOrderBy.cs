namespace CoreOne.Operations;

public class ClearOrderBy : IOperation
{
    public static readonly IOperation Default = new ClearOrderBy();
    public string Field { get; } = string.Empty;

    public bool Apply(List<IOperation> operations)
    {
        operations.RemoveAll(p => p is OrderBy);
        return true;
    }
}