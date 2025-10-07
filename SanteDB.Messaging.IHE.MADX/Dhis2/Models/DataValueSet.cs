using System.Collections.Generic;
using Newtonsoft.Json;

namespace SanteDB.Messaging.IHE.MADX.Dhis2.Models
{
    /// <summary>
    /// Represents a DHIS2 data value set
    /// </summary>
    [JsonObject("dataValueSet")]
    public class DataValueSet
    {
        /// <summary>
        /// Gets and sets the data set identifier
        /// </summary>
        [JsonProperty("dataSet")]
        public string DataSet { get; set; }

        /// <summary>
        /// Gets and sets the complete date
        /// </summary>
        [JsonProperty("completeDate")]
        public string CompleteDate { get; set; }

        /// <summary>
        /// Gets and sets the period
        /// </summary>
        [JsonProperty("period")]
        public string Period { get; set; }

        /// <summary>
        /// Gets and sets the organization unit
        /// </summary>
        [JsonProperty("orgUnit")]
        public string OrgUnit { get; set; }

        /// <summary>
        /// Gets and sets the attribute option combo
        /// </summary>
        [JsonProperty("attributeOptionCombo")]
        public string AttributeOptionCombo { get; set; }

        /// <summary>
        /// Gets and sets the data values
        /// </summary>
        [JsonProperty("dataValues")]
        public List<DataValue> DataValues { get; set; }
    }
}
