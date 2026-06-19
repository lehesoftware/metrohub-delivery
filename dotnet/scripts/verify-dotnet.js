#!/usr/bin/env node
/** Cross-platform restore/build/test for npm verify (Release job parity with validate.yml). */
const { spawnSync } = require("node:child_process");

const sln = process.env.SOLUTION_FILE || "MetroHub.Providers.Delivery.slnx";
const config = "Release";

function run(command, args) {
  const result = spawnSync(command, args, { stdio: "inherit", shell: false });
  if (result.status !== 0) {
    process.exit(result.status ?? 1);
  }
}

run("dotnet", [
  "restore",
  sln,
  ...(process.env.NUGET_RESTORE_CONFIGFILE
    ? ["--configfile", process.env.NUGET_RESTORE_CONFIGFILE]
    : []),
]);
run("dotnet", ["build", sln, "-c", config, "--no-restore"]);
run("dotnet", ["test", sln, "-c", config, "--no-build"]);
