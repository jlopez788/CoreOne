namespace CoreOne;

public interface IDebounce
{
    void Invoke();
}

public interface IDebounce<TModel>
{
    void Invoke(TModel model);
}