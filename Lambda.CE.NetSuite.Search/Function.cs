using System;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Celigo.CloudExtend.Netsuite.NetsuiteProxy;
using System.Collections.Generic;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Lambda.CloudExtend.NetSuite.Search
{
    public class Function
    {
        private static readonly ILogger<Function> _logger;
        private static readonly NetsuiteProxy NetsuiteProxy;
        static Function()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection()
            .AddOptions()
            .AddLogging(builder =>
            {
                var configLogLevel = configuration["Logging:LogLevel:Default"];
                if (configLogLevel != null && Enum.TryParse<LogLevel>(configLogLevel, out LogLevel minLogLevel))
                {
                    builder.SetMinimumLevel(minLogLevel);
                }

                builder.AddConsole();
            })
            .AddSingleton<NetsuiteProxy>();

            string clientName = "NsHttpClient";
            services.AddHttpClient(clientName, (_, client) => {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            var serviceProvider = services.BuildServiceProvider();

            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger<Function>();

            NetsuiteProxy = serviceProvider.GetRequiredService<NetsuiteProxy>();
        }


        /// <summary>
        /// A Lambda function to respond to HTTP Get methods from API Gateway
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The API Gateway response.</returns>
        //public APIGatewayProxyResponse Get(APIGatewayProxyRequest request, ILambdaContext context)
        //{
        //    context.Logger.LogLine("Get Request\n");

        //    var response = new APIGatewayProxyResponse
        //    {
        //        StatusCode = (int)HttpStatusCode.OK,
        //        Body = "Hello AWS Serverless",
        //        Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
        //    };

        //    return response;
        //}

        public APIGatewayProxyResponse Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            return NetsuiteProxy.HandleSearchRequest(request);
            //var response = new APIGatewayProxyResponse
            //{
            //    StatusCode = (int)System.Net.HttpStatusCode.OK,
            //    Body = "Hello AWS Serverless",
            //    Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            //};

            //return response;
        }
    }
}
