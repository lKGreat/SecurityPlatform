using Atlas.Application.DynamicTables.Abstractions;
using Atlas.Application.DynamicTables.Models;
using Atlas.Application.DynamicTables.Repositories;
using Atlas.Core.Exceptions;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.Domain.DynamicTables.Entities;

namespace Atlas.Infrastructure.Services;

public sealed class DynamicRecordQueryService : IDynamicRecordQueryService
{
    private readonly IDynamicTableRepository _tableRepository;
    private readonly IDynamicFieldRepository _fieldRepository;
    private readonly IDynamicRecordRepository _recordRepository;

    public DynamicRecordQueryService(
        IDynamicTableRepository tableRepository,
        IDynamicFieldRepository fieldRepository,
        IDynamicRecordRepository recordRepository)
    {
        _tableRepository = tableRepository;
        _fieldRepository = fieldRepository;
        _recordRepository = recordRepository;
    }

    public async Task<DynamicRecordListResult> QueryAsync(
        TenantId tenantId,
        string tableKey,
        DynamicRecordQueryRequest request,
        CancellationToken cancellationToken)
    {
        var table = await _tableRepository.FindByKeyAsync(tenantId, tableKey, cancellationToken);
        if (table is null)
        {
            throw new BusinessException(ErrorCodes.NotFound, "动态表不存在。");
        }

        var fields = await _fieldRepository.ListByTableIdAsync(tenantId, table.Id, cancellationToken);
        if (fields.Count == 0)
        {
            throw new BusinessException(ErrorCodes.ValidationError, "动态表字段为空。");
        }

        return await _recordRepository.QueryAsync(tenantId, table, fields, request, cancellationToken);
    }

    public async Task<DynamicRecordDto?> GetByIdAsync(
        TenantId tenantId,
        string tableKey,
        long id,
        CancellationToken cancellationToken)
    {
        var table = await _tableRepository.FindByKeyAsync(tenantId, tableKey, cancellationToken);
        if (table is null)
        {
            return null;
        }

        var fields = await _fieldRepository.ListByTableIdAsync(tenantId, table.Id, cancellationToken);
        if (fields.Count == 0)
        {
            return null;
        }

        return await _recordRepository.GetByIdAsync(tenantId, table, fields, id, cancellationToken);
    }

    public async Task<DynamicRecordExportResult> ExportAsync(
        TenantId tenantId,
        string tableKey,
        DynamicRecordExportRequest request,
        CancellationToken cancellationToken)
    {
        var table = await _tableRepository.FindByKeyAsync(tenantId, tableKey, cancellationToken);
        if (table is null)
        {
            throw new BusinessException(ErrorCodes.NotFound, "动态表不存在。");
        }

        var fields = await _fieldRepository.ListByTableIdAsync(tenantId, table.Id, cancellationToken);
        if (fields.Count == 0)
        {
            throw new BusinessException(ErrorCodes.ValidationError, "动态表字段为空。");
        }

        var selectedFields = ResolveExportFields(fields, request.Fields);
        var queryRequest = new DynamicRecordQueryRequest(
            1,
            1000,
            request.Keyword,
            request.SortBy,
            request.SortDesc,
            request.Filters ?? Array.Empty<DynamicFilterCondition>());
        var records = await _recordRepository.QueryAllAsync(tenantId, table, fields, queryRequest, cancellationToken);
        var content = BuildCsv(selectedFields, records);
        var fileName = $"{tableKey}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.csv";
        return new DynamicRecordExportResult(fileName, "text/csv; charset=utf-8", content);
    }

    private static IReadOnlyList<DynamicField> ResolveExportFields(
        IReadOnlyList<DynamicField> fields,
        IReadOnlyList<string>? requestedFields)
    {
        if (requestedFields is null || requestedFields.Count == 0)
        {
            return fields.OrderBy(x => x.SortOrder).ToArray();
        }

        var fieldSet = requestedFields
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selected = fields.Where(x => fieldSet.Contains(x.Name)).OrderBy(x => x.SortOrder).ToArray();
        return selected.Length == 0 ? fields.OrderBy(x => x.SortOrder).ToArray() : selected;
    }

    private static byte[] BuildCsv(
        IReadOnlyList<DynamicField> selectedFields,
        IReadOnlyList<DynamicRecordDto> records)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine(string.Join(",", selectedFields.Select(x => EscapeCsv(string.IsNullOrWhiteSpace(x.DisplayName) ? x.Name : x.DisplayName!))));

        foreach (var record in records)
        {
            var valueMap = record.Values.ToDictionary(x => x.Field, StringComparer.OrdinalIgnoreCase);
            var row = new List<string>(selectedFields.Count);
            foreach (var field in selectedFields)
            {
                if (!valueMap.TryGetValue(field.Name, out var value))
                {
                    row.Add(string.Empty);
                    continue;
                }

                row.Add(EscapeCsv(ResolveCsvValue(value)));
            }

            builder.AppendLine(string.Join(",", row));
        }

        return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string ResolveCsvValue(DynamicFieldValueDto value)
    {
        if (!string.IsNullOrWhiteSpace(value.StringValue))
        {
            return value.StringValue;
        }

        if (value.IntValue.HasValue)
        {
            return value.IntValue.Value.ToString();
        }

        if (value.LongValue.HasValue)
        {
            return value.LongValue.Value.ToString();
        }

        if (value.DecimalValue.HasValue)
        {
            return value.DecimalValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (value.BoolValue.HasValue)
        {
            return value.BoolValue.Value ? "true" : "false";
        }

        if (value.DateTimeValue.HasValue)
        {
            return value.DateTimeValue.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        if (value.DateValue.HasValue)
        {
            return value.DateValue.Value.ToString("yyyy-MM-dd");
        }

        return string.Empty;
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"'))
        {
            value = value.Replace("\"", "\"\"");
        }

        return value.Contains(',') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value}\""
            : value;
    }
}
