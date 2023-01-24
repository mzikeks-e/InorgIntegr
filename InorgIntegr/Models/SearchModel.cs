using InorgIntegr.Models.DBResponses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace InorgIntegr.Models
{
    public static class SearchModel
    {
        static readonly HttpClient client = new HttpClient();

        class PubChemGettingCidException : Exception { }

        class PubChemGettingInfoException : Exception { }

        public static async Task<PubChemInfoResponse> Find(SearchRequest searchRequest)
        {
            try
            {
                return await GetInfoByCidAsync(await GetCidByFormulaAsync(searchRequest.Formula));
            }
            catch (PubChemGettingCidException)
            {
                return new PubChemInfoResponse { Error = "Ошибка при получении CID введенного вещества. Проверьте корректность формулы введенного вещества" };
            }
            catch (PubChemGettingInfoException)
            {
                return new PubChemInfoResponse { Error = "Ошибка при получении информации о веществе на PubChem" };
            }
            catch
            {
                return new PubChemInfoResponse { Error = "непредвиденная ошибка" };
            }
        }

        public static async Task<int> GetCidByFormulaAsync(string formula)
        {
            try
            {

                HttpResponseMessage response = await client.GetAsync(
                    $"https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/fastformula/{formula}/cids/Json");
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                
                dynamic obj = JsonConvert.DeserializeObject(json);
                var cids = (JArray)obj.IdentifierList.CID;
                var cidsInt = cids.Select(cid => (int)cid);
                return cidsInt.Min();
            }
            catch
            {
                throw new PubChemGettingCidException();
            }
        }

        public static async Task<PubChemInfoResponse> GetInfoByCidAsync(int cid)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(
                    $"https://pubchem.ncbi.nlm.nih.gov/rest/pug_view/data/compound/{cid}/JSON");
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();;

                
                dynamic obj = JsonConvert.DeserializeObject(json);
                var sections = (JArray)obj.Record.Section;
                var description = sections.Children<JObject>()
                    .First(section => section["TOCHeading"] != null && section["TOCHeading"].ToString() == "Names and Identifiers")
                    ["Section"].Children<JObject>()
                    .First(section => section["Description"] != null && section["Description"].ToString() == "Summary Information")
                    ["Information"].Children<JObject>()
                    .First()
                    ["Value"]
                    ["StringWithMarkup"]
                    .First()
                    ["String"];

                return new PubChemInfoResponse
                {
                    Description = description.ToString(),
                    ImageLink = $"https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/cid/{cid}/PNG"
                };
            }

            catch
            {
                throw new PubChemGettingInfoException();
            }
        }
    }
}
