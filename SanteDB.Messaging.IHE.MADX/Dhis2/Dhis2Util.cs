using Newtonsoft.Json;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Queue;
using SanteDB.Messaging.IHE.MADX.Dhis2.Constants;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using SanteDB.Core.Security;
using DocumentFormat.OpenXml.Spreadsheet;
using SanteDB.Core.Configuration;

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
        private static Dhis2Dispatcher m_dhis2Dispatcher;

        private static string queueName = Dhis2Constants.QueueName;

        /// <summary>
        /// DHIS2 Utility
        /// </summary>
        static Dhis2Util()
        {
            try
            {
                m_dhis2Dispatcher = new Dhis2Dispatcher();
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
                    m_dhis2Dispatcher.SendMessage(serializedBody);
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


            if (indicatorDef == null)
            {
                throw new FhirException(System.Net.HttpStatusCode.BadRequest, OperationOutcome.IssueType.NotFound, $"Measure {indicatorId} not registered");
            }

            indicatorDef = BiUtils.ResolveRefs(indicatorDef);

            var dataSetId = indicatorDef.Identifier.FirstOrDefault(o => o.System == "DHIS 2")?.Value;

            var subjRepo = typeof(IRepositoryService<>).MakeGenericType(indicatorDef.Subject.ResourceType);
            var repo = ApplicationServiceContext.Current.GetService(subjRepo) as IRepositoryService;
            var subject = repo.Get(subjectId);

            var orgUnitId = ((Place)subject).Identifiers.FirstOrDefault(o => o.IdentityDomain.Name == "DHIS 2")?.Value;

            dataValueSet.DataSet = dataSetId;
            dataValueSet.CompleteDate = DateTimeOffset.Now.ToString("yyyy-MM-dd");
            dataValueSet.Period = DateTimeOffset.Now.AddMonths(-1).ToString("yyyyMM");
            dataValueSet.OrgUnit = orgUnitId;
            dataValueSet.DataValues = new List<DataValue>();

            var period = new BiIndicatorPeriod(new DateTime(2025, 08, 01), new DateTime(2025, 08, 31));

            foreach (var indicatorResult in m_biDataSource.ExecuteIndicator(indicatorDef, period, subject.Key.ToString()).GroupBy(o => o.Measure))
            {
                foreach (var measureResult in indicatorResult)
                {
                    if (String.IsNullOrEmpty(measureResult.StratifierPath))
                    {
                        var currentRecord = measureResult.Records.SingleOrDefault() as IDictionary<String, object>;

                        if (currentRecord == null) { continue; }

                        // Numerator and denominator
                        ConvertComputation(currentRecord, measureResult.Measure, out var numeratorValue, out var denominatorValue, out var scoreObtained);

                        var dataElement = measureResult.Measure.Identifier.FirstOrDefault(o => o.System == "DHIS 2")?.Value;

                        if (scoreObtained.HasValue)
                        {
                            dataValueSet.DataValues.Add(new DataValue()
                            {
                                DataElement = dataElement,
                                Value = scoreObtained.Value.ToString()
                            });
                        }
                        else
                        {
                            if (denominatorValue.HasValue)
                            {
                                dataValueSet.DataValues.Add(new DataValue()
                                {
                                    DataElement = dataElement,
                                    Value = ((float)numeratorValue / (float)denominatorValue).ToString(CultureInfo.InvariantCulture)
                                });
                            }
                            else
                            {
                                dataValueSet.DataValues.Add(new DataValue()
                                {
                                    DataElement = dataElement,
                                    Value = ((decimal)numeratorValue).ToString(CultureInfo.InvariantCulture)
                                });
                            }
                        }
                    }
                    
                }
            }

            return dataValueSet;
        }

        private static void ConvertComputation(IDictionary<String, object> currentRecord, BiIndicatorMeasureDefinition measure, out long? numeratorValue, out long? denominatorValue, out decimal? scoreObtained)
        {
            numeratorValue = null;
            denominatorValue = null;
            scoreObtained = null;

            foreach (var calc in measure.Computation)
            {
                switch (calc)
                {
                    case BiMeasureComputationNumerator cNumerator:
                        numeratorValue = (long)currentRecord[calc.Name];
                        break;
                    case BiMeasureComputationNumeratorExclusion cNumeratorExcl:
                        numeratorValue -= (long)currentRecord[calc.Name];
                        break;
                    case BiMeasureComputationDenominator cDenominator:
                        denominatorValue = (long)currentRecord[calc.Name];
                        break;
                    case BiMeasureComputationDenominatorExclusion cDenominator:
                        denominatorValue -= (long)currentRecord[calc.Name];
                        break;
                    case BiMeasureComputationScore cScore:
                        var rawValue = currentRecord[calc.Name];
                        if (rawValue == null)
                        {
                            scoreObtained = 0;
                        }
                        else if (rawValue is Decimal d || Decimal.TryParse(rawValue.ToString(), out d))
                        {
                            scoreObtained = d;
                        }
                        continue;
                    default:
                        continue;
                }

            }
        }
    }
}