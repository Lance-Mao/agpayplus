﻿using AGooday.AgPay.AopSdk.Models;
using AGooday.AgPay.AopSdk.Nets;
using AGooday.AgPay.AopSdk.Response;

namespace AGooday.AgPay.AopSdk.Request
{
    /// <summary>
    /// 退款请求实现
    /// </summary>
    public class RefundOrderCreateRequest : IAgPayRequest<RefundOrderCreateResponse>
    {
        private string ApiVersion = AgPay.VERSION;
        private readonly string ApiUri = "api/refund/refundOrder";
        private RequestOptions Options;
        private AgPayObject BizModel = null;

        public string GetApiUri()
        {
            return this.ApiUri;
        }

        public string GetApiVersion()
        {
            return this.ApiVersion;
        }

        public void SetApiVersion(string apiVersion)
        {
            this.ApiVersion = apiVersion;
        }

        public RequestOptions GetRequestOptions()
        {
            return this.Options;
        }

        public void SetRequestOptions(RequestOptions options)
        {
            this.Options = options;
        }

        public AgPayObject GetBizModel()
        {
            return this.BizModel;
        }

        public void SetBizModel(AgPayObject bizModel)
        {
            this.BizModel = bizModel;
        }
    }
}
