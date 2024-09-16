namespace AsyncUtils;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


/// <summary>
/// Async Multimap--we do not care about the ordering of the underlying values
/// </summary>
/// <typeparam name="TKey">the key type in the dictionary</typeparam>
/// <typeparam name="TValue">the type of the values to store in the mm</typeparam>
public class AsyncMultiMap<TKey, TValue> : IAsyncMultiMap<TKey, TValue>
{
    private readonly ConcurrentDictionary<TKey, ConcurrentBag<TValue>> _dictionary = new();
    
    //private readonly ConcurrentDictionary<TKey, List<TValue>> _dictionary =
    //    new ConcurrentDictionary<TKey, List<TValue>>();

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public async Task AddAsync(TKey key, TValue value)
    {
        await _semaphore.WaitAsync();
        try
        {
            var bag = _dictionary.GetOrAdd(key, _ => new ConcurrentBag<TValue>());
            bag.Add(value); // No need for locking since ConcurrentBag is thread-safe
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> RemoveAsync(TKey key, TValue value)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_dictionary.TryGetValue(key, out var bag))
            {
                var newBag = new ConcurrentBag<TValue>(
                    bag.Where(x => !EqualityComparer<TValue>.Default.Equals(x, value)));

                if (newBag.Count < bag.Count)
                {
                    _dictionary[key] = newBag; // Replace the old bag with the new one
                    if (newBag.Count == 0)
                    {
                        _dictionary.TryRemove(key, out _);
                    }
                    return true;
                }
            }
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<TValue>> GetValuesAsync(TKey key)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_dictionary.TryGetValue(key, out var bag))
            {
                return bag.ToList(); // Convert the ConcurrentBag to a List for the result
            }

            return new List<TValue>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ClearAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _dictionary.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> GetKeyCountAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _dictionary.Count;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> GetValueCountAsync(TKey key)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_dictionary.TryGetValue(key, out var bag))
            {
                return bag.Count;
            }

            return 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async IAsyncEnumerator<KeyValuePair<TKey, TValue>> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            foreach (var kvp in _dictionary)
            {
                foreach (var value in kvp.Value)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return new KeyValuePair<TKey, TValue>(kvp.Key, value);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }    }
}
