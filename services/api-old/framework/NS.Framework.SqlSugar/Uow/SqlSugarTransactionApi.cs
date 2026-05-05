using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Volo.Abp.Uow;

namespace NS.Framework.SqlSugar.Uow;
public class SqlSugarTransactionApi : ITransactionApi
{
    private readonly ISqlSugarClient _client;
    private readonly ILogger _logger;

    private readonly Guid _uowId;
    private readonly string _transactionApiKey;

    private readonly string? _connectionKey;
    private readonly string? _connectionName;
    private readonly string? _tenantId;

    private bool _completed;
    private bool _disposed;

    public SqlSugarTransactionApi(
        ISqlSugarClient client,
        ILogger logger,
        Guid uowId,
        string transactionApiKey,
        string? connectionKey = null,
        string? connectionName = null,
        string? tenantId = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger;
        _uowId = uowId;
        _transactionApiKey = transactionApiKey;
        _connectionKey = connectionKey;
        _connectionName = connectionName;
        _tenantId = tenantId;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (!_completed)
            {
                _client.Ado.RollbackTran();
                _completed = true; // 建议补上，语义更完整
                _logger.LogWarning(
                    "【SqlSugar事务在Dispose阶段检测到事务未提交，已自动执行回滚】UoW={UowId}, " +
                    "TranKey={TranKey}, DbKey={DbKey}, tenantId={TenantId}, connectionName={ConnectionName}",
                    _uowId, _transactionApiKey, _connectionKey, _tenantId, _connectionName);
            }
            else
            {
                _logger.LogDebug(
                    "【SqlSugar事务生命周期结束并已释放】UoW={UowId},TranKey={TranKey}, DbKey={DbKey}",
                    _uowId, _transactionApiKey, _connectionKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SqlSugar UoW={UowId} transaction dispose error. TranKey={TranKey}, DbKey={DbKey}, tenantId={TenantId}, connectionName={ConnectionName}",
                _uowId, _transactionApiKey, _connectionKey, _tenantId, _connectionName);
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_completed) return;

        try
        {
            await _client.Ado.CommitTranAsync();
            _completed = true;
            _logger.LogInformation(
                "【SqlSugar事务提交】UoW={UowId}, TranKey={TranKey}, DbKey={DbKey}, tenantId={TenantId}, connectionName={ConnectionName}",
                _uowId, _transactionApiKey, _connectionKey, _tenantId, _connectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "【SqlSugar事务失败】UoW={UowId}, TranKey={TranKey}, DbKey={DbKey}",
                _uowId, _transactionApiKey, _connectionKey);
            throw;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_completed) return;

        try
        {
            await _client.Ado.RollbackTranAsync();
            _completed = true;
            _logger.LogWarning(
                "【SqlSugar事务回滚】UoW={UowId}, TranKey={TranKey}, DbKey={DbKey}",
                _uowId, _transactionApiKey, _connectionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "【SqlSugar事务回滚发生错误】UoW={UowId} , TranKey={TranKey}, DbKey={DbKey}",
                _uowId, _transactionApiKey, _connectionKey);
            throw;
        }
    }
}
