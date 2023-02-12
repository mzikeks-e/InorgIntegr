namespace InorgIntegr.Models
{
    public enum ExportType
    {
        OpenHtml,
        ToXml,
        ToJson
    }

    public class SearchRequest
    {
        public string Formula { get; set; }

        public string Filename { get; set; }

        public ExportType ExportAs { get; set; }

        public bool IsPubChem { get; set; }
        public bool IsFoodB { get; set; }
        public bool IsVmh { get; set; }
    }
}
