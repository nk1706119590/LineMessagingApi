using Line.Messaging;
using Line.Messaging.Webhooks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace FunctionAppSample
{
    /// <summary>
    /// Main function
    /// </summary>
    public class HttpTriggerFunction
    {
        static LineMessagingClient lineMessagingClient;

        static HttpTriggerFunction()
        {
            lineMessagingClient = new LineMessagingClient(
                Environment.GetEnvironmentVariable("JOALJaFzXSS1/Iw0lRElqFUMiBHUF4LhFisSOpo9WpfG4Ju5l+o+o5yTWeYVIqOhwPafmf63J283XV1uMahQlwgdfCxzlKipJygVt7h4z9Fbt0mq+eQivXcy4jj4oyvvH8a6cp39m8SO/3I9OyLmVgdB04t89/1O/w1cDnyilFU="));
            var sp = ServicePointManager.FindServicePoint(new Uri("https://api.line.me"));
            sp.ConnectionLeaseTimeout = 60 * 1000;
        }

        /// <summary>
        /// Main run method
        /// </summary>
        /// <param name="req">HttpRequestMessage</param>
        /// <param name="log">TraceWriter</param>
        /// <returns>Result</returns>
        public static async Task<IActionResult> Run(HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                log.Info("C# HTTP trigger function processed a request.");
                var channelSecret = Environment.GetEnvironmentVariable("7410da12768dbb3db2632dd64ed33a12");
                var events = await req.GetWebhookEventsAsync(channelSecret);

                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var eventSourceState = await TableStorage<EventSourceState>.CreateAsync(connectionString, "eventsourcestate");
                var blobStorage = await BlobStorage.CreateAsync(connectionString, "linebotcontainer");

                // Create the LineBotApp and run it.
                var app = new LineBotApp(lineMessagingClient, eventSourceState, blobStorage, log);
                await app.RunAsync(events);
            }
            catch (InvalidSignatureException e)
            {
                return new ObjectResult(e.Message)
                {
                    StatusCode = (int)HttpStatusCode.Forbidden
                };
            }
            catch (LineResponseException e)
            {
                log.Error(e.ToString());
                var debugUserId = Environment.GetEnvironmentVariable("DebugUser");
                if (debugUserId != null)
                {
                    await lineMessagingClient.PushMessageAsync(debugUserId, $"{e.StatusCode}({(int)e.StatusCode}), {e.ResponseMessage.ToString()}");
                }
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                var debugUserId = Environment.GetEnvironmentVariable("DebugUser");
                if (debugUserId != null)
                {
                    await lineMessagingClient.PushMessageAsync(debugUserId, e.Message);
                }
            }

            return new OkObjectResult("OK");
        }    
    } 
}
