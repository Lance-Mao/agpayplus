﻿using AGooday.AgPay.Domain.Interfaces;
using AGooday.AgPay.Domain.Models;
using AGooday.AgPay.Infrastructure.Context;

namespace AGooday.AgPay.Infrastructure.Repositories
{
    public class SysUserRoleRelaRepository : AgPayRepository<SysUserRoleRela>, ISysUserRoleRelaRepository
    {
        public SysUserRoleRelaRepository(AgPayDbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// 当前角色是否已分配到用户
        /// </summary>
        /// <param name="telphone"></param>
        /// <param name="sysType"></param>
        /// <returns></returns>
        public bool IsAssignedToUser(string roleId)
        {
            return GetAllAsNoTracking().Any(c => c.RoleId == roleId);
        }

        public void RemoveByUserId(long userId)
        {
            var entitys = DbSet.Where(w => w.UserId == userId);
            foreach (var entity in entitys)
            {
                DbSet.Remove(entity);
            }
        }
    }
}
