using System;
using System.Collections.Generic;
using System.Text;

using HumanitarianAidPlatform.Domain.Entities.Users;

namespace HumanitarianAidPlatform.Application.Auth;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) Generate(User user, string roleName);
}
