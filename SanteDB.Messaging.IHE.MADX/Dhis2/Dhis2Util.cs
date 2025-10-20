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
using SanteDB.BI.Util;
using SanteDB.Messaging.FHIR.Exceptions;
using SanteDB.Messaging.IHE.MADX.Dhis2.Models;
using SanteDB.Messaging.FHIR.Util;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;

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

        //// FHIR operation handler
        //private static IFhirOperationHandler m_fhirOperationHandler;

        private static readonly IBiMetadataRepository m_repository;

        private static IBiDataSource m_biDataSource;

        private static IIdentityDomainRepositoryService m_identityDomainRepositoryService;

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
                //m_fhirOperationHandler = ApplicationServiceContext.Current.GetService<FhirEvaluateMeasureOperation>();
                m_identityDomainRepositoryService = ApplicationServiceContext.Current.GetService<IIdentityDomainRepositoryService>();
                m_biDataSource = ApplicationServiceContext.Current.GetService<IBiDataSource>();
                m_repository = ApplicationServiceContext.Current.GetService<IBiMetadataRepository>();
                m_queueService = ApplicationServiceContext.Current.GetService<IDispatcherQueueManagerService>();
                if (m_queueService != null)
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

        public static DataValueSet ConvertToDataValueSet(string indicatorId, Guid subjectId)
        {
            var dataValueSet = new DataValueSet();

            var indicatorDef = m_repository.Get<BiIndicatorDefinition>(indicatorId);

            indicatorDef = BiUtils.ResolveRefs(indicatorDef);

            if (indicatorDef == null)
            {
                throw new FhirException(System.Net.HttpStatusCode.BadRequest, OperationOutcome.IssueType.NotFound, $"Measure {indicatorId} not registered");
            }

            var dataSetId = indicatorDef.Identifier.FirstOrDefault()?.Value;

            var subjRepo = typeof(IRepositoryService<>).MakeGenericType(indicatorDef.Subject.ResourceType);
            var repo = ApplicationServiceContext.Current.GetService(subjRepo) as IRepositoryService;
            var subject = repo.Get(subjectId);

            var orgUnitId = ((Place)subject).Identifiers.FirstOrDefault(o => o.IdentityDomain.DomainName == "DHIS 2")?.Value;

            dataValueSet.DataSet = dataSetId;
            dataValueSet.CompleteDate = DateTimeOffset.Now.ToString("yyyy-MM-dd");
            dataValueSet.Period = DateTimeOffset.Now.AddMonths(-1).ToString("yyyyMM");
            dataValueSet.OrgUnit = orgUnitId;

            dataValueSet.DataValues.Add(new DataValue
            {
                DataElement = "JgjkI5wSi1Y",
                Value = "1"
            });

            foreach (var indicatorResult in m_biDataSource.ExecuteIndicator(indicatorDef, BiIndicatorPeriod.Empty).GroupBy(o => o.Measure))
            {
                //var measureGroup = new MeasureReport.GroupComponent();

                foreach (var measureResult in indicatorResult)
                {

                    //dataValueSet.DataValues.Add(new DataValue()
                    //{
                    //    DataElement = measureResult.Indicator.Name,
                    //    Value = measureResult.,
                    //});
                }
            }

            return dataValueSet;
        }
    }
}