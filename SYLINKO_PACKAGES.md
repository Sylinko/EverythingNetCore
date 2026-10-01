# Sylinko package publishing

`release-sylinko-nuget.yml` publishes the original EverythingNetCore package ID.
The release workflow generates a stable package version as
`999.YYYYMMDD.<github.run_number>`, for example `999.20261001.42`. The date
comes from the original workflow run's creation time in UTC. All packages and
platforms from that run share the same version, including when jobs are rerun.
Run numbers belong to this workflow; keep one publishing workflow per package.
Trigger it through workflow_dispatch (no version input), or push a `publish-*`
source tag. The generated version is written to the workflow summary, and the
publishing action creates a release tagged `nuget/<producer>/<version>`.

The workflow refuses to upload over a release that already contains assets,
because the feed action otherwise uses `--clobber`. If publishing stopped after
uploading assets, retain that release and start a new run to publish a new
version. Check any existing feed PR before completing or abandoning it.
The local draft version `999.0.0` is only for local builds and smoke tests; never
publish it. Package release versions do not change assembly or file versions.

Configure `SYLINKO_NUGET_FEED_TOKEN` as in Sylinko/Microsoft.ML and register
EverythingNetCore in Sylinko/nuget-feed before the first run. The publishing
action creates a GitHub Release and a feed PR; merge that PR to expose the version.

The package includes both x86 and x64 Everything.dll/Everything.exe. Consumer
smoke tests exercise RID-less builds and win-x64 publishes, verify the required
assets, and call the native API without starting or installing an Everything
service. The EXE lookup handles both NuGet output layouts.
The smoke project references the exact locally packed version and uses a private
package cache. It does not install or start an Everything service.

Local pack: `dotnet pack EverythingNet/EverythingNet.csproj -c Release`.
To validate a release-shaped version locally, pass `-p:PackageVersion=999.20261001.42`
and use that same value for the package smoke tests.
Always pack without a RID so both native architectures are included. Never
replace an existing ID/version with different contents.
