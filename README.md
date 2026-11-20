# HumanitarianAidPlatform

## Reset a local Admin password

If you forget the local Admin password, stop the running API with `Ctrl+C`, open PowerShell in `Solution1\backend`, and run:

```powershell
dotnet run --project .\WebApplication1\HumanitarianAidPlatform.Api.csproj -- --reset-admin-password
```

Enter the existing active Admin email, then enter and confirm a new password of at least 12 characters. Password input is hidden. This local command updates only that Admin's password hash; it does not expose a password-reset API or change other database records. Start the API normally afterward with:

```powershell
dotnet run --project .\WebApplication1\HumanitarianAidPlatform.Api.csproj
```