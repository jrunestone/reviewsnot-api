using System.Net;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ReviewsnotApi;

public class Reviews
{
    private readonly ILogger _logger;

    private class Review
    {
        public string? Id { get; set; }
        public int Rating { get; set; }
        public string? Text { get; set; }
    }

    public Reviews(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Reviews>();
    }

    [Function("Reviews")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "reviews/{id?}")] HttpRequestData req, 
        string? id)
    {
        using var client = GetClient();
        var container = client.GetContainer("cosmos-reviewsnot", "reviews");

        try
        {
            if (!string.IsNullOrEmpty(id))
            {
                var review = id.ToLower() == "random" ? 
                    await GetRandomReview(container) : 
                    await GetReview(container, id);
                
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(review);
                return response;
            }
            else
            {
                var reviews = await GetReviews(container);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(reviews);
                return response;
            }
        }
        catch (CosmosException e)
        {
            return req.CreateResponse(e.StatusCode);
        }
    }

    private async Task<List<Review>> GetReviews(Container container)
    {
        var query = container
            .GetItemLinqQueryable<Review>();
        
        var iterator = query.ToFeedIterator();
        var reviews = new List<Review>();

        do
        {
            var result = await iterator.ReadNextAsync();
            reviews.AddRange(result.Resource);
        } while (iterator.HasMoreResults);

        return reviews;
    }

    private async Task<Review> GetReview(Container container, string id)
    {
        return await container.ReadItemAsync<Review>(id, new PartitionKey(id));
    }
    
    private async Task<Review> GetRandomReview(Container container)
    {
        var allReviews = await GetReviews(container);
        var randomIndex = new Random().Next(0, allReviews.Count);
        return allReviews[randomIndex];
    }
    
    private CosmosClient GetClient()
    {
        var connStr = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING"); 
        var endpointStr = Environment.GetEnvironmentVariable("COSMOS_ENDPOINT");

        if (!string.IsNullOrEmpty(connStr))
        {
            return new CosmosClient(connStr);
        }
        
        if (!string.IsNullOrEmpty(endpointStr))
        {
            return new CosmosClient(endpointStr, new DefaultAzureCredential());
        }

        throw new InvalidOperationException("Missing database connection string");
    }
}