namespace ServicesStatusInformation.Model
{
    public class SantasServices
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
        public SantasServicesStatus Status { get; set; }
    }
}
