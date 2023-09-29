﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Exceptions;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Payment.Api.Exceptions;
using AGooday.AgPay.Payment.Api.Models;
using AGooday.AgPay.Payment.Api.RQRS.Msg;
using AGooday.AgPay.Payment.Api.Services;
using Aop.Api.Domain;
using Aop.Api.Request;
using Aop.Api.Response;
using Newtonsoft.Json;

namespace AGooday.AgPay.Payment.Api.Channel.AliPay
{
    /// <summary>
    /// 分账接口： 支付宝官方
    /// </summary>
    public class AliPayDivisionService : IDivisionService
    {
        private readonly ILogger<AliPayDivisionService> log;
        private readonly ConfigContextQueryService configContextQueryService;

        public AliPayDivisionService(ILogger<AliPayDivisionService> logger, ConfigContextQueryService configContextQueryService)
        {
            this.log = logger;
            this.configContextQueryService = configContextQueryService;
        }

        public string GetIfCode()
        {
            return CS.IF_CODE.ALIPAY;
        }

        public bool IsSupport()
        {
            return false;
        }

        public ChannelRetMsg Bind(MchDivisionReceiverDto mchDivisionReceiver, MchAppConfigContext mchAppConfigContext)
        {
            try
            {
                AlipayTradeRoyaltyRelationBindRequest request = new AlipayTradeRoyaltyRelationBindRequest();
                AlipayTradeRoyaltyRelationBindModel model = new AlipayTradeRoyaltyRelationBindModel();
                request.SetBizModel(model);
                model.OutRequestNo = SeqUtil.GenDivisionBatchId();

                //统一放置 isv接口必传信息
                AliPayKit.PutApiIsvInfo(mchAppConfigContext, request, model);

                RoyaltyEntity royaltyEntity = new RoyaltyEntity();

                royaltyEntity.Type = "loginName";
                if (RegUtil.IsAliPayUserId(mchDivisionReceiver.AccNo))
                {
                    royaltyEntity.Type = "userId";
                }
                royaltyEntity.Account = mchDivisionReceiver.AccNo;
                royaltyEntity.Name = mchDivisionReceiver.AccName;
                royaltyEntity.Memo = mchDivisionReceiver.RelationTypeName; //分账关系描述
                model.ReceiverList = new List<RoyaltyEntity>() { royaltyEntity };

                AlipayTradeRoyaltyRelationBindResponse alipayResp = configContextQueryService.GetAlipayClientWrapper(mchAppConfigContext).Execute(request);

                if (!alipayResp.IsError)
                {
                    return ChannelRetMsg.ConfirmSuccess(null);
                }

                //异常：
                ChannelRetMsg channelRetMsg = ChannelRetMsg.ConfirmFail();
                channelRetMsg.ChannelErrCode = AliPayKit.AppendErrCode(alipayResp.Code, alipayResp.SubCode);
                channelRetMsg.ChannelErrMsg = AliPayKit.AppendErrMsg(alipayResp.Msg, alipayResp.SubMsg);
                return channelRetMsg;

            }
            catch (ChannelException e)
            {
                ChannelRetMsg channelRetMsg = ChannelRetMsg.ConfirmFail();
                channelRetMsg.ChannelErrCode = e.ChannelRetMsg.ChannelErrCode;
                channelRetMsg.ChannelErrMsg = e.ChannelRetMsg.ChannelErrMsg;
                return channelRetMsg;
            }
            catch (Exception e)
            {
                log.LogError(e, "绑定支付宝账号异常");
                ChannelRetMsg channelRetMsg = ChannelRetMsg.ConfirmFail();
                channelRetMsg.ChannelErrMsg = e.Message;
                return channelRetMsg;
            }
        }

