using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequestDto dto);
    Task<AuthResult> LoginAsync(LoginRequestDto dto);
}
