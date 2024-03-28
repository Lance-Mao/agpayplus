﻿using AGooday.AgPay.Application;
using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Enumerator;
using AGooday.AgPay.Common.Exceptions;
using AGooday.AgPay.Common.Models;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Components.MQ.Models;
using AGooday.AgPay.Components.MQ.Vender;
using AGooday.AgPay.Payment.Api.Channel;
using AGooday.AgPay.Payment.Api.Exceptions;
using AGooday.AgPay.Payment.Api.Models;
using AGooday.AgPay.Payment.Api.RQRS.Msg;
using AGooday.AgPay.Payment.Api.RQRS.PayOrder;
using AGooday.AgPay.Payment.Api.RQRS.PayOrder.PayWay;
using AGooday.AgPay.Payment.Api.Services;
using AGooday.AgPay.Payment.Api.Utils;

namespace AGooday.AgPay.Payment.Api.Controllers.PayOrder
{
    /// <summary>
    /// 创建支付订单抽象类
    /// </summary>
    public abstract class AbstractPayOrderController : ApiControllerBase
    {
        protected readonly IMQSender mqSender;
        protected readonly Func<string, IPaymentService> _paymentServiceFactory;
        protected readonly PayOrderProcessService _payOrderProcessService;
        protected readonly ILogger<AbstractPayOrderController> _logger;
        protected readonly IMchPayPassageService _mchPayPassageService;
        protected readonly IPayRateConfigService _payRateConfigService;
        protected readonly IPayWayService _payWayService;
        protected readonly IPayOrderService _payOrderService;
        protected readonly IPayOrderProfitService _payOrderProfitService;
        protected readonly ISysConfigService _sysConfigService;

        protected AbstractPayOrderController(IMQSender mqSender,
            Func<string, IPaymentService> paymentServiceFactory,
            ConfigContextQueryService configContextQueryService,
            PayOrderProcessService payOrderProcessService,
            RequestKit requestKit,
            ILogger<AbstractPayOrderController> logger,
            IMchPayPassageService mchPayPassageService,
            IPayRateConfigService payRateConfigService,
            IPayWayService payWayService,
            IPayOrderService payOrderService,
            IPayOrderProfitService payOrderProfitService,
            ISysConfigService sysConfigService)
            : base(requestKit, configContextQueryService)
        {
            _paymentServiceFactory = paymentServiceFactory;
            _payOrderProcessService = payOrderProcessService;
            _logger = logger;
            _mchPayPassageService = mchPayPassageService;
            _payRateConfigService = payRateConfigService;
            _payWayService = payWayService;
            _payOrderService = payOrderService;
            _sysConfigService = sysConfigService;
            this.mqSender = mqSender;
            _payOrderProfitService = payOrderProfitService;
        }

        /// <summary>
        /// 统一下单 (新建订单模式)
        /// </summary>
        /// <param name="wayCode"></param>
        /// <param name="bizRQ">业务请求报文</param>
        /// <returns></returns>
        protected ApiRes UnifiedOrder(string wayCode, UnifiedOrderRQ bizRQ)
        {
            return UnifiedOrder(wayCode, bizRQ, null);
        }

