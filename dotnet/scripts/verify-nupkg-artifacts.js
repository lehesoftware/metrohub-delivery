#!/usr/bin/env node
/** Fail publish when dotnet pack produced no .nupkg files (semantic-release publishCmd). */
const fs = require("node:fs");
const path = require("node:path");

const dir = path.join(process.cwd(), "artifacts");
if (!fs.existsSync(dir)) {
  console.error("verify-nupkg-artifacts: artifacts/ directory is missing after dotnet pack");
  process.exit(1);
}

const nupkgs = fs.readdirSync(dir).filter((name) => name.endsWith(".nupkg"));
if (nupkgs.length === 0) {
  console.error("verify-nupkg-artifacts: no .nupkg files found in artifacts/ after dotnet pack");
  process.exit(1);
}

console.log(`verify-nupkg-artifacts: found ${nupkgs.length} package(s): ${nupkgs.join(", ")}`);
