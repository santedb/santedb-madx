using Newtonsoft.Json;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Queue;
using SanteDB.Messaging.IHE.MADX.Dhis2.Constants;
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
        private static Dhis2Dispatcher m_dispatcher;

        private static string queueName = Dhis2Constants.QueueName;

        /// <summary>
        /// DHIS2 Utility
        /// </summary>
        static Dhis2Util()
        {
            try
            {
                m_dispatcher = ApplicationServiceContext.Current.GetService<Dhis2Dispatcher>();
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
                    var serializedBody = JsonConvert.SerializeObject(queueEntry.Body);
                    m_dispatcher.SendMessage(serializedBody);
                }
                catch (Exception ex)
                {
                    traceSource.TraceError("Error dispatching message - {0}", ex);
                }
            }
        }
    }
}