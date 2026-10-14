<template>
  <VcBlade
    :title="$t(action === 'export' ? 'SITEMAPS.EXPORT' : 'SITEMAPS.DOWNLOAD')"
    :loading="!job && !failed"
    width="40%"
  >
    <VcContainer>
      <div
        v-if="job"
        class="sitemaps-form tw-space-y-4"
      >
        <VcCard :header="job.title ?? ''">
          <div class="sitemaps-card">
            <div>{{ job.description }}</div>
            <VcRow class="tw-gap-4">
              <VcCol>
                <VcField
                  :label="$t('SITEMAPS.JOB.STARTED')"
                  :model-value="job.created ? new Date(job.created) : undefined"
                  type="date"
                />
              </VcCol>
              <VcCol>
                <VcField
                  :label="$t('SITEMAPS.JOB.FINISHED')"
                  :model-value="job.finished ? new Date(job.finished) : '—'"
                  :type="job.finished ? 'date' : 'text'"
                />
              </VcCol>
            </VcRow>
            <VcRow class="tw-gap-4">
              <VcCol>
                <VcField
                  :label="$t('SITEMAPS.JOB.PROCESSED')"
                  :model-value="`${job.processedCount ?? 0} / ${job.totalCount ?? 0}`"
                />
              </VcCol>
              <VcCol>
                <VcField
                  :label="$t('SITEMAPS.JOB.ERRORS')"
                  :model-value="job.errorCount ?? 0"
                />
              </VcCol>
            </VcRow>
            <VcField
              v-if="resultUrl"
              :label="$t('SITEMAPS.JOB.RESULT')"
              :model-value="resultUrl"
              type="link"
              copyable
            />
            <VcBanner
              v-if="job.errors?.length"
              variant="danger"
            >
              <div
                v-for="(error, i) in job.errors"
                :key="i"
              >
                {{ error }}
              </div>
            </VcBanner>
          </div>
        </VcCard>
      </div>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import { PushNotificationClient, useApiClient, useBlade, useBladeNotifications } from "@vc-shell/framework";
import { VcBanner, VcBlade, VcCard, VcCol, VcContainer, VcField, VcRow } from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { SitemapsModuleApiClient, type SitemapDownloadNotification } from "../../api_client/virtocommerce.sitemaps";

defineBlade({
  url: "/sitemap-job",
  name: "SitemapJob",
  // its job settings come only in options: reopened from the URL (F5) it would start a job without a base URL
  routable: false,
});

// an export reports sitemapXmlUrl instead of downloadUrl
type SitemapJob = SitemapDownloadNotification & { sitemapXmlUrl?: string };

const { param, options, setError } = useBlade<{ action: "download" | "export"; baseUrl: string; sitemapIds: string[] }>();
const { getApiClient } = useApiClient(SitemapsModuleApiClient);
const { getApiClient: getNotificationsClient } = useApiClient(PushNotificationClient);
const action = computed(() => options.value?.action ?? "download");
const job = ref<SitemapJob>();
const failed = ref(false);
const resultUrl = computed(() => job.value?.downloadUrl || job.value?.sitemapXmlUrl);

// the job reports progress as push notifications, which the AngularJS blade listened to as well
useBladeNotifications<SitemapJob>({
  types: ["SitemapDownload", "SitemapExportToAsset"],
  filter: (msg) => msg.id === job.value?.id,
  onMessage: (msg) => (job.value = { ...job.value, ...msg }),
});

onMounted(async () => {
  const client = await getApiClient();
  const sitemapIds = options.value?.sitemapIds?.length ? options.value.sitemapIds : undefined;
  try {
    job.value = await (action.value === "export"
      ? client.exportToStoreAssets(param.value, options.value?.baseUrl, sitemapIds)
      : client.downloadSitemap(param.value, options.value?.baseUrl, sitemapIds));
  } catch (e) {
    failed.value = true;
    return setError(e);
  }
  // progress pushed before the job's id was known: catch up from the notification history
  // (unless a push already reported the end while this request was out)
  const history = await (await getNotificationsClient()).searchPushNotification({ ids: [job.value.id!], take: 1 }).catch(() => undefined);
  const latest = history?.notifyEvents?.[0];
  if (latest && !job.value?.finished) job.value = { ...job.value, ...latest };
});
</script>
