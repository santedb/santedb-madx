using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Queue;
using System;

namespace SanteDB.Messaging.IHE.MADX.Dhis2
{
    /// <summary>
    /// Utiility for handling the queue manager service for DHIS2 messages
    /// </summary>
    public static class Dhis2Util
    {
        private static Tracer traceSource = Tracer.GetTracer(typeof(Dhis2Util));

        // Queue service
        private static IDispatcherQueueManagerService m_queueService;

        // Dispatcher service
        private static IDhis2DispatchService m_dispatcher;

        // Temporary string for the queue name
        private static string queueName = "sys.dhis2";

        /// <summary>
        /// DHIS2 Utility
        /// </summary>
        static Dhis2Util()
        {
            try
            {
                m_dispatcher = ApplicationServiceContext.Current.GetService<IDhis2DispatchService>();
                m_queueService = ApplicationServiceContext.Current.GetService<IDispatcherQueueManagerService>();
                if (m_dispatcher == null)
                {
                    m_queueService.Open(queueName);
                    m_queueService.SubscribeTo(queueName, MessageQueued);
                }
            }
            catch (Exception ex)
            {
                throw;
            } 
        }

        private static void MessageQueued(DispatcherMessageEnqueuedInfo e)
        {
            DispatcherQueueEntry queueEntry = null;
            while (m_queueService.TryDequeue(queueName, out queueEntry))
            {
                try
                {
                    m_dispatcher.SendMessage(queueEntry.Body);
                } catch (Exception ex)
                {
                    traceSource.TraceError("Error dispatching message - {0}", ex);
                }
            }
        }
    }
}
