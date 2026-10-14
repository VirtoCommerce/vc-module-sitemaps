import * as blades from "./pages";
import * as locales from "./locales";
import { defineAppModule, registerExternalWidget } from "@vc-shell/framework";
import { markRaw } from "vue";
import StoreSitemapsWidget from "./widgets/StoreSitemapsWidget.vue";
import "./styles.css";

// The AngularJS module's sitemaps widget on the store blade ("storeDetail")
registerExternalWidget({
  id: "SitemapsStore",
  title: "Sitemaps",
  component: markRaw(StoreSitemapsWidget),
  targetBlades: ["StoreDetails"],
  // not on a store that isn't saved yet
  isVisible: (blade) => !!blade?.param,
});

export default defineAppModule({
  blades,
  locales,
});
