# Install

```powershell
installer\build.ps1      # publish JohnnyCastaway.scr + content bundle into installer\dist
installer\install.ps1    # copy to %LOCALAPPDATA%\JohnnyCastaway and register the saver
installer\uninstall.ps1  # remove and unregister
```

Requires the .NET 10 SDK and a populated `content/` bundle (run `scripts/export_content.py`,
which needs the extracted up-res'd assets). The screensaver reads its `content/` folder from
beside the installed `.scr`.
