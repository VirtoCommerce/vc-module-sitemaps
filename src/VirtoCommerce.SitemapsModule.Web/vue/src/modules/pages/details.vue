<template>
  <VcBlade
    :title="isNew ? $t('SITEMAPS.NEW_TITLE') : item.location ?? ''"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcContainer>
      <VcForm class="sitemaps-form tw-space-y-4">
        <VcCard :header="$t('SITEMAPS.GENERAL')">
          <div class="sitemaps-card">
            <VcInput
              v-model="item.location"
              :label="$t('SITEMAPS.FIELDS.LOCATION')"
              :hint="$t('SITEMAPS.LOCATION_HINT')"
              :error="!!locationError"
              :error-message="locationError"
              required
            />
            <VcInput
              v-model="item.urlTemplate"
              :label="$t('SITEMAPS.FIELDS.URL_TEMPLATE')"
              :error="!!templateError"
              :error-message="templateError"
              required
            />
            <VcSelect
              v-model="item.sitemapMode"
              :label="$t('SITEMAPS.FIELDS.MODE')"
              :options="modes"
              :clearable="false"
            />
          </div>
        </VcCard>
        <VcCard
          v-if="!isNew"
          :header="$t('SITEMAPS.FIELDS.ITEMS')"
        >
          <VcDataTable
            v-model:selection="selection"
            selection-mode="multiple"
            :items="sitemapItems"
            :total-count="itemsTotal"
            :pagination="pagination"
            :state-key="'STORE_SITEMAP_ITEMS'"
            :empty-state="{ icon: 'lucide-link', title: $t('SITEMAPS.NO_ITEMS') }"
            @pagination-click="pagination.goToPage"
          >
            <VcColumn
              id="imageUrl"
              :title="$t('SITEMAPS.FIELDS.IMAGE')"
              type="image"
              width="72px"
            />
            <VcColumn
              id="title"
              :title="$t('SITEMAPS.FIELDS.TITLE')"
              always-visible
            />
            <VcColumn
              id="objectType"
              :title="$t('SITEMAPS.FIELDS.TYPE')"
            />
          </VcDataTable>
        </VcCard>
      </VcForm>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import {
  useApiClient,
  useAsync,
  useBlade,
  useBladeForm,
  useDataTablePagination,
  useLoading,
  usePopup,
  type IBladeToolbar,
} from "@vc-shell/framework";
import { VcBlade, VcCard, VcColumn, VcContainer, VcDataTable, VcForm, VcInput, VcSelect } from "@vc-shell/framework/ui";
import { computed, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { SitemapsModuleApiClient, type Sitemap, type SitemapItem } from "../../api_client/virtocommerce.sitemaps";
import { toOptions } from "../api";

defineBlade({
  url: "/sitemap",
  name: "SitemapDetails",
});

const { t } = useI18n({ useScope: "global" });
const { param, options, callParent, openBlade, replaceWith, exposeToChildren } = useBlade<{ storeId: string }>();
const { showConfirmation } = usePopup();
const { getApiClient } = useApiClient(SitemapsModuleApiClient);

const isNew = computed(() => !param.value);
const item = ref<Sitemap>({ location: "sitemap/", urlTemplate: "{slug}", sitemapMode: "Full" as Sitemap["sitemapMode"], storeId: options.value?.storeId });
const sitemapItems = ref<SitemapItem[]>([]);
const selection = ref<SitemapItem[]>([]);
const itemsTotal = ref(0);
const taken = ref(false);
const modes = toOptions(["Full", "OnlyProducts", "OnlyCategories"]);

// set by a rejected save; a changed location is checked again by the next save
watch(
  () => item.value.location,
  () => (taken.value = false),
);

// the AngularJS rules: an .xml file name, not the index file, unique within the store
const locationError = computed(() => {
  const location = item.value.location ?? "";
  if (!/^[a-z0-9_/\-.]+\.(xml)$/i.test(location)) return t("SITEMAPS.ERRORS.LOCATION");
  if (location.toLowerCase() === "sitemap.xml") return t("SITEMAPS.ERRORS.INDEX");
  if (taken.value) return t("SITEMAPS.ERRORS.TAKEN");
  return "";
});

const templateError = computed(() =>
  item.value.urlTemplate && !/^[a-z0-9_/\-.{}]+$/i.test(item.value.urlTemplate) ? t("SITEMAPS.ERRORS.URL_TEMPLATE") : "",
);

const form = useBladeForm({
  data: item,
  canSaveOverride: computed(() => !locationError.value && !!item.value.urlTemplate && !templateError.value),
  closeConfirmMessage: computed(() => t("SITEMAPS.ALERTS.CLOSE_CONFIRMATION")),
});

// the sitemap's items, a page at a time (as the AngularJS blade): adding or removing them refreshes only these
const pagination = useDataTablePagination({ pageSize: 20, totalCount: itemsTotal, onPageChange: () => loadItems() });
const { loading: itemsLoading, action: loadItems } = useAsync(async () => {
  const result = await (await getApiClient()).searchSitemapItems({ sitemapId: param.value, skip: pagination.skip, take: pagination.pageSize });
  sitemapItems.value = result.results ?? [];
  itemsTotal.value = result.totalCount ?? 0;
  selection.value = [];
});

const { loading: itemLoading, action: load } = useAsync(async () => {
  item.value = await (await getApiClient()).getSitemapById(param.value!);
  form.setBaseline();
  await loadItems();
});

const { loading: saveLoading, action: save } = useAsync(async () => {
  const client = await getApiClient();
  const same = await client.searchSitemaps({ storeId: item.value.storeId, location: item.value.location, take: 10 });
  taken.value = (same.results ?? []).some((x) => x.id !== item.value.id && x.location?.toLowerCase() === item.value.location?.toLowerCase());
  if (taken.value) return;
  if (isNew.value) {
    await client.addSitemap(item.value);
    // the API returns no id: find the new sitemap by its location
    const created = (await client.searchSitemaps({ storeId: item.value.storeId, location: item.value.location, take: 1 })).results?.[0];
    form.setBaseline();
    await callParent("reload");
    if (created?.id) await replaceWith({ name: "SitemapDetails", param: created.id, options: options.value });
  } else {
    await client.updateSitemap({ ...item.value, items: undefined });
    // only the fields changed: the items stay as they are
    await Promise.all([
      client.getSitemapById(param.value!).then((saved) => {
        item.value = saved;
        form.setBaseline();
      }),
      callParent("reload"),
    ]);
  }
});

const loading = useLoading(itemLoading, itemsLoading, saveLoading);

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "save",
    title: computed(() => t(isNew.value ? "SITEMAPS.TOOLBAR.CREATE" : "SITEMAPS.TOOLBAR.SAVE")),
    icon: "lucide-save",
    permissions: isNew.value ? "sitemaps:create" : "sitemaps:update",
    disabled: computed(() => !form.canSave.value),
    clickHandler: () => save(),
  },
  {
    id: "reset",
    title: t("SITEMAPS.TOOLBAR.RESET"),
    icon: "lucide-undo-2",
    disabled: computed(() => !form.isModified.value),
    clickHandler: () => form.revert(),
  },
  {
    id: "addItems",
    title: t("SITEMAPS.ADD_ITEMS"),
    icon: "lucide-plus",
    permissions: "sitemaps:update",
    isVisible: computed(() => !isNew.value),
    clickHandler: () => openBlade({ name: "SitemapItemsAdd", param: param.value, options: { storeId: item.value.storeId } }),
  },
  {
    id: "removeItems",
    title: t("SITEMAPS.REMOVE_ITEMS"),
    icon: "lucide-trash-2",
    permissions: "sitemaps:update",
    isVisible: computed(() => !isNew.value),
    disabled: computed(() => !selection.value.length),
    async clickHandler() {
      if (await showConfirmation(t("SITEMAPS.ALERTS.DELETE_SELECTED", { count: selection.value.length }))) {
        await (await getApiClient()).removeSitemapItems(selection.value.map((x) => x.id!));
        // the whole last page removed: show the one before it
        if (selection.value.length === sitemapItems.value.length && pagination.currentPage > 1) pagination.setPage(pagination.currentPage - 1);
        await loadItems();
        await callParent("reload");
      }
    },
  },
]);

onMounted(async () => {
  if (!isNew.value) await load();
  else form.setBaseline();
});

// the add-items blade: only the items changed, so the unsaved field edits stay
exposeToChildren({
  async reload() {
    await loadItems();
    await callParent("reload");
  },
});
</script>
