namespace InorgIntegr.Models
{
    public class VmhResponse
    {
        public string Error { get; set; }
        public IEnumerable<VmhItem> Items { get; set; }
    }
}
