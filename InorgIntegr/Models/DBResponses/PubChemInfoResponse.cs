namespace InorgIntegr.Models.DBResponses
{
    public class PubChemInfoResponse
    {
        public IEnumerable<string> Descriptions { get; set; }
        public IDictionary<string, string> Ids { get; set; }
        public IDictionary<string, string> Properties { get; set; }
        public string ImageLink { get; set; }
        public string Error { get; set; }
    }
}
