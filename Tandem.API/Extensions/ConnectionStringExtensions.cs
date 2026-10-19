using Npgsql;

namespace Tandem.API.Extensions;

public static class ConnectionStringExtensions
{
    // Render gives the database address as a URL (postgresql://user:password@host/database),
    // while Npgsql expects "Host=...;Database=...". This accepts both, so the URL can be pasted as-is
    public static string ToNpgsqlConnectionString(this string connectionString)
    {
        var value = connectionString.Trim().Trim('"', '\'');

        if (!value.StartsWith("postgres://") && !value.StartsWith("postgresql://"))
        {
            return value;
        }

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null
        };
        return builder.ConnectionString;
    }
}
