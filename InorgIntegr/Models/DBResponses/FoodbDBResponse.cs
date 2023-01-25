namespace InorgIntegr.Models.DBResponses
{
    public class FoodbDBResponse
    {
        public string Error { get; set; }
        public IEnumerable<Food> Foods { get; set; }
    }
}