        /// <summary>
        /// 统一下单
        /// </summary>
        /// <param name="wayCode"></param>
        /// <param name="bizRQ"></param>
        /// <param name="payOrder"></param>
        /// <returns></returns>
        protected ApiRes UnifiedOrder(string wayCode, UnifiedOrderRQ bizRQ, PayOrderDto payOrder)
        {
            // 响应数据
            UnifiedOrderRS bizRS = null;

            //是否新订单模式 [  一般接口都为新订单模式，  由于QR_CASHIER支付方式，需要先 在DB插入一个新订单， 导致此处需要特殊判断下。 如果已存在则直接更新，否则为插入。  ]
            bool isNewOrder = payOrder == null;

            try
            {
                //当订单存在时，封装公共参数。
                if (payOrder != null)
                {
                    if (payOrder.State != (sbyte)PayOrderState.STATE_INIT)
                    {
                        throw new BizException("订单状态异常");
                    }

                    var wayType = _payWayService.GetWayTypeByWayCode(wayCode);
                    payOrder.WayCode = wayCode; // 需要将订单更新 支付方式
                    payOrder.WayType = wayType; // 需要将订单更新 支付类型
                    payOrder.ChannelUser = bizRQ.GetChannelUserId(); //更新渠道用户信息
                    bizRQ.MchNo = payOrder.MchNo;
                    bizRQ.AppId = payOrder.AppId;
                    bizRQ.StoreId = payOrder.StoreId;
                    bizRQ.QrcId = payOrder.QrcId;
                    bizRQ.MchOrderNo = payOrder.MchOrderNo;
                    bizRQ.WayCode = wayCode;
                    bizRQ.Amount = payOrder.Amount;
                    bizRQ.Currency = payOrder.Currency;
                    bizRQ.ClientIp = payOrder.ClientIp;
                    bizRQ.Subject = payOrder.Subject;
                    bizRQ.NotifyUrl = payOrder.NotifyUrl;
                    bizRQ.ReturnUrl = payOrder.ReturnUrl;
                    bizRQ.ChannelExtra = payOrder.ChannelExtra;
                    bizRQ.ExtParam = payOrder.ExtParam;
                    bizRQ.DivisionMode = payOrder.DivisionMode;
                }

                string mchNo = bizRQ.MchNo;
                string appId = bizRQ.AppId;

                // 只有新订单模式，进行校验
                if (isNewOrder && _payOrderService.IsExistOrderByMchOrderNo(mchNo, bizRQ.MchOrderNo))
                {
                    throw new BizException($"商户订单[{bizRQ.MchOrderNo}]已存在");
                }

                if (!string.IsNullOrWhiteSpace(bizRQ.NotifyUrl) && !StringUtil.IsAvailableUrl(bizRQ.NotifyUrl))
                {
                    throw new BizException("异步通知地址协议仅支持http:// 或 https:// !");
                }
                if (!string.IsNullOrWhiteSpace(bizRQ.ReturnUrl) && !StringUtil.IsAvailableUrl(bizRQ.ReturnUrl))
                {
                    throw new BizException("同步通知地址协议仅支持http:// 或 https:// !");
                }

                //获取支付参数 (缓存数据) 和 商户信息
                MchAppConfigContext mchAppConfigContext = _configContextQueryService.QueryMchInfoAndAppInfo(mchNo, appId);
                if (mchAppConfigContext == null)
                {
                    throw new BizException("获取商户应用信息失败");
                }

                //MchInfoDto mchInfo = mchAppConfigContext.MchInfo;
                //MchAppDto mchApp = mchAppConfigContext.MchApp;

                //收银台支付并且只有新订单需要走这里，  收银台二次下单的wayCode应该为实际支付方式。
                if (isNewOrder && CS.PAY_WAY_CODE.QR_CASHIER.Equals(wayCode))
                {
                    //生成订单
                    payOrder = GenPayOrder(bizRQ, mchAppConfigContext, null, null, null);
                    string payOrderId = payOrder.PayOrderId;
                    //订单入库 订单状态： 生成状态  此时没有和任何上游渠道产生交互。
                    _payOrderService.Add(payOrder);

                    QrCashierOrderRS qrCashierOrderRS = new QrCashierOrderRS();
                    QrCashierOrderRQ qrCashierOrderRQ = (QrCashierOrderRQ)bizRQ;

                    DBApplicationConfig dbApplicationConfig = _sysConfigService.GetDBApplicationConfig();

                    string payUrl = dbApplicationConfig.GenUniJsapiPayUrl(CS.GetTokenData(CS.TOKEN_DATA_TYPE.PAY_ORDER_ID, payOrderId));
                    if (CS.PAY_DATA_TYPE.CODE_IMG_URL.Equals(qrCashierOrderRQ.PayDataType))
                    {
                        //二维码地址
                        qrCashierOrderRS.CodeImgUrl = dbApplicationConfig.GenScanImgUrl(payUrl);
                    }
                    else
                    {
                        //默认都为跳转地址方式
                        qrCashierOrderRS.PayUrl = payUrl;
                    }

                    return PackageApiResByPayOrder(bizRQ, qrCashierOrderRS, payOrder);
                }

                // 根据支付方式， 查询出 该商户 可用的支付接口
                var mchPayPassage = _mchPayPassageService.FindMchPayPassage(mchAppConfigContext.MchNo, mchAppConfigContext.AppId, wayCode, bizRQ.Amount);
                if (mchPayPassage == null)
                {
                    throw new BizException("商户应用不支持该支付方式");
                }

                //获取支付接口
                IPaymentService paymentService = CheckMchWayCodeAndGetService(mchAppConfigContext, mchPayPassage);
                string ifCode = paymentService.GetIfCode();

                //生成订单
                if (isNewOrder)
                {
                    payOrder = GenPayOrder(bizRQ, mchAppConfigContext, ifCode, mchPayPassage, paymentService);
                }
                else
                {
                    payOrder.IfCode = ifCode;

                    // 查询支付方式的费率，并 在更新ing时更新费率信息
                    payOrder.MchFeeRate = mchPayPassage.Rate;
                    payOrder.MchFeeRateDesc = mchPayPassage.RateDesc;
                    //payOrder.MchFeeAmount = AmountUtil.CalPercentageFee(payOrder.Amount, payOrder.MchFeeRate); //商户手续费,单位分
                    payOrder.MchFeeAmount = paymentService.CalculateFeeAmount(payOrder.Amount, payOrder.MchFeeRate);
                    payOrder.MchOrderFeeAmount = payOrder.MchFeeAmount;
                }

                //预先校验
                string errMsg = paymentService.PreCheck(bizRQ, payOrder);
                if (!string.IsNullOrWhiteSpace(errMsg))
                {
                    throw new BizException(errMsg);
                }

                if (isNewOrder)
                {
                    //订单入库 订单状态： 生成状态  此时没有和任何上游渠道产生交互。
                    _payOrderService.Add(payOrder);
                }

                // 生成订单分润
                GenPayOrderProfit(payOrder, paymentService);

                //调起上游支付接口
                bizRS = (UnifiedOrderRS)paymentService.Pay(bizRQ, payOrder, mchAppConfigContext);

                //处理上游返回数据
                this.ProcessChannelMsg(bizRS.ChannelRetMsg, payOrder);

                return PackageApiResByPayOrder(bizRQ, bizRS, payOrder);
            }
            catch (BizException e)
            {
                return ApiRes.CustomFail(e.Message);
            }
            //处理上游返回数据
            catch (ChannelException e)
            {
                this.ProcessChannelMsg(e.ChannelRetMsg, payOrder);

                if (e.ChannelRetMsg.ChannelState == ChannelState.SYS_ERROR)
                {
                    return ApiRes.CustomFail(e.Message);
                }

                return this.PackageApiResByPayOrder(bizRQ, bizRS, payOrder);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"系统异常：{e.Message}");
                return ApiRes.CustomFail("系统异常");
            }
        }

