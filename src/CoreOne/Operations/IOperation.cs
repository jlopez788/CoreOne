namespace CoreOne.Operations;

public interface IOperation
{
    string Field { get; }

    bool Apply(List<IOperation> operations);
}