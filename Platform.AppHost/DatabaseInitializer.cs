using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Platform.AppHost;

/// <summary>
/// Виконує SQL-скрипти з Лаб. 01. Файли містять розділювач GO (команда SSMS/sqlcmd, не T-SQL),
/// тому скрипт ділиться на пакети по рядках «GO» і кожен пакет виконується окремо.
/// </summary>
internal static class DatabaseInitializer
{
    private static readonly Regex GoSeparator =
        new(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    public static async Task RunAsync(string connectionString, IEnumerable<string> scriptFiles,
        ILogger logger, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        foreach (var file in scriptFiles)
        {
            if (!File.Exists(file))
            {
                logger.LogWarning("SQL script not found: {File}", file);
                continue;
            }

            logger.LogInformation("Applying {Script}", Path.GetFileName(file));
            var text = await File.ReadAllTextAsync(file, ct);

            foreach (var batch in GoSeparator.Split(text))
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;

                await using var command = new SqlCommand(batch, connection) { CommandTimeout = 120 };
                await command.ExecuteNonQueryAsync(ct);
            }
        }

        logger.LogInformation("Database scripts applied");
    }
}
