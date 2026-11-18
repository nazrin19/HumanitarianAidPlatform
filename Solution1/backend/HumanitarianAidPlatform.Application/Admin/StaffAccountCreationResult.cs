namespace HumanitarianAidPlatform.Application.Admin;

public enum StaffAccountCreationError
{
    None,
    EmailAlreadyExists,
    InvalidRole
}

public sealed record StaffAccountCreationResult(
    bool Succeeded,
    AdminAccountResponse? Account,
    StaffAccountCreationError Error)
{
    public static StaffAccountCreationResult Ok(AdminAccountResponse account) =>
        new(true, account, StaffAccountCreationError.None);

    public static StaffAccountCreationResult Fail(StaffAccountCreationError error) =>
        new(false, null, error);
}
