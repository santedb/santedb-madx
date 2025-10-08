using SanteDB.Core.Diagnostics;
using SanteDB.Messaging.IHE.MADX.Dhis2.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace SanteDB.Messaging.IHE.MADX.Dhis2
{
    /// <summary>
    /// Dispatcher service for dispatching messages to a DHIS2 endpoint
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Dhis2Dispatcher
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "DHIS2 Dispatch Service";

        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(Dhis2Dispatcher));

        private static readonly HttpClient m_httpClient = new HttpClient();

        private Dhis2DispatcherTargetConfiguration m_configuration;

        /// <summary>
        /// DI constructor
        /// </summary>
        public Dhis2Dispatcher() 
        {
            this.m_configuration = new Dhis2DispatcherTargetConfiguration
            {
                Endpoint = Environment.GetEnvironmentVariable("DHIS2_ENDPOINT"),
                ApiToken = Environment.GetEnvironmentVariable("DHIS2_API_TOKEN")
            };
        }

        /// <summary>
        /// Send a message to DHIS2
        /// </summary>
        public void SendMessage(string message)
        {
            try
            {
                var authString = $"ApiToken {this.m_configuration.ApiToken}";
                m_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("X-API-Key", Convert.ToBase64String(Encoding.UTF8.GetBytes(authString)));
                m_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = m_httpClient.PostAsync(this.m_configuration.Endpoint, new StringContent(message)).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error dispatching message - {0}", e.Message);
            }
        }
    }
}