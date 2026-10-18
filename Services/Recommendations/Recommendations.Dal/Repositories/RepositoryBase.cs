using System.Data;
using Microsoft.Data.SqlClient;
using Recommendations.Dal.Errors;

namespace Recommendations.Dal.Repositories;

/// <summary>
/// Спільна основа всіх репозиторіїв. Репозиторій отримує НЕ рядок підключення, а готове з'єднання
/// та доступ до ПОТОЧНОЇ транзакції Unit of Work, тому всі репозиторії однієї операції працюють в одній транзакції.
/// </summary>
public abstract class RepositoryBase
{
    protected readonly SqlConnection Connection;
    private readonly Func<SqlTransaction?> _currentTransaction;

    protected RepositoryBase(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
    {
        Connection = connection;
        _currentTransaction = currentTransaction;
    }

    /// <summary>Поточна транзакція Unit of Work (або null, якщо її не розпочато).</summary>
    protected SqlTransaction? Transaction => _currentTransaction();

    protected async Task EnsureOpenAsync(CancellationToken ct)
    {
        if (Connection.State != ConnectionState.Open)
            await Connection.OpenAsync(ct);
    }

    /// <summary>Виконує дію й перетворює SqlException на доменний виняток (якщо помилка відома).</summary>
    protected async Task<TResult> GuardAsync<TResult>(Func<Task<TResult>> action)
    {
        try
        {
            return await action();
        }
        catch (SqlException ex)
        {
            var domain = SqlExceptionTranslator.Translate(ex);
            if (domain is null) throw;
            throw domain;
        }
    }

    protected async Task GuardVoidAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (SqlException ex)
        {
            var domain = SqlExceptionTranslator.Translate(ex);
            if (domain is null) throw;
            throw domain;
        }
    }
}
