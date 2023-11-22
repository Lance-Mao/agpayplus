﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Common.Models;
using AGooday.AgPay.Domain.Commands.SysUsers;
using AGooday.AgPay.Domain.Core.Bus;
using AGooday.AgPay.Domain.Interfaces;
using AGooday.AgPay.Domain.Models;
using AGooday.AgPay.Domain.Queries;
using AGooday.AgPay.Domain.Queries.SysUsers;
using AutoMapper;

namespace AGooday.AgPay.Application.Services
{
    /// <summary>
    /// 系统操作员表 服务实现类
    /// </summary>
    public class SysUserService : ISysUserService
    {
        // 注意这里是要IoC依赖注入的，还没有实现
        private readonly ISysUserRepository _sysUserRepository;
        private readonly ISysUserTeamRepository _sysUserTeamRepository;
        // 用来进行DTO
        private readonly IMapper _mapper;
        // 中介者 总线
        private readonly IMediatorHandler Bus;

        public SysUserService(IMapper mapper, IMediatorHandler bus,
            ISysUserRepository sysUserRepository,
            ISysUserTeamRepository sysUserTeamRepository)
        {
            _mapper = mapper;
            Bus = bus;
            _sysUserRepository = sysUserRepository;
            _sysUserTeamRepository = sysUserTeamRepository;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Add(SysUserDto dto)
        {
            var m = _mapper.Map<SysUser>(dto);
            _sysUserRepository.Add(m);
            _sysUserRepository.SaveChanges();
        }

        public async Task CreateAsync(SysUserCreateDto dto)
        {
            var command = _mapper.Map<CreateSysUserCommand>(dto);
            await Bus.SendCommand(command);
        }

        public void Remove(long recordId)
        {
            _sysUserRepository.Remove(recordId);
            _sysUserRepository.SaveChanges();
        }

        public async Task RemoveAsync(long sysUserId, long currentUserId, string sysType)
        {
            var command = new RemoveSysUserCommand()
            {
                SysUserId = sysUserId,
                CurrentSysUserId = currentUserId,
                SysType = sysType
            };
            await Bus.SendCommand(command);
        }

        public void Update(SysUserDto dto)
        {
            var renew = _mapper.Map<SysUser>(dto);
            renew.UpdatedAt = DateTime.Now;
            _sysUserRepository.Update(renew);
            _sysUserRepository.SaveChanges();
        }

        public void ModifyCurrentUserInfo(ModifyCurrentUserInfoDto dto)
        {
            var user = _sysUserRepository.GetByUserId(dto.SysUserId);
            if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
                user.AvatarUrl = dto.AvatarUrl;
            if (!string.IsNullOrWhiteSpace(dto.Realname))
                user.Realname = dto.Realname;
            if (!string.IsNullOrWhiteSpace(dto.SafeWord))
                user.SafeWord = dto.SafeWord;
            if (dto.Sex > 0)
                user.Sex = dto.Sex;
            user.UpdatedAt = DateTime.Now;
            _sysUserRepository.Update(user);
            _sysUserRepository.SaveChanges();
        }

        public async Task ModifyAsync(SysUserModifyDto dto)
        {
            var command = _mapper.Map<ModifySysUserCommand>(dto);
            await Bus.SendCommand(command);
        }

        public SysUserDto GetByKeyAsNoTracking(long recordId)
        {
            var entity = _sysUserRepository.GetByKeyAsNoTracking(recordId);
            var dto = _mapper.Map<SysUserDto>(entity);
            return dto;
        }

        public SysUserDto GetById(long recordId)
        {
            var entity = _sysUserRepository.GetById(recordId);
            var dto = _mapper.Map<SysUserDto>(entity);
            return dto;
        }

        public async Task<SysUserDto> GetByIdAsync(long recordId)
        {
            var entity = await Bus.SendQuery(new GetByIdQuery<SysUser, long>(recordId));
            var dto = _mapper.Map<SysUserDto>(entity);
            return dto;
        }

        public SysUserDto GetById(long recordId, string belongInfoId)
        {
            var entity = _sysUserRepository.GetAll().Where(w => w.SysUserId.Equals(recordId) && w.BelongInfoId.Equals(belongInfoId)).FirstOrDefault();
            var dto = _mapper.Map<SysUserDto>(entity);
            return dto;
        }

        public IEnumerable<SysUserDto> GetByIds(List<long> recordIds)
        {
            var sysUsers = _sysUserRepository.GetAll().Where(w => recordIds.Contains(w.SysUserId));
            return _mapper.Map<IEnumerable<SysUserDto>>(sysUsers);
        }

        public bool IsExistTelphone(string telphone, string sysType)
        {
            return _sysUserRepository.IsExistTelphone(telphone, sysType);
        }

        public SysUserDto GetByTelphone(string telphone, string sysType)
        {
            var entity = _sysUserRepository.GetByTelphone(telphone, sysType);
            var dto = _mapper.Map<SysUserDto>(entity);
            return dto;
        }

        public IEnumerable<SysUserDto> GetAll()
        {
            //第一种写法 Map
            var sysUsers = _sysUserRepository.GetAll();
            return _mapper.Map<IEnumerable<SysUserDto>>(sysUsers);

            //第二种写法 ProjectTo
            //return (_UsersRepository.GetAll()).ProjectTo<SysUserVM>(_mapper.ConfigurationProvider);
        }

        public PaginatedList<SysUserListDto> GetPaginatedData(SysUserQueryDto dto, long? currentUserId)
        {
            var sysUsers = (from u in _sysUserRepository.GetAllAsNoTracking()
                            join ut in _sysUserTeamRepository.GetAllAsNoTracking() on u.TeamId equals ut.TeamId into temp
                            from team in temp.DefaultIfEmpty()
                            where (string.IsNullOrWhiteSpace(dto.SysType) || u.SysType.Equals(dto.SysType))
                            && (string.IsNullOrWhiteSpace(dto.BelongInfoId) || u.BelongInfoId.Contains(dto.BelongInfoId))
                            && (string.IsNullOrWhiteSpace(dto.Realname) || u.Realname.Contains(dto.Realname))
                            && (dto.UserType.Equals(0) || u.UserType.Equals(dto.UserType))
                            && (dto.SysUserId.Equals(0) || u.SysUserId.Equals(dto.SysUserId))
                            && (currentUserId == null || !u.SysUserId.Equals(currentUserId))
                            select new { u, team }).ToList().Select(s =>
                            {
                                var item = _mapper.Map<SysUserListDto>(s.u);
                                item.TeamName = s.team?.TeamName;
                                return item;
                            }).OrderByDescending(o => o.CreatedAt);

            var records = PaginatedList<SysUserListDto>.Create(sysUsers, dto.PageNumber, dto.PageSize);
            return records;
        }

        public async Task<PaginatedList<SysUserListDto>> GetPaginatedDataAsync(SysUserQueryDto dto, long? currentUserId)
        {
            var query = _mapper.Map<SysUserQuery>(dto);
            query.CurrentUserId = currentUserId;
            var sysUsers = (IEnumerable<(SysUser SysUser, SysUserTeam SysUserTeam)>)await Bus.SendQuery(query);
            var result = sysUsers.Select(s =>
            {
                var item = _mapper.Map<SysUserListDto>(s.SysUser);
                item.TeamName = s.SysUserTeam?.TeamName;
                return item;
            });
            var records = PaginatedList<SysUserListDto>.Create(result, dto.PageNumber, dto.PageSize);
            return records;
        }
    }
}
