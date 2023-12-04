using AGooday.AgPay.Agent.Api.Extensions.AuthContext;
using AGooday.AgPay.Agent.Api.Models;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Exceptions;
using AGooday.AgPay.Common.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace AGooday.AgPay.Agent.Api.Controllers
{
    public abstract class CommonController : ControllerBase
    {
        private readonly ILogger<CommonController> _logger;
        private readonly int defaultDB;
        private readonly IDatabase redis;
        private readonly IServer redisServer;
        private readonly ISysUserService _sysUserService;
        private readonly ISysRoleEntRelaService _sysRoleEntRelaService;
        private readonly ISysUserRoleRelaService _sysUserRoleRelaService;

        public CommonController(ILogger<CommonController> logger, RedisUtil client,
            ISysUserService sysUserService,
            ISysRoleEntRelaService sysRoleEntRelaService,
            ISysUserRoleRelaService sysUserRoleRelaService)
        {
            _logger = logger;
            defaultDB = client.GetDefaultDB();
            redis = client.GetDatabase();
            redisServer = client.GetServer();
            _sysUserService = sysUserService;
            _sysRoleEntRelaService = sysRoleEntRelaService;
            _sysUserRoleRelaService = sysUserRoleRelaService;
        }

        protected CurrentUser GetCurrentUser()
        {
            if (AuthContextService.CurrentUser.CacheKey == null)
            {
                throw new UnauthorizeException();
                //throw new BizException("��¼ʧЧ");
            }
            string currentUser = redis.StringGet(AuthContextService.CurrentUser.CacheKey);
            if (currentUser == null)
            {
                throw new UnauthorizeException();
                //throw new BizException("��¼ʧЧ");
            }
            return JsonConvert.DeserializeObject<CurrentUser>(currentUser);
        }

        /// <summary>
        /// ��ȡ��ǰ�û�ID
        /// </summary>
        /// <returns></returns>
        protected long GetCurrentUserId()
        {
            return GetCurrentUser().SysUser.SysUserId;
        }

        /// <summary>
        /// ��ȡ��ǰ������ID
        /// </summary>
        /// <returns></returns>
        protected string GetCurrentAgentNo()
        {
            return GetCurrentUser().SysUser.BelongInfoId;
        }

        /// <summary>
        /// �����û�ID ɾ���û�������Ϣ
        /// </summary>
        /// <param name="sysUserIdList"></param>
        protected void DelAuthentication(List<long> sysUserIdList)
        {
            if (sysUserIdList == null || sysUserIdList.Count <= 0)
            {
                return;
            }
            foreach (var sysUserId in sysUserIdList)
            {
                var redisKeys = redisServer.Keys(defaultDB, CS.GetCacheKeyToken(sysUserId, "*"));
                foreach (var key in redisKeys)
                {
                    redis.KeyDelete(key);
                }
            }
        }

        /// <summary>
        /// �����û�ID ���»����е�Ȩ�޼��ϣ� ʹ�÷���ʵʱ��Ч
        /// </summary>
        /// <param name="sysUserIdList"></param>
        protected void RefAuthentication(List<long> sysUserIdList)
        {
            var sysUserMap = _sysUserService.GetByIds(sysUserIdList);
            sysUserIdList.ForEach(sysUserId =>
            {
                var redisKeys = redisServer.Keys(defaultDB, CS.GetCacheKeyToken(sysUserId, "*"));
                foreach (var key in redisKeys)
                {
                    //�û������� || �ѽ��� ��Ҫɾ��Redis
                    if (!sysUserMap.Any(a => a.SysUserId.Equals(sysUserId))
                    || sysUserMap.Any(a => a.SysUserId.Equals(sysUserId) || a.State.Equals(CS.PUB_DISABLE)))
                    {
                        redis.KeyDelete(key);
                        continue;
                    }
                    string currentUserJson = redis.StringGet(AuthContextService.CurrentUser.CacheKey);
                    var currentUser = JsonConvert.DeserializeObject<CurrentUser>(currentUserJson);
                    if (currentUser == null)
                    {
                        continue;
                    }
                    var auth = sysUserMap.Where(w => w.SysUserId.Equals(sysUserId)).First();
                    var authorities = _sysUserRoleRelaService.SelectRoleIdsByUserId(auth.SysUserId.Value).ToList();
                    authorities.AddRange(_sysRoleEntRelaService.SelectEntIdsByUserId(auth.SysUserId.Value, auth.IsAdmin, auth.SysType));
                    currentUser.Authorities = authorities;
                    currentUserJson = JsonConvert.SerializeObject(currentUser);
                    //����token  ʧЧʱ�䲻��
                    redis.StringSet(key, currentUserJson);
                }
            });
        }
    }
}