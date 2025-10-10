# VideoManager — Clean API (JWT, Error Handling, Tests)

**Zweck:** Kleine, fokussierte API, um Prinzipien & Herangehensweise zu zeigen:
- Clean Architecture-Light, SOLID, DI
- Fehlerbehandlung via RFC7807 (Problem Details)
- Auth: JWT + Refresh (Endpoints in `AuthController`)
- Tests: xUnit Unit 

## Run
```bash
dotnet build
dotnet run --project src/VideoManager.Api
