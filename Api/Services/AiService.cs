using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
    List<AiLaborRowContext>? LaborRows = null);

public record AiLaborRowContext(
    string Position, string Shift,
    decimal StRate, decimal OtRate, decimal DtRate,
    decimal StHours, decimal OtHours, decimal Subtotal);

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

    public AiService(IHttpClientFactory httpClientFactory, IConfiguration config, IDbContextFactory<AppDbContext> dbFactory, ToolExecutorService toolExecutor)
    {
        _http = httpClientFactory.CreateClient("groq");
        _model = config["Ai:GroqModel"] ?? "llama-3.3-70b-versatile";
        _dbFactory = dbFactory;
        _toolExecutor = toolExecutor;
    }

    public async Task<AiChatResponse> ChatAsync(
        AiChatRequest request,
        string username,
        CancellationToken ct = default)
    {
        var ctxData = await GetContextDataAsync(request.Context.CompanyCode, ct);
        var systemPrompt = await BuildSystemPromptAsync(request.Context, ctxData, ct);

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        foreach (var h in request.History.TakeLast(8))
            messages.Add(new { role = h.Role, content = h.Content });

        messages.Add(new { role = "user", content = request.Message });

        // Combine DB query tools + the update_estimate structured output tool
        var allTools = ToolExecutorService.BuildDbTools()
            .Append(BuildTool())
            .ToArray();

        // Agentic loop: DB tools may be called first, then update_estimate finalizes
        for (var iteration = 0; iteration < 3; iteration++)
        {
            var requestBody = new
            {
                model = _model,
                messages,
                tools = allTools,
                tool_choice = "auto",
                temperature = 0.10,
                max_tokens = 2500,
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync("/openai/v1/chat/completions", content, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"Groq API error {(int)resp.StatusCode}: {err}");
            }

            var body = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            var choice = doc.RootElement.GetProperty("choices")[0];
            var message = choice.GetProperty("message");

            // No tool calls — model may have responded with JSON in content field
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

            // Add assistant message with tool_calls to history
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
                    // This is the final structured output — parse and return
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
                    catch
                    {
                        return new AiChatResponse("I processed your request.", new List<JsonElement>());
                    }
                }

                // DB tool — execute and append result to messages
                using var argsDocDb = JsonDocument.Parse(argsJson);
                var toolResult = await _toolExecutor.ExecuteAsync(toolName, argsDocDb.RootElement, request.Context.CompanyCode, ct);
                messages.Add(new { role = "tool", tool_call_id = callId, content = toolResult });
            }

            // If all calls this iteration were DB tools, loop again for the AI to synthesize
            if (!allAreDbTools) break;
        }

        return new AiChatResponse("I couldn't complete that request. Please try again.", new List<JsonElement>());
    }

    // ── DB context enrichment ─────────────────────────────────────────────────

    private async Task<ContextData> GetContextDataAsync(string companyCode, CancellationToken ct)
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
        var activeStatuses = new[] { "Awarded", "Pending", "Proposed" };
        var activeJobs = await db.Set<Estimate>()
            .Where(e => e.CompanyCode == companyCode
                && activeStatuses.Contains(e.Status)
                && e.StartDate.HasValue && e.EndDate.HasValue
                && e.StartDate.Value.Date <= today
                && e.EndDate.Value.Date >= today)
            .Include(e => e.LaborRows)
            .OrderBy(e => e.StartDate)
            .Take(20)
            .ToListAsync(ct);

        var activeJobInfos = activeJobs.Select(e => new ActiveJobInfo(
            e.EstimateNumber, e.Name, e.Client,
            e.StartDate?.ToString("yyyy-MM-dd"),
            e.EndDate?.ToString("yyyy-MM-dd"),
            e.Status,
            e.LaborRows.Count > 0
                ? e.LaborRows.GroupBy(r => r.Position).Sum(g => 1) // rough position count
                : 0
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

        // Pipeline summary (for non-form pages and general Q&A)
        var allEstimates = await db.Estimates
            .Where(e => e.CompanyCode == companyCode && !e.IsScenario)
            .Include(e => e.Summary)
            .ToListAsync(ct);

        var pipeline = new PipelineSummaryInfo(
            TotalJobs: allEstimates.Count,
            ActiveToday: activeJobs.Count,
            TotalPipelineValue: allEstimates
                .Where(e => e.Status is "Pending" or "Submitted" or "Awarded")
                .Sum(e => e.Summary?.GrandTotal ?? 0),
            AwardedValue: allEstimates.Where(e => e.Status == "Awarded").Sum(e => e.Summary?.GrandTotal ?? 0),
            SubmittedValue: allEstimates.Where(e => e.Status == "Submitted").Sum(e => e.Summary?.GrandTotal ?? 0),
            PendingValue: allEstimates.Where(e => e.Status == "Pending").Sum(e => e.Summary?.GrandTotal ?? 0),
            AwardedCount: allEstimates.Count(e => e.Status == "Awarded"),
            SubmittedCount: allEstimates.Count(e => e.Status == "Submitted"),
            PendingCount: allEstimates.Count(e => e.Status == "Pending"),
            LostCount: allEstimates.Count(e => e.Status == "Lost")
        );

        return new ContextData(templateInfos, activeJobInfos, rateBookMeta, rateBenchmarks, pipeline);
    }

    // ── System prompt ─────────────────────────────────────────────────────────

    private async Task<string> BuildSystemPromptAsync(AiContext ctx, ContextData ctxData, CancellationToken ct)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"You are the Stronghold Estimating AI. COMPANY:{ctx.CompanyCode} TODAY:{DateTime.UtcNow:yyyy-MM-dd} PAGE:{ctx.CurrentPage}");
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

        // Active rate book
        if (ctx.CurrentRateBookId.HasValue)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var positions = await db.Set<RateBookLaborRate>()
                .Where(r => r.RateBookId == ctx.CurrentRateBookId.Value)
                .OrderBy(r => r.SortOrder)
                .Take(40)
                .ToListAsync(ct);

            if (positions.Count > 0)
            {
                sb.AppendLine($"RATE BOOK: {ctx.RateBookName} — use EXACT position names:");
                foreach (var p in positions)
                    sb.AppendLine($"  {p.Position} ST${p.StRate} OT${p.OtRate} DT${p.DtRate}");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("NO RATE BOOK LOADED. Standard positions: Project Manager, General Foreman, Foreman, Pipefitter Journeyman, Pipefitter Helper, Welder Journeyman, Welder Helper, Boilermaker Journeyman, Boilermaker Helper, Electrician Journeyman, Millwright Journeyman, Instrument Tech, NDT Technician, Crane Operator, Rigger, Scaffold Builder, Safety Watch, Fire Watch, Hole Watch, Driver/Teamster");
            sb.AppendLine();
        }

        // Rate benchmarks (capped at 15)
        if (ctxData.RateBenchmarks.Count > 0)
        {
            sb.AppendLine("RATE BENCHMARKS (awarded jobs, use for anomaly detection >20% deviation):");
            foreach (var b in ctxData.RateBenchmarks.Take(15))
                sb.AppendLine($"  {b.Position}|{b.Client ?? "All"} ST${b.AvgStRate:F2} OT${b.AvgOtRate:F2} ({b.DataPoints} jobs)");
            sb.AppendLine();
        }

        // Rate books (for client matching)
        if (ctxData.RateBookMeta.Count > 0)
        {
            sb.AppendLine("ALL RATE BOOKS:");
            foreach (var rb in ctxData.RateBookMeta)
                sb.AppendLine($"  ID={rb.Id} {rb.Name} | Client={rb.Client ?? "—"} | {rb.City},{rb.State}");
            sb.AppendLine();
        }

        // Crew templates (capped at 10)
        if (ctxData.Templates.Count > 0)
        {
            sb.AppendLine("CREW TEMPLATES:");
            foreach (var t in ctxData.Templates.Take(10))
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
        sb.AppendLine("LOCATION: extract city + 2-letter state. 'california'→CA 'texas'→TX 'louisiana'→LA 'mississippi'→MS. Always emit fill_header with city+state.");
        sb.AppendLine($"DATES: today={DateTime.UtcNow:yyyy-MM-dd}. 'June 10 for 14 days'→start=2026-06-10 end=2026-06-23. Emit BOTH set_dates AND fill_header(days).");
        sb.AppendLine("SHIFT: day→Day | night→Night | both/two shifts→Both. Both shift = emit TWO add_labor_rows entries (Day + Night).");
        sb.AppendLine("HELPER: 'add a helper' (no type) → always ask_clarification: 'Which type?' options: [Pipefitter Helper, Welder Helper, Boilermaker Helper, Scaffold Builder]");
        sb.AppendLine("JOB TYPE: lump sum/ls→Lump Sum | t&m/time and material→T&M | turnaround/ta→Turnaround | inspection→Inspection | maintenance→Maintenance");
        sb.AppendLine("RATE BOOK MATCH: fuzzy/substring match on Client+Name fields. Exact city+state→load it. Different location→suggest_rate_book + clone option. No match→ask_clarification. Already loaded→don't prompt again.");
        sb.AppendLine("RATE ANOMALY: when adding rows, if |rate - benchmark| > 20% and 3+ data points → emit rate_anomaly_warning (non-blocking).");
        sb.AppendLine("DB TOOLS: call BEFORE update_estimate for business Q&A. get_pipeline_summary | get_active_jobs | search_estimates(status,client,startAfter,startBefore) | get_win_loss_stats(client) | get_labor_utilization");
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
                                    lost_reason = new { type = "string", description = "Required if new_status is Lost" }
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
