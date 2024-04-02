using AGooday.AgPay.Notice.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AGooday.AgPay.Notice.UnitTests
{
    public class EmailNoticeTest
    {
        private readonly IEmailProvider _emailProvider;

        public EmailNoticeTest()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddNotice(config =>
                    {
                        config.IntervalSeconds = 10;
                        config.UseEmail(option =>
                        {
                            option.Host = "smtp.qq.com";
                            option.Port = 465;
                            option.FromName = "xxx@foxmail.com";
                            option.FromAddress = "xxx@foxmail.com";
                            option.Password = "******";
                            option.ToAddress = new List<string>()
                            {
                                "123@qq.com"
                            };
                        });
                    });
                })
                .Build();

            _emailProvider = host.Services.GetRequiredService<IEmailProvider>();
        }

        [Fact]
        public async Task EmailSendShouldBeSucceed()
        {
            var response = await _emailProvider.SendAsync("�ʼ�����", new Exception("custom exception"));
            Assert.True(response.IsSuccess);

            var subject = "�̻�ע��ɹ�֪ͨ";
            var body = $@"�𾴵��̻���<br/><br/>
            ��ϲ���ɹ�ע��Ϊ���ǵ��̻������Ƿǳ����˵ػ�ӭ���������ǵ�ƽ̨��<br/><br/>
            ����������ע����Ϣ��<br/>
            �̻��ţ�M0000000000<br/>
            �̻����ƣ�����ע��֪ͨ<br/>
            �˻����ƣ�ceshimch<br/>
            ��ϵ�˵绰��18888888888<br/>
            �˻����룺888888<br/>
            ע��ʱ�䣺{DateTime.Now:yyyy-MM-dd HH:mm:ss}<br/><br/>
            �����ڿ���ʹ�������̻��˺Ż��ֻ��ŵ�¼�����ǵ�ƽ̨������ʼ�������·���<br/>
            - ���������̻����Ϻ���Ϣ<br/>
            - ���ɱ����ͳ������<br/>
            - ����������֧�����ԡ�ת�˺����÷���<br/>
            - ����֧�������ͽ��׼�¼<br/>
            - ����֧����ʽ�ͷ���<br/>
            - �����豸��Ա����Ϣ��ϵͳ����<br/><br/>
            ��������κ����⡢���ʻ���Ҫ����������ʱ�����ǵĿͻ������Ŷ���ϵ�����ǽ��߳�Ϊ���ṩ֧�ֺ�Э����<br/><br/>
            �ٴθ�л��ѡ���Ϊ���ǵ��̻����ڴ������������ڵĺ�����ϵ��<br/><br/>
            ף������¡��<br/><br/>
            ���ֿ���ʺ�<br/>
            [���տƼ�]";
            var request = new EmailSendRequest()
            {
                ToAddress = new List<string>() {
                    "123@qq.com"
                },
                Subject = subject,
                Body = body,
            };
            response = await _emailProvider.SendAsync(request);
            Assert.True(response.IsSuccess);

            _emailProvider.SetToAddress(new List<string>() { "123@qq.com" });
            response = await _emailProvider.SendAsync(subject, body);

            Assert.True(response.IsSuccess);
        }
    }
}