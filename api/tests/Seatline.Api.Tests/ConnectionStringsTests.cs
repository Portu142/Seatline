using Npgsql;
using Seatline.Api.Data;

namespace Seatline.Api.Tests;

public sealed class ConnectionStringsTests
{
    [Fact]
    public void Key_value_connection_strings_keep_their_values()
    {
        var result = new NpgsqlConnectionStringBuilder(
            ConnectionStrings.Normalize("Host=db;Database=seatline;Username=u;Password=p"));

        Assert.Equal("db", result.Host);
        Assert.Equal("seatline", result.Database);
        Assert.Equal("u", result.Username);
        Assert.Equal("p", result.Password);
    }

    [Fact]
    public void Gss_encryption_is_disabled_unless_explicitly_configured()
    {
        Assert.Equal(GssEncryptionMode.Disable,
            new NpgsqlConnectionStringBuilder(ConnectionStrings.Normalize("Host=db")).GssEncryptionMode);
        Assert.Equal(GssEncryptionMode.Require,
            new NpgsqlConnectionStringBuilder(ConnectionStrings.Normalize("Host=db;Gss Encryption Mode=Require")).GssEncryptionMode);
    }

    [Fact]
    public void Postgres_urls_are_converted_to_npgsql_format()
    {
        var result = new NpgsqlConnectionStringBuilder(
            ConnectionStrings.Normalize("postgresql://app:s%40cret@db.example.com:6543/seatline?sslmode=require"));

        Assert.Equal("db.example.com", result.Host);
        Assert.Equal(6543, result.Port);
        Assert.Equal("seatline", result.Database);
        Assert.Equal("app", result.Username);
        Assert.Equal("s@cret", result.Password);
        Assert.Equal(SslMode.Require, result.SslMode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Missing_connection_string_fails_fast(string? value) =>
        Assert.Throws<InvalidOperationException>(() => ConnectionStrings.Normalize(value));
}
