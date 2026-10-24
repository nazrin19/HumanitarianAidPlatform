using System;
using System.Collections.Generic;
using System.Text;

using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Requests;

public record RequestStatusHistoryDto(
    RequestStatus? FromStatus,
    RequestStatus ToStatus,
    Guid? ChangedByUserId,
    DateTime ChangedAt,
    string? Note);
