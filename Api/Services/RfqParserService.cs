using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace Stronghold.EnterpriseEstimating.Api.Services;

public class RfqParserService
{
    private readonly HttpClient _http;
    private readonly string _model;

    private static readonly string[] CanonicalPositions = new[]
    {
        "Project Manager", "General Foreman", "Foreman",
        "Pipefitter Journeyman", "Pipefitter Helper",
        "Welder Journeyman", "Welder Helper",
        "Boilermaker Journeyman", "Boilermaker Helper",
        "Electrician Journeyman", "Millwright Journeyman",
        "Instrument Tech", "NDT Technician", "Crane Operator", "Rigger",
        "Scaffold Builder", "Safety Watch", "Fire Watch", "Hole Watch", "Driver/Teamster"
    };

    public RfqParserService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _http = httpClientFactory.CreateClient("groq");
        _model = config["Ai:GroqModel"] ?? "llama-3.3-70b-versatile";
    }

    public async Task<AiChatResponse> ParseAsync(IFormFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        string text;

        try
        {
            text = ext switch
            {
                ".pdf"  => await ExtractPdfTextAsync(file, ct),
                ".docx" => await ExtractDocxTextAsync(file, ct),
                _       => throw new InvalidOperationException($"Unsupported file type: {ext}")
            };
        }
        catch (Exception ex)
        {
            return new AiChatResponse($"Could not read the document: {ex.Message}", new List<JsonElement>());
        }

        if (string.IsNullOrWhiteSpace(text))
            return new AiChatResponse("The document appears to be empty or could not be read.", new List<JsonElement>());

        // Truncate to ~8000 chars to stay within token limits
        if (text.Length > 8000)
            text = text[..8000] + "\n[Document truncated — showing first 8000 characters]";

        var positionList = string.Join(", ", CanonicalPositions);
        var prompt = $$"""
You are an RFQ data extraction assistant for an industrial estimating system.
Extract structured information from the following RFQ/scope-of-work document.

Return ONLY a valid JSON object — no markdown, no prose outside the JSON.
REQUIRED SCHEMA:
{
  "response": "<1-2 sentence summary of what was found in the document>",
  "actions": [
    { "action": "fill_header", "fields": { "name": "...", "client": "...", "city": "...", "state": "...", "jobType": "..." } },
    { "action": "set_dates", "start_date": "YYYY-MM-DD", "end_date": "YYYY-MM-DD", "days": N },
    { "action": "add_labor_rows", "rows": [ { "position": "...", "shift": "Day", "qty": N } ] }
  ]
}

EXTRACTION RULES:
- Only emit actions for fields you find with reasonable confidence.
- Canonical position names to use: {{positionList}}
- Map any role found in the document to the nearest canonical name.
- If a start/end date range is found, compute days = (end - start inclusive).
- Client = the REQUESTING company in the RFQ, not the contractor.
- jobType: one of Lump Sum, T&M, Maintenance, Installation, Consulting, Repair, Other.
- If shift is not specified, use "Day".
- Return empty actions array [] if the document has no estimating-relevant data.
- Do NOT fabricate data — only extract what is explicitly in the document.

RFQ DOCUMENT TEXT:
{{text}}
""";

        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            response_format = new { type = "json_object" },
            temperature = 0.05,
            max_tokens = 2000,
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync("/openai/v1/chat/completions", content, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            return new AiChatResponse($"AI extraction failed: {err[..Math.Min(200, err.Length)]}", new List<JsonElement>());
        }

        var body = await resp.Content.ReadAsStringAsync(ct);
        return ParseGroqJson(body);
    }

    private static async Task<string> ExtractPdfTextAsync(IFormFile file, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        ms.Position = 0;

        var sb = new StringBuilder();
        using var doc = PdfDocument.Open(ms);
        foreach (var page in doc.GetPages())
            sb.AppendLine(page.Text);

        return sb.ToString();
    }

    private static async Task<string> ExtractDocxTextAsync(IFormFile file, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        ms.Position = 0;

        using var wordDoc = WordprocessingDocument.Open(ms, isEditable: false);
        var body = wordDoc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;

        var sb = new StringBuilder();
        foreach (var para in body.Elements<Paragraph>())
            sb.AppendLine(para.InnerText);

        return sb.ToString();
    }

    private static AiChatResponse ParseGroqJson(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var contentStr = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            var trimmed = contentStr.Trim();
            if (trimmed.StartsWith("```"))
            {
                var start = trimmed.IndexOf('{');
                var end = trimmed.LastIndexOf('}');
                if (start >= 0 && end > start) trimmed = trimmed[start..(end + 1)];
            }

            using var argsDoc = JsonDocument.Parse(trimmed);
            var args = argsDoc.RootElement;
            var response = args.TryGetProperty("response", out var r) ? r.GetString() ?? "" : "";
            var actions = new List<JsonElement>();
            if (args.TryGetProperty("actions", out var actEl) && actEl.ValueKind == JsonValueKind.Array)
                foreach (var a in actEl.EnumerateArray())
                    actions.Add(a.Clone());

            return new AiChatResponse(response, actions);
        }
        catch (Exception ex)
        {
            return new AiChatResponse($"Could not parse AI response: {ex.Message}", new List<JsonElement>());
        }
    }
}
