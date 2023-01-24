namespace InorgIntegr.Models.DBResponses
{
    public class PubChemInfoResponse
    {
        public string Description { get; set; } = null;
        public string ImageLink { get; set; } = null;
        public string Error { get; set; } = null;

        public override string ToString()
        {
            return Description;
        }
    }
}
