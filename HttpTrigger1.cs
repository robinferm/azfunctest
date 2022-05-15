using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Company.Function
{
    public static class HttpTrigger1
    {
        [FunctionName("HttpTrigger1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("TestTable");

                string requestBody = await new System.IO.StreamReader(req.Body).ReadToEndAsync();
                
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                string dateFrom = data.dateFrom.ToString("o");
                string dateTo = data.dateTo.ToString("o");
                
                var res = await GetEntitiesFromTable(table, dateFrom, dateTo);
                return new OkObjectResult(res);
            }
            catch(Exception e){
                log.LogError(e.Message);
                return new BadRequestObjectResult(e);
            }
        }

        private static async Task<List<MyTableEntity>> GetEntitiesFromTable(CloudTable table, string dateFrom, string dateTo)
        {
            TableQuerySegment<MyTableEntity> querySegment = null;
            var entities = new List<MyTableEntity>();
            string filter = $"(Timestamp ge datetime'{dateFrom}' and Timestamp lt datetime'{dateTo}')";
            TableQuery<MyTableEntity> query = new TableQuery<MyTableEntity>().Where(filter);

            do
            {
                querySegment = await table.ExecuteQuerySegmentedAsync(query, querySegment?.ContinuationToken);
                entities.AddRange(querySegment.Results);
            } while (querySegment.ContinuationToken != null);

            return entities;
        }
    }
}
