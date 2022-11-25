using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Extensions;
using AGooday.AgPay.Components.MQ.Vender;
using AGooday.AgPay.Components.MQ.Vender.RabbitMQ;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Payment.Api.Channel;
using AGooday.AgPay.Payment.Api.Channel.AliPay;
using AGooday.AgPay.Payment.Api.Channel.WxPay;
using AGooday.AgPay.Payment.Api.Channel.AliPay.PayWay;
using AGooday.AgPay.Payment.Api.Channel.YsfPay;
using AGooday.AgPay.Payment.Api.Extensions;
using AGooday.AgPay.Payment.Api.FilterAttributes;
using AGooday.AgPay.Payment.Api.Utils;
using AGooday.AgPay.Payment.Api.MQ;
using AGooday.AgPay.Payment.Api.Services;
using AGooday.AgPay.Components.MQ.Vender.RabbitMQ.Receive;
using AGooday.AgPay.Components.MQ.Models;
using AGooday.AgPay.Components.OSS.Config;
using AGooday.AgPay.Components.OSS.Constants;
using AGooday.AgPay.Components.OSS.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

#region PayWay
#region AliPay
using AliBar = AGooday.AgPay.Payment.Api.Channel.AliPay.PayWay.AliBar;
using AliJsapi = AGooday.AgPay.Payment.Api.Channel.AliPay.PayWay.AliJsapi;
#endregion
#region WxPay
using WxApp = AGooday.AgPay.Payment.Api.Channel.WxPay.PayWay.WxApp;
using WxBar = AGooday.AgPay.Payment.Api.Channel.WxPay.PayWay.WxBar;
using WxH5 = AGooday.AgPay.Payment.Api.Channel.WxPay.PayWay.WxH5;
using WxJsapi = AGooday.AgPay.Payment.Api.Channel.WxPay.PayWay.WxJsapi;
using WxLite = AGooday.AgPay.Payment.Api.Channel.WxPay.PayWay.WxLite;
using WxNative = AGooday.AgPay.Payment.Api.Channel.WxPay.PayWay.WxNative;

using WxAppV3 = AGooday.AgPay.Payment.Api.Channel.WxPay.PayWayV3.WxApp;
using WxBarV3 = AGooday.AgPay.Payment.Api.Channel.WxPay.PayWayV3.WxBar;
using WxH5V3 = AGooday.AgPay.Payment.Api.Channel.WxPay.PayWayV3.WxH5;
using WxJsapiV3 = AGooday.AgPay.Payment.Api.Channel.WxPay.PayWayV3.WxJsapi;
using WxLiteV3 = AGooday.AgPay.Payment.Api.Channel.WxPay.PayWayV3.WxLite;
using WxNativeV3 = AGooday.AgPay.Payment.Api.Channel.WxPay.PayWayV3.WxNative;
#endregion
#region YsfPay
using YsfAliBar = AGooday.AgPay.Payment.Api.Channel.YsfPay.PayWay.AliBar;
using YsfAliJsapi = AGooday.AgPay.Payment.Api.Channel.YsfPay.PayWay.AliJsapi;

using YsfWxBar = AGooday.AgPay.Payment.Api.Channel.YsfPay.PayWay.WxBar;
using YsfWxJsapi = AGooday.AgPay.Payment.Api.Channel.YsfPay.PayWay.WxJsapi;
#endregion
#endregion

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

services.AddSingleton(new Appsettings(Env.ContentRootPath));

//// ע����־
//services.AddLogging(config =>
//{
//    //Microsoft.Extensions.Logging.Log4Net.AspNetCore
//    config.AddLog4Net();
//});
services.AddSingleton<ILoggerProvider, Log4NetLoggerProvider>();

