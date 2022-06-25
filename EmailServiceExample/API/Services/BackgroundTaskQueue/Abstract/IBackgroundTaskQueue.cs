using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.Infrastructure.BackgroundTask.BackgroundTaskQueue.Abstract
{
    public interface IBackgroundTaskQueue
    {
        // Enqueues the given task.
        void EnqueueTask(Func<IServiceScopeFactory, CancellationToken, Task> task);

        // Dequeues and returns one task. This method blocks until a task becomes available.
        Task<Func<IServiceScopeFactory, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
    }
}
