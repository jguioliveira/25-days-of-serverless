using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using SantaServicesSeed.Model;

namespace SantaServicesSeed.Controllers
{
    public class SantaServicesController : Controller
    {
        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The database we will create
        private Database database;

        // The container we will create.
        private Container container;


        // The Azure Cosmos DB endpoint for running this sample.
        private readonly string _endpointUri;

        // The primary key for the Azure Cosmos account.
        private readonly string _primaryKey;

        // The name of the database and container we will create
        private readonly string _databaseId;
        private readonly string _containerId;

        public SantaServicesController(IConfiguration configuration)
        {
            _endpointUri = configuration.GetValue<string>("EndPointUri");
            _primaryKey = configuration.GetValue<string>("PrimaryKey");
            _databaseId = configuration.GetValue<string>("AzureSantasServicesDbName");
            _containerId = configuration.GetValue<string>("AzureSantasServicesCollectionName");
        }

        public async Task<IActionResult> Index()
        {
            // Create a new instance of the Cosmos Client
            this.cosmosClient = new CosmosClient(_endpointUri, _primaryKey, new CosmosClientOptions() { ApplicationName = "CosmosDBDotnetQuickstart" });

            await CreateDatabaseAsync();
            await CreateContainerAsync();

            var services = await GetSantasServicesAsync();

            if (services is null || !services.Any())
            {
                services = GenerateSantasServices();
                await CreateSantasServicesAsync(services);
            }

            ViewBag.StatusServices = StatusServices;

            return View(services);
        }

        [HttpPost]
        public async Task<IActionResult> Index(IEnumerable<SantasServices> services)
        {
            // Create a new instance of the Cosmos Client
            this.cosmosClient = new CosmosClient(_endpointUri, _primaryKey, new CosmosClientOptions() { ApplicationName = "CosmosDBDotnetQuickstart" });

            await CreateDatabaseAsync();
            await CreateContainerAsync();

            foreach (var service in services)
            {
                ItemResponse<SantasServices> serviceResponse = await this.container.ReadItemAsync<SantasServices>(service.Id, new PartitionKey(service.Region));
                var itemBody = serviceResponse.Resource;

                if (itemBody.Status != service.Status)
                {
                    // replace the item with the updated content
                    await this.container.ReplaceItemAsync(service, service.Id, new PartitionKey(itemBody.Region));
                }
            }

            ViewBag.StatusServices = StatusServices;

            return View(services);
        }

        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            this.database = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseId);
            Console.WriteLine("Created Database: {0}\n", this.database.Id);
        }

        /// <summary>
        /// Create the container if it does not exist. 
        /// Specifiy "/LastName" as the partition key since we're storing family information, to ensure good distribution of requests and storage.
        /// </summary>
        /// <returns></returns>
        private async Task CreateContainerAsync()
        {
            // Create a new container
            this.container = await this.database.CreateContainerIfNotExistsAsync(_containerId, "/Region", 400);
            Console.WriteLine("Created Container: {0}\n", this.container.Id);
        }

        /// <summary>
        /// Add Family items to the container
        /// </summary>
        private async Task CreateSantasServicesAsync(IEnumerable<SantasServices> services)
        {
            foreach (var service in services)
            {
                _ = await container.CreateItemAsync(service, new PartitionKey(service.Region));
            }


            //try
            //{
            //    // Read the item to see if it exists.  
            //    ItemResponse<SantasServices> serviceResponse = await this.container.ReadItemAsync<SantasServices>(service.Id, new PartitionKey(service.Region));
            //    Console.WriteLine("Item in database with id: {0} already exists\n", serviceResponse.Resource.Id);
            //}
            //catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            //{
            //    // Create an item in the container representing the Andersen family. Note we provide the value of the partition key for this item, which is "Andersen"
            //    ItemResponse<SantasServices> serviceResponse = await this.container.CreateItemAsync(service, new PartitionKey(service.Region));

            //    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
            //    Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", serviceResponse.Resource.Id, serviceResponse.RequestCharge);
            //}


        }

        /// <summary>
        /// Run a query (using Azure Cosmos DB SQL syntax) against the container
        /// Including the partition key value of lastName in the WHERE filter results in a more efficient query
        /// </summary>
        private async Task<IEnumerable<SantasServices>> GetSantasServicesAsync()
        {
            var sqlQueryText = "SELECT * FROM c";

            Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<SantasServices> queryResultSetIterator = this.container.GetItemQueryIterator<SantasServices>(queryDefinition);

            List<SantasServices> services = new List<SantasServices>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<SantasServices> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (SantasServices service in currentResultSet)
                {
                    services.Add(service);
                    Console.WriteLine("\tRead {0}\n", service);
                }
            }

            return services;
        }

        private IEnumerable<SantasServices> GenerateSantasServices()
        {
            return new List<SantasServices>
            {
                new SantasServices
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "CustomerService",
                    Region = "East US",
                    Status = SantasServicesStatus.Closed
                },
                new SantasServices
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "PaymentService",
                    Region = "East US",
                    Status = SantasServicesStatus.Closed
                },
                new SantasServices
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "OrdersService",
                    Region = "East US",
                    Status = SantasServicesStatus.Closed
                },
                new SantasServices
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "StockService",
                    Region = "East US",
                    Status = SantasServicesStatus.Closed
                }
            };
        }

        /// <summary>
        /// Delete an item in the container
        /// </summary>
        private async Task DeleteFamilyItemAsync()
        {
            var partitionKeyValue = "Wakefield";
            var familyId = "Wakefield.7";

            // Delete an item. Note we must provide the partition key value and id of the item to delete
            ItemResponse<Family> wakefieldFamilyResponse = await this.container.DeleteItemAsync<Family>(familyId, new PartitionKey(partitionKeyValue));
            Console.WriteLine("Deleted Family [{0},{1}]\n", partitionKeyValue, familyId);
        }

        private IEnumerable<SelectListItem> StatusServices => new List<SelectListItem>
        {
            new SelectListItem{ Text = SantasServicesStatus.Open.ToString(), Value = SantasServicesStatus.Open.GetHashCode().ToString() },
            new SelectListItem{ Text = SantasServicesStatus.Closed.ToString(), Value = SantasServicesStatus.Closed.GetHashCode().ToString() },
            new SelectListItem{ Text = SantasServicesStatus.Ongoing.ToString(), Value = SantasServicesStatus.Ongoing.GetHashCode().ToString() }
        };
    }
}
