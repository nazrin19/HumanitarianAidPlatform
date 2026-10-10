using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Application.Requests;

public interface IRequestService
{
    Task<RequestResponseDto> SubmitAsync(CreateRequestDto dto);
    Task<RequestResponseDto?> GetByIdAsync(Guid id);
}
