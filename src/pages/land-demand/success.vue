<script setup lang="ts">
import PageShell from '@/components/ui/page-shell/index.vue'
import { replace } from '@/router/navigation'

definePageJson({
  navigationBarTitleText: '填报完成',
})

async function backHome(): Promise<void> {
  await replace('/pages/home/index')
}
</script>

<template>
  <PageShell title="填报完成" subtitle="用地需求已提交" icon="list-check">
    <view data-testid="submit-success" class="u-card land-demand-success__notice">
      <text class="land-demand-success__status">提交成功</text>
      <text class="land-demand-success__copy">感谢填报，相关部门将跟进服务。</text>
      <view data-testid="success-back-home">
        <t-button
          data-testid="back-home"
          class="land-demand-success__button"
          theme="primary"
          block
          @tap="backHome"
        >
          返回首页
        </t-button>
      </view>
    </view>
  </PageShell>
</template>

<style lang="scss">
@use '@/styles/tokens' as *;

.land-demand-success__notice {
  padding: $space-4;
  text-align: center;
}

.land-demand-success__status,
.land-demand-success__copy {
  display: block;
}

.land-demand-success__status {
  font-size: 36rpx;
  font-weight: 700;
  color: $color-success;
}

.land-demand-success__copy {
  margin-top: $space-2;
  font-size: 28rpx;
  color: $color-text-secondary;
}

.land-demand-success__button {
  margin-top: $space-4;
}
</style>
