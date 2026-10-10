using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Application.Requests;

public record RequestResponseDto(
    Guid Id,
    string Category,
    string Description,
    string Status,
    double? Latitude,
    double? Longitude,
    string? Address,
    DateTime CreatedAt);
