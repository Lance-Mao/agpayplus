﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Common.Exceptions;
using AGooday.AgPay.Payment.Api.Models;
using AGooday.AgPay.Payment.Api.RQRS;
using AGooday.AgPay.Payment.Api.RQRS.Msg;
using AGooday.AgPay.Payment.Api.RQRS.PayOrder;
using AGooday.AgPay.Payment.Api.RQRS.PayOrder.PayWay;
using AGooday.AgPay.Payment.Api.Services;
using AGooday.AgPay.Payment.Api.Utils;
using Newtonsoft.Json.Linq;

namespace AGooday.AgPay.Payment.Api.Channel.YsfPay.PayWay
{
    public class AliJsapi : YsfPayPaymentService
    {
        /// <summary>
        /// 云闪付 支付宝 jsapi
        /// </summary>
        /// <param name="serviceProvider"></param>
        public AliJsapi(IServiceProvider serviceProvider,
            ISysConfigService sysConfigService,
            ConfigContextQueryService configContextQueryService)
            : base(serviceProvider, sysConfigService, configContextQueryService)
        {
        }

        public override AbstractRS Pay(UnifiedOrderRQ rq, PayOrderDto payOrder, MchAppConfigContext mchAppConfigContext)
        {
            string logPrefix = "【云闪付(alipayJs)jsapi支付】";
            JObject reqParams = new JObject();
            AliJsapiOrderRS res = ApiResBuilder.BuildSuccess<AliJsapiOrderRS>();
            ChannelRetMsg channelRetMsg = new ChannelRetMsg();
            res.ChannelRetMsg = channelRetMsg;

            // 请求参数赋值
            JsapiParamsSet(reqParams, payOrder, GetNotifyUrl(), GetReturnUrl());

            AliJsapiOrderRQ bizRQ = (AliJsapiOrderRQ)rq;
            //云闪付扫一扫支付， 需要传入buyerUserId参数
            reqParams.Add("userId", bizRQ.BuyerUserId);// buyerUserId

            //客户端IP
            reqParams.Add("customerIp", !string.IsNullOrWhiteSpace(payOrder.ClientIp) ? payOrder.ClientIp : "127.0.0.1");

            // 发送请求
            JObject resJSON = PackageParamAndReq("/gateway/api/pay/unifiedorder", reqParams, logPrefix, mchAppConfigContext);
            //请求 & 响应成功， 判断业务逻辑
            string respCode = resJSON.GetValue("respCode").ToString(); //应答码
            string respMsg = resJSON.GetValue("respMsg").ToString(); //应答信息
            try
            {
                //00-交易成功， 02-用户支付中 , 12-交易重复， 需要发起查询处理    其他认为失败
                if ("00".Equals(respCode))
                {
                    //付款信息
                    JObject payDataJSON = JObject.Parse(resJSON.GetValue("payData").ToString());
                    string tradeNo = "";
                    if (!string.IsNullOrWhiteSpace(payDataJSON.GetValue("tradeNo").ToString()))
                    {
                        tradeNo = payDataJSON.GetValue("tradeNo").ToString();
                    }
                    else
                    {
                        string prepayId = payDataJSON.GetValue("prepayId").ToString();
                        if (prepayId != null && prepayId.Length > 2 && !prepayId.StartsWith($"{DateTime.Now:yyyy}"))
                        {
                            tradeNo = prepayId.Substring(2);
                        }
                        else
                        {
                            tradeNo = prepayId;
                        }
                    }
                    res.AlipayTradeNo = tradeNo;
                    res.PayData = payDataJSON.ToString();
                    channelRetMsg.ChannelState = ChannelState.WAITING;
                }
            }
            catch (Exception)
            {
                channelRetMsg.ChannelErrCode = respCode;
                channelRetMsg.ChannelErrMsg = respMsg;
            }
            return res;
        }

        public override string PreCheck(UnifiedOrderRQ rq, PayOrderDto payOrder)
        {
            AliJsapiOrderRQ bizRQ = (AliJsapiOrderRQ)rq;
            if (string.IsNullOrWhiteSpace(bizRQ.BuyerUserId))
            {
                throw new BizException("[buyerUserId]不可为空");
            }

            return null;
        }
    }
}