        private PayOrderDto GenPayOrder(UnifiedOrderRQ rq, MchAppConfigContext configContext, string ifCode, MchPayPassageDto mchPayPassage, IPaymentService paymentService)
        {
            return GenPayOrder(rq, configContext.MchInfo, configContext.MchApp, configContext.AgentConfigContext?.AgentInfo, configContext.IsvConfigContext?.IsvInfo, ifCode, mchPayPassage, paymentService);
        }

        private PayOrderDto GenPayOrder(UnifiedOrderRQ rq, MchInfoDto mchInfo, MchAppDto mchApp, AgentInfoDto agentInfo, IsvInfoDto isvInfo, string ifCode, MchPayPassageDto mchPayPassage, IPaymentService paymentService)
        {
            var wayType = _payWayService.GetWayTypeByWayCode(rq.WayCode);
            PayOrderDto payOrder = new PayOrderDto();
            payOrder.PayOrderId = SeqUtil.GenPayOrderId(); //生成订单ID
            payOrder.MchNo = mchInfo.MchNo; //商户号
            payOrder.MchName = mchInfo.MchName; //商户名称
            payOrder.MchShortName = mchInfo.MchShortName; //商户简称
            payOrder.AgentNo = mchInfo.AgentNo; //代理商号
            payOrder.AgentName = agentInfo?.AgentName; //代理商名称
            payOrder.AgentShortName = agentInfo?.AgentShortName; //代理商简称
            payOrder.IsvNo = mchInfo.IsvNo; //服务商号
            payOrder.IsvName = isvInfo?.IsvName; //服务商名称
            payOrder.IsvShortName = isvInfo?.IsvShortName; //服务商简称
            payOrder.MchType = mchInfo.Type; //商户类型
            payOrder.MchOrderNo = rq.MchOrderNo; //商户订单号
            payOrder.AppId = mchApp.AppId; //商户应用appId
            payOrder.AppName = mchApp.AppName; //商户应用名称
            payOrder.StoreId = rq.StoreId; //商户门店ID
            payOrder.QrcId = rq.QrcId; //商户码牌ID
            payOrder.IfCode = ifCode; //接口代码
            payOrder.WayCode = rq.WayCode; //支付方式
            payOrder.WayType = wayType; //支付类型
            payOrder.Amount = rq.Amount; //订单金额

            if (mchPayPassage != null)
            {
                payOrder.MchFeeRate = mchPayPassage.Rate; //商户手续费费率快照
                payOrder.MchFeeRateDesc = mchPayPassage.RateDesc;
            }
            else
            {
                payOrder.MchFeeRate = Decimal.Zero; //预下单模式， 按照0计算入库， 后续进行更新
            }

            //payOrder.MchFeeAmount = AmountUtil.CalPercentageFee(payOrder.Amount, payOrder.MchFeeRate); //商户手续费,单位分
            payOrder.MchFeeAmount = paymentService?.CalculateFeeAmount(payOrder.Amount, payOrder.MchFeeRate) ?? 0;
            payOrder.MchOrderFeeAmount = payOrder.MchFeeAmount;

            payOrder.Currency = rq.Currency; //币种
            payOrder.State = (byte)PayOrderState.STATE_INIT; //订单状态, 默认订单生成状态
            payOrder.ClientIp = string.IsNullOrWhiteSpace(rq.ClientIp) ? GetClientIp() : rq.ClientIp; //客户端IP
            payOrder.Subject = rq.Subject; //商品标题
            payOrder.Body = rq.Body; //商品描述信息
            //payOrder.ChannelExtra = rq.ChannelExtra; //特殊渠道发起的附件额外参数,  是否应该删除该字段了？？ 比如authCode不应该记录， 只是在传输阶段存在的吧？  之前的为了在payOrder对象需要传参。
            payOrder.ChannelUser = rq.GetChannelUserId(); //渠道用户标志
            payOrder.ExtParam = rq.ExtParam; //商户扩展参数
            payOrder.NotifyUrl = rq.NotifyUrl; //异步通知地址
            payOrder.ReturnUrl = rq.ReturnUrl; //页面跳转地址

            // 分账模式
            payOrder.DivisionMode ??= (byte)PayOrderDivisionMode.DIVISION_MODE_FORBID;
            payOrder.DivisionState ??= (byte)PayOrderDivisionState.DIVISION_STATE_UNHAPPEN;

            var nowDate = DateTime.Now;

            //订单过期时间 单位： 秒
            if (rq.ExpiredTime.HasValue)
            {
                payOrder.ExpiredTime = nowDate.AddSeconds(rq.ExpiredTime.Value);
            }
            else
            {
                payOrder.ExpiredTime = nowDate.AddHours(2); //订单过期时间 默认两个小时
            }

            payOrder.CreatedAt = nowDate; //订单创建时间
            return payOrder;
        }

