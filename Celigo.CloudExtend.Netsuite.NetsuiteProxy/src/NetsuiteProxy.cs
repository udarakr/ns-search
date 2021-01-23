using System;
using System.Net;
using System.Web;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Amazon.Lambda.APIGatewayEvents;

using Microsoft.Extensions.Logging;

namespace Celigo.CloudExtend.Netsuite.NetsuiteProxy
{
    public class NetsuiteProxy
    {
        private readonly ILogger<NetsuiteProxy> _logger;
        private readonly IHttpClientFactory _clientFactory;

        public NetsuiteProxy(ILogger<NetsuiteProxy> logger, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _clientFactory = clientFactory;
        }

        public APIGatewayProxyResponse HandleSearchRequest(APIGatewayProxyRequest request)
        {
            //var searchReq = System.Text.Json.JsonSerializer.Deserialize<SearchTerm>(request.Body);

            var Searchtext = request.PathParameters["text"];
            var Recordtypes = request.PathParameters["type"];

            _logger.LogInformation("requests received with: {Searchtext}", Searchtext);
            _logger.LogInformation("requests received with: {Recordtypes}", Recordtypes);

            Task<List<Record>> result = SearchAsync(Searchtext, Recordtypes);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Headers = new Dictionary<string, string> {
                    {"Content-Type", "application/json"}
                },
                Body = JsonSerializer.Serialize(result)
            };

        }

        public async Task<List<Record>> SearchAsync(string searchtext, string recordtypes)
        {
            bool isBundleInstalled = await IsBundleInstalledAsync();
            if (isBundleInstalled)
            {
                HttpClient client = _clientFactory.CreateClient("NsHttpClient");
                UriBuilder builder = GetUriBuilder();

                var query = HttpUtility.ParseQueryString(builder.Query);
                query["script"] = "1186";
                query["deploy"] = "1";
                //TODO validate input
                query["searchtext"] = searchtext;
                query["recordtypes"] = recordtypes;
                builder.Query = query.ToString(); // remove and test

                string url = builder.ToString();

                Console.WriteLine(url);
                HttpResponseMessage response = await InvokeRestletAsync(client, url);
                if (response.IsSuccessStatusCode)
                {
                    String searchResponseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(searchResponseBody);
                    //return search response
                    return System.Text.Json.JsonSerializer.Deserialize<List<Record>>(searchResponseBody);

                }
            }
            else
            {
                //TODO invoke NS webservice
                return new List<Record>();
            }
            return new List<Record>();
        }

        public async Task<bool> IsBundleInstalledAsync()
        {
            HttpClient client = _clientFactory.CreateClient("NsHttpClient");
            UriBuilder builder = GetUriBuilder();

            var query = HttpUtility.ParseQueryString(builder.Query);
            query["script"] = "1186";
            query["deploy"] = "1";
            query["ping"] = "true";
            builder.Query = query.ToString();

            string url = builder.ToString();

            Console.WriteLine(url);
            HttpResponseMessage response = await InvokeRestletAsync(client, url);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(responseBody);
                return System.Text.Json.JsonSerializer.Deserialize<Bundle>(responseBody).BundleInstalled;

            }
            return false;
        }

        private static async Task<HttpResponseMessage> InvokeRestletAsync(HttpClient client, String url)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
                request.Headers.TryAddWithoutValidation("Authorization", "<>");

                return await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                throw;

            }

        }

        private UriBuilder GetUriBuilder()
        {
            UriBuilder uriBuilder = new UriBuilder("https://tstdrv1291203.restlets.api.netsuite.com/app/site/hosting/restlet.nl");
            return uriBuilder;
        }

        public class SearchTerm
        {
            [JsonPropertyName("searchtext")]
            public String Searchtext { get; set; }
            [JsonPropertyName("recordtypes")]
            public String Recordtypes { get; set; }
        }


        public class Bundle
        {
            [JsonPropertyName("bundleInstalled")]
            public Boolean BundleInstalled { get; set; }
        }

        public class Record
        {
            [JsonPropertyName("recordType")]
            public String RecordType { get; set; }
            [JsonPropertyName("internalId")]
            public String InternalId { get; set; }
            [JsonPropertyName("email")]
            public String Email { get; set; }
            [JsonPropertyName("entityId")]
            public String EntityId { get; set; }
            [JsonPropertyName("tranId")]
            public String TranId { get; set; }
            [JsonPropertyName("title")]
            public String Title { get; set; }
            [JsonPropertyName("name")]
            public String Name { get; set; }
            [JsonPropertyName("netsuiteType")]
            public String NetsuiteType { get; set; }
            [JsonPropertyName("_title")]
            public String _Title { get; set; }
            [JsonPropertyName("_subTitle")]
            public String _SubTitle { get; set; }
            [JsonPropertyName("displayRecordType")]
            public String DisplayRecordType { get; set; }
        }
    }
}
