﻿using AGooday.AgPay.Application.Interfaces;
using Quartz;

namespace AGooday.AgPay.Payment.Api.Jobs
{
    /// <summary>
    /// 退款订单过期定时任务
    /// </summary>
    [DisallowConcurrentExecution]
    public class RefundOrderExpiredJob : IJob
    {
        private readonly ILogger<RefundOrderExpiredJob> logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public RefundOrderExpiredJob(ILogger<RefundOrderExpiredJob> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            this.logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public Task Execute(IJobExecutionContext context)
        {
            return Task.Run(() =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var refundOrderService = scope.ServiceProvider.GetService<IRefundOrderService>();
                    int updateCount = refundOrderService.UpdateOrderExpired();
                    logger.LogInformation($"处理退款订单超时{updateCount}条.");
                }
            });
        }
    }
}
