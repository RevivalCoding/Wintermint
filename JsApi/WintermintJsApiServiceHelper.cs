using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace WintermintClient.JsApi
{
    public static class WintermintJsApiServiceHelper
    {
        public static void PropagateExceptions(this Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }
            if (!task.IsCompleted)
            {
                throw new InvalidOperationException("Task has not completed.");
            }
            if (task.IsFaulted)
            {
                task.Wait();
            }
        }
    }
}