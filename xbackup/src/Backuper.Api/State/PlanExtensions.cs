using System;
using System.Linq;
using System.Threading.Tasks;
using Annium.Logging;
using Backuper.Notification.Abstract;

namespace Backuper.Api.State;

internal static class PlanExtensions
{
    /// <summary>
    /// Notifies every channel of a plan, containing each failure: a channel that throws would
    /// otherwise escape the caller's catch block — killing the plan's recurring schedule when called
    /// from the scheduler, and replacing the diagnostic response with a generic one when called from a
    /// request.
    /// </summary>
    /// <param name="plan">The plan whose channels to notify.</param>
    /// <param name="subject">The caller, whose logger records channels that fail.</param>
    /// <param name="notifyChannel">The notification to send on each channel.</param>
    /// <returns>A task that completes once every channel has been tried.</returns>
    public static Task NotifyAllAsync(this Plan plan, ILogSubject subject, Func<IChannel, Task> notifyChannel) =>
        Task.WhenAll(
            plan.Notifications.Values.Select(async channel =>
            {
                try
                {
                    await notifyChannel(channel);
                }
                catch (Exception ex)
                {
                    subject.Error(ex);
                }
            })
        );
}
