using Atlas.Application.Approval.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Enums;

namespace Atlas.Infrastructure.Services.ApprovalFlow.NotificationSenders;

/// <summary>
/// 邮件通知发送适配器（占位实现）
/// </summary>
public sealed class EmailNotificationSender : IApprovalNotificationSender
{
    public ApprovalNotificationChannel SupportedChannel => ApprovalNotificationChannel.Email;

    public Task<bool> SendAsync(
        TenantId tenantId,
        long recipientUserId,
        string title,
        string content,
        CancellationToken cancellationToken)
    {
        // 当前约束：邮件渠道在本版本默认禁用，保持 no-op 以避免误发送。
        // 跟踪任务：MSG-302（https://tracker.local/MSG-302），预计版本：v1.5。
        // 1. 根据 recipientUserId 查询用户邮箱
        // 2. 调用邮件服务发送
        // 3. 记录发送日志
        return Task.FromResult(true);
    }
}
