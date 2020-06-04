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
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;

namespace DevBootcampPrecompiledFunctions
{
    public class FakeEventHubTrigger1
    {
        static string url = "https://REPLACEWITHYOURSITENAME.azurewebsites.net";
        static Uri baseAddress = new Uri(url);
        static readonly HttpClient client = new HttpClient() { BaseAddress = baseAddress };

        private readonly TelemetryClient telemetryClient;
        public FakeEventHubTrigger1(TelemetryConfiguration telemetryConfiguration)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        [FunctionName("FakeEventHubTrigger1")]
        public async Task Run([EventHubTrigger("fakeeh1", Connection = "MyEventHubConn", ConsumerGroup = "consumergroup2")] EventData[] events, ILogger log, ExecutionContext context)
        {
            var exceptions = new List<Exception>();

            int i = 0;

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

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
                        log.LogInformation("ILog-SlowMethod={method}, ILog-Milliseconds={ms},  ILog-FunctionInvocationId={FunctionInvocationId}", method, ms, FunctionInvocationId);
                    }
                    stopWatch.Reset();

                    stopWatch.Start();
                    FakePerfClass.MysteryMethod2(messageBody);
                    stopWatch.Stop();
                    ms = stopWatch.ElapsedMilliseconds;

                    if (ms > 1000)
                    {
                        method = "MysteryMethod2";
                        //structured logging via Ilogger:
                        log.LogInformation("ILog-SlowMethod={method}, ILog-Milliseconds={ms},  ILog-FunctionInvocationId={FunctionInvocationId}", method, ms, FunctionInvocationId);

                        //unstructured logging (bad, not very useful for sorting data):
                        log.LogInformation($"Method 2 took {stopWatch.ElapsedMilliseconds} ms during FunctionInvocationId {FunctionInvocationId}");

                        //here is how to use App Insights SDK to log comparably to the above structured ILogger example:

                        var telemetry = new TraceTelemetry("AISDK-Slow Method Detected", SeverityLevel.Warning);
                        telemetry.Properties.Add("AISDK-SlowMethod", method);
                        telemetry.Properties.Add("AISDK-Milliseconds", ms.ToString());
                        telemetry.Properties.Add("AISDK-FunctionInvocationId", FunctionInvocationId.ToString());
                        telemetryClient.TrackTrace(telemetry);
                    }
                    stopWatch.Reset();

                    stopWatch.Start();
                    FakePerfClass.MysteryMethod3(messageBody);
                    stopWatch.Stop();

                    ms = stopWatch.ElapsedMilliseconds;

                    if (ms > 1000)
                    {
                        method = "MysteryMethod3";
                        log.LogInformation("ILog-SlowMethod={method}, ILog-Milliseconds={ms}, ILog-FunctionInvocationId={FunctionInvocationId}", method, ms, FunctionInvocationId);
                    }
                    stopWatch.Reset();

                    //App Insights SDK has built-in functionality for tracking time taken on async calls, using StartOperation/StopOperation.
                    //Here is an example of how to do this.
                    // StartOperation will automatically be stopped when disposed, so don't need to call StopOperation when wrapped in using block


                    using (var operation = telemetryClient.StartOperation<DependencyTelemetry>("MysteryMethod4", FunctionInvocationId.ToString()))
                    {
                        int TaskIntReturned = 0;
                        try
                        {
                            TaskIntReturned = await FakePerfClass.MysteryMethod4(messageBody);
                        }
                        catch (Exception ex)
                        {
                            telemetryClient.TrackException(ex);
                        }
                        finally
                        {
                            var telemetryAsyncExample = new TraceTelemetry("AISDK-Slow Async Method Detected", SeverityLevel.Warning);
                            telemetryAsyncExample.Properties.Add("AISDK-SlowMethodAsync", "MysteryMethod4");
                            telemetryAsyncExample.Properties.Add("AISDK-FunctionInvocationId", FunctionInvocationId.ToString());

                            //TaskIntReturned happens to be the duration of the call, for the sake of this demo.
                            //We are only logging this to demonstrate that App Insights StartOperation can help track the time of async calls. 
                            //In a real-world example, we wouldn't log this.
                            telemetryAsyncExample.Properties.Add("AISDK-TaskIntReturned", TaskIntReturned.ToString());
                            telemetryClient.TrackTrace(telemetryAsyncExample);
                        }
                    }

                    //Another potential way to get time taken on async calls.
                    /* 
                    stopWatch.Start();
                    List<Task<int>> list = new List<Task<int>>();
                    list.Add(FakePerfClass.MysteryMethod4(messageBody));
                    int[] results = await Task.WhenAll(list);
                    stopWatch.Stop();

                    ms = stopWatch.ElapsedMilliseconds;

                    //TaskIntReturned happens to be the duration of the call, for the sake of this demo.
                    //We are only logging this to demonstrate that App Insights StartOperation can help track the time of async calls. 
                    //In a real-world example, we wouldn't log this.
                    log.LogInformation("ILog-SlowMethod={method}, ILog-Milliseconds={ms}, ILog-TaskIntReturned={TaskIntReturned}", "MysteryMethod4", ms, results[0]);

                    stopWatch.Reset();
                    */

                    // We are making an HTTP request to an external API, to demonstrate how this automatically gets logged to the dependencies table in App Insights
                    var message = new HttpRequestMessage(HttpMethod.Get, "/api/fake");
                    message.Headers.Add("FakeHeader2", "messageBody");
                    var result = await client.SendAsync(message);

                    string content = await result.Content.ReadAsStringAsync();

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
