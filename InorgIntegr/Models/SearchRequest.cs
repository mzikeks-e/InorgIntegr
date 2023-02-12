namespace InorgIntegr.Models
{
    public enum ExportType
    {
        OpenHtml,
        ToHtml,
        ToJson
    }

    public class SearchRequest
    {
        public string Formula { get; set; }

        public ExportType ExportAs { get; set; }

        public bool IsPubChem { get; set; }
        public bool IsFoodB { get; set; }
        public bool IsDB { get; set; }
    }
}
