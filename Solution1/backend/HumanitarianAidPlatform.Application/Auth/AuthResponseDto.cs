using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Application.Auth;

public record AuthResponseDto(string Token, DateTime ExpiresAt, string Email, string FullName, string Role);
