using Newtonsoft.Json;
using System.ComponentModel;

namespace PeoplesTaskApp.Models
{
    [JsonObject("Settings")]
    public sealed class AppConfiguration
    {
        [DefaultValue("{0:yyyy_MM_dd}.log")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string LogFullFilePathTemplate { get; set; } = "{0:yyyy_MM_dd}.log";

        [DefaultValue("json")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string DataFileFormat { get; set; } = "json";

        [DefaultValue("Data.json")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string DataFullFilePath { get; set; } = "Data.json";


        [JsonIgnore]
        public static string DataFileServiceContractName => "DataFile";
    }
}
