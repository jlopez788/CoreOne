namespace CoreOne;

public interface IDebounce<TModel>
{
    void Invoke(TModel model);
}