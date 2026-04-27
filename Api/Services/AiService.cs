using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Api.Services;

// ── Request / Response DTOs ───────────────────────────────────────────────────

public record AiChatRequest(
    string Message,
    List<AiChatMessage> History,
    AiContext Context);

public record AiChatMessage(string Role, string Content);

public record AiContext(
    string CompanyCode,
    string CurrentPage,
    int? CurrentEstimateId,
    AiHeaderSnapshot? HeaderSnapshot,
    string? RateBookName,
    int? CurrentRateBookId,
    List<AiRateBookSummary>? AvailableRateBooks,
    List<AiLaborRowContext>? LaborRows = null,
    List<AiEquipmentRowContext>? EquipmentRows = null);

public record AiLaborRowContext(
    string Position, string Shift,
    decimal StRate, decimal OtRate, decimal DtRate,
    decimal StHours, decimal OtHours, decimal Subtotal);

public record AiEquipmentRowContext(
    string Name, string RateType, decimal Rate, int Qty, int Days, decimal Subtotal);

public record AiHeaderSnapshot(
    string? Name, string? Client, string? City, string? State,
    string? StartDate, string? EndDate, int? Days, string? Shift,
    string? JobType, string? JobLetter, string? Branch);

public record AiRateBookSummary(int RateBookId, string Name, string? Client, string? City, string? State, int LaborCount);

public record AiChatResponse(string Response, List<JsonElement> Actions);

// ── Internal context models ───────────────────────────────────────────────────

internal record ContextData(
    List<TemplateInfo> Templates,
    List<ActiveJobInfo> ActiveJobs,
    List<RateBookMetadata> RateBookMeta,
    List<RateBenchmark> RateBenchmarks,
    PipelineSummaryInfo? Pipeline = null);

internal record PipelineSummaryInfo(
    int TotalJobs, int ActiveToday,
    decimal TotalPipelineValue, decimal AwardedValue, decimal SubmittedValue, decimal PendingValue,
    int AwardedCount, int SubmittedCount, int PendingCount, int LostCount);

internal record TemplateInfo(int Id, string Name, string? Description, int RowCount, string PositionSummary);
internal record ActiveJobInfo(string Number, string Name, string? Client, string? StartDate, string? EndDate, string Status, int HeadCount);
internal record RateBookMetadata(int Id, string Name, string? Client, string? City, string? State, int LaborCount);
internal record RateBenchmark(string Position, string? Client, double AvgStRate, double AvgOtRate, double AvgDtRate, int DataPoints);

// ── Service ───────────────────────────────────────────────────────────────────

