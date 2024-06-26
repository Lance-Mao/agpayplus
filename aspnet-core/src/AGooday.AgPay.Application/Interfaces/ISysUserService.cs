﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Common.Models;

namespace AGooday.AgPay.Application.Interfaces
{
    public interface ISysUserService : IAgPayService<SysUserDto, long>
    {
        Task CreateAsync(SysUserCreateDto dto);
        Task RemoveAsync(long sysUserId, long currentUserId, string sysType);
        void ModifyCurrentUserInfo(ModifyCurrentUserInfoDto user);
        Task ModifyAsync(SysUserModifyDto dto);
        SysUserDto GetByKeyAsNoTracking(long recordId);
        IEnumerable<SysUserDto> GetByBelongInfoIdAsNoTracking(string belongInfoId);
        SysUserDto GetById(long recordId, string belongInfoId);
        bool IsExistTelphone(string telphone, string sysType);
        SysUserDto GetByTelphone(string telphone, string sysType);
        IEnumerable<SysUserDto> GetByIds(List<long> recordIds);
        PaginatedList<SysUserListDto> GetPaginatedData(SysUserQueryDto dto, long? currentUserId);
        Task<PaginatedList<SysUserListDto>> GetPaginatedDataAsync(SysUserQueryDto dto, long? currentUserId);
    }
}
