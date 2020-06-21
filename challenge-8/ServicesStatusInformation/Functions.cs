using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using System.Collections.Generic;
using Microsoft.Azure.Documents;
using ServicesStatusInformation.Model;

namespace ServicesStatusInformation
{
    public static class Functions
    {
        [FunctionName("services")]
        public static IActionResult GetServices(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(databaseName: "SantasServicesDb", collectionName: "SantasServicesCollection", ConnectionStringSetting = "AzureSantasServicesDbConnection")]IEnumerable<SantasServices> services,
            ILogger log)
        {
            log.LogInformation("GetServices HTTP trigger function processed a request.");
            return new OkObjectResult(services);
        }

        [FunctionName("negotiate")]
        public static SignalRConnectionInfo Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
            [SignalRConnectionInfo(HubName = "status", ConnectionStringSetting = "AzureSantasServicesSignalRConnection")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

        [FunctionName("serviceschanged")]
        public static Task ServicesChanged([CosmosDBTrigger(
            databaseName: "SantasServicesDb",
            collectionName: "SantasServicesCollection",
            ConnectionStringSetting = "AzureSantasServicesDbConnection",
            LeaseCollectionName = "leases",
            CreateLeaseCollectionIfNotExists = true)]IReadOnlyList<Document> services,
            [SignalR(HubName = "status", ConnectionStringSetting = "AzureSantasServicesSignalRConnection")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            log.LogInformation("ServicesChanged CosmosDBTrigger was called.");

            return signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "updated",
                    Arguments = new[] { services }
                });
        }
    }
}
