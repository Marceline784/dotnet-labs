using System.Data;
using Microsoft.Data.SqlClient;
using Recommendations.Dal.Interfaces;
using Recommendations.Dal.Repositories;

namespace Recommendations.Dal;

/// <summary>
/// Unit of Work: відкриває ОДНЕ з'єднання на всю операцію, починає транзакцію з потрібним рівнем ізоляції,
/// координує кілька репозиторіїв в одній транзакції та гарантує Commit або Rollback.
/// Реєструється в DI як Scoped: один екземпляр і одне з'єднання на HTTP-запит.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SqlConnection _connection;
    private SqlTransaction? _transaction;

    public UnitOfWork(string connectionString)
    {
        _connection = new SqlConnection(connectionString);

        // усі репозиторії отримують ОДНЕ з'єднання і доступ до ПОТОЧНОЇ транзакції
        Users = new UserRepository(_connection, () => _transaction);
        WardrobeItems = new WardrobeItemRepository(_connection, () => _transaction);
        Outfits = new OutfitRepository(_connection, () => _transaction);
        Recommendations = new RecommendationRepository(_connection, () => _transaction);
    }

    public IUserRepository Users { get; }
    public IWardrobeItemRepository WardrobeItems { get; }
    public IOutfitRepository Outfits { get; }
    public IRecommendationRepository Recommendations { get; }

    public async Task BeginTransactionAsync(IsolationLevel level = IsolationLevel.ReadCommitted, CancellationToken ct = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("Transaction is already started");

        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(ct);

        _transaction = (SqlTransaction)await _connection.BeginTransactionAsync(level, ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("Transaction is not started");

        try
        {
            await _transaction.CommitAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;

        try
        {
            await _transaction.RollbackAsync(ct);
        }
        catch (InvalidOperationException)
        {
            // транзакцію вже відкотив сам SQL Server (наприклад, SET XACT_ABORT ON у процедурі після помилки);
            // повторний Rollback недоступний, а нам потрібен саме результат «змін немає»
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync();   // незавершена транзакція відкочується при закритті з'єднання

        await _connection.DisposeAsync();
    }
}
