﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Application.Permissions;
using AGooday.AgPay.Common.Extensions;
using AGooday.AgPay.Common.Models;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Components.MQ.Models;
using AGooday.AgPay.Components.MQ.Vender;
using AGooday.AgPay.Merchant.Api.Attributes;
using AGooday.AgPay.Merchant.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace AGooday.AgPay.Merchant.Api.Controllers.Merchant
{
    /// <summary>
    /// 商户应用管理类
    /// </summary>
    [Route("/api/mchApps")]
    [ApiController, Authorize]
    public class MchAppController : CommonController
    {
        private readonly IMQSender mqSender;
        private readonly ILogger<MchAppController> _logger;
        private readonly IMchAppService _mchAppService;
        private readonly IMchInfoService _mchInfoService;

        public MchAppController(IMQSender mqSender, ILogger<MchAppController> logger, RedisUtil client,
            IMchAppService mchAppService,
            IMchInfoService mchInfoService,
            ISysUserService sysUserService,
            ISysRoleEntRelaService sysRoleEntRelaService,
            ISysUserRoleRelaService sysUserRoleRelaService)
            : base(logger, client, sysUserService, sysRoleEntRelaService, sysUserRoleRelaService)
        {
            this.mqSender = mqSender;
            _logger = logger;
            _mchAppService = mchAppService;
            _mchInfoService = mchInfoService;
        }

        /// <summary>
        /// 应用列表
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpGet, Route(""), NoLog]
        [PermissionAuth(PermCode.MCH.ENT_MCH_APP_LIST)]
        public ApiRes List([FromQuery] MchAppQueryDto dto)
        {
            var mchNo = GetCurrentMchNo();
            dto.MchNo = mchNo;
            var data = _mchAppService.GetPaginatedData(dto);
            var mchInfo = _mchInfoService.GetById(mchNo);
            JArray records = new JArray();
            foreach (var item in data)
            {
                var jitem = JObject.FromObject(item);
                jitem["mchType"] = mchInfo.Type;
                records.Add(jitem);
            }
            return ApiRes.Ok(new { Records = records, Total = data.TotalCount, Current = data.PageIndex, HasNext = data.HasNext });
        }

        /// <summary>
        /// 新建应用
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost, Route(""), MethodLog("新建应用")]
        [PermissionAuth(PermCode.MCH.ENT_MCH_APP_ADD)]
        public ApiRes Add(MchAppDto dto)
        {
            var sysUser = GetCurrentUser().SysUser;
            dto.MchNo = sysUser.BelongInfoId;
            dto.AppId = Guid.NewGuid().ToString("N").Substring(0, 24);
            dto.CreatedBy = sysUser.Realname;
            dto.CreatedUid = sysUser.SysUserId;
            if (!_mchInfoService.IsExistMchNo(dto.MchNo))
            {
                return ApiRes.Fail(ApiCode.SYS_OPERATION_FAIL_SELETE);
            }

            var result = _mchAppService.Add(dto);
            if (!result)
            {
                return ApiRes.Fail(ApiCode.SYS_OPERATION_FAIL_CREATE);
            }
            return ApiRes.Ok();
        }

        /// <summary>
        /// 删除应用
        /// </summary>
        /// <param name="appId"></param>
        /// <returns></returns>
        [HttpDelete, Route("{appId}"), MethodLog("删除应用")]
        [PermissionAuth(PermCode.MCH.ENT_MCH_APP_DEL)]
        public ApiRes Delete(string appId)
        {
            var mchApp = _mchAppService.GetById(appId);
            _mchAppService.Remove(appId);

            // 推送mq到目前节点进行更新数据
            mqSender.Send(ResetIsvMchAppInfoConfigMQ.Build(ResetIsvMchAppInfoConfigMQ.RESET_TYPE_MCH_APP, null, mchApp.MchNo, appId));

            return ApiRes.Ok();
        }

        /// <summary>
        /// 更新应用信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut, Route("{appId}"), MethodLog("更新应用信息")]
        [PermissionAuth(PermCode.MCH.ENT_MCH_APP_EDIT)]
        public ApiRes Update(string appId, MchAppDto dto)
        {
            var result = _mchAppService.Update(dto);
            if (!result)
            {
                return ApiRes.Fail(ApiCode.SYS_OPERATION_FAIL_UPDATE);
            }
            // 推送修改应用消息
            mqSender.Send(ResetIsvMchAppInfoConfigMQ.Build(ResetIsvMchAppInfoConfigMQ.RESET_TYPE_MCH_APP, null, dto.MchNo, dto.AppId));

            return ApiRes.Ok();
        }

        /// <summary>
        /// 应用详情
        /// </summary>
        /// <param name="appId"></param>
        /// <returns></returns>
        [HttpGet, Route("{appId}"), NoLog]
        [PermissionAuth(PermCode.MCH.ENT_MCH_APP_VIEW, PermCode.MCH.ENT_MCH_APP_EDIT)]
        public ApiRes Detail(string appId)
        {
            var mchApp = _mchAppService.GetById(appId);
            if (mchApp == null)
            {
                return ApiRes.Fail(ApiCode.SYS_OPERATION_FAIL_SELETE);
            }
            mchApp.AppSecret = mchApp.AppSecret.Mask();
            return ApiRes.Ok(mchApp);
        }

        /// <summary>
        /// 获取支付网关系统公钥
        /// </summary>
        /// <param name="appId"></param>
        /// <returns></returns>
        [HttpGet, Route("sysRSA2PublicKey"), AllowAnonymous, NoLog]
        public ApiRes SysRSA2PublicKey(string appId)
        {
            var sysRSA2PublicKey = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAi1NJVybvb3CQsK8BRsE9Qql1EmPiTFtokkT7YC8OpcY1ONN4UtVcisQObk2aJL/NFnfm3MQzKzPQ8B3hqoY5Vr2wf5amkZYiSUXYC1VHnaw2Pt2Eje+bhwbS5sWW52lKVev2lgP2vpZDah8WAlgdY4IBQfQ4VYNkoKDBgzmBwzQOWQ5eO7CqWp1tJHxvZSDUleMYAz5gCcVJ4ZBv+3lRAQ3r/RCIXPiyDAu2Y/lGHPrP0yuHN9XxU1uHWQKdy1RHXLfal1Oapv31yF8XqNxNG1sjj91S+F5sdkvR6LLdWM481z0otUyY1+68UJIZmxP3UCfsLP1byj7lKZixDxrJvwIDAQAB";
            return ApiRes.Ok(sysRSA2PublicKey);
        }
    }
}
