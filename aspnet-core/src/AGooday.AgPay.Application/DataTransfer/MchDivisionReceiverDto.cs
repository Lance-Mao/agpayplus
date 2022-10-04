﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGooday.AgPay.Application.DataTransfer
{
    /// <summary>
    /// 商户分账接收者账号绑定关系表
    /// </summary>
    public class MchDivisionReceiverDto
    {
        /// <summary>
        /// 分账接收者ID
        /// </summary>
        public long? ReceiverId { get; set; }

        /// <summary>
        /// 接收者账号别名
        /// </summary>
        public string ReceiverAlias { get; set; }

        /// <summary>
        /// 组ID（便于商户接口使用）
        /// </summary>
        public long? ReceiverGroupId { get; set; }

        /// <summary>
        /// 组名称
        /// </summary>
        public string ReceiverGroupName { get; set; }

        /// <summary>
        /// 商户号
        /// </summary>
        public string MchNo { get; set; }

        /// <summary>
        /// 服务商号
        /// </summary>
        public string IsvNo { get; set; }

        /// <summary>
        /// 应用iD
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// 支付接口代码
        /// </summary>
        public string IfCode { get; set; }

        /// <summary>
        /// 分账接收账号类型: 0-个人(对私) 1-商户(对公)
        /// </summary>
        public byte AccType { get; set; }

        /// <summary>
        /// 分账接收账号
        /// </summary>
        public string AccNo { get; set; }

        /// <summary>
        /// 分账接收账号名称
        /// </summary>
        public string AccName { get; set; }

        /// <summary>
        /// 分账关系类型（参考微信）， 如： SERVICE_PROVIDER 服务商等
        /// </summary>
        public string RelationType { get; set; }

        /// <summary>
        /// 当选择自定义时，需要录入该字段。 否则为对应的名称
        /// </summary>
        public string RelationTypeName { get; set; }

        /// <summary>
        /// 分账比例
        /// </summary>
        public decimal DivisionProfit { get; set; }

        /// <summary>
        /// 分账状态（本系统状态，并不调用上游关联关系）: 1-正常分账, 0-暂停分账
        /// </summary>
        public byte State { get; set; }

        /// <summary>
        /// 上游绑定返回信息，一般用作查询绑定异常时的记录
        /// </summary>
        public string ChannelBindResult { get; set; }

        /// <summary>
        /// 渠道特殊信息
        /// </summary>
        public string ChannelExtInfo { get; set; }

        /// <summary>
        /// 绑定成功时间
        /// </summary>
        public DateTime BindSuccessTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
