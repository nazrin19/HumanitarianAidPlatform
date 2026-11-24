# HumanitarianAidPlatform

## Run the app on your own computer

Pushing to GitHub does not push the SQL LocalDB database or JWT secrets. Each teammate must start the backend, create its local database, and configure the same JWT settings.

### 1. Restore and configure the backend

Open PowerShell in `Solution1\backend`, then run:

```powershell
dotnet restore
dotnet user-secrets init --project .\WebApplication1\HumanitarianAidPlatform.Api.csproj
dotnet user-secrets set "Jwt:Key" "<a-random-secret-at-least-32-characters>" --project .\WebApplication1\HumanitarianAidPlatform.Api.csproj
dotnet ef database update --project .\HumanitarianAidPlatform.Infrastructure\HumanitarianAidPlatform.Infrastructure.csproj --startup-project .\WebApplication1\HumanitarianAidPlatform.Api.csproj
```

Use a different secret only when testing independently. Do not use a real production key in GitHub or commit a secret to source control.

### 2. Start the backend

```powershell
dotnet run --project .\WebApplication1\HumanitarianAidPlatform.Api.csproj
```

The API normally listens at `http://localhost:5001`.

### 3. Start the frontend

Open another PowerShell window in `Solution1\frontend`, then run:

```powershell
npm install
npm run dev
```

Open `http://localhost:3000`. The `/` route is the public homepage. Use `/login` to sign in.

If `localhost:5001` is not reachable, make sure the backend terminal is still running. Both frontend and backend must run on the same computer for the default local setup.

## Reset a local Admin password

If you forget the local Admin password, stop the running API with `Ctrl+C`, open PowerShell in `Solution1\backend`, and run:

```powershell
dotnet run --project .\WebApplication1\HumanitarianAidPlatform.Api.csproj -- --reset-admin-password
```

Enter the existing active Admin email, then enter and confirm a new password of at least 12 characters. Password input is hidden. This local command updates only that Admin's password hash; it does not expose a password-reset API or change other database records. Start the API normally afterward with:

```powershell
dotnet run --project .\WebApplication1\HumanitarianAidPlatform.Api.csproj
```