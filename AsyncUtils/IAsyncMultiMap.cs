
namespace AsyncUtils;

public interface IAsyncMultiMap<TKey, TValue>  : IAsyncEnumerable<KeyValuePair<TKey,TValue>>
{
    Task AddAsync(TKey key, TValue value);
    Task<bool> RemoveAsync(TKey key, TValue value);
    Task<List<TValue>> GetValuesAsync(TKey key);
    Task ClearAsync();
    Task<int> GetKeyCountAsync();
    Task<int> GetValueCountAsync(TKey key);
    
    //IAsyncEnumerable<KeyValuePair<TKey, TValue>> ExplodeAsync([EnumeratorCancellation] CancellationToken cancellationToken);
    //Task<IList<KeyValuePair<TKey, TValue>>> ExplodeAsyncAsTask();
}