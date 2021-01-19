using System;
using System.Web;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Amazon.Lambda.Core;


[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Celigo.CE.Nestsuite.Search
{
    public class Function
    {
        public async Task<List<Record>> FunctionHandlerAsync(string search, ILambdaContext context)
        {
            return await new NetSuiteProxy().SearchAsync(search);
        }
    }


    public class NetSuiteProxy
    {
        public async Task<List<Record>> SearchAsync(string request)
        {
            var searchReq = System.Text.Json.JsonSerializer.Deserialize<SearchTerm>(request);
            LambdaLogger.Log(searchReq.Searchtext);
            LambdaLogger.Log(searchReq.Recordtypes);

            bool isBundleInstalled = await IsBundleInstalledAsync();
            if (isBundleInstalled)
            {
                HttpClient client = new HttpClient();
                UriBuilder builder = GetUriBuilder();

                var query = HttpUtility.ParseQueryString(builder.Query);
                query["script"] = "1186";
                query["deploy"] = "1";
                //TODO validate input
                query["searchtext"] = searchReq.Searchtext;
                query["recordtypes"] = searchReq.Recordtypes;
                builder.Query = query.ToString(); // remove and test

                string url = builder.ToString();

                Console.WriteLine(url);
                HttpResponseMessage response = await InvokeRestletAsync(client, url);
                if (response.IsSuccessStatusCode)
                {
                    String searchResponseBody = await response.Content.ReadAsStringAsync();
                    LambdaLogger.Log(searchResponseBody);
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
            HttpClient client = new HttpClient(); //TODO DI use HttpClientFactory
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
                LambdaLogger.Log(responseBody);
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
                request.Headers.TryAddWithoutValidation("Authorization", <>);

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
            //uriBuilder.Port = -1; //TODO test
            return uriBuilder;
        }
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