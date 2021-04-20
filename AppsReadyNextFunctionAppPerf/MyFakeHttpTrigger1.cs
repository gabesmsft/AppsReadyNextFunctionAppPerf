using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DevBootcampPrecompiledFunctions
{
    public static class MyFakeHttpTrigger1
    {
        [FunctionName("MyFakeHttpTrigger1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            throw new System.IO.InvalidDataException("invalid data");

            /*
            try
            {
                throw new System.IO.InvalidDataException("invalid data");
            }

            catch (Exception ex)
            {
                log.LogInformation("invalid data exception occurred :( while passing in argument named invalid data.");
            }
            */

            log.LogInformation("C# HTTP trigger function processed a request.");

            return (ActionResult)new OkObjectResult($"Well, at least it didn't return an HTTP error");
        }
    }
}
