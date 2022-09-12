﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGooday.AgPay.Domain.Models
{
    /// <summary>
    /// 系统角色表
    /// </summary>
    [Comment("系统角色表")]
    [Table("t_sys_role")]
    public class SysRole
    {
        /// <summary>
        /// 角色ID, ROLE_开头
        /// </summary>
        [Comment("角色ID, ROLE_开头")]
        [Key, Required, Column("role_id", TypeName = "varchar(32)")]
        public string RoleId { get; set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        [Comment("角色名称")]
        [Required, Column("role_name", TypeName = "varchar(32)")]
        public string RoleName { get; set; }

        /// <summary>
        /// 所属系统： MGR-运营平台, MCH-商户中心
        /// </summary>
        [Comment("所属系统： MGR-运营平台, MCH-商户中心")]
        [Required, Column("sys_type", TypeName = "varchar(8)")]
        public string SysType { get; set; }

        /// <summary>
        /// 所属商户ID / 0(平台)
        /// </summary>
        [Comment("所属商户ID / 0(平台)")]
        [Required, Column("belong_info_id", TypeName = "varchar(64)")]
        public string BelongInfoId { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Comment("更新时间")]
        [Required, Column("updated_at", TypeName = "timestamp(6)")]
        public DateTime UpdatedAt { get; set; }
    }
}
