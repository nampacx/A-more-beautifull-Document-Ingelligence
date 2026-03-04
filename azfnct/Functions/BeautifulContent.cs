using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
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
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(string), Required = true, Description = "Full Document Intelligence JSON payload")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Transformed page and paragraph content")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Description = "Invalid input or JSON payload")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("BeautifulContent function processing request.");

        try
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return new BadRequestObjectResult("Request body cannot be empty. Provide a JSON payload.");
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