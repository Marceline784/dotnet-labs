using System.Data;
using Microsoft.Data.SqlClient;
using Recommendations.Dal.Interfaces;
using Recommendations.Domain.Models;

namespace Recommendations.Dal.Repositories;

/// <summary>
/// ЧИСТИЙ ADO.NET (без Dapper): SqlCommand, типізовані параметри, SqlDataReader.
/// Усі значення передаються лише як параметри; команди та reader звільняються через await using.
/// </summary>
public sealed class UserRepository : RepositoryBase, IUserRepository
{
    public UserRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction) { }

    public Task<User?> GetByIdAsync(long id, CancellationToken ct = default) =>
        GuardAsync<User?>(async () =>
        {
            const string sql = "SELECT Id, Email, DisplayName FROM dbo.Users WHERE Id = @Id AND IsDeleted = 0";
            await EnsureOpenAsync(ct);
            await using var command = new SqlCommand(sql, Connection, Transaction);
            command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;
            await using var reader = await command.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? MapUser(reader) : null;
        });

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        GuardAsync<User?>(async () =>
        {
            const string sql = "SELECT Id, Email, DisplayName FROM dbo.Users WHERE Email = @Email AND IsDeleted = 0";
            await EnsureOpenAsync(ct);
            await using var command = new SqlCommand(sql, Connection, Transaction);
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = email;
            await using var reader = await command.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? MapUser(reader) : null;
        });

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) =>
        GuardAsync<IReadOnlyList<User>>(async () =>
        {
            const string sql = "SELECT Id, Email, DisplayName FROM dbo.Users WHERE IsDeleted = 0 ORDER BY Id";
            await EnsureOpenAsync(ct);
            await using var command = new SqlCommand(sql, Connection, Transaction);
            await using var reader = await command.ExecuteReaderAsync(ct);
            var list = new List<User>();
            while (await reader.ReadAsync(ct))
                list.Add(MapUser(reader));
            return list;
        });

    public Task<long> AddAsync(User user, CancellationToken ct = default) =>
        GuardAsync<long>(async () =>
        {
            const string sql = @"
                INSERT INTO dbo.Users (Email, DisplayName)
                OUTPUT INSERTED.Id
                VALUES (@Email, @DisplayName)";
            await EnsureOpenAsync(ct);
            await using var command = new SqlCommand(sql, Connection, Transaction);
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = user.Email;
            command.Parameters.Add("@DisplayName", SqlDbType.NVarChar, 100).Value = user.DisplayName;
            var id = await command.ExecuteScalarAsync(ct);
            return Convert.ToInt64(id);
        });

    public Task<bool> DeleteAsync(long id, CancellationToken ct = default) =>
        GuardAsync<bool>(async () =>
        {
            // м'яке видалення: історія рекомендацій користувача зберігається
            const string sql = "UPDATE dbo.Users SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id AND IsDeleted = 0";
            await EnsureOpenAsync(ct);
            await using var command = new SqlCommand(sql, Connection, Transaction);
            command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;
            return await command.ExecuteNonQueryAsync(ct) > 0;
        });

    public Task<UserProfile?> GetProfileAsync(long userId, CancellationToken ct = default) =>
        GuardAsync<UserProfile?>(async () =>
        {
            const string sql = @"
                SELECT UserId, City, Latitude, Longitude, ColdSensitivity, StylePreference
                FROM dbo.UserProfiles WHERE UserId = @UserId AND IsDeleted = 0";
            await EnsureOpenAsync(ct);
            await using var command = new SqlCommand(sql, Connection, Transaction);
            command.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return null;

            return new UserProfile
            {
                UserId = reader.GetInt64(0),
                City = reader.GetString(1),
                Latitude = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                Longitude = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                ColdSensitivity = reader.GetDecimal(4),
                StylePreference = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        });

    public Task SaveProfileAsync(UserProfile profile, CancellationToken ct = default) =>
        GuardVoidAsync(async () =>
        {
            // upsert: спочатку UPDATE, а якщо рядка немає, то INSERT (профіль 1:1 з користувачем)
            const string sql = @"
                UPDATE dbo.UserProfiles
                SET City = @City, Latitude = @Latitude, Longitude = @Longitude,
                    ColdSensitivity = @Cold, StylePreference = @Style, UpdatedAt = SYSUTCDATETIME()
                WHERE UserId = @UserId;
                IF @@ROWCOUNT = 0
                    INSERT INTO dbo.UserProfiles (UserId, City, Latitude, Longitude, ColdSensitivity, StylePreference)
                    VALUES (@UserId, @City, @Latitude, @Longitude, @Cold, @Style);";
            await EnsureOpenAsync(ct);
            await using var command = new SqlCommand(sql, Connection, Transaction);
            command.Parameters.Add("@UserId", SqlDbType.BigInt).Value = profile.UserId;
            command.Parameters.Add("@City", SqlDbType.NVarChar, 100).Value = profile.City;
            command.Parameters.Add(DecimalParam("@Latitude", profile.Latitude, 9, 6));
            command.Parameters.Add(DecimalParam("@Longitude", profile.Longitude, 9, 6));
            command.Parameters.Add(DecimalParam("@Cold", profile.ColdSensitivity, 3, 1));
            command.Parameters.Add("@Style", SqlDbType.NVarChar, 30).Value = (object?)profile.StylePreference ?? DBNull.Value;
            await command.ExecuteNonQueryAsync(ct);
        });

    private static User MapUser(SqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Email = reader.GetString(1),
        DisplayName = reader.GetString(2)
    };

    // для decimal обов'язково вказуємо precision/scale, інакше значення може бути округлене
    private static SqlParameter DecimalParam(string name, decimal? value, byte precision, byte scale) =>
        new(name, SqlDbType.Decimal)
        {
            Precision = precision,
            Scale = scale,
            Value = (object?)value ?? DBNull.Value
        };
}
