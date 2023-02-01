﻿using AGooday.AgPay.Agent.Api.Attributes;
using AGooday.AgPay.Agent.Api.Authorization;
using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Application.Permissions;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Models;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Components.MQ.Vender;
using AGooday.AgPay.Domain.Core.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AGooday.AgPay.Agent.Api.Controllers.Merchant
{
    /// <summary>
    /// 商户管理类
    /// </summary>
    [Route("/api/mchInfo")]
    [ApiController, Authorize]
    public class MchInfoController : CommonController
    {
        private readonly IMQSender mqSender;
        private readonly ILogger<MchInfoController> _logger;
        private readonly IMchInfoService _mchInfoService;
        private readonly IAgentInfoService _agentInfoService;

        private readonly DomainNotificationHandler _notifications;

        public MchInfoController(IMQSender mqSender, ILogger<MchInfoController> logger, INotificationHandler<DomainNotification> notifications,
            IMchInfoService mchInfoService, IAgentInfoService agentInfoService, RedisUtil client,
            ISysUserService sysUserService,
            ISysRoleEntRelaService sysRoleEntRelaService,
            ISysUserRoleRelaService sysUserRoleRelaService)
            : base(logger, client, sysUserService, sysRoleEntRelaService, sysUserRoleRelaService)
        {
            this.mqSender = mqSender;
            _logger = logger;
            _mchInfoService = mchInfoService;
            _agentInfoService = agentInfoService;
            _notifications = (DomainNotificationHandler)notifications;
        }

        /// <summary>
        /// 商户信息列表
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpGet, Route(""), NoLog]
        [PermissionAuth(PermCode.AGENT.ENT_MCH_LIST)]
        public ApiRes List([FromQuery] MchInfoQueryDto dto)
        {
            dto.AgentNo = GetCurrentAgentNo();
            var data = _mchInfoService.GetPaginatedData(dto);
            return ApiRes.Ok(new { Records = data.ToList(), Total = data.TotalCount, Current = data.PageIndex, HasNext = data.HasNext });
        }

        /// <summary>
        /// 新增商户信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost, Route(""), MethodLog("新增商户信息")]
        [PermissionAuth(PermCode.AGENT.ENT_MCH_INFO_ADD)]
        public ApiRes Add(MchInfoCreateDto dto)
        {
            var agentNo = GetCurrentAgentNo();
            var agentInfo = _agentInfoService.GetByAgentNo(agentNo);
            dto.RefundMode = "[\"plat\", \"api\"]";
            dto.Type = CS.MCH_TYPE_ISVSUB;
            dto.AgentNo = agentInfo.AgentNo;
            dto.IsvNo = agentInfo.IsvNo;
            _mchInfoService.Create(dto);
            // 是否存在消息通知
            if (!_notifications.HasNotifications())
                return ApiRes.Ok();
            else
                return ApiRes.CustomFail(_notifications.GetNotifications().Select(s => s.Value).ToArray());
        }

        /// <summary>
        /// 删除商户信息
        /// </summary>
        /// <param name="mchNo"></param>
        /// <returns></returns>
        [HttpDelete, Route("{mchNo}"), MethodLog("删除商户信息")]
        [PermissionAuth(PermCode.AGENT.ENT_MCH_INFO_DEL)]
        public ApiRes Delete(string mchNo)
        {
            _mchInfoService.Remove(mchNo);
            return ApiRes.Ok();
        }

        /// <summary>
        /// 更新商户信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut, Route("{mchNo}"), MethodLog("更新商户信息")]
        [PermissionAuth(PermCode.AGENT.ENT_MCH_INFO_EDIT)]
        public async Task<ApiRes> Update(string mchNo, MchInfoModifyDto dto)
        {
            _mchInfoService.Modify(dto);
            // 是否存在消息通知
            if (!_notifications.HasNotifications())
                return ApiRes.Ok();
            else
                return ApiRes.CustomFail(_notifications.GetNotifications().Select(s => s.Value).ToArray());
        }

        /// <summary>
        /// 查询商户信息
        /// </summary>
        /// <param name="mchNo"></param>
        /// <returns></returns>
        [HttpGet, Route("{mchNo}"), NoLog]
        [PermissionAuth(PermCode.AGENT.ENT_MCH_INFO_VIEW, PermCode.AGENT.ENT_MCH_INFO_EDIT)]
        public ApiRes Detail(string mchNo)
        {
            var mchInfo = _mchInfoService.GetByMchNo(mchNo);
            if (mchInfo == null)
            {
                return ApiRes.Fail(ApiCode.SYS_OPERATION_FAIL_SELETE);
            }
            return ApiRes.Ok(mchInfo);
        }
    }
}