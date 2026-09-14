# Signing key

Dataverse requires plugin assemblies to be strong-named.

**The production key is NOT in source control.** Generate a local development key:

```powershell
sn -k Contoso.Crm.snk
```

or cross-platform:

```bash
dotnet tool install -g dotnet-strong-name   # or use `sn.exe` from the Windows SDK
```

CI injects the real key from the `SIGNING_KEY_BASE64` GitHub secret before build.
Rotating the key changes the assembly public key token and requires re-registering
the plugin assembly in every environment — treat it as a breaking change.
