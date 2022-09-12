using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Infrastructure.Context;
using AGooday.AgPay.Manager.Api.Extensions;
using AGooday.AgPay.Manager.Api.Middlewares;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var services = builder.Services;
var Env = builder.Environment;


//services.AddDbContext<AgPayDbContext>(options =>
//    options.UseMySql(builder.Configuration.GetConnectionString("Default"),
//    MySqlServerVersion.LatestSupportedServerVersion));

services.AddDbContext<AgPayDbContext>();

// Automapper ע��
services.AddAutoMapperSetup();
services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

// Adding MediatR for Domain Events
// ������������¼���ע��
// ���ð� MediatR.Extensions.Microsoft.DependencyInjection
//services.AddMediatR(typeof(MyxxxHandler));//����ע��ĳһ���������
//��
services.AddMediatR(typeof(Program));//Ŀ����Ϊ��ɨ��Handler��ʵ�ֶ�����ӵ�IOC��������

// .NET Core ԭ������ע��
// ��дһ����������������չʾ�� Presentation �и���
NativeInjectorBootStrapper.RegisterServices(services);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();