        private void GenPayOrderProfit(PayOrderDto payOrder, IPaymentService paymentService)
        {
            var payRateConfigs = _payRateConfigService.GetPayRateConfigInfos(payOrder.MchNo, payOrder.IfCode, payOrder.WayCode, payOrder.Amount);

            var payOrderProfit = new PayOrderProfitDto();
            var agentPayRateConfigs = payRateConfigs.Where(w => w.InfoType.Equals(CS.INFO_TYPE.AGENT)).OrderByDescending(o => o.AgentLevel);
            var preFeeRate = payOrder.MchFeeRate;
            var totalProfitAmount = 0L;
            var totalProfitRate = 0M;
            foreach (var agentPayRateConfig in agentPayRateConfigs)
            {
                var feeRate = agentPayRateConfig.FeeRate.Value;
                var feeAmount = paymentService?.CalculateFeeAmount(payOrder.Amount, feeRate) ?? 0;
                var profitRate = preFeeRate - feeRate;
                var profitAmount = paymentService?.CalculateProfitAmount(payOrder.Amount, profitRate) ?? 0;
                payOrderProfit = new PayOrderProfitDto();
                payOrderProfit.InfoId = agentPayRateConfig.InfoId;
                payOrderProfit.InfoName = agentPayRateConfig.InfoName;
                payOrderProfit.InfoType = agentPayRateConfig.InfoType;
                payOrderProfit.PayOrderId = payOrder.PayOrderId;
                payOrderProfit.FeeRate = feeRate;
                payOrderProfit.FeeRateDesc = agentPayRateConfig.FeeRateDesc;
                payOrderProfit.ProfitRate = profitRate;
                payOrderProfit.FeeAmount = feeAmount;
                payOrderProfit.OrderFeeAmount = feeAmount;
                payOrderProfit.ProfitAmount = profitAmount;
                payOrderProfit.OrderProfitAmount = profitAmount;
                _payOrderProfitService.Add(payOrderProfit);
                preFeeRate = feeRate;
                totalProfitAmount += profitAmount;
                totalProfitRate += profitRate;
            }

            var isvPayRateConfigs = payRateConfigs.FirstOrDefault(w => w.InfoType.Equals(CS.INFO_TYPE.ISV));
            var platformFeeRate = isvPayRateConfigs.FeeRate.Value;
            var platformFeeAmount = paymentService?.CalculateFeeAmount(payOrder.Amount, platformFeeRate) ?? 0;
            var platformProfitRate = payOrder.MchFeeRate - isvPayRateConfigs.FeeRate.Value;
            var platformProfitAmount = payOrder.MchFeeAmount - platformFeeAmount;//paymentService?.CalculateProfitAmount(payOrder.Amount, platformProfitRate) ?? 0;
            payOrderProfit = new PayOrderProfitDto();
            payOrderProfit.InfoId = CS.PAY_ORDER_PROFIT_INFO_ID.PLATFORM_INACCOUNT;
            payOrderProfit.InfoName = "运营平台";
            payOrderProfit.InfoType = CS.PAY_ORDER_PROFIT_INFO_TYPE.PLATFORM;
            payOrderProfit.PayOrderId = payOrder.PayOrderId;
            payOrderProfit.FeeRate = platformFeeRate;
            payOrderProfit.FeeRateDesc = isvPayRateConfigs.FeeRateDesc;
            payOrderProfit.FeeAmount = platformFeeAmount;
            payOrderProfit.OrderFeeAmount = platformFeeAmount;
            payOrderProfit.ProfitRate = platformProfitRate;
            payOrderProfit.ProfitAmount = platformProfitAmount;
            payOrderProfit.OrderProfitAmount = platformProfitAmount;
            _payOrderProfitService.Add(payOrderProfit);

            payOrderProfit = new PayOrderProfitDto();
            payOrderProfit.InfoId = CS.PAY_ORDER_PROFIT_INFO_ID.PLATFORM_PROFIT;
            payOrderProfit.InfoName = "运营平台";
            payOrderProfit.InfoType = CS.PAY_ORDER_PROFIT_INFO_TYPE.PLATFORM;
            payOrderProfit.PayOrderId = payOrder.PayOrderId;
            payOrderProfit.FeeRate = platformFeeRate;
            payOrderProfit.FeeRateDesc = isvPayRateConfigs.FeeRateDesc;
            payOrderProfit.FeeAmount = platformFeeAmount;
            payOrderProfit.OrderFeeAmount = platformFeeAmount;
            payOrderProfit.ProfitRate = platformProfitRate - totalProfitRate;
            payOrderProfit.ProfitAmount = platformProfitAmount - totalProfitAmount;
            payOrderProfit.OrderProfitAmount = platformProfitAmount - totalProfitAmount;
            _payOrderProfitService.Add(payOrderProfit);
        }

