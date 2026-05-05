using System;
using System.Collections.Generic;

namespace NS.Module.AuditLogging.Application.Contracts.Dtos;

public class AuditLogStatisticsDto
{
    public Dictionary<DateTime, double> AverageExecutionDurationPerDay { get; set; } = new();
}

