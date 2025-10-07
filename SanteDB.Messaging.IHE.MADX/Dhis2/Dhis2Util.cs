using Newtonsoft.Json;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Queue;
using SanteDB.Messaging.IHE.MADX.Dhis2.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using SanteDB.BI.Model;
using SanteDB.BI.Services;
using SanteDB.Messaging.FHIR.Extensions;
using SanteDB.Messaging.FHIR.Operations;
using SanteDB.Messaging.IHE.MADX.Dhis2.Models;
using SanteDB.Messaging.FHIR.Exceptions;
using DocumentFormat.OpenXml.Drawing.Charts;

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

        // FHIR operation handler
        private static IFhirOperationHandler m_fhirOperationHandler;

        private static readonly IBiMetadataRepository m_repository;

        private static IBiDataSource m_biDataSource;

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
                m_fhirOperationHandler = ApplicationServiceContext.Current.GetService<FhirEvaluateMeasureOperation>();
                m_biDataSource = ApplicationServiceContext.Current.GetService<IBiDataSource>();
                m_repository = ApplicationServiceContext.Current.GetService<IBiMetadataRepository>();
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

        private static DataValueSet ConvertToDataValueSet(string indicatorId)
        {
            var dataValueSet = new DataValueSet();

            var indicatorDef = m_repository.Get<BiIndicatorDefinition>(indicatorId);

            if (indicatorDef == null)
            {
                throw new FhirException(System.Net.HttpStatusCode.BadRequest, OperationOutcome.IssueType.NotFound, $"Measure org.santedb.ims.bi.indicators.stock.alarmEvents not registered");
            }

            foreach (var indicatorResult in m_biDataSource.ExecuteIndicator(indicatorDef, BiIndicatorPeriod.Empty).GroupBy(o => o.Measure))
            {
                foreach (var measureResult in indicatorResult)
                {
                    dataValueSet.DataSet = measureResult.Measure.Id;
                    dataValueSet.CompleteDate = DateTime.Now.ToString("yyyy-MM-dd");
                    dataValueSet.Period = measureResult.StartTime.ToString();
                    dataValueSet.OrgUnit = "";
                    dataValueSet.AttributeOptionCombo = "";
                    dataValueSet.DataValues = new List<DataValue>();
                }
            }

            return dataValueSet;
        }
    }
}