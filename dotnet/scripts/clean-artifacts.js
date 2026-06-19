#!/usr/bin/env node
/** Cross-platform artifacts/ reset before dotnet pack (semantic-release publishCmd). */
const fs = require("node:fs");
const path = require("node:path");

const dir = path.join(process.cwd(), "artifacts");
fs.rmSync(dir, { recursive: true, force: true });
fs.mkdirSync(dir, { recursive: true });
console.log(`Artifacts directory reset: ${dir}`);
