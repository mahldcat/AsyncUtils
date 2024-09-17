# Purpose
This is to get some quick documentation on where certain beartraps may occur when doing async code execution

## Best Practices
* For CPU-Bound Work: Use Task.Run to offload the work to a background thread.
* For I/O-Bound Work: Make PerformOperation truly asynchronous using await Task.Delay or other asynchronous APIs like file I/O, network calls, etc.
* Avoid Thread.Sleep in Async Code: Use await Task.Delay instead to simulate delays without blocking the thread.

## `Task.Run()` completion
```csharp
private async Task Safe()
{
    await Task.Run(() => PerformOperation());
}
private async Task NotSafe()
{
    await PerformOperation();
}

private Task PerformOperation(){
  Thread.Sleep(1000);
  return Task.CompletedTask;
}
```

## Usage of Task.Delay

### Retry
```csharp
public async Task FetchDataWithRetryAsync()
{
    int maxRetries = 3;
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            // Attempt to fetch data from a remote service
            await FetchDataAsync();
            break; // Break out of the loop if successful
        }
        catch (Exception ex) when (i < maxRetries - 1)
        {
            // Wait for a delay before retrying
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i))); // Exponential backoff
        }
    }
}
```
### Update Poll
```csharp
public async Task PollForUpdatesAsync()
{
    while (true)
    {
        // Check for updates
        var updates = await GetUpdatesAsync();
        if (updates != null)
        {
            ProcessUpdates(updates);
        }

        // Wait for 5 seconds before polling again
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}
```
### Rate Limiting
```csharp
public async Task SendRequestsAsync(IEnumerable<string> requests)
{
    foreach (var request in requests)
    {
        await SendRequestAsync(request);
        
        // Delay to avoid hitting rate limits
        await Task.Delay(1000); // Wait 1 second between requests
    }
}

```

## Multiple calls to await on the same task
### WhenAny
```csharp
private async Task<string> GetFirstAvailableDataAsync()
{
    Task<string> fetchFromService1 = FetchDataFromService1Async();
    Task<string> fetchFromService2 = FetchDataFromService2Async();

    // Await the first task to complete
    Task<string> firstCompletedTask = await Task.WhenAny(fetchFromService1, fetchFromService2);

    // Now, get the result of the first completed task
    string result = await firstCompletedTask;

    // Optionally, log or reuse the result
    Console.WriteLine($"First completed data: {result}");

    // If you want, you can also await the other task later
    if (firstCompletedTask == fetchFromService1)
    {
        string otherResult = await fetchFromService2;
        Console.WriteLine($"Data from the second service: {otherResult}");
    }
    else
    {
        string otherResult = await fetchFromService1;
        Console.WriteLine($"Data from the first service: {otherResult}");
    }

    return result;
}
```

### Memoization
```csharp
private Task<string> _cachedDataTask;

public Task<string> GetDataAsync()
{
    // If the task has not been started yet, start it
    if (_cachedDataTask == null)
    {
        _cachedDataTask = FetchDataFromRemoteServiceAsync();
    }

    // Return the cached task, which can be awaited multiple times
    return _cachedDataTask;
}

private async Task<string> FetchDataFromRemoteServiceAsync()
{
    await Task.Delay(2000); // Simulate a network delay
    return "Data from remote service";
}

public async Task UseDataAsync()
{
    // The first call will trigger the fetch and cache the task
    string data1 = await GetDataAsync();
    Console.WriteLine(data1);

    // The second call will reuse the cached task and return immediately
    string data2 = await GetDataAsync();
    Console.WriteLine(data2);
}
```



