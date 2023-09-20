﻿using AGooday.AgPay.Payment.Api.Channel.SxfPay.PayWay;

namespace AGooday.AgPay.Payment.Api.Channel.SxfPay.Extensions
{
    public class SxfPayNativeInjectorBootStrapper
    {
        public static void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<IPaymentService, AliBar>();
            services.AddScoped<IPaymentService, AliQr>();
            services.AddScoped<IPaymentService, AliJsapi>();

            services.AddScoped<IPaymentService, WxBar>();
            services.AddScoped<IPaymentService, WxNative>();
            services.AddScoped<IPaymentService, WxJsapi>();

            services.AddScoped<IPaymentService, YsfBar>();
            services.AddScoped<IPaymentService, YsfJsapi>();
        }
    }
}
