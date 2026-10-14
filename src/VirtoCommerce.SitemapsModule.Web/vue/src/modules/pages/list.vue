<template>
  <VcBlade
    :title="$t('SITEMAPS.TITLE')"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <div class="sitemaps-inline-form">
      <VcInput
        v-model="baseUrl"
        :label="$t('SITEMAPS.BASE_URL')"
        :hint="$t('SITEMAPS.BASE_URL_HINT')"
        placeholder="https://"
        type="url"
        :error="!!baseUrlError"
        :error-message="baseUrlError"
      />
    </div>
    <VcDataTable
      v-model:selection="selection"
      selection-mode="multiple"
      :items="items"
      :total-count="totalCount"
      :state-key="'STORE_SITEMAPS'"
      :active-item-id="activeChildParam"
      :empty-state="{ icon: 'lucide-network', title: $t('SITEMAPS.EMPTY') }"
      @row-click="(e: { data: Sitemap }) => openDetails(e.data.id)"
    >
      <VcColumn
        id="location"
        :title="$t('SITEMAPS.FIELDS.LOCATION')"
        always-visible
      />
      <VcColumn
        id="urlTemplate"
        :title="$t('SITEMAPS.FIELDS.URL_TEMPLATE')"
      />
      <VcColumn
        id="totalItemsCount"
        :title="$t('SITEMAPS.FIELDS.ITEMS')"
        width="100px"
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { useApiClient, useAsync, useBlade, usePopup, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcInput } from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { SitemapsModuleApiClient, type Sitemap } from "../../api_client/virtocommerce.sitemaps";
import { api } from "../api";

defineBlade({
  url: "/sitemaps",
  name: "SitemapsList",
});

const { t } = useI18n({ useScope: "global" });
const { param, expanded, openBlade, callParent, closeChildren, exposeToChildren, activeChildParam } = useBlade();
const { showConfirmation } = usePopup();
const { getApiClient } = useApiClient(SitemapsModuleApiClient);

const items = ref<Sitemap[]>([]);
const totalCount = ref(0);
const selection = ref<Sitemap[]>([]);
// the base URL the sitemap files are generated for (the AngularJS blade asked for it in a dialog): the store URL by default
const baseUrl = ref("");
// the server job needs an absolute URL: without one it fails before it reports back
const baseUrlError = computed(() => (baseUrl.value && !/^https?:\/\/[^\s/]+\S*$/i.test(baseUrl.value) ? t("SITEMAPS.ERRORS.BASE_URL") : ""));
const canRun = computed(() => !!totalCount.value && !!baseUrl.value && !baseUrlError.value);

const { loading, action: load } = useAsync(async () => {
  const result = await (await getApiClient()).searchSitemaps({ storeId: param.value, take: 1000 });
  items.value = result.results ?? [];
  totalCount.value = result.totalCount ?? 0;
});

// download (zip) or export to the store's assets: a background job, followed in a child blade
function runJob(action: "download" | "export") {
  openBlade({
    name: "SitemapJob",
    param: param.value,
    options: { action, baseUrl: baseUrl.value, sitemapIds: selection.value.map((x) => x.id!) },
  });
}

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("SITEMAPS.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
  { id: "add", title: t("SITEMAPS.TOOLBAR.ADD"), icon: "lucide-plus", permissions: "sitemaps:create", clickHandler: () => openDetails() },
  {
    id: "download",
    title: computed(() => t(selection.value.length ? "SITEMAPS.DOWNLOAD_SELECTED" : "SITEMAPS.DOWNLOAD")),
    icon: "lucide-download",
    disabled: computed(() => !canRun.value),
    clickHandler: () => runJob("download"),
  },
  {
    id: "export",
    title: t("SITEMAPS.EXPORT"),
    icon: "lucide-upload",
    permissions: "sitemaps:exportToStoreAssets",
    disabled: computed(() => !canRun.value),
    clickHandler: () => runJob("export"),
  },
  {
    id: "delete",
    title: t("SITEMAPS.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    permissions: "sitemaps:delete",
    disabled: computed(() => !selection.value.length),
    async clickHandler() {
      // a deleted sitemap's details blade would re-create it on save: close it first, as the AngularJS blade did
      await closeChildren();
      if (!expanded.value) return; // the user kept its unsaved changes
      if (await showConfirmation(t("SITEMAPS.ALERTS.DELETE_SELECTED", { count: selection.value.length }))) {
        await (await getApiClient()).deleteSitemap(selection.value.map((x) => x.id!));
        selection.value = [];
        await reload();
      }
    },
  },
]);

function openDetails(id?: string) {
  openBlade({ name: "SitemapDetails", param: id, options: { storeId: param.value } });
}

async function reload() {
  await load();
  await callParent("reloadWidgets").catch(() => undefined);
}

onMounted(async () => {
  const [store] = await Promise.all([
    api.get<{ url?: string; secureUrl?: string }>(`/api/stores/${encodeURIComponent(param.value!)}`).catch(() => undefined),
    load(),
  ]);
  baseUrl.value ||= store?.url || store?.secureUrl || "";
});
exposeToChildren({ reload });
</script>
