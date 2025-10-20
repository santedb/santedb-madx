/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 */
using System;
using System.Collections.Generic;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Jobs;
using SanteDB.BI.Model;
using SanteDB.BI.Services;
using SanteDB.Core;
using SanteDB.Core.Queue;
using SanteDB.Messaging.IHE.MADX.Dhis2.Constants;

namespace SanteDB.Messaging.IHE.MADX.Jobs
{
    /// <summary>
    /// </summary>
    public class Dhis2ReportSendingJob : IJob
    {
        private readonly IJobStateManagerService m_jobStateManager;

        private readonly IBiMetadataRepository m_repository;

        private IDispatcherQueueManagerService m_queueService;

        private string dhis2QueueName = Dhis2Constants.QueueName;

        private string dhis2SystemName = Dhis2Constants.SystemName;

        private readonly Tracer m_tracer;

        private bool m_cancelRequested = false;

        /// <summary>
        /// Job Id
        /// </summary>
        public static readonly Guid JOB_ID = Guid.Parse("05566FAF-52E0-4D35-B327-8DAB62529961");

        /// <inheritdoc/>
        public Guid Id => JOB_ID;

        /// <summary>
        /// Completed successfully state key
        /// </summary>
        private static readonly Guid COMPLETED_SUCCESSFULLY_STATE_KEY = Guid.Parse("2726BC79-A55A-4FEA-BE2C-627265872DB5");

        /// <summary>
        /// Completed with errors state key
        /// </summary>
        private static readonly Guid COMPLETED_WITH_ERRORS_STATE_KEY = Guid.Parse("92455ACD-ECC2-4E89-9227-94B77B27420D");

        /// <inheritdoc/>
        public DateTime? LastStarted { get; private set; }

        /// <inheritdoc/>
        public DateTime? LastFinished { get; private set; }

        /// <inheritdoc/>
        public string Name => "DHIS2 Report Sending Job";

        /// <inheritdoc/>
        public string Description => "Sends the indicator reports to the DHIS2 endpoint";

        /// <inheritdoc/>87
        public IDictionary<string, Type> Parameters => null;

        /// <inheritdoc/>
        public bool CanCancel => true;

        /// <summary>
        /// Dependency injected constructor
        /// </summary>
        public Dhis2ReportSendingJob(IJobStateManagerService jobStateManagerService)
        {
            m_tracer = Tracer.GetTracer(GetType());
            m_jobStateManager = jobStateManagerService;
            m_repository = ApplicationServiceContext.Current.GetService<IBiMetadataRepository>();
            m_queueService = ApplicationServiceContext.Current.GetService<IDispatcherQueueManagerService>();
            if (m_queueService != null)
            {
                m_queueService.Open(dhis2QueueName);
            }
        }

        /// <inheritdoc/>
        public void Cancel()
        {
            m_cancelRequested = true;
            m_jobStateManager.SetState(this, JobStateType.Cancelled);

        }

        /// <inheritdoc/>
        public void Run(object sender, EventArgs e, object[] parameters)
        {
            try
            {
                m_jobStateManager.SetState(this, JobStateType.Running);
                m_cancelRequested = false;
                LastStarted = DateTime.Now;

                // Query for indicators that have DHIS2 identifiers
                var indicatorDefinitions = m_repository.Query<BiIndicatorDefinition>(i => i.Identifier.System.Equals(dhis2SystemName));
                foreach (var indicatorDefinition in indicatorDefinitions)
                {
                    Console.WriteLine(indicatorDefinition.Name);

                    /*
                    // Convert indicator to a DataValueSet instance
                    var dataValueSet = Dhis2Util.ConvertToDataValueSet(indicatorDefinition.Id);

                    // Enqueue DataValueSet instance using the DHIS2 dispatcher queue
                    this.m_queueService.Enqueue(dhis2QueueName, dataValueSet);
                    */
                }

                if (m_cancelRequested)
                {
                    m_jobStateManager.SetState(this, JobStateType.Cancelled);
                }
                else
                {
                    m_jobStateManager.SetState(this, JobStateType.Completed);
                }

                LastFinished = DateTime.Now;
            }
            catch (Exception ex)
            {
                m_jobStateManager.SetState(this, JobStateType.Aborted, ex.ToHumanReadableString());
            }
        }
    }
}