        /// <summary>
        /// 校验： 商户的支付方式是否可用
        /// 返回： 支付接口
        /// </summary>
        /// <param name="mchAppConfigContext"></param>
        /// <param name="mchPayPassage"></param>
        /// <returns></returns>
        private IPaymentService CheckMchWayCodeAndGetService(MchAppConfigContext mchAppConfigContext, MchPayPassageDto mchPayPassage)
        {
            // 接口代码
            string ifCode = mchPayPassage.IfCode;
            IPaymentService paymentService = _paymentServiceFactory(ifCode);
            if (paymentService == null)
            {
                throw new BizException("无此支付通道接口");
            }

            if (!paymentService.IsSupport(mchPayPassage.WayCode))
            {
                throw new BizException("接口不支持该支付方式");
            }

            if (mchAppConfigContext.MchType == (byte)MchInfoType.TYPE_NORMAL)//普通商户
            {

                if (_configContextQueryService.QueryNormalMchParams(mchAppConfigContext.MchNo, mchAppConfigContext.AppId, ifCode) == null)
                {
                    throw new BizException("商户应用参数未配置");
                }
            }
            else if (mchAppConfigContext.MchType == (byte)MchInfoType.TYPE_ISVSUB)//特约商户
            {

                if (_configContextQueryService.QueryIsvSubMchParams(mchAppConfigContext.MchNo, mchAppConfigContext.AppId, ifCode) == null)
                {
                    throw new BizException("特约商户参数未配置");
                }

                if (_configContextQueryService.QueryIsvParams(mchAppConfigContext.MchInfo.IsvNo, ifCode) == null)
                {
                    throw new BizException("服务商参数未配置");
                }
            }

            return paymentService;
        }