#region Redis
//redis����
var section = builder.Configuration.GetSection("Redis:Default");
//�����ַ���
string _connectionString = section.GetSection("Connection").Value;
//ʵ������
string _instanceName = section.GetSection("InstanceName").Value;
//Ĭ�����ݿ� 
int _defaultDB = int.Parse(section.GetSection("DefaultDB").Value ?? "0");
services.AddSingleton(new RedisUtil(_connectionString, _instanceName, _defaultDB));
#endregion

#region MQ
var mqconfiguration = builder.Configuration.GetSection("MQ:RabbitMQ");
services.Configure<RabbitMQConfiguration>(mqconfiguration);
#endregion

#region OSS
builder.Configuration.GetSection("OSS").Bind(LocalOssConfig.Oss);
builder.Configuration.GetSection("OSS:AliyunOss").Bind(AliyunOssConfig.Oss);
#endregion

services.AddMemoryCache();

// Automapper ע��
services.AddAutoMapperSetup();
services.AddControllersWithViews()
    //.AddNewtonsoftJson();
    .AddNewtonsoftJson(options =>
    {
        //https://blog.poychang.net/using-newtonsoft-json-in-asp-net-core-projects/
        options.SerializerSettings.Formatting = Formatting.Indented;
        //options.SerializerSettings.ContractResolver = new DefaultContractResolver();
        options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();//Json key ���ַ�Сд�����շ�תС�շ壩
    });

// Newtonsoft.Json ȫ������ 
JsonConvert.DefaultSettings = () => new JsonSerializerSettings
{
    Formatting = Formatting.Indented,
    DateFormatString = "yyyy-MM-dd HH:mm:ss",
    ContractResolver = new CamelCasePropertyNamesContractResolver()
};

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

#region RabbitMQ
services.AddSingleton<IMQSender, RabbitMQSender>();
services.AddSingleton<IMQMsgReceiver, PayOrderDivisionRabbitMQReceiver>();
services.AddSingleton<IMQMsgReceiver, PayOrderMchNotifyRabbitMQReceiver>();
services.AddSingleton<IMQMsgReceiver, PayOrderReissueRabbitMQReceiver>();
services.AddSingleton<IMQMsgReceiver, ResetAppConfigRabbitMQReceiver>();
services.AddSingleton<IMQMsgReceiver, ResetIsvMchAppInfoRabbitMQReceiver>();
services.AddSingleton<PayOrderDivisionMQ.IMQReceiver, PayOrderDivisionMQReceiver>();
services.AddSingleton<PayOrderMchNotifyMQ.IMQReceiver, PayOrderMchNotifyMQReceiver>();
services.AddSingleton<PayOrderReissueMQ.IMQReceiver, PayOrderReissueMQReceiver>();
services.AddSingleton<ResetAppConfigMQ.IMQReceiver, ResetAppConfigMQReceiver>();
services.AddSingleton<ResetIsvMchAppInfoConfigMQ.IMQReceiver, ResetIsvMchAppInfoMQReceiver>();
services.AddHostedService<RabbitListener>();
#endregion

#region OSS
if (OssServiceTypeEnum.LOCAL.GetDescription().Equals(LocalOssConfig.Oss.ServiceType))
{
    services.AddScoped<IOssService, LocalFileService>();
}
#endregion

services.AddSingleton<ChannelCertConfigKit>();

