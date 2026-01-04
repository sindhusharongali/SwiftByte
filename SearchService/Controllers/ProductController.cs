namespace SearchService.Controllers;

using Microsoft.AspNetCore.Mvc;
using Elastic.Clients.Elasticsearch;
using SearchService.Models;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ElasticsearchClient _elasticsearchClient;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ElasticsearchClient elasticsearchClient, ILogger<ProductController> logger)
    {
        _elasticsearchClient = elasticsearchClient;
        _logger = logger;
    }

    [HttpGet("search")]
    public async Task<ActionResult<SearchResponse>> SearchMenuItems([FromQuery] string query, [FromQuery] string? category = null)
    {
        try
        {
            _logger.LogInformation("Searching for menu items with query: {Query}, category: {Category}", query, category ?? "all");

            var searchRequest = new SearchRequest("menu_items")
            {
                Query = new BoolQuery
                {
                    Must = new List<Query>
                    {
                        new MatchQuery { Field = "name", Query = query }
                        | new MatchQuery { Field = "description", Query = query }
                    },
                    Filter = category != null ? new List<Query>
                    {
                        new TermQuery { Field = "category.keyword", Value = category }
                    } : null
                },
                From = 0,
                Size = 20
            };

            var response = await _elasticsearchClient.SearchAsync<MenuItem>(searchRequest);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Elasticsearch error: {Error}", response.ApiCallDetails?.OriginalException?.Message);
                return StatusCode(500, "Search service error");
            }

            var items = response.Documents.ToList();
            return Ok(new SearchResponse 
            { 
                Total = response.Total, 
                Items = items,
                Query = query
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching menu items");
            return StatusCode(500, "An error occurred while searching");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MenuItem>> GetMenuItem(Guid id)
    {
        try
        {
            var response = await _elasticsearchClient.GetAsync<MenuItem>("menu_items", id.ToString());

            if (!response.Found)
            {
                return NotFound();
            }

            return Ok(response.Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving menu item");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost("index")]
    public async Task<IActionResult> IndexMenuItem([FromBody] MenuItem menuItem)
    {
        try
        {
            var response = await _elasticsearchClient.IndexAsync(menuItem, idx => idx
                .Index("menu_items")
                .Id(menuItem.Id.ToString()));

            if (!response.IsValidResponse)
            {
                _logger.LogError("Indexing failed: {Error}", response.ApiCallDetails?.OriginalException?.Message);
                return StatusCode(500, "Indexing failed");
            }

            return Accepted(new { id = menuItem.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing menu item");
            return StatusCode(500, "An error occurred");
        }
    }
}

public record SearchResponse
{
    public long Total { get; set; }
    public List<MenuItem> Items { get; set; } = new();
    public string Query { get; set; } = string.Empty;
}
