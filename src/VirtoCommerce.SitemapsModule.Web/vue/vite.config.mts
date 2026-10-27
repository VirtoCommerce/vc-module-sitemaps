import path from "node:path";
import { getDynamicModuleConfiguration } from "@vc-shell/mf-module";

// Sitemaps of stores: a Module Federation remote of the Admin Shell host.
// Output: <module>/plugins/admin-shell/ (remoteEntry.js + public/plugin.json),
// served by the platform at /modules/$(VirtoCommerce.Sitemaps)/plugins/admin-shell/.
const base = "/modules/$(VirtoCommerce.Sitemaps)/plugins/admin-shell/";
process.env.APP_BASE_PATH = base;

export default getDynamicModuleConfiguration({
  appId: "admin-shell",
  moduleRoot: path.resolve(__dirname, ".."),
  entry: "./src/modules/index.ts",
  remoteName: "sitemaps_admin",
  base,
});
