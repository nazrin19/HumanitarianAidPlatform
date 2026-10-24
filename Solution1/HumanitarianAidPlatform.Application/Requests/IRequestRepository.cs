using System;
using System.Collections.Generic;
using System.Text;

using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Requests;

public interface IRequestRepository
{
    Task AddAsync(AssistanceRequest request);
    Task<AssistanceRequest?> GetByIdAsync(Guid id);
}
