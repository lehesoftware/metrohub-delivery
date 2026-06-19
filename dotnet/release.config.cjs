/**
 * semantic-release: Conventional Commits → bump MetroHubProvidersDeliveryVersion, pack, push to GitHub Packages NuGet.
 * CI sets NUGET_AUTH_TOKEN from github.token (packages: write); GITHUB_TOKEN is MetroHub Publish app token for git/releases.
 *
 * @type {import('semantic-release').GlobalConfig}
 */
const sln = "MetroHub.Providers.Delivery.slnx";
const packs = [
  "src/MetroHub.Providers.Delivery/MetroHub.Providers.Delivery.csproj",
];
const nugetOwner = process.env.NUGET_GITHUB_OWNER || "lehesoftware";
const nugetSource =
  process.env.NUGET_FEED_URL ||
  `https://nuget.pkg.github.com/${nugetOwner}/index.json`;

module.exports = {
  branches: [
    "main",
    { name: "develop", prerelease: "beta" },
    { name: "feature/wip", prerelease: "wip" },
  ],
  plugins: [
    "@semantic-release/commit-analyzer",
    "@semantic-release/release-notes-generator",
    [
      "@semantic-release/exec",
      {
        prepareCmd:
          "node scripts/bump-metrohub-provider-versions.js ${nextRelease.version}",
        publishCmd: [
          '_BASE="$RUNNER_TEMP"; [ -z "$_BASE" ] && _BASE=/tmp; nuget_cfg="$(mktemp "$_BASE/nuget-release.XXXXXX")"',
          'chmod 600 "$nuget_cfg"',
          'cleanup() { rm -f "$nuget_cfg"; }',
          "trap cleanup EXIT",
          "rm -rf artifacts && mkdir -p artifacts",
          `if [ -n "$NUGET_RESTORE_CONFIGFILE" ]; then dotnet restore ${sln} --configfile "$NUGET_RESTORE_CONFIGFILE"; else dotnet restore ${sln}; fi`,
          `dotnet build ${sln} -c Release --no-restore`,
          ...packs.map(
            (p) => `dotnet pack ${p} -c Release -o artifacts --no-build`,
          ),
          "node scripts/verify-nupkg-artifacts.js",
          'bash scripts/write-nuget-github-credentials-config.sh "$nuget_cfg" metrohub-github ' +
            JSON.stringify(nugetSource),
          'dotnet nuget push artifacts/*.nupkg --source metrohub-github --configfile "$nuget_cfg" --skip-duplicate',
        ].join(" && "),
      },
    ],
    [
      "@semantic-release/git",
      {
        assets: ["Directory.Build.props"],
        message:
          "chore(release): ${nextRelease.version} [skip ci]\n\n${nextRelease.notes}",
      },
    ],
    [
      "@semantic-release/github",
      {
        successComment: false,
        failComment: false,
      },
    ],
  ],
};