//var provider = services.BuildServiceProvider();
//var mchAppService = (IMchAppService)provider.GetService(typeof(IMchAppService));
//var mchInfoService = (IMchInfoService)provider.GetService(typeof(IMchInfoService));
//var isvInfoService = (IIsvInfoService)provider.GetService(typeof(IIsvInfoService));
//var payInterfaceConfigService = (IPayInterfaceConfigService)provider.GetService(typeof(IPayInterfaceConfigService));
//services.AddSingleton(new ConfigContextService(mchAppService, mchInfoService, isvInfoService, payInterfaceConfigService));
//services.AddSingleton(typeof(ConfigContextQueryService));
//services.AddSingleton(typeof(ConfigContextService));
services.AddSingleton<ChannelOrderReissueService>();
services.AddSingleton<ConfigContextQueryService>();
services.AddSingleton<ConfigContextService>();
services.AddSingleton<PayMchNotifyService>();
services.AddSingleton<PayOrderDivisionProcessService>();
services.AddSingleton<PayOrderProcessService>();
services.AddSingleton<RefundOrderProcessService>();
#region DivisionService
//services.AddSingleton<IDivisionService, AliPayDivisionService>();
//services.AddSingleton<IDivisionService, WxPayDivisionService>();
services.AddSingleton<AliPayDivisionService>();
services.AddSingleton<WxPayDivisionService>();
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
#endregion
#region PaymentService
services.AddSingleton<AliPayPaymentService>();
services.AddSingleton<WxPayPaymentService>();
services.AddSingleton<YsfPayPaymentService>();
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
#endregion
#region RefundService
services.AddSingleton<AliPayRefundService>();
services.AddSingleton<WxPayRefundService>();
services.AddSingleton<YsfPayRefundService>();
services.AddSingleton(provider =>
{
    Func<string, IRefundService> funcFactory = ifCode =>
    {
        switch (ifCode)
        {
            case CS.IF_CODE.ALIPAY:
                return provider.GetService<AliPayRefundService>();
            case CS.IF_CODE.WXPAY:
                return provider.GetService<WxPayRefundService>();
            case CS.IF_CODE.YSFPAY:
                return provider.GetService<YsfPayRefundService>();
            default:
                return null;
        }
    };
    return funcFactory;
});
#endregion
#region QueryService
services.AddSingleton<AliPayPayOrderQueryService>();
services.AddSingleton<WxPayPayOrderQueryService>();
services.AddSingleton<YsfPayPayOrderQueryService>();
services.AddSingleton(provider =>
{
    Func<string, IPayOrderQueryService> funcFactory = ifCode =>
    {
        switch (ifCode)
        {
            case CS.IF_CODE.ALIPAY:
                return provider.GetService<AliPayPayOrderQueryService>();
            case CS.IF_CODE.WXPAY:
                return provider.GetService<WxPayPayOrderQueryService>();
            case CS.IF_CODE.YSFPAY:
                return provider.GetService<YsfPayPayOrderQueryService>();
            default:
                return null;
        }
    };
    return funcFactory;
});
#endregion
#region AliPay
services.AddSingleton<IPaymentService, AliApp>();
services.AddSingleton<IPaymentService, AliBar>();
services.AddSingleton<IPaymentService, AliJsapi>();
services.AddSingleton<IPaymentService, AliPc>();
services.AddSingleton<IPaymentService, AliQr>();
services.AddSingleton<IPaymentService, AliWap>();
#endregion
#region WxPay
services.AddSingleton<IPaymentService, WxApp>();
services.AddSingleton<IPaymentService, WxBar>();
services.AddSingleton<IPaymentService, WxH5>();
services.AddSingleton<IPaymentService, WxJsapi>();
services.AddSingleton<IPaymentService, WxLite>();
services.AddSingleton<IPaymentService, WxNative>();

services.AddSingleton<IPaymentService, WxAppV3>();
services.AddSingleton<IPaymentService, WxBarV3>();
services.AddSingleton<IPaymentService, WxH5V3>();
services.AddSingleton<IPaymentService, WxJsapiV3>();
services.AddSingleton<IPaymentService, WxLiteV3>();
services.AddSingleton<IPaymentService, WxNativeV3>();
#endregion
#region YsfPay
services.AddSingleton<IPaymentService, YsfAliBar>();
services.AddSingleton<IPaymentService, YsfAliJsapi>();

services.AddSingleton<IPaymentService, YsfWxBar>();
services.AddSingleton<IPaymentService, YsfWxJsapi>();
#endregion
#region ChannelUserService
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
#endregion

services.AddSingleton<IQRCodeService, QRCodeService>();

var serviceProvider = services.BuildServiceProvider();
ChannelCertConfigKit.ServiceProvider = serviceProvider;
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
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
