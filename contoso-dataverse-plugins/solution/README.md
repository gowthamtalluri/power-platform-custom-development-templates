# Dataverse solution source

Keep the unpacked solution here so solution changes are reviewable diffs.

```bash
pac solution sync --solution-folder solution/Contoso_Core --async
pac solution pack --folder solution/Contoso_Core --zipfile solution/Contoso_Core_managed.zip --packagetype Managed
```

Never commit the packed `.zip` — CD builds it from source.
