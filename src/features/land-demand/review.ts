import type { LandDemandForm } from './models'

import { PARK_OPTIONS } from './dictionaries/parks'
import { getIndustryDisplay } from './industry-selector'

export interface ReviewItem {
  field: keyof LandDemandForm
  label: string
  value: string
}

export interface ReviewGroup {
  step: 1 | 2 | 3 | 4
  title: string
  items: ReviewItem[]
}

function parkLabel(value: string): string {
  return PARK_OPTIONS.find(option => option.value === value)?.label ?? value
}

function text(value: string): string {
  return value || '未填写'
}

function unit(value: string, suffix: string): string {
  return value ? `${value}${suffix}` : '未填写'
}

export function buildReviewGroups(form: LandDemandForm): ReviewGroup[] {
  const landItems: ReviewItem[] = [
    { field: 'area', label: '用地面积', value: unit(form.area, '亩') },
    { field: 'building_area', label: '项目建筑面积', value: unit(form.building_area, '平方米') },
    { field: 'expect_park', label: '期望获得土地位置', value: text(parkLabel(form.expect_park)) },
    { field: 'expect_time', label: '期望拿到土地时间', value: text(form.expect_time) },
    { field: 'is_deploy', label: '是否接受跨区域调配', value: text(form.is_deploy) },
  ]

  if (form.is_deploy === '是') {
    landItems.push({
      field: 'deploy_park',
      label: '期望调配区域',
      value: form.deploy_park.length > 0
        ? form.deploy_park.map(parkLabel).join('、')
        : '未填写',
    })
  }

  landItems.push({
    field: 'is_specialuse',
    label: '是否接受其他用地形式',
    value: text(form.is_specialuse),
  })
  if (form.is_specialuse === '是') {
    landItems.push({
      field: 'deploy_landtype',
      label: '期望用地形式',
      value: text(form.deploy_landtype),
    })
  }
  landItems.push(
    { field: 'deploy_height', label: '期望层高', value: unit(form.deploy_height, '米') },
    { field: 'deploy_weight', label: '期望承重', value: unit(form.deploy_weight, '吨/平方米') },
  )

  const contactItems: ReviewItem[] = [
    { field: 'contact', label: '法人姓名', value: text(form.contact) },
    { field: 'office', label: '联系人职务', value: text(form.office) },
    { field: 'phone', label: '法人手机号', value: text(form.phone) },
  ]

  return [
    {
      step: 1,
      title: '基本信息',
      items: [
        { field: 'businessname', label: '企业名称', value: text(form.businessname) },
        { field: 'creditcode', label: '信用代码', value: text(form.creditcode) },
        { field: 'county', label: '所属区县', value: text(form.county) },
        { field: 'region', label: '所属乡镇', value: text(form.region) },
      ],
    },
    { step: 2, title: '用地需求', items: landItems },
    {
      step: 3,
      title: '投资项目',
      items: [
        { field: 'investment', label: '固定资产投资额', value: unit(form.investment, '万元') },
        { field: 'project_hydm', label: '项目所属国民行业', value: text(getIndustryDisplay(form.project_hydm)) },
        { field: 'keyindustry', label: '项目所属产业赛道', value: text(form.keyindustry) },
        { field: 'futureindustry', label: '项目发展方向', value: text(form.futureindustry) },
        { field: 'pred_ys', label: '项目预计营收', value: unit(form.pred_ys, '万元') },
        { field: 'pred_tax', label: '项目预计税收', value: unit(form.pred_tax, '万元') },
        { field: 'pred_rdex', label: '项目预计研发费用', value: unit(form.pred_rdex, '万元') },
        { field: 'pred_unitenergy', label: '项目单位能耗增加值', value: unit(form.pred_unitenergy, '万元/吨标煤') },
        { field: 'projectdata', label: '项目建设内容', value: text(form.projectdata) },
      ],
    },
    { step: 4, title: '联系人信息', items: contactItems },
  ]
}
