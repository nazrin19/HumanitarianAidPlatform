using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Auth;

public class RegisterRequestDto
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = default!;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = default!;

    [Required, StringLength(100)]
    public string FullName { get; set; } = default!;
}
