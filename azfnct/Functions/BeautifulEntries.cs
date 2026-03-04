using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using nampacx.docintel.Services;

namespace nampacx.docintel.Functions;

public class BeautifulEntries
{
    private readonly ILogger<BeautifulEntries> _logger;
    private readonly IEntryExtractionService _entryExtractionService;

    public BeautifulEntries(ILogger<BeautifulEntries> logger, IEntryExtractionService entryExtractionService)
    {
        _logger = logger;
        _entryExtractionService = entryExtractionService;
    }

    [Function("BeautifulEntries")]
    [OpenApiOperation(operationId: "BeautifulEntries", tags: ["Entries"], Summary = "Extract invoice entries", Description = "Extracts invoice position entries from paragraph-based JSON and returns a structured result.")]
    [OpenApiParameter(name: "filePath", In = ParameterLocation.Query, Required = false, Type = typeof(string), Summary = "Optional local file path", Description = "Optional path to a local JSON file. If provided, request body is ignored.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(string), Required = false, Description = "JSON payload containing a paragraphs array.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Structured invoice entries")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Description = "Invalid input or JSON payload")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("BeautifulEntries function processing request.");

        try
        {
            string? filePath = req.Query["filePath"];
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                var fileResult = _entryExtractionService.ExtractEntriesFromFile(filePath);
                return new OkObjectResult(fileResult);
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new BadRequestObjectResult("Request body cannot be empty. Provide a JSON payload or use the 'filePath' query parameter.");
            }

            var result = _entryExtractionService.ExtractEntries(requestBody);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract entries.");
            return new BadRequestObjectResult($"Failed to extract entries: {ex.Message}");
        }
    }
}
