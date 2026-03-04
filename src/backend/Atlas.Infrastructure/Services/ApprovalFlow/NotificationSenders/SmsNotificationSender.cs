using Atlas.Application.Approval.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Enums;

namespace Atlas.Infrastructure.Services.ApprovalFlow.NotificationSenders;

/// <summary>
/// 短信通知发送适配器（占位实现）
/// </summary>
public sealed class SmsNotificationSender : IApprovalNotificationSender
{
    public ApprovalNotificationChannel SupportedChannel => ApprovalNotificationChannel.Sms;

    public Task<bool> SendAsync(
        TenantId tenantId,
        long recipientUserId,
        string title,
        string content,
        CancellationToken cancellationToken)
    {
        // 当前约束：短信渠道在本版本默认禁用，保持 no-op 以避免误发送。
        // 跟踪任务：MSG-301（https://tracker.local/MSG-301），预计版本：v1.5。
        // 1. 根据 recipientUserId 查询用户手机号
        // 2. 调用短信服务发送
        // 3. 记录发送日志
        return Task.FromResult(true);
    }
}
