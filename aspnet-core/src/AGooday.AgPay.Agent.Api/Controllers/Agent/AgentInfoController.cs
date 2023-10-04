﻿using AGooday.AgPay.Agent.Api.Attributes;
using AGooday.AgPay.Agent.Api.Authorization;
using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Application.Permissions;
using AGooday.AgPay.Common.Models;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Components.MQ.Vender;
using AGooday.AgPay.Domain.Core.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AGooday.AgPay.Agent.Api.Controllers.Agent
{
    /// <summary>
    /// 代理商管理类
    /// </summary>
    [Route("/api/agentInfo")]
    [ApiController, Authorize]
    public class AgentInfoController : CommonController
    {
        private readonly IMQSender mqSender;
        private readonly ILogger<AgentInfoController> _logger;
        private readonly IAgentInfoService _agentInfoService;
        private readonly ISysUserService _sysUserService;

        private readonly DomainNotificationHandler _notifications;

        public AgentInfoController(IMQSender mqSender, ILogger<AgentInfoController> logger, INotificationHandler<DomainNotification> notifications,
            IAgentInfoService agentInfoService, RedisUtil client,
            ISysUserService sysUserService,
            ISysRoleEntRelaService sysRoleEntRelaService,
            ISysUserRoleRelaService sysUserRoleRelaService)
            : base(logger, client, sysUserService, sysRoleEntRelaService, sysUserRoleRelaService)
        {
            this.mqSender = mqSender;
            _logger = logger;
            _agentInfoService = agentInfoService;
            _sysUserService = sysUserService;
            _notifications = (DomainNotificationHandler)notifications;
        }

        /// <summary>
        /// 代理商信息列表
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpGet, Route(""), NoLog]
        [PermissionAuth(PermCode.AGENT.ENT_AGENT_LIST)]
        public ApiRes List([FromQuery] AgentInfoQueryDto dto)
        {
            dto.Pid = GetCurrentAgentNo();
            var data = _agentInfoService.GetPaginatedData(dto);
            return ApiRes.Ok(new { Records = data.ToList(), Total = data.TotalCount, Current = data.PageIndex, HasNext = data.HasNext });
        }

        /// <summary>
        /// 新增代理商信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost, Route(""), MethodLog("新增代理商信息")]
        [PermissionAuth(PermCode.AGENT.ENT_AGENT_INFO_ADD)]
        public async Task<ApiRes> Add(AgentInfoCreateDto dto)
        {
            var agentNo = GetCurrentAgentNo();
            var agentInfo = _agentInfoService.GetById(agentNo);
            dto.Pid = agentInfo.AgentNo;
            dto.IsvNo = agentInfo.IsvNo;
            await _agentInfoService.Create(dto);
            // 是否存在消息通知
            if (!_notifications.HasNotifications())
                return ApiRes.Ok();
            else
                return ApiRes.CustomFail(_notifications.GetNotifications().Select(s => s.Value).ToArray());
        }

        /// <summary>
        /// 删除代理商信息
        /// </summary>
        /// <param name="agentNo"></param>
        /// <returns></returns>
        [HttpDelete, Route("{agentNo}"), MethodLog("删除代理商信息")]
        [PermissionAuth(PermCode.AGENT.ENT_AGENT_INFO_DEL)]
        public async Task<ApiRes> Delete(string agentNo)
        {
            await _agentInfoService.Remove(agentNo);
            return ApiRes.Ok();
        }

        /// <summary>
        /// 更新代理商信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut, Route("{agentNo}"), MethodLog("更新代理商信息")]
        [PermissionAuth(PermCode.AGENT.ENT_AGENT_INFO_EDIT)]
        public async Task<ApiRes> Update(string agentNo, AgentInfoModifyDto dto)
        {
            await _agentInfoService.Modify(dto);
            // 是否存在消息通知
            if (!_notifications.HasNotifications())
                return ApiRes.Ok();
            else
                return ApiRes.CustomFail(_notifications.GetNotifications().Select(s => s.Value).ToArray());
        }

        /// <summary>
        /// 查询代理商信息
        /// </summary>
        /// <param name="agentNo"></param>
        /// <returns></returns>
        [HttpGet, Route("{agentNo}"), NoLog]
        [PermissionAuth(PermCode.AGENT.ENT_AGENT_INFO_VIEW, PermCode.AGENT.ENT_AGENT_INFO_EDIT)]
        public ApiRes Detail(string agentNo)
        {
            var agentInfo = _agentInfoService.GetById(agentNo);
            if (agentInfo == null)
            {
                return ApiRes.Fail(ApiCode.SYS_OPERATION_FAIL_SELETE);
            }
            var sysUser = _sysUserService.GetById(agentInfo.InitUserId.Value);
            if (sysUser != null)
            {
                agentInfo.AddExt("loginUsername", sysUser.LoginUsername);
            }
            return ApiRes.Ok(agentInfo);
        }
    }
}
