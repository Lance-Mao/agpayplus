using AGooday.AgPay.Agent.Api.Authorization;
using AGooday.AgPay.Agent.Api.Extensions;
using AGooday.AgPay.Agent.Api.Extensions.AuthContext;
using AGooday.AgPay.Agent.Api.Filter;
using AGooday.AgPay.Agent.Api.Logs;
using AGooday.AgPay.Agent.Api.Middlewares;
using AGooday.AgPay.Agent.Api.Models;
using AGooday.AgPay.Agent.Api.MQ;
using AGooday.AgPay.Common.Extensions;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Components.MQ.Models;
using AGooday.AgPay.Components.MQ.Vender;
using AGooday.AgPay.Components.MQ.Vender.RabbitMQ;
using AGooday.AgPay.Components.MQ.Vender.RabbitMQ.Receive;
using AGooday.AgPay.Components.OSS.Config;
using AGooday.AgPay.Components.OSS.Constants;
using AGooday.AgPay.Components.OSS.Controllers;
using AGooday.AgPay.Components.OSS.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var logging = builder.Logging;
// ���� ClearProviders �Դ���������ɾ������ ILoggerProvider ʵ��
logging.ClearProviders();
//// ͨ������־����Ӧ��������ָ�����������ڴ�����ָ����
//logging.AddFilter("Microsoft", LogLevel.Warning);
// ���ӿ���̨��־��¼�ṩ����
logging.AddConsole();

// Add services to the container.
var services = builder.Services;
var Env = builder.Environment;

services.AddSingleton(new Appsettings(Env.ContentRootPath));

//�û���Ϣ
services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

//// ע����־
//services.AddLogging(config =>
//{
//    //Microsoft.Extensions.Logging.Log4Net.AspNetCore
//    config.AddLog4Net();
//});
services.AddSingleton<ILoggerProvider, Log4NetLoggerProvider>();

services.AddScoped<ILogHandler, LogHandler>();

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
builder.Configuration.GetSection("MQ:RabbitMQ").Bind(RabbitMQConfig.MQ);
#endregion

#region OSS
builder.Configuration.GetSection("OSS").Bind(LocalOssConfig.Oss);
builder.Configuration.GetSection("OSS:AliyunOss").Bind(AliyunOssConfig.Oss);
#endregion

var cors = builder.Configuration.GetSection("Cors").Value;
services.AddCors(o =>
    o.AddPolicy("CorsPolicy",
        builder => builder
            .WithOrigins(cors.Split(","))
            .AllowAnyHeader()
            .AllowAnyMethod()
            //.AllowAnyOrigin()
            .AllowCredentials()
    ));

services.AddMemoryCache();
services.AddHttpContextAccessor();
services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
var jwtSettingsSection = builder.Configuration.GetSection("JWT");
services.Configure<JwtSettings>(jwtSettingsSection);
// JWT ��֤
var appSettings = jwtSettingsSection.Get<JwtSettings>();
services.AddJwtBearerAuthentication(appSettings);

services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Automapper ע��
services.AddAutoMapperSetup();
services.AddControllers(options =>
{
    ////����ȫ���쳣������
    //options.Filters.Add<GlobalExceptionsFilter>();
    //��־������
    options.Filters.Add<LogActionFilter>();
})
    .AddApplicationPart(typeof(OssFileController).Assembly)
    //.AddNewtonsoftJson();
    .AddNewtonsoftJson(options =>
    {
        //https://blog.poychang.net/using-newtonsoft-json-in-asp-net-core-projects/
        options.SerializerSettings.Formatting = Formatting.None;
        //options.SerializerSettings.ContractResolver = new DefaultContractResolver();
        options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();//Json key ���ַ�Сд�����շ�תС�շ壩
    });

// Newtonsoft.Json ȫ������ 
JsonConvert.DefaultSettings = () => new JsonSerializerSettings
{
    Formatting = Formatting.None,//��ʽ��
    DateFormatString = "yyyy-MM-dd HH:mm:ss",
    ContractResolver = new CamelCasePropertyNamesContractResolver()
};

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AGooday.AgPay.Agent.Api", Version = "1.0" });
    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Description = $"JWT Authorization header using the Bearer scheme. \r\n\r\nEnter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
    });
    options.OperationFilter<SwaggerSecurityScheme>();
    //options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    //{
    //    {
    //        new OpenApiSecurityScheme
    //        {
    //            Reference = new OpenApiReference
    //            {
    //                Type = ReferenceType.SecurityScheme,
    //                Id = JwtBearerDefaults.AuthenticationScheme
    //            },
    //            Scheme = "oauth2",
    //            Name = JwtBearerDefaults.AuthenticationScheme,
    //            In = ParameterLocation.Header,
    //        },
    //        new List<string>()
    //    }
    //});
});

// Adding MediatR for Domain Events
// ������������¼���ע��
// ���ð� MediatR.Extensions.Microsoft.DependencyInjection
//services.AddMediatR(typeof(MyxxxHandler));//����ע��ĳһ����������
//��
services.AddMediatR(typeof(Program));//Ŀ����Ϊ��ɨ��Handler��ʵ�ֶ������ӵ�IOC��������

// .NET Core ԭ������ע��
// ��дһ�����������������չʾ�� Presentation �и���
NativeInjectorBootStrapper.RegisterServices(services);

#region RabbitMQ
services.AddSingleton<IMQSender, RabbitMQSender>();
services.AddSingleton<IMQMsgReceiver, ResetAppConfigRabbitMQReceiver>();
services.AddSingleton<ResetAppConfigMQ.IMQReceiver, ResetAppConfigMQReceiver>();
services.AddHostedService<RabbitListener>();
#endregion

#region OSS
if (OssServiceTypeEnum.LOCAL.GetDescription().Equals(LocalOssConfig.Oss.ServiceType))
{
    services.AddScoped<IOssService, LocalFileService>();
}
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCalculateExecutionTime();

app.UseRequestResponseLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

// ��Ȩ ����û��Ƿ��¼
app.UseAuthentication();
app.UseCors("CorsPolicy");

var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
AuthContextService.Configure(httpContextAccessor);

// ��Ȩ �����û��Ȩ�޷��ʺ���ҳ��
app.UseAuthorization();

app.UseExceptionHandling();

app.MapControllers();

app.Run();