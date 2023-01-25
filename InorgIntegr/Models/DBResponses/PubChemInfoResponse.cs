namespace InorgIntegr.Models.DBResponses
{
    public class PubChemInfoResponse
    {
        public IEnumerable<string> Descriptions { get; set; } = null;
        public IDictionary<string, string> Ids { get; set; } = null;
        public IDictionary<string, string> Properties { get; set; } = null;
        public string ImageLink { get; set; } = null;
        public string Error { get; set; } = null;
    }
}
