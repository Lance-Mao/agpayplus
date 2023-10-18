﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Common.Models;

namespace AGooday.AgPay.Application.Interfaces
{
    public interface IAgentInfoService : IDisposable
    {
        bool IsExistAgentNo(string mchNo);
        bool IsExistAgent(string isvNo);
        bool Add(AgentInfoDto dto);
        Task CreateAsync(AgentInfoCreateDto dto);
        Task RemoveAsync(string recordId);
        bool Update(AgentInfoDto dto);
        bool UpdateById(AgentInfoUpdateDto dto);
        Task ModifyAsync(AgentInfoModifyDto dto);
        AgentInfoDto GetById(string recordId);
        IEnumerable<AgentInfoDto> GetAll();
        IEnumerable<AgentInfoDto> GetParents(string agentNo);
        PaginatedList<AgentInfoDto> GetPaginatedData(AgentInfoQueryDto dto);
        PaginatedList<AgentInfoDto> GetPaginatedData(string agentNo, AgentInfoQueryDto dto);
    }
}
