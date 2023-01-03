using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Exceptions;
using AGooday.AgPay.Common.Models;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Domain.Core.Notifications;
using AGooday.AgPay.Agent.Api.Attributes;
using AGooday.AgPay.Agent.Api.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;

namespace AGooday.AgPay.Agent.Api.Controllers
{
    [Route("api/current")]
    [ApiController]
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

        [HttpGet, Route("user"), NoLog]
        public ApiRes CurrentUserInfo()
        {
            try
            {
                //��ǰ�û���Ϣ
                var currentUser = GetCurrentUser();

                //1. ��ǰ�û�����Ȩ��ID����
                var entIds = currentUser.Authorities.ToList();

                //2. ��ѯ���û����в˵����� (���������ʾ�˵� �� �������Ͳ˵� )
                var sysEnts = _sysEntService.GetBySysType(CS.SYS_TYPE.AGENT, entIds, new List<string> { CS.ENT_TYPE.MENU_LEFT, CS.ENT_TYPE.MENU_OTHER });

                //�ݹ�ת��Ϊ��״�ṹ
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
                var jsonArray = JArray.FromObject(sysEnts);
                var allMenuRouteTree = new TreeDataBuilder(jsonArray, "entId", "pid", "children", "entSort", true).BuildTreeObject();
                var user = JObject.FromObject(currentUser.SysUser);
                user.Add("entIdList", JArray.FromObject(entIds));
                user.Add("allMenuRouteTree", JToken.FromObject(allMenuRouteTree));
                return ApiRes.Ok(user);
            }
            catch (Exception)
            {
                throw new UnauthorizeException();
                //throw new BizException("��¼ʧЧ");
                //return ApiRes.CustomFail("��¼ʧЧ");
            }
        }

        /// <summary>
        /// �޸ĸ�����Ϣ
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut, Route("user"), MethodLog("�޸ĸ�����Ϣ")]
        public ApiRes ModifyCurrentUserInfo(ModifyCurrentUserInfoDto dto)
        {
            var currentUser = GetCurrentUser();
            _sysUserService.ModifyCurrentUserInfo(dto);
            var userinfo = _sysUserAuthService.GetUserAuthInfoById(currentUser.SysUser.SysUserId);
            currentUser.SysUser = userinfo;
            //����redis��������
            var currentUserJson = JsonConvert.SerializeObject(currentUser);
            _redis.StringSet(currentUser.CacheKey, currentUserJson, new TimeSpan(0, 0, CS.TOKEN_TIME));
            return ApiRes.Ok();
        }

        /// <summary>
        /// �޸�����
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        /// <exception cref="BizException"></exception>
        [HttpPut, Route("modifyPwd"), MethodLog("�޸�����")]
        public ApiRes ModifyPwd(ModifyPwd dto)
        {
            var currentUser = GetCurrentUser();
            string currentUserPwd = Base64Util.DecodeBase64(dto.OriginalPwd); //��ǰ�û���¼����currentUser
            var user = _sysUserAuthService.GetUserAuthInfoById(currentUser.SysUser.SysUserId);
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
            _sysUserAuthService.ResetAuthInfo(dto.RecordId, null, null, opUserPwd, CS.SYS_TYPE.AGENT);
            return Logout();
        }

        /// <summary>
        /// �ǳ�
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("logout"), MethodLog("�ǳ�")]
        public ApiRes Logout()
        {
            var currentUser = GetCurrentUser();
            _redis.KeyDelete(currentUser.CacheKey);
            return ApiRes.Ok();
        }
    }
}