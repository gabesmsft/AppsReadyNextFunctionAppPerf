using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net;
using System.Data.SqlClient;
using System.Data;
using FakePerfClassLibrary;
using System.Diagnostics;

namespace DevBootcampPrecompiledFunctions
{
    public static class FakeEventHubTrigger1
    {
        static string url = "https://gabefakeexternalservicerandomperf.azurewebsites.net";
        static Uri baseAddress = new Uri(url);
        static readonly HttpClient client = new HttpClient() { BaseAddress = baseAddress };

        [FunctionName("FakeEventHubTrigger1")]
        public static async Task Run([EventHubTrigger("fakeeh", Connection = "MyEventHubConn", ConsumerGroup = "consumergroup2")] EventData[] events, ILogger log, ExecutionContext context)
        {
            var exceptions = new List<Exception>();

            int i = 0;

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    /*
                    var sqlConn = Environment.GetEnvironmentVariable("SQLAZURECONNSTR_fakeSqlConn");
                    using (SqlConnection conn = new SqlConnection(sqlConn))
                    {
                        conn.Open();

                        var query = "INSERT INTO dbo.FakeTable1 (Message) VALUES (@Message)";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 250).Value = messageBody;
                            // Execute the command and log the # rows affected.
                            var rows = await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    */

                    var FunctionInvocationId = context.InvocationId;
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    FakePerfClass.MysteryMethod1(messageBody);
                    stopWatch.Stop();
                    var ms = stopWatch.ElapsedMilliseconds;
                    string method;
                    if (ms > 1000)
                    {
                        method = "MysteryMethod1";
                        //log.LogInformation($"Method 1 took {stopWatch.ElapsedMilliseconds} ms during FunctionInvocationId {FunctionInvocationId}");
                        log.LogInformation("SlowMethod={method}, Milliseconds={ms},  FunctionInvocationId={FunctionInvocationId}", method, ms, FunctionInvocationId);
                    }
                    stopWatch.Reset();

                    stopWatch.Start();
                    FakePerfClass.MysteryMethod2(messageBody);
                    stopWatch.Stop();
                    ms = stopWatch.ElapsedMilliseconds;

                    if (ms > 1000)
                    {
                        method = "MysteryMethod2";
                        log.LogInformation("SlowMethod={method}, Milliseconds={ms},  FunctionInvocationId={FunctionInvocationId}", method, ms, FunctionInvocationId);
                    }
                    stopWatch.Reset();

                    stopWatch.Start();
                    FakePerfClass.MysteryMethod3(messageBody);
                    stopWatch.Stop();

                    ms = stopWatch.ElapsedMilliseconds;

                    if (ms > 1000)
                    {
                        method = "MysteryMethod3";
                        log.LogInformation("SlowMethod={method}, Milliseconds={ms},  FunctionInvocationId={FunctionInvocationId}", method, ms, FunctionInvocationId); ;
                    }
                    stopWatch.Reset();

                    var message = new HttpRequestMessage(HttpMethod.Get, "/api/fake");
                    message.Headers.Add("FakeHeader2", "messageBody");
                    var result = await client.SendAsync(message);
                    
                    //var result = client.SendAsync(message).Result;

                    string content = await result.Content.ReadAsStringAsync();

                    //var result = await client.GetAsync("/api/fake");
                  
                    // Replace these two lines with your processing logic.
                    log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");
                    await Task.Yield();

                    i++;
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }

    }
}
