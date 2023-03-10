using InorgIntegr.Models.DBResponses;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace InorgIntegr.Models
{
    public static class SearchModel
    {
        static readonly HttpClient client = new HttpClient();

        class PubChemGettingCidException : Exception { }

        class PubChemGettingInfoException : Exception { }
        
        class GettingFDBException : Exception { }
        
        class FDBGettingInfoException : Exception { }

        class VmhGettingException : Exception { }

        public static async Task<PubChemInfoResponse> FindPubChem(SearchRequest searchRequest)
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

        public static async Task<VmhResponse> FindVmh(SearchRequest searchRequest)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(
                    $"https://www.vmh.life/_api/smatrix/?abbreviation={searchRequest.Formula}&format=json&page_size=1000");
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();

                dynamic obj = JsonConvert.DeserializeObject(json);
                var results = (JArray)obj.results;
                var items = results.Select(r => new VmhItem
                {
                    Abbreviation = r["rxn"]["abbreviation"].ToString(),
                    Description = r["rxn"]["description"].ToString(),
                    Formula = r["rxn"]["formula"].ToString(),
                    Subsystem = r["rxn"]["subsystem"].ToString(),
                })
                    .Where(r => r.Formula.ToLower().Contains(searchRequest.Formula.ToLower()))
                    .GroupBy(r => r.Abbreviation)
                    .Select(r => r.First())
                    .ToList();
                if (items.Any())
                    return new VmhResponse { Items = items };
                throw new VmhGettingException();
            }
            catch (VmhGettingException)
            {
                return new VmhResponse { Error = "Вещество не найдено в БД VMH" };
            }
            catch
            {
                return new VmhResponse { Error = "Ошибка при поиске вещества на VMH"};
            }
        }

        public static async Task<FoodbDBResponse> FindFoodb(SearchRequest searchRequest)
        {
            try
            {
                var data = await GetInfoByFDBAsync(await GetFDBByFormulaAsync(searchRequest.Formula));
                if (data == null || data.Foods.Count() == 0)
                    throw new GettingFDBException();
                return data;
            }
            catch (FDBGettingInfoException)
            {
                return new FoodbDBResponse { Error = "Ошибка при получении информации о веществе на FoodDb. Проверьте корректность формулы введенного вещества" };
            }
            catch (GettingFDBException)
            {
                return new FoodbDBResponse { Error = "Ошибка при получении FDB введенного вещества на FoodDb" };
            }
            catch
            {
                return new FoodbDBResponse { Error = "непредвиденная ошибка" };
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

        public static async Task<string> GetFDBByFormulaAsync(string formula)
        {
            try
            {

                HttpResponseMessage response = await client.GetAsync(
                    $"https://foodb.ca/unearth/q?utf8=%E2%9C%93&query={formula}&searcher=compounds&button=");
                response.EnsureSuccessStatusCode();
                string html = await response.Content.ReadAsStringAsync();
                    
                return Regex.Match(html, "<a href=\"/compounds/([^\"]+)\">").Groups[1].Value;
            }
            catch
            {
                throw new GettingFDBException();
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
                var descriptions = sections.Children<JObject>()
                    .FirstOrDefault(section => section["TOCHeading"] != null && section["TOCHeading"].ToString() == "Names and Identifiers")
                    ?["Section"]?.Children<JObject>()
                    .FirstOrDefault(section => section["Description"] != null && section["Description"].ToString() == "Summary Information")
                    ?["Information"]?.Children<JObject>()
                    .Select(s => s["Value"]["StringWithMarkup"].First()["String"].ToString())
                    .ToList();

                var identifiers = sections.Children<JObject>()
                    .FirstOrDefault(section => section["TOCHeading"] != null && section["TOCHeading"].ToString() == "Names and Identifiers")
                    ?["Section"]?.Children<JObject>()
                    .FirstOrDefault(section => section["TOCHeading"] != null && section["TOCHeading"].ToString() == "Computed Descriptors")
                    ?["Section"]?.Children<JObject>()
                    .ToDictionary(k => k["TOCHeading"].ToString(),
                        v => v["Information"].First()["Value"]["StringWithMarkup"].First()["String"].ToString());

                var properties = sections.Children<JObject>()
                    .FirstOrDefault(section => section["TOCHeading"] != null && section["TOCHeading"].ToString() == "Chemical and Physical Properties")
                    ?["Section"]?.Children<JObject>()
                    .FirstOrDefault(section => section["TOCHeading"] != null && section["TOCHeading"].ToString() == "Computed Properties")
                    ?["Section"]?.Children<JObject>()
                    .ToDictionary(k => k["TOCHeading"].ToString(),
                        v =>
                        {
                            var info = v?["Information"].FirstOrDefault()?["Value"];
                            var answ = string.Empty;
                            
                            answ = info?["StringWithMarkup"]?.FirstOrDefault()?["String"]?.ToString() 
                            ?? info?["Number"]?.FirstOrDefault()?.ToString();

                            return answ ?? string.Empty;
                        });

                return new PubChemInfoResponse
                {
                    Descriptions = descriptions ?? Enumerable.Empty<string>(),
                    Ids = identifiers ?? new Dictionary<string, string>(),
                    Properties = properties ?? new Dictionary<string, string>(),
                    ImageLink = $"https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/cid/{cid}/PNG"
                };
            }

            catch
            {
                throw new PubChemGettingInfoException();
            }
        }

        public static async Task<FoodbDBResponse> GetInfoByFDBAsync(string fdb)
        {
            try
            {
                var httpRequestMessage = new HttpRequestMessage
                {
                    RequestUri = new Uri($"https://foodb.ca/compounds/{fdb}"),
                    Headers = {
                    { HttpRequestHeader.Accept.ToString(), "application/xml" },
                    }
                };

                var response = client.SendAsync(httpRequestMessage).Result;
                string xml = await response.Content.ReadAsStringAsync();


                var obj = XDocument.Parse(xml);
                var foodsXml = obj.XPathSelectElements("/compound/foods/food");

                var foods = foodsXml.Select(f => new Food {
                    Name = f.Element("name").Value,
                    NameSci = f.Element("name_scientific").Value,
                    NcbiId = f.Element("ncbi_taxonomy_id").Value
                });


                return new FoodbDBResponse
                {
                    Foods = foods ?? Enumerable.Empty<Food>()
                };
            }

            catch
            {
                throw new FDBGettingInfoException();
            }
        }

        public static async Task<(PubChemInfoResponse, FoodbDBResponse, VmhResponse)> GetResponses(SearchRequest request)
        {
            var pubChemInfoResponseTask = request.IsPubChem ? FindPubChem(request) : Task.FromResult<PubChemInfoResponse>(null);
            var fdbInfoResponseTask = request.IsFoodB ? FindFoodb(request) : Task.FromResult<FoodbDBResponse>(null);
            var VmhInfoResponseTask = request.IsVmh ? FindVmh(request) : Task.FromResult<VmhResponse>(null);

            await Task.WhenAll(pubChemInfoResponseTask, fdbInfoResponseTask, VmhInfoResponseTask);

            var pubChemInfoResponse = pubChemInfoResponseTask.Result;
            var fdbInfoResponse = fdbInfoResponseTask.Result;
            var VmhInfoResponse = VmhInfoResponseTask.Result;

            return (pubChemInfoResponse, fdbInfoResponse, VmhInfoResponse); 
        }

        public static async Task<object> BuildObjectFromResponses(SearchRequest request)
        {
            var (pubChemInfoResponse, fdbInfoResponse, dBInfoResponse) = await GetResponses(request);

            return new
            {
                PubChemInfo = pubChemInfoResponse,
                FdbInfo = fdbInfoResponse,
                VmhInfo = dBInfoResponse,
            };
        }

        public static async Task<MemoryStream?> BuildXmlFromResponse(SearchRequest request)
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(object));
            var obj = await BuildObjectFromResponses(request);
            var xml = "";
            using (var sww = new StringWriter())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(sww))
                {
                    xsSubmit.Serialize(xmlWriter, obj);
                    xml = sww.ToString();
                }
            }
            var stream = new MemoryStream();
            var writer = XmlWriter.Create(stream);
            writer.WriteRaw(xml);
            stream.Position = 0;
            return stream;
        }
    }
}
