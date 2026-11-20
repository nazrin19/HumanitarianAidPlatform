using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int ExpiryMinutes { get; set; } = 60;
}
