using System;
using SqlSugar;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

namespace NS.Framework.SqlSugar.Interceptors;

public class AuditInterceptor : SqlSugarInterceptorBase, ITransientDependency
{
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _tenant;
    private readonly IGuidGenerator _guid;

    public override int ExecutionOrder => 10;

    public AuditInterceptor(
        ICurrentUser currentUser,
        ICurrentTenant tenant,
        IGuidGenerator guid)
    {
        _currentUser = currentUser;
        _tenant = tenant;
        _guid = guid;
    }

    public override void DataExecuting(object value, DataFilterModel model)
    {
        var prop = model.PropertyName;
        var type = model.EntityColumnInfo.PropertyInfo.PropertyType;

        // --- Insert 审计 ---
        if (model.OperationType == DataFilterType.InsertByObject)
        {
            if (prop == nameof(IEntity<Guid>.Id) && type == typeof(Guid) && (Guid)value == Guid.Empty)
                model.SetValue(_guid.Create());

            if (prop == nameof(IAuditedObject.CreationTime))
                model.SetValue(DateTime.Now);

            if (prop == nameof(IAuditedObject.CreatorId) && _currentUser.Id != null)
                model.SetValue(_currentUser.Id);

            if (prop == nameof(IMultiTenant.TenantId) && _tenant.Id != null)
                model.SetValue(_tenant.Id);
        }

        // --- Update 审计 ---
        else if (model.OperationType == DataFilterType.UpdateByObject)
        {
            if (prop == nameof(IAuditedObject.LastModificationTime))
                model.SetValue(DateTime.Now);

            if (prop == nameof(IAuditedObject.LastModifierId) && _currentUser.Id != null)
                model.SetValue(_currentUser.Id);
        }
    }
}