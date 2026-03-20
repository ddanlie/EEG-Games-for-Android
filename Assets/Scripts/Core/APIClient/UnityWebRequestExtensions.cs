using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.Networking;

public static class UnityWebRequestExtensions
{
    public static TaskAwaiter GetAwaiter(this UnityWebRequestAsyncOperation operation)
    {
        var tcs = new TaskCompletionSource<object>();
        operation.completed += _ => tcs.SetResult(null);
        return ((Task)tcs.Task).GetAwaiter();
    }
}