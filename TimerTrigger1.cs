using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Company.Function
{
    public class TimerTrigger1
    {
        static readonly HttpClient client = new HttpClient();
        
        private string connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        [FunctionName("TimerTrigger1")]
        public async Task Run(
            [TimerTrigger("0 */5 * * * *")]TimerInfo myTimer,
            ILogger log
            )
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var guid = Guid.NewGuid().ToString();

            try{
                log.LogInformation("Fetching API data");
                var res = await client.GetAsync("https://api.publicapis.org/random?auth=null");
                var content = await res.Content.ReadAsStreamAsync();
                res.EnsureSuccessStatusCode();
                

                // Upload to blob
                log.LogInformation("Uploading to blod");
                WriteToBlob(content, guid);


                
                // save success attempt to table
                log.LogInformation("Saving success attempt to table");
                WriteToTable(true, guid);
            
            }
            catch(Exception e){
                log.LogError(e, "Error fetching data from API" + e.Message);
                log.LogInformation("Saving fail attempt to table");
                WriteToTable(false, guid);
            }
        }

        async void WriteToTable(Boolean success, string guid){
                
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connection);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("TestTable");

                var entity = new MyTableEntity();
                entity.Success = success;
                entity.Guid = guid;
                var addEntryOperation = TableOperation.InsertOrReplace(entity);
                
                await table.CreateIfNotExistsAsync();
                await table.ExecuteAsync(addEntryOperation);
        }

        async void WriteToBlob(System.IO.Stream content, string guid){
            var containerName = "test-container";
            BlobContainerClient containerClient = new BlobContainerClient(connection, containerName);
            containerClient.CreateIfNotExists();

            BlobClient blobClient = containerClient.GetBlobClient(guid);
            await blobClient.UploadAsync(content, true);
        }
    }

    public class MyTableEntity : TableEntity 
    {
        
        private readonly Random rand = new Random();
        public Boolean Success { get; set; }
        public string Guid { get; set; }
        public MyTableEntity()
        {
            this.PartitionKey = rand.Next().ToString();
            this.RowKey = rand.Next().ToString();
        }
    }
}