        /// <summary>
        /// 处理返回的渠道信息，并更新订单状态
        /// payOrder将对部分信息进行 赋值操作。
        /// </summary>
        /// <param name="channelRetMsg"></param>
        /// <param name="payOrder"></param>
        /// <exception cref="BizException"></exception>
        private void ProcessChannelMsg(ChannelRetMsg channelRetMsg, PayOrderDto payOrder)
        {
            //对象为空 || 上游返回状态为空， 则无需操作
            if (channelRetMsg == null || channelRetMsg.ChannelState == null)
            {
                return;
            }

            string payOrderId = payOrder.PayOrderId;

            //明确成功
            if (ChannelState.CONFIRM_SUCCESS == channelRetMsg.ChannelState)
            {
                this.UpdateInitOrderStateThrowException((byte)PayOrderState.STATE_SUCCESS, payOrder, channelRetMsg);

                //订单支付成功，其他业务逻辑
                _payOrderProcessService.ConfirmSuccess(payOrder);
            }
            //明确失败
            else if (ChannelState.CONFIRM_FAIL == channelRetMsg.ChannelState)
            {
                this.UpdateInitOrderStateThrowException((byte)PayOrderState.STATE_FAIL, payOrder, channelRetMsg);
            }
            // 上游处理中 || 未知 || 上游接口返回异常  订单为支付中状态
            else if (ChannelState.WAITING == channelRetMsg.ChannelState ||
                ChannelState.UNKNOWN == channelRetMsg.ChannelState ||
                ChannelState.API_RET_ERROR == channelRetMsg.ChannelState)
            {
                this.UpdateInitOrderStateThrowException((byte)PayOrderState.STATE_ING, payOrder, channelRetMsg);
            }
            // 系统异常：  订单不再处理。  为： 生成状态
            else if (ChannelState.SYS_ERROR == channelRetMsg.ChannelState)
            {
            }
            else
            {
                throw new BizException("ChannelState 返回异常！");
            }

            //判断是否需要轮询查单
            if (channelRetMsg.IsNeedQuery)
            {
                //推送到MQ
                mqSender.Send(PayOrderReissueMQ.Build(payOrderId, 1), 5);
            }
        }

