#!/usr/bin/env node
/**
 * Sets MetroHubProvidersDeliveryVersion in Directory.Build.props (semantic-release exec prepareCmd).
 * @param {string} version Semver from semantic-release (e.g. 1.2.3)
 */
const fs = require("node:fs");
const path = require("node:path");

const version = process.argv[2];
const semverLoose =
  /^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$/;
if (!version || !semverLoose.test(version)) {
  console.error("Usage: node bump-metrohub-provider-versions.js <semver>");
  process.exit(1);
}

const propsPath = path.join(__dirname, "..", "Directory.Build.props");
let text;
try {
  text = fs.readFileSync(propsPath, "utf8");
} catch (err) {
  console.error(`Failed to read ${propsPath}:`, err && err.message ? err.message : err);
  process.exit(1);
}

const requiredTags = ["MetroHubProvidersDeliveryVersion"];
const re = /<(MetroHubProviders\w+Version)>([^<]*)<\/\1>/g;
const updatedTags = new Set();
const next = text.replace(re, (match, tag) => {
  updatedTags.add(tag);
  return `<${tag}>${version}</${tag}>`;
});

if (updatedTags.size === 0) {
  console.error("No MetroHubProviders*Version elements found in Directory.Build.props.");
  process.exit(1);
}
const missing = requiredTags.filter((tag) => !updatedTags.has(tag));
if (missing.length > 0) {
  console.error(
    `Directory.Build.props is missing required version element(s): ${missing.join(", ")}.`,
  );
  process.exit(1);
}

try {
  fs.writeFileSync(propsPath, next);
} catch (err) {
  console.error(`Failed to write ${propsPath}:`, err && err.message ? err.message : err);
  process.exit(1);
}
console.log(
  `Set ${updatedTags.size} MetroHubProviders*Version element(s) to ${version} (${requiredTags.join(", ")}).`,
);
