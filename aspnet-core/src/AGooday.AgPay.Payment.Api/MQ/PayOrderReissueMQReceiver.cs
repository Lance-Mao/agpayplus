﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Common.Enumerator;
using AGooday.AgPay.Components.MQ.Models;
using AGooday.AgPay.Components.MQ.Vender;
using AGooday.AgPay.Payment.Api.RQRS.Msg;
using AGooday.AgPay.Payment.Api.Services;
using Newtonsoft.Json;

namespace AGooday.AgPay.Payment.Api.MQ
{
    /// <summary>
    /// 接收MQ消息
    /// 业务：支付订单补单（一般用于没有回调的接口，比如微信的条码支付）
    /// </summary>
    public class PayOrderReissueMQReceiver : PayOrderReissueMQ.IMQReceiver
    {
        private readonly IMQSender mqSender;
        private readonly ILogger<PayOrderReissueMQReceiver> log;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public PayOrderReissueMQReceiver(IMQSender mqSender,
            ILogger<PayOrderReissueMQReceiver> log,
            IServiceScopeFactory serviceScopeFactory)
        {
            this.mqSender = mqSender;
            this.log = log;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public void Receive(PayOrderReissueMQ.MsgPayload payload)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var payOrderService = scope.ServiceProvider.GetService<IPayOrderService>();
                var channelOrderReissueService = scope.ServiceProvider.GetService<ChannelOrderReissueService>();
                try
                {
                    log.LogInformation($"接收轮询查单通知MQ, msg={JsonConvert.SerializeObject(payload)}");
                    string payOrderId = payload.PayOrderId;
                    int currentCount = payload.Count;
                    currentCount++;

                    PayOrderDto payOrder = payOrderService.GetById(payOrderId);
                    if (payOrder == null)
                    {
                        log.LogWarning($"查询支付订单为空,payOrderId={payOrderId}");
                        return;
                    }

                    if (payOrder.State != (byte)PayOrderState.STATE_ING)
                    {
                        log.LogWarning($"订单状态不是支付中,不需查询渠道.payOrderId={payOrderId}");
                        return;
                    }

                    ChannelRetMsg channelRetMsg = channelOrderReissueService.ProcessPayOrder(payOrder);

                    //返回null 可能为接口报错等， 需要再次轮询
                    if (channelRetMsg == null || channelRetMsg.ChannelState == null || channelRetMsg.ChannelState.Equals(ChannelState.WAITING))
                    {
                        //最多查询6次
                        if (currentCount <= 6)
                        {
                            mqSender.Send(PayOrderReissueMQ.Build(payOrderId, currentCount), 5); //延迟5s再次查询
                        }
                        else
                        {
                            //TODO 调用【撤销订单】接口
                        }
                    }
                    else
                    {
                        //其他状态， 不需要再次轮询。
                    }
                }
                catch (Exception e)
                {
                    log.LogError(e, e.Message);
                    return;
                }
            }
        }
    }
}
