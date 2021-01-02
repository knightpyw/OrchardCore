using System;
using System.Threading.Tasks;
using OrchardCore.Locking;
using OrchardCore.Locking.Distributed;
using OrchardCore.Workflows.Models;

namespace OrchardCore.Workflows.Helpers
{
    public static class DistributedLockExtensions
    {
        /// <summary>
        /// Tries to acquire a lock on this workflow type if it is a singleton or
        /// if the event is exclusive, otherwise returns true with a null locker.
        /// </summary>
        public static Task<(ILocker locker, bool locked)> TryAcquireWorkflowTypeLockAsync(
            this IDistributedLock distributedLock,
            WorkflowType workflowType,
            bool isExclusiveEvent = false)
        {
            if (workflowType.IsSingleton || isExclusiveEvent)
            {
                return distributedLock.TryAcquireLockAsync(
                    "WFT_" + workflowType.WorkflowTypeId + "_LOCK",
                    TimeSpan.FromMilliseconds(workflowType.LockTimeout > 0 ? workflowType.LockTimeout : 20_000),
                    TimeSpan.FromMilliseconds(workflowType.LockExpiration > 0 ? workflowType.LockExpiration : 20_000));
            }

            return Task.FromResult<(ILocker, bool)>((null, true));
        }

        /// <summary>
        /// Tries to acquire a lock on this workflow instance.
        /// </summary>
        public static Task<(ILocker locker, bool locked)> TryAcquireWorkflowLockAsync(
            this IDistributedLock distributedLock,
            Workflow workflow)
        {
            return distributedLock.TryAcquireLockAsync(
                "WFI_" + workflow.WorkflowId + "_LOCK",
                TimeSpan.FromMilliseconds(workflow.LockTimeout > 0 ? workflow.LockTimeout : 20_000),
                TimeSpan.FromMilliseconds(workflow.LockExpiration > 0 ? workflow.LockExpiration : 20_000));
        }
    }
}