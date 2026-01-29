using System.Text;
using System.Text.Json;
using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Abstractions.Persistence;
using Atlas.WorkflowCore.Exceptions;
using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.WorkflowCore.Services;

/// <summary>
/// 活动控制器实现
/// </summary>
public class ActivityController : IActivityController
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkflowController _workflowController;
    private readonly ILogger<ActivityController> _logger;

    public ActivityController(
        ISubscriptionRepository subscriptionRepository,
        IWorkflowController workflowController,
        IDateTimeProvider dateTimeProvider,
        IDistributedLockProvider lockProvider,
        ILogger<ActivityController> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _dateTimeProvider = dateTimeProvider;
        _lockProvider = lockProvider;
        _workflowController = workflowController;
        _logger = logger;
    }

    /// <summary>
    /// 获取待处理的活动（长轮询）
    /// </summary>
    public async Task<PendingActivity?> GetPendingActivity(string activityName, string workerId, TimeSpan? timeout = null)
    {
        var endTime = _dateTimeProvider.UtcNow.Add(timeout ?? TimeSpan.Zero);
        var firstPass = true;
        EventSubscription? subscription = null;

        while ((subscription == null && _dateTimeProvider.UtcNow < endTime) || firstPass)
        {
            if (!firstPass)
                await Task.Delay(100);

            subscription = await _subscriptionRepository.GetFirstOpenSubscription(
                Event.EventTypeActivity, 
                activityName, 
                _dateTimeProvider.UtcNow);

            if (subscription != null)
            {
                if (!await _lockProvider.AcquireLock($"sub:{subscription.Id}", CancellationToken.None))
                    subscription = null;
            }

            firstPass = false;
        }

        if (subscription == null)
            return null;

        try
        {
            var token = Token.Create(subscription.Id, subscription.EventKey);
            var result = new PendingActivity
            {
                Token = token.Encode(),
                ActivityName = subscription.EventKey,
                Parameters = subscription.SubscriptionData,
                TokenExpiry = new DateTime(DateTime.MaxValue.Ticks, DateTimeKind.Utc)
            };

            if (!await _subscriptionRepository.SetSubscriptionToken(
                subscription.Id, 
                result.Token, 
                workerId, 
                result.TokenExpiry))
            {
                return null;
            }

            return result;
        }
        finally
        {
            await _lockProvider.ReleaseLock($"sub:{subscription.Id}");
        }
    }

    /// <summary>
    /// 释放活动令牌
    /// </summary>
    public async Task ReleaseActivityToken(string token)
    {
        var tokenObj = Token.Decode(token);
        await _subscriptionRepository.ClearSubscriptionToken(tokenObj.SubscriptionId, token);
    }

    /// <summary>
    /// 提交活动成功结果
    /// </summary>
    public async Task SubmitActivitySuccess(string token, object result)
    {
        await SubmitActivityResult(token, new ActivityResult
        {
            Data = result,
            Status = ActivityResultStatus.Success
        });
    }

    /// <summary>
    /// 提交活动失败结果
    /// </summary>
    public async Task SubmitActivityFailure(string token, object result)
    {
        await SubmitActivityResult(token, new ActivityResult
        {
            Data = result,
            Status = ActivityResultStatus.Failure
        });
    }

    /// <summary>
    /// 提交活动结果（内部方法）
    /// </summary>
    private async Task SubmitActivityResult(string token, ActivityResult result)
    {
        var tokenObj = Token.Decode(token);
        var sub = await _subscriptionRepository.GetSubscription(tokenObj.SubscriptionId);
        
        if (sub == null)
            throw new NotFoundException("EventSubscription", tokenObj.SubscriptionId);

        if (sub.ExternalToken != token)
            throw new InvalidOperationException("Token mismatch");

        result.SubscriptionId = sub.Id;

        await _workflowController.PublishEventAsync(sub.EventName, sub.EventKey, result);
    }

    /// <summary>
    /// 活动令牌类（内部使用）
    /// </summary>
    private class Token
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public string ActivityName { get; set; } = string.Empty;
        public string Nonce { get; set; } = string.Empty;

        /// <summary>
        /// 编码为 Base64 字符串
        /// </summary>
        public string Encode()
        {
            var json = JsonSerializer.Serialize(this);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        /// <summary>
        /// 创建新的令牌
        /// </summary>
        public static Token Create(string subscriptionId, string activityName)
        {
            return new Token
            {
                SubscriptionId = subscriptionId,
                ActivityName = activityName,
                Nonce = Guid.NewGuid().ToString()
            };
        }

        /// <summary>
        /// 从 Base64 字符串解码
        /// </summary>
        public static Token Decode(string encodedToken)
        {
            var raw = Convert.FromBase64String(encodedToken);
            var json = Encoding.UTF8.GetString(raw);
            return JsonSerializer.Deserialize<Token>(json) ?? throw new InvalidOperationException("Invalid token");
        }
    }
}
