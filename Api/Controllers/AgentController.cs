using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[Route("api/agent")]
public class AgentController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    private static readonly string[] BlockedKeywords =
    [
        "DROP", "DELETE", "UPDATE", "INSERT", "EXEC", "EXECUTE",
        "XP_", "SP_", "--", ";--", "TRUNCATE", "ALTER", "CREATE"
    ];

    public AgentController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ── GET /api/agent/health ─────────────────────────────────────────────────

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken ct = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            return Ok(new { status = "ok", database = "StrongholdEstimating_Demo" });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { status = "unavailable", error = ex.Message });
        }
    }

    // ── GET /api/agent/schema ─────────────────────────────────────────────────

    [HttpGet("schema")]
    public async Task<IActionResult> Schema(CancellationToken ct = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync(ct);

            var tableDict = new Dictionary<string, (string Schema, List<object> Columns)>();

            // Load tables
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT TABLE_SCHEMA, TABLE_NAME
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_TYPE = 'BASE TABLE'
                      AND TABLE_SCHEMA NOT IN ('sys','INFORMATION_SCHEMA','guest','db_owner')
                    ORDER BY TABLE_SCHEMA, TABLE_NAME
                    """;
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var schema = reader.GetString(0);
                    var name = reader.GetString(1);
                    tableDict[name] = (schema, []);
                }
            }

            // Load columns
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA NOT IN ('sys','INFORMATION_SCHEMA','guest','db_owner')
                    ORDER BY TABLE_NAME, ORDINAL_POSITION
                    """;
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var tableName = reader.GetString(0);
                    if (!tableDict.TryGetValue(tableName, out var entry)) continue;
                    entry.Columns.Add(new
                    {
                        columnName = reader.GetString(1),
                        dataType = reader.GetString(2),
                        isNullable = reader.GetString(3)
                    });
                }
            }

            await conn.CloseAsync();

            var tables = tableDict.Select(kvp => new
            {
                tableName = kvp.Key,
                schema = kvp.Value.Schema,
                columns = kvp.Value.Columns
            }).ToList();

            return Ok(new { tables });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ── POST /api/agent/query ─────────────────────────────────────────────────

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] AgentQueryRequest body, CancellationToken ct = default)
    {
        var sql = body?.Sql?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sql))
            return BadRequest(new { error = "sql is required." });

        var upper = sql.ToUpperInvariant();
        if (!upper.StartsWith("SELECT"))
            return StatusCode(403, new { error = "Only SELECT queries are permitted." });

        foreach (var keyword in BlockedKeywords)
            if (upper.Contains(keyword))
                return StatusCode(403, new { error = "Only SELECT queries are permitted." });

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync(ct);

            var rows = new List<Dictionary<string, object?>>();

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandTimeout = 15;
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var row = new Dictionary<string, object?>();
                    for (var i = 0; i < reader.FieldCount; i++)
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    rows.Add(row);
                }
            }

            await conn.CloseAsync();
            return Ok(new { data = rows, rowCount = rows.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public record AgentQueryRequest(string Sql);
