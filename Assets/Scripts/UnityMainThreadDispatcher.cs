using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            var go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    private void Update()
    {
        while (_executionQueue.TryDequeue(out Action action))
        {
            action?.Invoke();
        }
    }

    public void Enqueue(Action action)
    {
        _executionQueue.Enqueue(action);
    }

    // Optional: Method for async/await support
    public async Task EnqueueAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        Enqueue(() =>
        {
            action?.Invoke();
            tcs.SetResult(true);
        });
        await tcs.Task;
    }
}