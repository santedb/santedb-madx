/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * User: webberj
 * Date: 2025-09-18
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Model.Attributes;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.Rest;

namespace SanteDB.Messaging.IHE.MADX.Dhis2.Configuration
{
    /// <summary>
    /// Get the dispatcher target configuration
    /// </summary>
    [ExcludeFromCodeCoverage]
    [XmlType(nameof(Dhis2DispatcherTargetConfiguration), Namespace = "http://santedb.org/configuration")]
    public class Dhis2DispatcherTargetConfiguration
    {
        /// <summary>
        /// Gets or sets the endpoint for dhis2
        /// </summary>
        [XmlElement("endpoint")]
        [DisplayName("Endpoint URL")]
        [Description("The remote endpoint for DHIS2")]
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the username or authentication data
        /// </summary>
        [XmlElement("token")]
        [DisplayName("Secret")]
        [Description("If the remote endpoint requires an authentication scheme, this is an Api Token to pass to the authenticator")]
        public string ApiToken { get; set; }

        /// <summary>
        /// Gets or sets the domain
        /// </summary>
        [XmlElement("domain")]
        [DisplayName("Domain")]
        [Description("The domain used for reconciling DHIS2 indicators. This domain must be the same as the domain when setting up the identity domain for DHIS2 identifiers.")]
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the class which authenticates requests
        /// </summary>
        [XmlElement("authenticator")]
        [DisplayName("Authenticator")]
        [Description("The authentication plugin to use to pre-authenticate this SanteDB server against the master server")]
        [Editor("SanteDB.Configuration.Editors.TypeSelectorEditor, SanteDB.Configuration", "System.Drawing.Design.UITypeEditor, System.Drawing")]
        [Binding(typeof(IFhirClientAuthenticator))]
        public TypeReferenceConfiguration Authenticator { get; set; }
    }
}
