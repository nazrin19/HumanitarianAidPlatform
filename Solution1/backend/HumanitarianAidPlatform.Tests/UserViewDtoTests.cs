using System.Text.Json;
using HumanitarianAidPlatform.Application.UserViews;
using Xunit;

namespace HumanitarianAidPlatform.Tests;

public class UserViewDtoTests
{
    [Fact]
    public void DonorViewDoesNotExposeContactFields()
    {
        var view = new DonorUserViewDto(
            Guid.NewGuid(),
            "A Donor",
            "Individual",
            true,
            [],
            []);

        var json = JsonSerializer.Serialize(view);

        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phone", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VolunteerViewDoesNotExposeContactFields()
    {
        var view = new VolunteerUserViewDto(
            Guid.NewGuid(),
            "A Volunteer",
            "First aid",
            "Available",
            []);

        var json = JsonSerializer.Serialize(view);

        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phone", json, StringComparison.OrdinalIgnoreCase);
    }
}
