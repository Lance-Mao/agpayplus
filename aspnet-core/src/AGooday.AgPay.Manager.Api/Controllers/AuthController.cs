using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Application.Services;
using AGooday.AgPay.Application.ViewModels;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Exceptions;
using AGooday.AgPay.Common.Models;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Domain.Core.Notifications;
using AGooday.AgPay.Domain.Models;
using CaptchaGen.NetCore;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Buffers.Text;
using System.Runtime.InteropServices;

namespace AGooday.AgPay.Manager.Api.Controllers
{
    [ApiController]
    [Route("api/anon/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly ISysUserService _sysUserService;
        private IMemoryCache _cache;
        // ������֪ͨ�������ע��Controller
        private readonly DomainNotificationHandler _notifications;

        public AuthController(ILogger<AuthController> logger, ISysUserService sysUserService, IMemoryCache cache, INotificationHandler<DomainNotification> notifications)
        {
            _logger = logger;
            _sysUserService = sysUserService;
            _cache = cache;
            _notifications = (DomainNotificationHandler)notifications;
        }

        [HttpPost]
        [Route("validate")]
        public ApiRes Validate(string ia, string ip, string vc, string vt)
        {
            string account = Base64Util.DecodeBase64(ia);  //�û��� i account, ����base64����
            string ipassport = Base64Util.DecodeBase64(ip);    //���� i passport,  ����base64����
            string vercode = Base64Util.DecodeBase64(vc);  //��֤�� vercode,  ����base64����
            string vercodeToken = Base64Util.DecodeBase64(vt);	//��֤��token, vercode token ,  ����base64����

            string cacheCode = RedisUtil.GetString(CS.GetCacheKeyImgCode(vercodeToken));
            if (string.IsNullOrWhiteSpace(cacheCode) || !cacheCode.Equals(vercode))
            {
                throw new BizException("��֤������");
            }
            //https://jasonwatmore.com/post/2022/01/16/net-6-hash-and-verify-passwords-with-bcrypt
            //https://bcrypt.online/
            bool verified = BCrypt.Net.BCrypt.Verify(ipassport, "$2y$10$hsnrQ/BqHwOQpIC13sO40el6nR2NMvDvj0sjdQy86mxo4h7m7aAXi");
            // ����ǰ�� accessToken
            string accessToken = string.Empty;//authService.Auth(account, ipassport);

            //// ɾ��ͼ����֤�뻺������
            //RedisUtil.Del(CS.GetCacheKeyImgCode(vercodeToken));

            return ApiRes.Ok4newJson(CS.ACCESS_TOKEN_NAME, accessToken);
        }

        [HttpGet]
        [Route("Vercode")]
        public ApiRes Vercode()
        {
            //����ͼ����֤��ĳ��Ϳ� // 4λ��֤��
            string code = ImageFactory.CreateCode(6);
            string imageBase64Data;
            using (var picStream = ImageFactory.BuildImage(code, 40, 137, 20, 10))
            {
                var imageBytes = picStream.ToArray();
                imageBase64Data = $"data:image/jpg;base64,{Convert.ToBase64String(imageBytes)}";
            }

            //redis
            string vercodeToken = Guid.NewGuid().ToString("N");
            RedisUtil.SetString(CS.GetCacheKeyImgCode(vercodeToken), code, CS.VERCODE_CACHE_TIME); //ͼƬ��֤�뻺��ʱ��: 1����

            return ApiRes.Ok(new { imageBase64Data = imageBase64Data, vercodeToken = vercodeToken, expireTime = CS.VERCODE_CACHE_TIME });
        }
    }
}