        public ChannelRetMsg SingleDivision(PayOrderDto payOrder, List<PayOrderDivisionRecordDto> recordList, MchAppConfigContext mchAppConfigContext)
        {
            try
            {
                if (recordList.Count == 0)
                {
                    // 当无分账用户时，支付宝不允许发起分账请求，支付宝没有完结接口，直接响应成功即可。
                    return ChannelRetMsg.ConfirmSuccess(null);
                }

                var request = new AlipayTradeOrderSettleRequest();
                var model = new AlipayTradeOrderSettleModel();
                request.SetBizModel(model);

                model.OutRequestNo = recordList[0].BatchOrderId; // 结算请求流水号，由商家自定义。32个字符以内，仅可包含字母、数字、下划线。需保证在商户端不重复。
                model.TradeNo = recordList[0].PayOrderChannelOrderNo; // 支付宝订单号

                // 统一放置 isv 接口必传信息
                AliPayKit.PutApiIsvInfo(mchAppConfigContext, request, model);

                var reqReceiverList = new List<OpenApiRoyaltyDetailInfoPojo>();

                foreach (var record in recordList)
                {
                    if (record.CalDivisionAmount <= 0)
                    {
                        // 金额为 0 不参与分账处理
                        continue;
                    }

                    var reqReceiver = new OpenApiRoyaltyDetailInfoPojo();
                    reqReceiver.RoyaltyType = "transfer"; // 分账类型：普通分账

                    // 出款信息
                    // reqReceiver.TransOutType = "loginName";
                    // reqReceiver.TransOut = "xqxemt4735@sandbox.com";

                    // 入款信息
                    reqReceiver.TransIn = record.AccNo; // 收入方账号
                    reqReceiver.TransInType = "loginName";
                    if (RegUtil.IsAliPayUserId(record.AccNo))
                    {
                        reqReceiver.TransInType = "userId";
                    }
                    // 分账金额
                    reqReceiver.Amount = AmountUtil.ConvertCent2Dollar(record.CalDivisionAmount);
                    reqReceiver.Desc = "[" + payOrder.PayOrderId + "]订单分账";
                    reqReceiverList.Add(reqReceiver);
                }

                if (reqReceiverList.Count == 0)
                {
                    // 当无分账用户时，支付宝不允许发起分账请求，支付宝没有完结接口，直接响应成功即可。
                    return ChannelRetMsg.ConfirmSuccess(null);
                }

                model.RoyaltyParameters = reqReceiverList; // 分账明细信息

                // 完结
                var settleExtendParams = new SettleExtendParams();
                settleExtendParams.RoyaltyFinish = "true";
                model.ExtendParams = settleExtendParams;

                // 调起支付宝分账接口
                if (log.IsEnabled(LogLevel.Information))
                {
                    log.LogInformation($"订单：[{payOrder.PayOrderId}], 支付宝分账请求：{JsonConvert.SerializeObject(model)}");
                }
                var alipayResp = configContextQueryService.GetAlipayClientWrapper(mchAppConfigContext).Execute(request);
                log.LogInformation($"订单：[{payOrder.PayOrderId}], 支付宝分账响应：{alipayResp.Body}");
                if (!alipayResp.IsError)
                {
                    return ChannelRetMsg.ConfirmSuccess(alipayResp.TradeNo);
                }

                // 异常
                var channelRetMsg = ChannelRetMsg.ConfirmFail();
                channelRetMsg.ChannelErrCode = AliPayKit.AppendErrCode(alipayResp.Code, alipayResp.SubCode);
                channelRetMsg.ChannelErrMsg = AliPayKit.AppendErrMsg(alipayResp.Msg, alipayResp.SubMsg);
                return channelRetMsg;
            }
            catch (ChannelException e)
            {
                var channelRetMsg = ChannelRetMsg.ConfirmFail();
                channelRetMsg.ChannelErrCode = e.ChannelRetMsg.ChannelErrCode;
                channelRetMsg.ChannelErrMsg = e.ChannelRetMsg.ChannelErrMsg;
                return channelRetMsg;
            }
            catch (Exception e)
            {
                log.LogError(e, "绑定支付宝账号异常");
                var channelRetMsg = ChannelRetMsg.ConfirmFail();
                channelRetMsg.ChannelErrMsg = e.Message;
                return channelRetMsg;
            }
        }

