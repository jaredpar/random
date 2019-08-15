using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Configuration;
using DevOps.Util.DotNet;

namespace DevOps.Functions
{
    public static class Functions
    {
        [FunctionName("build")]
        public static async Task<IActionResult> OnBuild(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
            [Queue("build-complete", Connection = "AzureWebJobsStorage")] IAsyncCollector<string> queueCollector)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            int id = data.resource.id;
            await queueCollector.AddAsync(id.ToString());
            return new OkResult();
        }

        [FunctionName("build-upload")]
        public static async Task OnBuildComplete(
            [QueueTrigger("build-complete", Connection = "AzureWebJobsStorage")] string message,
            ILogger logger)
        {
            var buildId = int.Parse(message);
            var connectionString = ConfigurationManager.AppSettings.Get("SQL_CONNECTION_STRING");
            using var cloneTimeUtil = new CloneTimeUtil(connectionString, logger);
            await cloneTimeUtil.UpdateBuildAsync(buildId);
        }
    }
}
