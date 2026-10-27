<template>
  <VcWidget
    icon="lucide-network"
    :title="$t('SITEMAPS.TITLE')"
    :value="count"
    :disabled="!storeId"
    @click="open"
  />
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { injectBladeContext, useApiClient, useBlade, useWidgetTrigger } from "@vc-shell/framework";
import { VcWidget } from "@vc-shell/framework/ui";
import { SitemapsModuleApiClient } from "../../api_client/virtocommerce.sitemaps";

// the store shared by the Store module's blade via defineBladeContext({ item }); the badge counts its sitemaps
const ctx = injectBladeContext();
const storeId = computed(() => (ctx.value.item as { id?: string } | undefined)?.id);
const { openBlade } = useBlade();
const { getApiClient } = useApiClient(SitemapsModuleApiClient);
const count = ref<number | string>("");

async function load() {
  if (!storeId.value) return;
  const result = await (await getApiClient()).searchSitemaps({ storeId: storeId.value, take: 0 }).catch(() => undefined);
  count.value = result?.totalCount ?? "";
}
watch(storeId, load, { immediate: true });
useWidgetTrigger({ onRefresh: load });

const open = () => openBlade({ name: "SitemapsList", param: storeId.value });
</script>
