using System.ComponentModel.DataAnnotations;
using HumanitarianAidPlatform.Application.Auth;
using Xunit;

namespace HumanitarianAidPlatform.Tests;

public class ApplicationFormValidationTests
{
    [Fact]
    public void DonorFormRejectsUnknownDonorType()
    {
        var request = new RegisterDonorRequest
        {
            FullName = "A Donor",
            Email = "donor@example.com",
            Password = "password123",
            Type = "Unknown"
        };

        var errors = Validate(request);

        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(RegisterDonorRequest.Type)));
    }

    [Fact]
    public void VolunteerFormRejectsUnknownAvailability()
    {
        var request = new RegisterVolunteerRequest
        {
            FullName = "A Volunteer",
            Email = "volunteer@example.com",
            Password = "password123",
            Availability = "Unknown"
        };

        var errors = Validate(request);

        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(RegisterVolunteerRequest.Availability)));
    }

    [Fact]
    public void VolunteerFormRequiresNameEmailAndPassword()
    {
        var request = new RegisterVolunteerRequest
        {
            FullName = "",
            Email = "not-an-email",
            Password = "short"
        };

        var errors = Validate(request);

        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(RegisterVolunteerRequest.FullName)));
        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(RegisterVolunteerRequest.Email)));
    }

    private static List<ValidationResult> Validate(object request)
    {
        var context = new ValidationContext(request);
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(request, context, errors, validateAllProperties: true);
        return errors;
    }
}
