namespace Internship_Backend_cpt.Models
{
    public class ConnectionModel
    {
        public Guid Guid { get; set; }
        public string FirstConnectionString { get; set; } = null!;
        public string SecondConnectionString { get; set; } = null!;
        public string FirstProviderName { get; set; } = null!;
        public string SecondProviderName { get; set; } = null!;
        public ConnectionModel() { }
    }
}
