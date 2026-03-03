using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using nampacx.docintel.Services;

namespace nampacx.docintel.Functions;

public class BeautifulContent
{
    private readonly ILogger<BeautifulContent> _logger;
    private readonly IContentTransformService _contentTransformService;

    public BeautifulContent(ILogger<BeautifulContent> logger, IContentTransformService contentTransformService)
    {
        _logger = logger;
        _contentTransformService = contentTransformService;
    }

    [Function("BeautifulContent")]
    [OpenApiOperation(operationId: "BeautifulContent", tags: ["Content"], Summary = "Transform document pages and paragraphs", Description = "Accepts a full Document Intelligence analyze result JSON and returns page and paragraph content.")]
    [OpenApiParameter(name: "filePath", In = ParameterLocation.Query, Required = false, Type = typeof(string), Summary = "Optional local file path", Description = "Optional path to a local JSON file. If provided, request body is ignored.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(string), Required = false, Description = "Full Document Intelligence JSON payload")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Transformed page and paragraph content")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Description = "Invalid input or JSON payload")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("BeautifulContent function processing request.");

        try
        {
            string? filePath = req.Query["filePath"];

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                var fileResult = _contentTransformService.ExtractPageAndParagraphContentFromFile(filePath);
                return new OkObjectResult(fileResult);
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new BadRequestObjectResult("Request body cannot be empty. Provide a JSON payload or use the 'filePath' query parameter.");
            }

            var result = _contentTransformService.ExtractPageAndParagraphContent(requestBody);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transform content.");
            return new BadRequestObjectResult($"Failed to transform content: {ex.Message}");
        }
    }
}