using System;
using DearImGuiKSP.Application.Interfaces;

namespace DearImGuiKSP.Application
{
    /// <summary>
    /// Catches consumer callback exceptions, counts consecutive failures per consumer,
    /// and disables the consumer at LibraryConfig.ConsumerFailureThreshold (spec §5.3).
    /// </summary>
    internal sealed class FaultBarrier
    {
        private readonly ILogger _log;

        internal FaultBarrier(ILogger log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Invokes one consumer's callback with fault isolation.
        /// </summary>
        internal void Invoke(ConsumerRegistry.ConsumerRegistration consumer)
        {
            if (consumer == null)
            {
                throw new ArgumentNullException(nameof(consumer));
            }

            if (!consumer.Enabled)
            {
                return;
            }

            try
            {
                consumer.Callback();
                consumer.ConsecutiveFailureCount = 0;
            }
            catch (Exception ex)
            {
                consumer.ConsecutiveFailureCount++;
                _log.Error("Consumer '" + consumer.Id + "' threw an exception: " + ex);

                if (consumer.ConsecutiveFailureCount >= LibraryConfig.ConsumerFailureThreshold)
                {
                    consumer.Enabled = false;
                    _log.Error("Consumer '" + consumer.Id + "' has been auto-disabled after " +
                               LibraryConfig.ConsumerFailureThreshold + " consecutive throwing frames. Exception: " + ex);
                }
            }
        }
    }
}
