using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Models;
using AGooday.AgPay.Domain.Core.Notifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.InteropServices;
using AGooday.AgPay.Common.Utils;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;
using AGooday.AgPay.Merchant.Api.Models;
using AGooday.AgPay.Common.Exceptions;

namespace AGooday.AgPay.Merchant.Api.Controllers
{
    [ApiController]
    [Route("api/current")]
    public class CurrentUserController : CommonController
    {
        private readonly ILogger<CurrentUserController> _logger;
        private readonly IDatabase _redis;
        private readonly ISysUserService _sysUserService;
        private readonly ISysEntitlementService _sysEntService;
        private readonly ISysUserAuthService _sysUserAuthService;
        private IMemoryCache _cache;
        // ������֪ͨ�������ע��Controller
        private readonly DomainNotificationHandler _notifications;

        public CurrentUserController(ILogger<CurrentUserController> logger, IMemoryCache cache, INotificationHandler<DomainNotification> notifications, RedisUtil client,
            ISysUserService sysUserService,
            ISysEntitlementService sysEntService,
            ISysUserAuthService sysUserAuthService,
            ISysRoleEntRelaService sysRoleEntRelaService,
            ISysUserRoleRelaService sysUserRoleRelaService)
            : base(logger, client, sysUserService, sysRoleEntRelaService, sysUserRoleRelaService)
        {
            _logger = logger;
            _sysUserService = sysUserService;
            _sysEntService = sysEntService;
            _sysUserAuthService = sysUserAuthService;
            _cache = cache;
            _redis = client.GetDatabase();
            _notifications = (DomainNotificationHandler)notifications;
        }

        [HttpGet, Route("user")]
        public ApiRes CurrentUserInfo()
        {
            //��ǰ�û���Ϣ
            var currentUser = GetCurrentUser();

            //1. ��ǰ�û�����Ȩ��ID����
            var entIds = currentUser.Authorities.ToList();

            //2. ��ѯ���û����в˵����� (���������ʾ�˵� �� �������Ͳ˵� )
            var sysEnts = _sysEntService.GetBySysType(CS.SYS_TYPE.MCH, entIds, new List<string> { CS.ENT_TYPE.MENU_LEFT, CS.ENT_TYPE.MENU_OTHER });

            //�ݹ�ת��Ϊ��״�ṹ
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var jsonArray = JArray.FromObject(sysEnts);
            var leftMenuTree = new TreeDataBuilder(jsonArray, "entId", "pid", "children", "entSort", true).BuildTreeObject();
            return ApiRes.Ok(new { currentUser.User, leftMenuTree });
        }

        [HttpPut, Route("user")]
        public ApiRes ModifyCurrentUserInfo(ModifyCurrentUserInfo dto)
        {
            var user = _sysUserService.GetById(dto.SysUserId);
            if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
                user.AvatarUrl = dto.AvatarUrl;
            if (!string.IsNullOrWhiteSpace(dto.Realname))
                user.Realname = dto.Realname;
            if (dto.Sex > 0)
                user.Sex = dto.Sex;
            _sysUserService.Update(user);
            var currentUser = GetCurrentUser();
            var userinfo = _sysUserAuthService.GetUserAuthInfoById(dto.SysUserId);
            currentUser.User = userinfo;
            //����redis��������
            var currentUserJson = JsonConvert.SerializeObject(currentUser);
            _redis.StringSet(currentUser.CacheKey, currentUserJson, new TimeSpan(0, 0, CS.TOKEN_TIME));
            return ApiRes.Ok();
        }

        [HttpPut, Route("modifyPwd")]
        public ApiRes ModifyPwd(ModifyPwd dto)
        {
            string currentUserPwd = Base64Util.DecodeBase64(dto.OriginalPwd); //��ǰ�û���¼����
            var user = _sysUserAuthService.GetUserAuthInfoById(dto.SysUserId);
            bool verified = BCrypt.Net.BCrypt.Verify(currentUserPwd, user.Credential);
            //��֤��ǰ�����Ƿ���ȷ
            if (!verified)
            {
                throw new BizException("ԭ������֤ʧ�ܣ�");
            }
            string opUserPwd = Base64Util.DecodeBase64(dto.ConfirmPwd);
            // ��֤ԭ�������������Ƿ���ͬ
            if (opUserPwd.Equals(currentUserPwd))
            {
                throw new BizException("��������ԭ���벻����ͬ��");
            }
            _sysUserAuthService.ResetAuthInfo(dto.SysUserId, null, null, opUserPwd, CS.SYS_TYPE.MCH);
            return Logout();
        }

        [HttpPost, Route("logout")]
        public ApiRes Logout()
        {
            var currentUser = GetCurrentUser();
            _redis.KeyDelete(currentUser.CacheKey);
            return ApiRes.Ok();
        }
    }
}