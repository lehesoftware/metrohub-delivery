/**
 * PR preview only — minimal plugins so PR-controlled release.config.cjs
 * cannot run exec/git/github with privileged tokens in the dry-run step.
 *
 * @type {import('semantic-release').GlobalConfig}
 */
module.exports = {
  branches: [
    "main",
    { name: "develop", prerelease: "beta" },
    { name: "feature/wip", prerelease: "wip" },
  ],
  plugins: ["@semantic-release/commit-analyzer", "@semantic-release/release-notes-generator"],
};
