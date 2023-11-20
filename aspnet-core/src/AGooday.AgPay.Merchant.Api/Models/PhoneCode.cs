﻿namespace AGooday.AgPay.Merchant.Api.Models
{
    public class PhoneCode
    {
        /// <summary>
        /// 手机号
        /// </summary>
        public string phone { get; set; }
        /// <summary>
        /// 验证码
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// 登录类型： APP-app登录， lite-小程序登录
        /// </summary>
        public string lt { get; set; }
    }
}
