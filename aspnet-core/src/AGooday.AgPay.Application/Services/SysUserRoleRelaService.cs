﻿using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Application.ViewModels;
using AGooday.AgPay.Domain.Commands.SysUsers;
using AGooday.AgPay.Domain.Core.Bus;
using AGooday.AgPay.Domain.Interfaces;
using AGooday.AgPay.Domain.Models;
using AGooday.AgPay.Infrastructure.Repositories;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGooday.AgPay.Application.Services
{
    public class SysUserRoleRelaService : ISysUserRoleRelaService
    {
        // 注意这里是要IoC依赖注入的，还没有实现
        private readonly ISysUserRoleRelaRepository _sysUserRoleRelaRepository;
        // 用来进行DTO
        private readonly IMapper _mapper;
        // 中介者 总线
        private readonly IMediatorHandler Bus;

        public SysUserRoleRelaService(ISysUserRoleRelaRepository sysUserRoleRelaRepository, IMapper mapper, IMediatorHandler bus)
        {
            _sysUserRoleRelaRepository = sysUserRoleRelaRepository;
            _mapper = mapper;
            Bus = bus;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Add(SysUserRoleRelaVM vm)
        {
            var m = _mapper.Map<SysUserRoleRela>(vm);
            _sysUserRoleRelaRepository.Add(m);
        }

        public void Remove(string recordId)
        {
            _sysUserRoleRelaRepository.Remove(recordId);
        }

        public void Update(SysUserRoleRelaVM vm)
        {
            var m = _mapper.Map<SysUserRoleRela>(vm);
            _sysUserRoleRelaRepository.Update(m);
        }

        public SysUserRoleRelaVM GetById(string recordId)
        {
            var entity = _sysUserRoleRelaRepository.GetById(recordId);
            var vm = _mapper.Map<SysUserRoleRelaVM>(entity);
            return vm;
        }

        public IEnumerable<SysUserRoleRelaVM> GetAll()
        {
            var sysUserRoleRelas = _sysUserRoleRelaRepository.GetAll();
            return _mapper.Map<IEnumerable<SysUserRoleRelaVM>>(sysUserRoleRelas);
        }
    }
}
