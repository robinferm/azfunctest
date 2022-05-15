using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Company.Function
{
    public static class HttpTrigger2
    {
        [FunctionName("HttpTrigger2")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string guid = data.guid;

            var res = await GetBlob(guid);
            return new OkObjectResult(res);
        }

        static async Task<string> GetBlob(string guid){
            var containerName = "test-container";
            BlobContainerClient containerClient = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), containerName);
            containerClient.CreateIfNotExists();

            BlobClient blobClient = containerClient.GetBlobClient(guid);
            var res = await blobClient.OpenReadAsync();
            var reader = new StreamReader(res);
            var data = reader.ReadToEnd();
            return data;
        }
    }
}
