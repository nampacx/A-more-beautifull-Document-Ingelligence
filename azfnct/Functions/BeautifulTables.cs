using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using nampacx.docintel.Models;
using nampacx.docintel.Services;

namespace nampacx.docintel.Functions;

public class BeautifulTables
{
    private readonly ILogger<BeautifulTables> _logger;
    private readonly ITableTransformService _tableTransformService;

    public BeautifulTables(ILogger<BeautifulTables> logger, ITableTransformService tableTransformService)
    {
        _logger = logger;
        _tableTransformService = tableTransformService;
    }

    [Function("BeautifulTables")]
    [OpenApiOperation(operationId: "BeautifulTables", tags: ["Tables"], Summary = "Transform Document Intelligence tables", Description = "Accepts a JSON array of DocumentTable objects from Azure Document Intelligence and returns a normalized, generic table schema.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(DocumentTableDto[]), Required = true, Description = "JSON array of DocumentTable objects")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Normalized table schema as JSON")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Description = "Invalid or empty request body")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("BeautifulTables function processing request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(requestBody))
        {
            return new BadRequestObjectResult("Request body cannot be empty. Provide a JSON array of DocumentTable objects.");
        }

        DocumentTableDto[] tables;
        try
        {
            tables = JsonSerializer.Deserialize<DocumentTableDto[]>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize request body.");
            return new BadRequestObjectResult($"Invalid JSON: {ex.Message}");
        }

        var genericSchema = _tableTransformService.BuildGenericTables(tables);

        return new OkObjectResult(genericSchema);
    }
}