        /// <summary>
        /// 更新订单状态 --》 订单生成--》 其他状态  (向外抛出异常)
        /// </summary>
        /// <param name="orderState"></param>
        /// <param name="payOrder"></param>
        /// <param name="channelRetMsg"></param>
        /// <exception cref="BizException"></exception>
        private void UpdateInitOrderStateThrowException(byte orderState, PayOrderDto payOrder, ChannelRetMsg channelRetMsg)
        {
            payOrder.State = orderState;
            payOrder.ChannelOrderNo = channelRetMsg.ChannelOrderId;
            payOrder.ErrCode = channelRetMsg.ChannelErrCode;
            payOrder.ErrMsg = channelRetMsg.ChannelErrMsg;

            // 聚合码场景 订单对象存在会员信息， 不可全部以上游为准。
            if (!string.IsNullOrWhiteSpace(channelRetMsg.ChannelUserId))
            {
                payOrder.ChannelUser = channelRetMsg.ChannelUserId;
            }

            bool isSuccess = _payOrderService.UpdateInit2Ing(payOrder.PayOrderId, payOrder);
            if (!isSuccess)
            {
                throw new BizException("更新订单异常!");
            }

            isSuccess = _payOrderService.UpdateIng2SuccessOrFail(payOrder.PayOrderId, payOrder.State,
                    channelRetMsg.ChannelMchNo, channelRetMsg.ChannelIsvNo, channelRetMsg.ChannelOrderId, channelRetMsg.ChannelUserId, channelRetMsg.PlatformOrderId, channelRetMsg.PlatformMchOrderId, channelRetMsg.ChannelErrCode, channelRetMsg.ChannelErrMsg);
            if (!isSuccess)
            {
                throw new BizException("更新订单异常!");
            }
        }

        /// <summary>
        /// 统一封装订单数据
        /// </summary>
        /// <param name="bizRQ"></param>
        /// <param name="bizRS"></param>
        /// <param name="payOrder"></param>
        /// <returns></returns>
        private ApiRes PackageApiResByPayOrder(UnifiedOrderRQ bizRQ, UnifiedOrderRS bizRS, PayOrderDto payOrder)
        {
            // 返回接口数据
            bizRS.PayOrderId = payOrder.PayOrderId;
            bizRS.OrderState = payOrder.State;
            bizRS.MchOrderNo = payOrder.MchOrderNo;

            if (payOrder.State == (byte)PayOrderState.STATE_FAIL)
            {
                bizRS.ErrCode = bizRS.ChannelRetMsg?.ChannelErrCode;
                bizRS.ErrMsg = bizRS.ChannelRetMsg?.ChannelErrMsg;
            }

            return ApiRes.OkWithSign(bizRS, bizRQ.SignType, _configContextQueryService.QueryMchApp(bizRQ.MchNo, bizRQ.AppId).AppSecret);
        }
    }
}
