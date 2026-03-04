using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
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
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(string), Required = true, Description = "JSON payload containing a paragraphs array.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Structured invoice entries")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Description = "Invalid input or JSON payload")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("BeautifulEntries function processing request.");

        try
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new BadRequestObjectResult("Request body cannot be empty. Provide a JSON payload.");
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
