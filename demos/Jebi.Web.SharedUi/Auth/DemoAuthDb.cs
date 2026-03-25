using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;

namespace Jebi.Web.Auth;

internal sealed class DemoAuthDb
{
    private readonly string _connectionString;

    public DemoAuthDb(DemoAuthOptions options, IHostEnvironment env)
    {
        var dbPath = string.IsNullOrWhiteSpace(options.DbPath)
            ? Path.Combine(env.ContentRootPath, "Database", "demo-auth.db")
            : options.DbPath;
        _connectionString = $"Data Source={dbPath}";
    }

    public async Task EnsureCreatedAsync()
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS "DemoUsers" (
                "Id"                  TEXT NOT NULL PRIMARY KEY,
                "Email"               TEXT NOT NULL UNIQUE COLLATE NOCASE,
                "PasswordHash"        TEXT NOT NULL,
                "Salt"                TEXT NOT NULL,
                "ResetToken"          TEXT NULL,
                "ResetTokenExpiresAt" TEXT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<DemoAuthUser?> FindByEmailAsync(string email)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT "Id","Email","PasswordHash","Salt","ResetToken","ResetTokenExpiresAt"
            FROM "DemoUsers" WHERE "Email" = @email LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@email", email);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<DemoAuthUser?> FindByResetTokenAsync(string token)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT "Id","Email","PasswordHash","Salt","ResetToken","ResetTokenExpiresAt"
            FROM "DemoUsers" WHERE "ResetToken" = @token LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@token", token);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task CreateAsync(DemoAuthUser user)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO "DemoUsers" ("Id","Email","PasswordHash","Salt")
            VALUES (@id, @email, @hash, @salt)
            """;
        cmd.Parameters.AddWithValue("@id", user.Id.ToString());
        cmd.Parameters.AddWithValue("@email", user.Email);
        cmd.Parameters.AddWithValue("@hash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@salt", user.Salt);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(DemoAuthUser user)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE "DemoUsers"
            SET "PasswordHash"        = @hash,
                "Salt"                = @salt,
                "ResetToken"          = @resetToken,
                "ResetTokenExpiresAt" = @resetTokenExpiresAt
            WHERE "Id" = @id
            """;
        cmd.Parameters.AddWithValue("@id", user.Id.ToString());
        cmd.Parameters.AddWithValue("@hash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@salt", user.Salt);
        cmd.Parameters.AddWithValue("@resetToken", (object?)user.ResetToken ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@resetTokenExpiresAt",
            user.ResetTokenExpiresAt.HasValue
                ? (object)user.ResetTokenExpiresAt.Value.ToString("O")
                : DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    private static DemoAuthUser Map(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(0)),
        Email = r.GetString(1),
        PasswordHash = r.GetString(2),
        Salt = r.GetString(3),
        ResetToken = r.IsDBNull(4) ? null : r.GetString(4),
        ResetTokenExpiresAt = r.IsDBNull(5) ? null : DateTime.Parse(r.GetString(5))
    };
}
