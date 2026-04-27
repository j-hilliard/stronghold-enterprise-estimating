using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stronghold.EnterpriseEstimating.Api.Services;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly AiService _ai;
    private readonly RfqParserService _rfqParser;

    public AiController(AiService ai, RfqParserService rfqParser)
    {
        _ai = ai;
        _rfqParser = rfqParser;
    }

    private string Username => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? User.FindFirst("username")?.Value ?? "unknown";

    // Always derive company from the verified JWT claim — never trust the request body for this.
    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;

    [HttpPost("chat")]
    public async Task<IActionResult> Chat(
        [FromBody] AiChatRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "Message is required." });

        var companyCode = CompanyCode;
        if (string.IsNullOrEmpty(companyCode))
            return Unauthorized(new { message = "Company context missing from token." });

        try
        {
            var result = await _ai.ChatAsync(request, Username, companyCode, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("AI API error"))
        {
            return StatusCode(502, new { message = "AI service error.", detail = ex.Message });
        }
    }

    [HttpPost("parse-rfq")]
    [RequestSizeLimit(10_485_760)] // 10 MB
    public async Task<IActionResult> ParseRfq([FromForm] IFormFile file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".pdf" and not ".docx")
            return BadRequest(new { message = "Only PDF and DOCX files are supported." });

        try
        {
            var result = await _rfqParser.ParseAsync(file, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "RFQ parsing failed.", detail = ex.Message });
        }
    }
}
