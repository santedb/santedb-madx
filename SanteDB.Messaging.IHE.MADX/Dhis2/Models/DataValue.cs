using Newtonsoft.Json;

namespace SanteDB.Messaging.IHE.MADX.Dhis2.Models
{
    /// <summary>
    /// Represents a DHIS2 data value
    /// </summary>
    [JsonObject("dataValue")]
    public class DataValue
    {
        /// <summary>
        /// Gets and sets the data element
        /// </summary>
        [JsonProperty("dataElement")]
        public string DataElement { get; set; }

        /// <summary>
        /// Gets and sets the category option combo
        /// </summary>
        [JsonProperty("categoryOptionCombo")]
        public string CategoryOptionCombo { get; set; }

        /// <summary>
        /// Gets and sets the value
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }

        /// <summary>
        /// Gets and sets the comment
        /// </summary>
        [JsonProperty("comment")]
        public string Comment { get; set; }
    }
}
