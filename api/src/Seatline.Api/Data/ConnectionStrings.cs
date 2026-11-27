using System.Data.Common;
using Npgsql;

namespace Seatline.Api.Data;

public static class ConnectionStrings
{
    /// <summary>
    /// Accepts either an Npgsql key/value connection string or a URL such as
    /// <c>postgresql://user:pass@host:5432/db?sslmode=require</c> (the format Render, Neon and Railway hand out).
    /// </summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is not configured. Set ConnectionStrings__Default (see .env.example).");
        }

        var isUrl = value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                    value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
        var builder = isUrl ? FromUrl(value) : new NpgsqlConnectionStringBuilder(value);

        // Kerberos/GSS is never used here, and the chiseled runtime image has no libgssapi.
        // Without this, Npgsql probes for GSS encryption and logs a missing-library error.
        var raw = isUrl ? null : new DbConnectionStringBuilder { ConnectionString = value };
        if (raw is null || (!raw.ContainsKey("Gss Encryption Mode") && !raw.ContainsKey("GssEncryptionMode")))
        {
            builder.GssEncryptionMode = GssEncryptionMode.Disable;
        }

        return builder.ConnectionString;
    }

    private static NpgsqlConnectionStringBuilder FromUrl(string value)
    {
        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
        };

        var query = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(p => p[0], p => p.Length > 1 ? Uri.UnescapeDataString(p[1]) : string.Empty,
                StringComparer.OrdinalIgnoreCase);

        if (query.TryGetValue("sslmode", out var sslMode) &&
            Enum.TryParse<SslMode>(sslMode.Replace("-", string.Empty, StringComparison.Ordinal), true, out var mode))
        {
            builder.SslMode = mode;
        }

        return builder;
    }
}
