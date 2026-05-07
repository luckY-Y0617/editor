import { build } from "esbuild";
import { mkdir, rm } from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const appRoot = process.cwd();
const cacheDir = path.join(appRoot, ".comment-test-cache");
const bundlePath = path.join(cacheDir, "comment-tests.mjs");

const testFiles = [
  "src/lib/authClient.test.ts",
  "src/lib/apiClient.test.ts",
  "src/lib/commentAnchorMatching.test.ts",
  "src/lib/commentComposerModel.test.ts",
  "src/lib/commentProductionState.test.ts",
  "src/lib/commentRepository.test.ts",
  "src/lib/commentAnchorRelocation.test.ts",
  "src/lib/commentSelectionUx.test.ts",
  "src/lib/commentThreadState.test.ts",
  "src/lib/documentShareLinksModel.test.ts",
  "src/lib/hashRouting.test.ts",
  "src/lib/i18n.test.ts",
  "src/lib/librariesPageModel.test.ts",
  "src/lib/permissionAdminModel.test.ts",
  "src/lib/searchDiscoveryModel.test.ts",
  "src/lib/workspaceSettingsModel.test.ts",
  "src/lib/workspaceHomeModel.test.ts",
  "src/lib/workspaceUpdatesModel.test.ts",
  "src/extensions/CommentDecorations.test.ts",
  "src/extensions/BlockIdentity.test.ts",
  "src/utils/knowledgeTransfer.test.ts",
];

await rm(cacheDir, { force: true, recursive: true });
await mkdir(cacheDir, { recursive: true });

const entryContents = [
  ...testFiles.map((filePath) => `import "./${filePath.replaceAll("\\", "/")}";`),
  `import { runTests } from "./src/test/harness";`,
  `await runTests();`,
].join("\n");

try {
  await build({
    absWorkingDir: appRoot,
    bundle: true,
    format: "esm",
    logLevel: "silent",
    outfile: bundlePath,
    platform: "node",
    sourcemap: "inline",
    stdin: {
      contents: entryContents,
      loader: "ts",
      resolveDir: appRoot,
      sourcefile: "comment-tests-entry.ts",
    },
    target: "node18",
  });

  await import(`${pathToFileURL(bundlePath).href}?cacheBust=${Date.now()}`);
} finally {
  await rm(cacheDir, { force: true, recursive: true });
}