        public Dictionary<long, ChannelRetMsg> QueryDivision(PayOrderDto payOrder, List<PayOrderDivisionRecordDto> recordList, MchAppConfigContext mchAppConfigContext)
        {    
            // 创建返回结果
            Dictionary<long, ChannelRetMsg> resultMap = new Dictionary<long, ChannelRetMsg>();

            // 同批次分账记录结果集
            Dictionary<string, RoyaltyDetail> aliAcMap = new Dictionary<string, RoyaltyDetail>();
            try
            {
                // 当无分账用户时，支付宝不允许发起分账请求，支付宝没有完结接口，直接响应成功即可。
                if (recordList.Count == 0)
                {
                    throw new BizException("payOrderId:" + payOrder.PayOrderId + "分账记录为空。recordList：" + recordList);
                }

                var request = new AlipayTradeOrderSettleQueryRequest();
                var model = new AlipayTradeOrderSettleQueryModel();
                request.SetBizModel(model);

                // 统一放置 isv 接口必传信息
                AliPayKit.PutApiIsvInfo(mchAppConfigContext, request, model);

                // 支付宝分账请求单号
                model.SettleNo = recordList[0].BatchOrderId;
                // 结算请求流水号，由商家自定义。32个字符以内，仅可包含字母、数字、下划线。需保证在商户端不重复。
                model.OutRequestNo = payOrder.PayOrderId;
                // 支付宝订单号
                model.TradeNo = payOrder.ChannelOrderNo;

                // 调起支付宝分账接口
                if (log.IsEnabled(LogLevel.Information))
                {
                    log.LogInformation($"订单：[{recordList[0].BatchOrderId}], 支付宝查询分账请求：{JsonConvert.SerializeObject(model)}");
                }
                var alipayResp = configContextQueryService.GetAlipayClientWrapper(mchAppConfigContext).Execute(request);
                log.LogInformation($"订单：[{payOrder.PayOrderId}], 支付宝查询分账响应：{alipayResp.Body}");
                if (!alipayResp.IsError)
                {
                    var detailList = alipayResp.RoyaltyDetailList;
                    if (detailList != null && detailList.Count > 0)
                    {
                        // 遍历匹配与当前账户相同的分账单
                        foreach (var item in detailList)
                        {
                            // 分账操作类型为转账类型
                            if ("transfer".Equals(item.OperationType))
                            {
                                aliAcMap[item.TransIn] = item;
                            }
                        }
                    }
                }
                else
                {
                    log.LogError($"支付宝分账查询响应异常, alipayResp:{0}", JsonConvert.SerializeObject(alipayResp));
                    throw new BizException("支付宝分账查询响应异常：" + alipayResp.SubMsg);
                }

                // 返回结果
                foreach (var record in recordList)
                {
                    // 对应入账账号匹配
                    if (aliAcMap.ContainsKey(record.AccNo))
                    {
                        var detail = aliAcMap[record.AccNo];
                        var channelRetMsg = new ChannelRetMsg();
                        // 错误码
                        channelRetMsg.ChannelErrCode = detail.ErrorCode;
                        // 错误信息
                        channelRetMsg.ChannelErrMsg = detail.ErrorDesc;

                        // 仅返回分账记录为最终态的结果 处理中的分账单不做返回处理
                        if ("SUCCESS".Equals(detail.State))
                        {
                            channelRetMsg.ChannelState = ChannelState.CONFIRM_SUCCESS;

                            resultMap[record.RecordId] = channelRetMsg;
                        }
                        else if ("FAIL".Equals(detail.State))
                        {
                            channelRetMsg.ChannelState = ChannelState.CONFIRM_FAIL;

                            resultMap[record.RecordId] = channelRetMsg;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e, "查询分账信息异常");
                throw new BizException(e.Message);
            }

            return resultMap;
        }
    }
}
