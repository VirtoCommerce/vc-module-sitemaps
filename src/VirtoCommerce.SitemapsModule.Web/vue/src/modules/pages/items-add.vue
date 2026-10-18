<template>
  <VcBlade
    :title="$t('SITEMAPS.ADD_ITEMS')"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <div class="sitemaps-inline-form">
      <VcInput
        v-model="customUrl"
        :label="$t('SITEMAPS.CUSTOM_URL')"
        :hint="$t('SITEMAPS.CUSTOM_URL_HINT')"
      />
      <VcSelect
        v-model="source"
        :label="$t('SITEMAPS.SOURCE')"
        :options="sources"
        :clearable="false"
      />
    </div>
    <!-- the item types the AngularJS blade offered: catalog items, vendors, static content (and custom URLs above) -->
    <VcDataTable
      v-model:search-value="keyword"
      v-model:selection="selection"
      selection-mode="multiple"
      :items="entries"
      :total-count="total"
      :pagination="source === 'content' ? undefined : pagination"
      :searchable="true"
      :state-key="'STORE_SITEMAP_ADD'"
      :empty-state="{ icon: 'lucide-folder', title: $t('SITEMAPS.NO_SOURCE_ITEMS') }"
      @row-click="(e: { data: Entry }) => onRowClick(e.data)"
      @pagination-click="pagination.goToPage"
    >
      <VcColumn
        id="imageUrl"
        title=""
        width="72px"
      >
        <template #body="{ data: e }">
          <VcIcon
            v-if="e.isFolder"
            icon="lucide-folder"
          />
          <VcImage
            v-else-if="e.imageUrl"
            :src="e.imageUrl"
            size="s"
            bordered
          />
          <VcIcon
            v-else
            :icon="source === 'content' ? 'lucide-file' : 'lucide-image'"
          />
        </template>
      </VcColumn>
      <VcColumn
        id="name"
        :title="$t('SITEMAPS.FIELDS.TITLE')"
        always-visible
      />
      <VcColumn
        id="type"
        :title="$t('SITEMAPS.FIELDS.TYPE')"
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import {
  notification,
  useApiClient,
  useAsync,
  useBlade,
  useDataTablePagination,
  useFunctions,
  useLoading,
  type IBladeToolbar,
} from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcIcon, VcImage, VcInput, VcSelect } from "@vc-shell/framework/ui";
import { computed, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { SitemapsModuleApiClient, type SitemapItem } from "../../api_client/virtocommerce.sitemaps";
import { api, qs } from "../api";

defineBlade({
  url: "/sitemap-add",
  name: "SitemapItemsAdd",
  // the store and sitemap come only in options
  routable: false,
});

type Source = "catalog" | "vendor" | "content";
interface Entry {
  id: string;
  name?: string;
  type?: string;
  imageUrl?: string;
  isFolder?: boolean;
  item: SitemapItem;
}

const { t } = useI18n({ useScope: "global" });
const { param, options, callParent, closeSelf } = useBlade<{ storeId: string }>();
const { debounce } = useFunctions();
const { getApiClient } = useApiClient(SitemapsModuleApiClient);

const sources = computed(() => [
  { id: "catalog", title: t("SITEMAPS.SOURCES.CATALOG") },
  { id: "vendor", title: t("SITEMAPS.SOURCES.VENDOR") },
  { id: "content", title: t("SITEMAPS.SOURCES.CONTENT") },
]);
const source = ref<Source>("catalog");
const customUrl = ref("");
const keyword = ref("");
const catalogId = ref<string>();
const folder = ref<string>(); // static content: the open folder (relative url), root when empty
const categories = ref<string[]>([]); // catalog: the path of open categories, the catalog's root when empty
const entries = ref<Entry[]>([]);
const selection = ref<Entry[]>([]);
// catalog items and vendors come a page at a time; static content lists a whole folder
const total = ref(0);
const pagination = useDataTablePagination({ pageSize: 50, totalCount: total, onPageChange: () => search() });

interface ContentItem {
  name: string;
  type: string;
  relativeUrl: string;
}

async function contentItems(contentType: "pages" | "blogs") {
  const query = qs({ keyword: keyword.value || undefined, folderUrl: folder.value });
  return api.get<ContentItem[]>(`/api/content/${contentType}/${encodeURIComponent(options.value!.storeId)}/search?${query}`).catch(() => []);
}

const { loading: searching, action: search } = useAsync(async () => {
  selection.value = [];
  if (source.value === "catalog") {
    if (!catalogId.value) return;
    const result = await api.post<{ listEntries?: { id: string; type: string; name?: string; imageUrl?: string }[]; totalCount?: number }>(
      "/api/catalog/listentries",
      {
        catalogId: catalogId.value,
        categoryId: categories.value[categories.value.length - 1],
        keyword: keyword.value || undefined,
        searchInChildren: !!keyword.value,
        skip: pagination.skip,
        take: pagination.pageSize,
      },
    );
    total.value = result.totalCount ?? 0;
    entries.value = (result.listEntries ?? []).map((x) => ({
      ...x,
      isFolder: x.type === "category", // opened by a click, added when checked (as the AngularJS catalog picker)
      item: { title: x.name, imageUrl: x.imageUrl, objectId: x.id, objectType: x.type },
    }));
  } else if (source.value === "vendor") {
    const result = await api.post<{ results?: { id: string; name?: string; memberType?: string; iconUrl?: string }[]; totalCount?: number }>(
      "/api/members/search",
      {
        memberTypes: ["Vendor"],
        keyword: keyword.value || undefined,
        deepSearch: true,
        skip: pagination.skip,
        take: pagination.pageSize,
      },
    );
    total.value = result.totalCount ?? 0;
    entries.value = (result.results ?? []).map((x) => ({
      id: x.id,
      name: x.name,
      type: x.memberType,
      imageUrl: x.iconUrl,
      item: { title: x.name, imageUrl: x.iconUrl, objectId: x.id, objectType: x.memberType },
    }));
  } else {
    // pages, and at the root also the blogs, of the store's static content
    const [blogs, pages] = await Promise.all([folder.value ? [] : contentItems("blogs"), contentItems("pages")]);
    const items = [...blogs, ...pages];
    entries.value = items.map((x) => ({
      id: x.relativeUrl,
      name: x.name,
      type: x.type,
      isFolder: x.type.toLowerCase() === "folder",
      item: { title: x.relativeUrl, urlTemplate: x.relativeUrl, objectType: x.type.toLowerCase() !== "folder" ? "ContentItem" : x.type },
    }));
    total.value = entries.value.length;
  }
});
// a new folder, category, source or keyword starts at the first page
const restart = () => {
  pagination.setPage(1);
  return search();
};

const { loading: adding, action: add } = useAsync<SitemapItem[]>(async (items) => {
  await (await getApiClient()).addSitemapItems(param.value!, items);
  await callParent("reload");
  await closeSelf();
});

const loading = useLoading(searching, adding);

function onRowClick(entry: Entry) {
  if (!entry.isFolder) return;
  if (source.value === "catalog") categories.value = [...categories.value, entry.id];
  else folder.value = entry.id;
  restart();
}

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "addCustom",
    title: t("SITEMAPS.ADD_CUSTOM"),
    icon: "lucide-link",
    disabled: computed(() => !customUrl.value.trim()),
    clickHandler: () => add([{ title: customUrl.value.trim(), urlTemplate: customUrl.value.trim(), objectType: "Custom" }]),
  },
  {
    id: "addSelected",
    title: t("SITEMAPS.ADD_SELECTED"),
    icon: "lucide-plus",
    disabled: computed(() => !selection.value.length),
    clickHandler: () => add(selection.value.map((x) => x.item)),
  },
  {
    id: "up",
    title: t("SITEMAPS.UP"),
    icon: "lucide-corner-left-up",
    isVisible: computed(() => source.value !== "vendor"),
    disabled: computed(() => (source.value === "catalog" ? !categories.value.length : !folder.value)),
    clickHandler() {
      if (source.value === "catalog") categories.value = categories.value.slice(0, -1);
      else folder.value = folder.value?.split("/").slice(0, -1).join("/") || undefined;
      restart();
    },
  },
]);

watch(keyword, debounce(restart, 400));
watch(source, () => {
  folder.value = undefined;
  categories.value = [];
  restart();
});

onMounted(async () => {
  try {
    catalogId.value = (await api.get<{ catalog?: string }>(`/api/stores/${encodeURIComponent(options.value!.storeId)}`)).catalog;
  } catch (e) {
    notification.error(String(e)); // the catalog source stays empty; the others still work
  }
  await search();
});
</script>
