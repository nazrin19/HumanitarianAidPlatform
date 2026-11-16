using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Application.Auth;

public class AuthResult
{
    public bool Succeeded { get; private init; }
    public string? Error { get; private init; }
    public AuthResponseDto? Response { get; private init; }

    public static AuthResult Ok(AuthResponseDto response) => new() { Succeeded = true, Response = response };
    public static AuthResult Fail(string error) => new() { Succeeded = false, Error = error };
}
