using Newtonsoft.Json;

namespace SantaServicesSeed.Model
{
    public class SantasServices
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
        public SantasServicesStatus Status { get; set; }
    }
}
