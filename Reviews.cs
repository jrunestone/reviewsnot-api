using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ReviewsnotApi
{
    public class Reviews
    {
        private readonly ILogger _logger;

        public Reviews(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Reviews>();
        }

        [Function("Reviews")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }
}
