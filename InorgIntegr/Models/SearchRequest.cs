namespace InorgIntegr.Models
{
    public class SearchRequest
    {
        public string Formula { get; set; }
        
        public bool IsHtmlExport { get; set; }
        public bool IsJsonExport { get; set; }

        public bool IsPubChem { get; set; }
        public bool IsFoodB { get; set; }
        public bool IsDB { get; set; }
    }
}
