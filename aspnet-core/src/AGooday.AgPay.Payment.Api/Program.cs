using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Application.Services;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Payment.Api.Channel;
using AGooday.AgPay.Payment.Api.Channel.AliPay;
using AGooday.AgPay.Payment.Api.Channel.WxPay;
using AGooday.AgPay.Payment.Api.Extensions;
using AGooday.AgPay.Payment.Api.FilterAttributes;
using AGooday.AgPay.Payment.Api.Models;
using MediatR;
using AGooday.AgPay.Payment.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using AGooday.AgPay.Payment.Api.Channel.AliPay.PayWay;
using AGooday.AgPay.Payment.Api.Channel.WxPay.PayWay;
using AGooday.AgPay.Payment.Api.Channel.YsfPay;
using AGooday.AgPay.Payment.Api.Channel.XxPay;
using AGooday.AgPay.Payment.Api.Utils;
using AGooday.AgPay.Payment.Api.Channel.YsfPay.PayWay;
using AliBar = AGooday.AgPay.Payment.Api.Channel.AliPay.PayWay.AliBar;
using YsfAliBar = AGooday.AgPay.Payment.Api.Channel.YsfPay.PayWay.AliBar;

var builder = WebApplication.CreateBuilder(args);

var logging = builder.Logging;
// ���� ClearProviders �Դ���������ɾ������ ILoggerProvider ʵ��
logging.ClearProviders();
//// ͨ������־����Ӧ��������ָ�����������ڴ�����ָ����
//logging.AddFilter("Microsoft", LogLevel.Warning);
// ��ӿ���̨��־��¼�ṩ����
logging.AddConsole();

// Add services to the container.
var services = builder.Services;
var Env = builder.Environment;

services.AddMemoryCache();

// Automapper ע��
services.AddAutoMapperSetup();
services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        //https://blog.poychang.net/using-newtonsoft-json-in-asp-net-core-projects/
        //options.SerializerSettings.Formatting = Formatting.Indented;
        //options.SerializerSettings.ContractResolver = new DefaultContractResolver();
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();//Json key ���ַ�Сд�����շ�תС�շ壩
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

// Adding MediatR for Domain Events
// ������������¼���ע��
// ���ð� MediatR.Extensions.Microsoft.DependencyInjection
//services.AddMediatR(typeof(MyxxxHandler));//����ע��ĳһ���������
//��
services.AddMediatR(typeof(Program));//Ŀ����Ϊ��ɨ��Handler��ʵ�ֶ�����ӵ�IOC��������

services.Configure<ApiBehaviorOptions>(options =>
{
    // ����Ĭ��ģ����֤������
    options.SuppressModelStateInvalidFilter = true;
});
services.Configure<MvcOptions>(options =>
{
    // ȫ������Զ���ģ����֤������
    options.Filters.Add<ValidateModelAttribute>();
});

services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
services.AddSingleton<RequestIpUtil>();
// .NET Core ԭ������ע��
// ��дһ����������������չʾ�� Presentation �и���
NativeInjectorBootStrapper.RegisterServices(services);

//var provider = services.BuildServiceProvider();
//var mchAppService = (IMchAppService)provider.GetService(typeof(IMchAppService));
//var mchInfoService = (IMchInfoService)provider.GetService(typeof(IMchInfoService));
//var isvInfoService = (IIsvInfoService)provider.GetService(typeof(IIsvInfoService));
//var payInterfaceConfigService = (IPayInterfaceConfigService)provider.GetService(typeof(IPayInterfaceConfigService));
//services.AddSingleton(new ConfigContextService(mchAppService, mchInfoService, isvInfoService, payInterfaceConfigService));
//services.AddSingleton(typeof(ConfigContextQueryService));
//services.AddSingleton(typeof(ConfigContextService));
services.AddSingleton<ConfigContextQueryService>();
services.AddSingleton<ConfigContextService>();
//services.AddSingleton<IDivisionService, AliPayDivisionService>();
//services.AddSingleton<IDivisionService, WxPayDivisionService>();
services.AddSingleton<AliPayDivisionService>();
services.AddSingleton<WxPayDivisionService>();
services.AddSingleton<PayMchNotifyService>();
services.AddSingleton<PayOrderProcessService>();
services.AddSingleton(provider =>
{
    Func<string, IDivisionService> funcFactory = ifCode =>
    {
        switch (ifCode)
        {
            case CS.IF_CODE.ALIPAY:
                return provider.GetService<AliPayDivisionService>();
            case CS.IF_CODE.WXPAY:
                return provider.GetService<WxPayDivisionService>();
            default:
                return null;
        }
    };
    return funcFactory;
});

services.AddSingleton<AliPayPaymentService>();
services.AddSingleton<WxPayPaymentService>();
services.AddSingleton(provider =>
{
    Func<string, IPaymentService> funcFactory = ifCode =>
    {
        switch (ifCode)
        {
            case CS.IF_CODE.ALIPAY:
                return provider.GetService<AliPayPaymentService>();
            case CS.IF_CODE.WXPAY:
                return provider.GetService<WxPayPaymentService>();
            case CS.IF_CODE.YSFPAY:
                return provider.GetService<YsfPayPaymentService>();
            default:
                return null;
        }
    };
    return funcFactory;
});

services.AddSingleton<IPaymentService, AliApp>();
services.AddSingleton<IPaymentService, AliBar>();
services.AddSingleton<IPaymentService, WxApp>();
services.AddSingleton<IPaymentService, WxBar>();

services.AddSingleton<AliPayChannelUserService>();
services.AddSingleton<WxPayChannelUserService>();
services.AddSingleton(provider =>
{
    Func<string, IChannelUserService> funcFactory = ifCode =>
    {
        switch (ifCode)
        {
            case CS.IF_CODE.ALIPAY:
                return provider.GetService<AliPayChannelUserService>();
            case CS.IF_CODE.WXPAY:
                return provider.GetService<WxPayChannelUserService>();
            default:
                return null;
        }
    };
    return funcFactory;
});

services.AddSingleton<IQRCodeService, QRCodeService>();

var serviceProvider = services.BuildServiceProvider();
PayWayUtil.ServiceProvider = serviceProvider;
AliPayKit.ServiceProvider = serviceProvider;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
