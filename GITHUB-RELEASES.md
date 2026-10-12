# Zerafi DPS Monitor releases

This GPL-3.0 customization is based on cyberbadger6969/aion2-dps-meter.
Keep LICENSE and data/NOTICE.md when redistributing it.

Push a version tag such as v1.0.0 to run the Windows release workflow.
It tests the source, builds the app and installer, and publishes a public release
with installer, portable ZIP and full source. Packet captures and userdata are ignored.

The workflow compiles its own GitHub repository name into the update feed.
Local builds default to Zerafithekitten/zerafi-dps-monitor; the workflow uses its own repository.
Installed copies automatically check after startup and every two hours, and apply
an available installer during an idle period. Settings can disable this behavior.
Portable copies offer downloads; use the Setup release for automatic installation.

The custom installer uses a separate app name, installer ID and instance locks.
It does not update an installation of the original upstream meter.
Npcap is downloaded and installed separately from its official website.

Manual build example:
dotnet publish src/AionMeter.App/AionMeter.App.csproj -c Release -r win-x64 --self-contained true -p:Version=1.0.0 -p:UpdateRepository=OWNER/REPOSITORY

The GitHub workflow requires Actions enabled and permission to write releases.
Build results are available in the repository's GitHub Actions tab.


