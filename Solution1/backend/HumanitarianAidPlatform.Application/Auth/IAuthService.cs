using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken);
    Task<RegistrationResult> RegisterVolunteerAsync(RegisterVolunteerRequest dto, CancellationToken cancellationToken);
    Task<RegistrationResult> RegisterDonorAsync(RegisterDonorRequest dto, CancellationToken cancellationToken);
    Task<AuthResult> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken);
}