public class AiService
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ToolExecutorService _toolExecutor;
    private readonly IMemoryCache _cache;

    private readonly string _chatPath;

    public AiService(IHttpClientFactory httpClientFactory, IConfiguration config, IDbContextFactory<AppDbContext> dbFactory, ToolExecutorService toolExecutor, IMemoryCache cache)
    {
        var provider = config["Ai:Provider"] ?? "groq";
        if (provider == "azure")
        {
            _http = httpClientFactory.CreateClient("azure-ai");
            _model = config["Ai:AzureFoundryModel"] ?? "gpt-4.1-mini-1";
            _chatPath = "chat/completions";
        }
        else
        {
            _http = httpClientFactory.CreateClient("groq");
            _model = config["Ai:GroqModel"] ?? "llama-3.3-70b-versatile";
            _chatPath = "openai/v1/chat/completions";
        }
        _dbFactory = dbFactory;
        _toolExecutor = toolExecutor;
        _cache = cache;
    }

    public async Task<AiChatResponse> ChatAsync(
        AiChatRequest request,
        string username,
        string companyCode,
        CancellationToken ct = default)
    {
        var ctxData = await GetContextDataAsync(companyCode, ct);

        // If the message contains labor keywords, fire header + labor agents in parallel
        if (IsLaborRequest(request.Message))
        {
            var headerTask = RunHeaderAgentAsync(request, ctxData, companyCode, ct);
            var laborTask = RunLaborAgentAsync(request, ctxData, ct);
            await Task.WhenAll(headerTask, laborTask);

            var header = await headerTask;
            var labor  = await laborTask;

            // Deterministic guard: header agent must NEVER emit add_labor_rows in parallel mode
            var headerActionsFiltered = header.Actions
                .Where(a =>
                {
                    try
                    {
                        using var d = JsonDocument.Parse(a.GetRawText());
                        var act = d.RootElement.TryGetProperty("action", out var av) ? av.GetString() : null;
                        return act != "add_labor_rows";
                    }
                    catch { return true; }
                }).ToList();

            var merged = headerActionsFiltered.Concat(labor.Actions).ToList();
            return new AiChatResponse(header.Response, merged);
        }

        // Single agent for Q&A, app control, rate books, status updates
        return await RunHeaderAgentAsync(request, ctxData, companyCode, ct);
    }

    private static bool IsLaborRequest(string msg)
    {
        var m = msg.ToLowerInvariant();
        return m.Contains("crew") || m.Contains("foreman") ||
               m.Contains("journeyman") || m.Contains("journeymen") || m.Contains("helper") ||
               m.Contains("welder") || m.Contains("pipefitter") || m.Contains("boilermaker") ||
               m.Contains("electrician") || m.Contains("millwright") || m.Contains("instrument") ||
               m.Contains("ndt") || m.Contains("rigger") || m.Contains("scaffold") ||
               m.Contains("safety watch") || m.Contains("fire watch") || m.Contains("driver") ||
               m.Contains(" pm ") || m.Contains(" gf ") || m.Contains(" pf ") || m.Contains(" bm ");
    }

    // ── Header Agent: handles form header, dates, rate books, Q&A, app control ─

    private async Task<AiChatResponse> RunHeaderAgentAsync(
        AiChatRequest request, ContextData ctxData, string companyCode, CancellationToken ct)
    {
        var systemPrompt = await BuildSystemPromptAsync(request.Context, ctxData, companyCode, ct);
        var messages = new List<object> { new { role = "system", content = systemPrompt } };
        foreach (var h in request.History.TakeLast(4))
            messages.Add(new { role = h.Role, content = h.Content });
        messages.Add(new { role = "user", content = request.Message });

        var allTools = ToolExecutorService.BuildDbTools().Append(BuildTool()).ToArray();

        for (var iteration = 0; iteration < 3; iteration++)
        {
            var requestBody = new { model = _model, messages, tools = allTools, tool_choice = "auto", temperature = 0.10, max_tokens = 1500 };
            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync(_chatPath, content, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"AI API error {(int)resp.StatusCode}: {err}");
            }

            var body = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            var choice = doc.RootElement.GetProperty("choices")[0];
            var message = choice.GetProperty("message");

            if (!message.TryGetProperty("tool_calls", out var toolCalls) || toolCalls.GetArrayLength() == 0)
            {
                var textContent = message.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                var trimmed = textContent.Trim();
                if (trimmed.StartsWith("{"))
                {
                    try
                    {
                        using var jd = JsonDocument.Parse(trimmed);
                        var jRoot = jd.RootElement;
                        var jResp = jRoot.TryGetProperty("response", out var re) ? re.GetString() ?? textContent : textContent;
                        var jActs = new List<JsonElement>();
                        if (jRoot.TryGetProperty("actions", out var ae) && ae.ValueKind == JsonValueKind.Array)
                            foreach (var a in ae.EnumerateArray()) jActs.Add(a.Clone());
                        return new AiChatResponse(jResp, jActs);
                    }
                    catch { }
                }
                return new AiChatResponse(textContent, new List<JsonElement>());
            }

            messages.Add(JsonSerializer.Deserialize<object>(message.GetRawText())!);
            bool allAreDbTools = true;
            foreach (var call in toolCalls.EnumerateArray())
            {
                var toolName = call.GetProperty("function").GetProperty("name").GetString() ?? "";
                var callId = call.GetProperty("id").GetString() ?? "";
                var argsJson = call.GetProperty("function").GetProperty("arguments").GetString() ?? "{}";

                if (toolName == "update_estimate")
                {
                    allAreDbTools = false;
                    try
                    {
                        using var argsDoc = JsonDocument.Parse(argsJson);
                        var args = argsDoc.RootElement;
                        var response = args.TryGetProperty("response", out var respEl) ? respEl.GetString() ?? "" : "";
                        var actions = new List<JsonElement>();
                        if (args.TryGetProperty("actions", out var actionsEl) && actionsEl.ValueKind == JsonValueKind.Array)
                            foreach (var a in actionsEl.EnumerateArray()) actions.Add(a.Clone());
                        return new AiChatResponse(response, actions);
                    }
                    catch { return new AiChatResponse("I processed your request.", new List<JsonElement>()); }
                }

                using var argsDocDb = JsonDocument.Parse(argsJson);
                var toolResult = await _toolExecutor.ExecuteAsync(toolName, argsDocDb.RootElement, companyCode, ct);
                messages.Add(new { role = "tool", tool_call_id = callId, content = toolResult });
            }
            if (!allAreDbTools) break;
        }

        return new AiChatResponse("I couldn't complete that request. Please try again.", new List<JsonElement>());
    }

    // ── Labor Agent: focused solely on extracting labor rows ─────────────────

    private static readonly string[] CanonicalPositions =
    {
        "Project Manager", "General Foreman", "Foreman",
        "Pipefitter Journeyman", "Pipefitter Helper",
        "Welder Journeyman", "Welder Helper",
        "Boilermaker Journeyman", "Boilermaker Helper",
        "Electrician Journeyman", "Millwright Journeyman",
        "Instrument Tech", "NDT Technician", "Crane Operator", "Rigger",
        "Scaffold Builder", "Safety Watch", "Fire Watch", "Hole Watch", "Driver/Teamster"
    };

    private async Task<AiChatResponse> RunLaborAgentAsync(
        AiChatRequest request, ContextData ctxData, CancellationToken ct)
    {
        var positions = string.Join(", ", CanonicalPositions);
        var rateInfo = ctxData.RateBenchmarks.Count > 0
            ? "BENCHMARKS: " + string.Join(" | ", ctxData.RateBenchmarks.Take(6).Select(b => $"{b.Position} ST${b.AvgStRate:F2}"))
            : "";

        // Pass the current estimate shift so labor agent respects it
        var currentShift = request.Context?.HeaderSnapshot?.Shift ?? "";
        var shiftConstraint = currentShift switch
        {
            "Day"   => "SHIFT CONSTRAINT: The estimate is Day shift only — emit ONLY Day rows regardless of what the message says.",
            "Night" => "SHIFT CONSTRAINT: The estimate is Night shift only — emit ONLY Night rows regardless of what the message says.",
            _       => "SHIFT: If message says 'day shift' or 'day only'→emit ONLY Day rows. If 'night shift' or 'nights only'→emit ONLY Night rows. If 'both shifts', 'both', or 'two shifts'→emit EACH position TWICE (one Day row + one Night row). Default to Day if not specified."
        };

        var laborPrompt = $$"""
You extract labor crew from user messages for an industrial estimating system.
TODAY:{{DateTime.UtcNow:yyyy-MM-dd}}
POSITIONS: {{positions}}
ALIASES: pm=Project Manager | gf=General Foreman | pf=Pipefitter Journeyman | bm=Boilermaker Journeyman | mw=Millwright Journeyman | elec=Electrician Journeyman | it=Instrument Tech | ndt=NDT Technician
{{shiftConstraint}}
{{rateInfo}}

Return ONLY a JSON object: {"rows": [{"position":"...", "shift":"Day|Night", "qty":N}]}
Return {"rows":[]} if no labor crew is mentioned.
Only use positions from the POSITIONS list above. Never fabricate quantities.
""";

        var msgs = new[] { new { role = "user", content = $"{laborPrompt}\n\nUSER MESSAGE: {request.Message}" } };
        var reqBody = new { model = _model, messages = msgs, response_format = new { type = "json_object" }, temperature = 0.05, max_tokens = 600 };

        var json = JsonSerializer.Serialize(reqBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync(_chatPath, content, ct);
        if (!resp.IsSuccessStatusCode) return new AiChatResponse("", new List<JsonElement>());

        var body = await resp.Content.ReadAsStringAsync(ct);
        try
        {
            using var doc = JsonDocument.Parse(body);
            var contentStr = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "{}";
            using var parsed = JsonDocument.Parse(contentStr);
            if (!parsed.RootElement.TryGetProperty("rows", out var rowsEl) || rowsEl.GetArrayLength() == 0)
                return new AiChatResponse("", new List<JsonElement>());

            // Wrap rows into an add_labor_rows action element
            var actionJson = JsonSerializer.Serialize(new { action = "add_labor_rows", rows = JsonSerializer.Deserialize<object>(rowsEl.GetRawText()) });
            using var actionDoc = JsonDocument.Parse(actionJson);
            return new AiChatResponse("", new List<JsonElement> { actionDoc.RootElement.Clone() });
        }
        catch { return new AiChatResponse("", new List<JsonElement>()); }
    }

    // ── DB context enrichment ─────────────────────────────────────────────────

    private async Task<ContextData> GetContextDataAsync(string companyCode, CancellationToken ct)
    {
        var cacheKey = $"ai-ctx-{companyCode}";
        if (_cache.TryGetValue(cacheKey, out ContextData? cached) && cached != null)
            return cached;

        var result = await LoadContextDataAsync(companyCode, ct);
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(2));
        return result;
    }

    private async Task<ContextData> LoadContextDataAsync(string companyCode, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var today = DateTime.UtcNow.Date;

        // Crew templates
        var templates = await db.Set<CrewTemplate>()
            .Where(t => t.CompanyCode == companyCode)
            .Include(t => t.Rows)
            .OrderBy(t => t.Name)
            .Take(20)
            .ToListAsync(ct);

        var templateInfos = templates.Select(t =>
        {
            var preview = t.Rows.OrderBy(r => r.SortOrder).Take(5)
                .Select(r => $"{r.Qty}x {r.Position}").ToList();
            var suffix = t.Rows.Count > 5 ? $" +{t.Rows.Count - 5} more" : "";
            return new TemplateInfo(t.CrewTemplateId, t.Name, t.Description, t.Rows.Count,
                string.Join(", ", preview) + suffix);
        }).ToList();

        // Today's active jobs
        var activeJobs = await db.Set<Estimate>()
            .Where(e => e.CompanyCode == companyCode
                && e.Status == "Awarded"
                && e.StartDate.HasValue && e.EndDate.HasValue
                && e.StartDate.Value.Date <= today
                && e.EndDate.Value.Date >= today)
            .OrderBy(e => e.StartDate)
            .Take(10)
            .ToListAsync(ct);

        var activeJobInfos = activeJobs.Select(e => new ActiveJobInfo(
            e.EstimateNumber, e.Name, e.Client,
            e.StartDate?.ToString("yyyy-MM-dd"),
            e.EndDate?.ToString("yyyy-MM-dd"),
            e.Status,
            0
        )).ToList();

        // Rate book metadata
        var rateBooks = await db.Set<RateBook>()
            .Where(r => r.CompanyCode == companyCode)
            .OrderBy(r => r.Name)
            .Take(50)
            .ToListAsync(ct);

        var rateBookMeta = rateBooks.Select(r => new RateBookMetadata(
            r.RateBookId, r.Name, r.Client, r.City, r.State,
            r.LaborRates?.Count ?? 0)).ToList();

        // Historical rate benchmarks (awarded jobs, min 3 data points)
        var laborRows = await db.LaborRows
            .Where(r => r.Estimate.CompanyCode == companyCode
                     && r.Estimate.Status == "Awarded"
                     && !r.Estimate.IsScenario
                     && r.BillStRate > 0)
            .Select(r => new { r.Position, r.Estimate.Client, r.BillStRate, r.BillOtRate, r.BillDtRate, r.EstimateId })
            .ToListAsync(ct);

        var rateBenchmarks = laborRows
            .GroupBy(r => new { r.Position, r.Client })
            .Where(g => g.Select(r => r.EstimateId).Distinct().Count() >= 3)
            .Select(g => new RateBenchmark(
                g.Key.Position,
                g.Key.Client,
                Math.Round((double)g.Average(r => r.BillStRate), 2),
                Math.Round((double)g.Average(r => r.BillOtRate), 2),
                Math.Round((double)g.Average(r => r.BillDtRate), 2),
                g.Select(r => r.EstimateId).Distinct().Count()))
            .OrderBy(b => b.Position)
            .ToList();

        // Pipeline summary — aggregate query, no row loading
        var pipelineStats = await db.Estimates
            .Where(e => e.CompanyCode == companyCode && !e.IsScenario)
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var pipelineTotals = await db.Estimates
            .Where(e => e.CompanyCode == companyCode && !e.IsScenario
                && (e.Status == "Awarded" || e.Status == "Submitted" || e.Status == "Pending"))
            .Join(db.Set<EstimateSummary>(), e => e.EstimateId, s => s.EstimateId,
                (e, s) => new { e.Status, s.GrandTotal })
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Total = g.Sum(x => x.GrandTotal) })
            .ToListAsync(ct);

        decimal GetTotal(string status) => pipelineTotals.FirstOrDefault(x => x.Status == status)?.Total ?? 0;
        int GetCount(string status) => pipelineStats.FirstOrDefault(x => x.Status == status)?.Count ?? 0;

        var pipeline = new PipelineSummaryInfo(
            TotalJobs: pipelineStats.Sum(x => x.Count),
            ActiveToday: activeJobs.Count,
            TotalPipelineValue: GetTotal("Awarded") + GetTotal("Submitted") + GetTotal("Pending"),
            AwardedValue: GetTotal("Awarded"),
            SubmittedValue: GetTotal("Submitted"),
            PendingValue: GetTotal("Pending"),
            AwardedCount: GetCount("Awarded"),
            SubmittedCount: GetCount("Submitted"),
            PendingCount: GetCount("Pending"),
            LostCount: GetCount("Lost")
        );

        return new ContextData(templateInfos, activeJobInfos, rateBookMeta, rateBenchmarks, pipeline);
    }

    // ── System prompt ─────────────────────────────────────────────────────────

    private async Task<string> BuildSystemPromptAsync(
        AiContext ctx,
        ContextData ctxData,
        string companyCode,
        CancellationToken ct)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"You are the Stronghold Estimating AI. COMPANY:{companyCode} TODAY:{DateTime.UtcNow:yyyy-MM-dd} PAGE:{ctx.CurrentPage}");
        sb.AppendLine("Industrial estimating system: turnarounds, maintenance, installation, tank work, electrical, instrumentation.");
        sb.AppendLine();

        // Current estimate state (compact)
        if (ctx.HeaderSnapshot is { } h)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(h.Name))      parts.Add($"Name={h.Name}");
            if (!string.IsNullOrEmpty(h.Client))    parts.Add($"Client={h.Client}");
            if (!string.IsNullOrEmpty(h.City))      parts.Add($"City={h.City}");
            if (!string.IsNullOrEmpty(h.State))     parts.Add($"State={h.State}");
            if (!string.IsNullOrEmpty(h.StartDate)) parts.Add($"Start={h.StartDate}");
            if (!string.IsNullOrEmpty(h.EndDate))   parts.Add($"End={h.EndDate}");
            if (h.Days.HasValue)                    parts.Add($"Days={h.Days}");
            if (!string.IsNullOrEmpty(h.Shift))     parts.Add($"Shift={h.Shift}");
            if (!string.IsNullOrEmpty(h.JobType))   parts.Add($"Type={h.JobType}");
            if (parts.Count > 0) sb.AppendLine("ESTIMATE: " + string.Join(" | ", parts));
            sb.AppendLine();
        }

        // Current labor rows
        if (ctx.LaborRows is { Count: > 0 } rows)
        {
            sb.AppendLine("LABOR ROWS:");
            foreach (var r in rows)
                sb.AppendLine($"  {r.Position} {r.Shift} ST${r.StRate} OT${r.OtRate} DT${r.DtRate} → ${r.Subtotal:N0}");
            sb.AppendLine($"  TOTAL ${rows.Sum(r => r.Subtotal):N0}");
            sb.AppendLine();
        }

        // Current equipment rows
        if (ctx.EquipmentRows is { Count: > 0 } equip)
        {
            sb.AppendLine("CURRENT EQUIPMENT:");
            foreach (var e in equip)
                sb.AppendLine($"  {e.Name} {e.RateType} ${e.Rate}/unit × {e.Qty} × {e.Days}d → ${e.Subtotal:N0}");
            sb.AppendLine();
        }
        else if (ctx.CurrentPage == "estimate")
        {
            sb.AppendLine("CURRENT EQUIPMENT: none");
            sb.AppendLine();
        }

        // Active rate book
        if (ctx.CurrentRateBookId.HasValue)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var positions = await db.Set<RateBookLaborRate>()
                .Where(r => r.RateBookId == ctx.CurrentRateBookId.Value
                         && r.RateBook.CompanyCode == companyCode)
                .OrderBy(r => r.SortOrder)
                .Take(15)
                .ToListAsync(ct);

            var expenseItems = await db.Set<RateBookExpenseItem>()
                .Where(r => r.RateBookId == ctx.CurrentRateBookId.Value)
                .OrderBy(r => r.SortOrder)
                .Take(10)
                .ToListAsync(ct);

            if (positions.Count > 0)
            {
                sb.AppendLine($"RATE BOOK: {ctx.RateBookName}");
                foreach (var p in positions)
                    sb.AppendLine($"  {p.Position} ST${p.StRate} OT${p.OtRate} DT${p.DtRate}");
                if (expenseItems.Count > 0)
                {
                    sb.AppendLine($"  EXPENSE ITEMS:");
                    foreach (var e in expenseItems)
                        sb.AppendLine($"    [{e.Category}] {e.Description} ${e.Rate}/{e.Unit}");
                }
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("NO RATE BOOK LOADED. Standard positions: Project Manager, General Foreman, Foreman, Pipefitter Journeyman, Pipefitter Helper, Welder Journeyman, Welder Helper, Boilermaker Journeyman, Boilermaker Helper, Electrician Journeyman, Millwright Journeyman, Instrument Tech, NDT Technician, Crane Operator, Rigger, Scaffold Builder, Safety Watch, Fire Watch, Hole Watch, Driver/Teamster");
            sb.AppendLine();
        }

        // Rate benchmarks (capped at 8)
        if (ctxData.RateBenchmarks.Count > 0)
        {
            sb.AppendLine("BENCHMARKS:");
            foreach (var b in ctxData.RateBenchmarks.Take(8))
                sb.AppendLine($"  {b.Position} ST${b.AvgStRate:F2} OT${b.AvgOtRate:F2}");
            sb.AppendLine();
        }

        // Rate books (capped at 15 for client matching)
        if (ctxData.RateBookMeta.Count > 0)
        {
            sb.AppendLine("RATE BOOKS:");
            foreach (var rb in ctxData.RateBookMeta.Take(15))
                sb.AppendLine($"  ID={rb.Id} {rb.Name}|{rb.Client ?? ""}|{rb.City},{rb.State}");
            sb.AppendLine();
        }

        // Crew templates (capped at 5)
        if (ctxData.Templates.Count > 0)
        {
            sb.AppendLine("TEMPLATES:");
            foreach (var t in ctxData.Templates.Take(5))
                sb.AppendLine($"  ID={t.Id} {t.Name}: {t.PositionSummary}");
            sb.AppendLine();
        }

        // Pipeline + active jobs
        if (ctxData.Pipeline is { } pip)
            sb.AppendLine($"PIPELINE: {pip.TotalJobs} jobs | Active today:{pip.ActiveToday} | Awarded:{pip.AwardedCount}=${pip.AwardedValue:N0} | Submitted:{pip.SubmittedCount}=${pip.SubmittedValue:N0} | Pending:{pip.PendingCount}=${pip.PendingValue:N0} | Lost:{pip.LostCount}");

        if (ctxData.ActiveJobs.Count > 0)
        {
            sb.AppendLine("ACTIVE JOBS TODAY:");
            foreach (var j in ctxData.ActiveJobs)
                sb.AppendLine($"  {j.Number} {j.Name} ({j.Client}) {j.StartDate}→{j.EndDate}");
        }
        sb.AppendLine();

        // Rules (compressed)
        sb.AppendLine("ALIASES: pm→Project Manager | gf→General Foreman | pf→Pipefitter Journeyman | pf helper→Pipefitter Helper | welder/jw→Welder Journeyman | bm→Boilermaker Journeyman | mw→Millwright Journeyman | elec→Electrician Journeyman | it/i&e→Instrument Tech | ndt→NDT Technician | sw→Safety Watch | fw→Fire Watch | hw→Hole Watch");
        sb.AppendLine("TYPOS: silently fix — bakersfeild→Bakersfield | pasedena→Pasadena | millright→Millwright | electrican→Electrician | valuero→Valero | sheel→Shell | cheveron→Chevron");
        sb.AppendLine("LOCATION: extract city + 2-letter state. 'california'→CA 'texas'→TX 'louisiana'→LA 'mississippi'→MS. fill_header MUST use fields:{} — example: {action:'fill_header',fields:{client:'Shell',city:'Deer Park',state:'TX',name:'Shell Deer Park TX'}}. NEVER put client/city/state at top level of the action.");
        sb.AppendLine($"DATES: today={DateTime.UtcNow:yyyy-MM-dd}. 'June 10 for 14 days'→start=2026-06-10 end=2026-06-23. Emit BOTH set_dates AND fill_header(days).");
        sb.AppendLine("SHIFT: day→Day | night→Night | both/two shifts→Both. When 'both shifts' is requested, set fields:{shift:'Both'} in fill_header. The labor agent handles adding the Day+Night rows separately — do NOT emit add_labor_rows here.");
        sb.AppendLine("HELPER: 'add a helper' (no type) → always ask_clarification: 'Which type?' options: [Pipefitter Helper, Welder Helper, Boilermaker Helper, Scaffold Builder]");
        sb.AppendLine("JOB TYPE: lump sum/ls→Lump Sum | t&m/time and material→T&M | turnaround/ta→Turnaround | inspection→Inspection | maintenance→Maintenance. If user does not mention job type when creating an estimate → emit ask_clarification: 'What is the job type?' options:[Lump Sum, T&M, Turnaround, Maintenance, Inspection, Consulting]");
        sb.AppendLine("PER DIEM: when user says 'per diem' / 'standard per diem' / 'add per diem' → look at RATE BOOK EXPENSE ITEMS for a [PerDiem] entry and use that rate. If multiple PerDiem options exist, pick the one matching the job context (out of town→Standard Per Diem Out of Town; high cost area→High Cost Area). If no rate book loaded → ask_clarification listing available rate books first. Emit add_expense with category='PerDiem', description=<description from rate book>, rate=<rate from rate book>, days_or_qty=<days from header or 1 if unknown>, people=<total headcount from labor rows or 1>, billable=true, type='Direct'.");
        sb.AppendLine("RATE BOOK MATCH: fuzzy/substring match on Client+Name fields. Exact city+state→load it. Different location→suggest_rate_book + clone option. No match→ask_clarification. Already loaded→don't prompt again.");
        sb.AppendLine("RATE ANOMALY: when adding rows, if |rate - benchmark| > 20% and 3+ data points → emit rate_anomaly_warning (non-blocking).");
        sb.AppendLine($"DB TOOLS: call BEFORE update_estimate for business Q&A. Tools:");
        sb.AppendLine($"  get_active_jobs → 'what jobs are going on today' / 'what are we working on right now' / 'active jobs'");
        sb.AppendLine($"  search_estimates(status,client,city,state,year,startAfter,startBefore) → 'upcoming jobs' use startAfter={DateTime.UtcNow:yyyy-MM-dd} | 'jobs coming up in 2 weeks' use startAfter={DateTime.UtcNow:yyyy-MM-dd} startBefore={DateTime.UtcNow.AddDays(14):yyyy-MM-dd} | 'Shell jobs this year' use client=Shell year={DateTime.UtcNow.Year} | 'Dow jobs in Deer Park' use client=Dow city=Deer Park");
        sb.AppendLine($"  get_pipeline_summary → 'revenue this quarter' / 'pipeline summary' / 'how are we doing' / 'awarded revenue' — returns totals by status");
        sb.AppendLine($"  get_win_loss_stats(client) → 'win rate' / 'how many did we win vs lose'");
        sb.AppendLine($"  get_labor_utilization → 'how many people do we have working'");
        sb.AppendLine("REVIEW REVENUE IMPACT: At the END of any review response, after listing missing positions, include a dollar estimate for each missing position (benchmark ST rate × 8 hrs/day × job Days from ESTIMATE header × typical headcount: 1 for GF/PM/NDT/Crane Op, 2 for journeymen, 2 for helpers, 2 for Safety Watch). Sum all missing positions and state: 'These missing positions represent approximately $X in potential revenue.'");
        sb.AppendLine("STATUS UPDATE: search_estimates first → then update_estimate_status with correct estimate_id.");
        sb.AppendLine("APP CONTROL (auto-execute, no confirmation): light mode→app_control switch_theme light | dark mode→switch_theme dark | go to estimates→navigate_to /estimating/estimates | new estimate→open_new_estimate | new staffing→open_new_staffing_plan | cost book→navigate_to /estimating/cost-book | rate book→navigate_to /estimating/rate-book | staffing plans→navigate_to /estimating/staffing");
        sb.AppendLine();
        sb.AppendLine("OUTPUT: call update_estimate tool with {response, actions}. response=1-3 sentences. actions=[] for Q&A. Never fabricate rates.");

        return sb.ToString();
    }

    // ── Tool definition ───────────────────────────────────────────────────────

    private static object BuildTool()
    {
        return new
        {
            type = "function",
            function = new
            {
                name = "update_estimate",
                description = "Apply changes to the estimate/staffing form and respond to the user. ALWAYS call this.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        response = new
                        {
                            type = "string",
                            description = "Message shown to user. Confirm what was done or answer their question. 1-3 sentences."
                        },
                        actions = new
                        {
                            type = "array",
                            description = "List of form actions. Empty array for Q&A responses.",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    action = new
                                    {
                                        type = "string",
                                        @enum = new[]
                                        {
                                            "fill_header",
                                            "add_labor_rows",
                                            "add_expense",
                                            "set_dates",
                                            "load_rate_book",
                                            "suggest_rate_book",
                                            "apply_template",
                                            "ask_clarification",
                                            "navigate",
                                            "rate_anomaly_warning",
                                            "update_estimate_status",
                                            "app_control"
                                        }
                                    },
                                    // fill_header fields
                                    fields = new
                                    {
                                        type = "object",
                                        description = "For fill_header: name, client, clientCode, city, state, site, branch, jobLetter, jobType, msaNumber, shift, hoursPerShift, days, startDate, endDate, otMethod, dtWeekends, status, confidencePct, lostReason, vp, director, region",
                                        additionalProperties = new { type = "string" }
                                    },
                                    // app_control fields
                                    command = new { type = "string", description = "For app_control: switch_theme | navigate_to | open_new_estimate | open_new_staffing_plan" },
                                    theme = new { type = "string", description = "For switch_theme: 'light' or 'dark'" },
                                    // add_labor_rows
                                    rows = new
                                    {
                                        type = "array",
                                        items = new
                                        {
                                            type = "object",
                                            properties = new
                                            {
                                                position = new { type = "string" },
                                                shift = new { type = "string", @enum = new[] { "Day", "Night", "Both" } },
                                                qty = new { type = "integer" }
                                            },
                                            required = new[] { "position", "shift", "qty" }
                                        }
                                    },
                                    // set_dates
                                    start_date = new { type = "string" },
                                    end_date = new { type = "string" },
                                    days = new { type = "integer" },
                                    // load_rate_book
                                    rate_book_id = new { type = "integer" },
                                    rate_book_name = new { type = "string" },
                                    // suggest_rate_book
                                    nearest_rate_book_id = new { type = "integer" },
                                    nearest_rate_book_name = new { type = "string" },
                                    similarity_reason = new { type = "string" },
                                    clone_suggested_name = new { type = "string" },
                                    // apply_template
                                    template_id = new { type = "integer" },
                                    template_name = new { type = "string" },
                                    // ask_clarification
                                    question = new { type = "string" },
                                    options = new { type = "array", items = new { type = "string" } },
                                    // navigate
                                    route = new { type = "string" },
                                    // update_estimate_status
                                    estimate_id = new { type = "integer", description = "EstimateId (integer) of the estimate to update" },
                                    new_status = new { type = "string", description = "New status: Draft, Pending, Submitted, Awarded, Lost, Active" },
                                    lost_reason = new { type = "string", description = "Required if new_status is Lost" },
                                    // add_expense
                                    expense_category = new { type = "string", description = "For add_expense: PerDiem | Travel | Lodging | Meals | Other" },
                                    expense_description = new { type = "string", description = "For add_expense: description text, e.g. 'Standard Per Diem (Out of Town)'" },
                                    expense_rate = new { type = "number", description = "For add_expense: rate per unit (e.g. 125.00)" },
                                    expense_days_or_qty = new { type = "integer", description = "For add_expense: number of days or quantity" },
                                    expense_people = new { type = "integer", description = "For add_expense: number of people" },
                                    expense_billable = new { type = "boolean", description = "For add_expense: whether this expense is billable to client" },
                                    expense_type = new { type = "string", description = "For add_expense: Direct or Indirect" }
                                },
                                required = new[] { "action" }
                            }
                        }
                    },
                    required = new[] { "response", "actions" }
                }
            }
        };
    }

    // ── Parse Groq response ───────────────────────────────────────────────────

    private static AiChatResponse ParseGroqResponse(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var choice = root.GetProperty("choices")[0];
        var message = choice.GetProperty("message");

        // JSON mode: content is the JSON object
        var contentStr = message.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";

        if (!string.IsNullOrWhiteSpace(contentStr))
        {
            try
            {
                // Strip markdown code fences if the model wraps in ```json ... ```
                var trimmed = contentStr.Trim();
                if (trimmed.StartsWith("```"))
                {
                    var start = trimmed.IndexOf('{');
                    var end = trimmed.LastIndexOf('}');
                    if (start >= 0 && end > start)
                        trimmed = trimmed[start..(end + 1)];
                }

                using var argsDoc = JsonDocument.Parse(trimmed);
                var args = argsDoc.RootElement;

                var response = args.TryGetProperty("response", out var respEl) ? respEl.GetString() ?? "" : "";
                var actions = new List<JsonElement>();
                if (args.TryGetProperty("actions", out var actionsEl) && actionsEl.ValueKind == JsonValueKind.Array)
                    foreach (var a in actionsEl.EnumerateArray())
                        actions.Add(a.Clone());

                return new AiChatResponse(response, actions);
            }
            catch
            {
                // JSON parse failed — return the raw text as a plain response
                return new AiChatResponse(contentStr, new List<JsonElement>());
            }
        }

        // Fallback: tool_calls path (shouldn't be reached in JSON mode)
        if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
        {
            var call = toolCalls[0];
            var argsJson = call.GetProperty("function").GetProperty("arguments").GetString() ?? "{}";
            try
            {
                using var argsDoc = JsonDocument.Parse(argsJson);
                var args = argsDoc.RootElement;
                var response = args.TryGetProperty("response", out var respEl) ? respEl.GetString() ?? "" : "";
                var actions = new List<JsonElement>();
                if (args.TryGetProperty("actions", out var actionsEl) && actionsEl.ValueKind == JsonValueKind.Array)
                    foreach (var a in actionsEl.EnumerateArray())
                        actions.Add(a.Clone());
                return new AiChatResponse(response, actions);
            }
            catch { /* fall through */ }
        }

        return new AiChatResponse("I couldn't process that request.", new List<JsonElement>());
    }
}
