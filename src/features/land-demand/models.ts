export type YesNo = '是' | '否'
export type LandDemandStatus = '1' | '2'
export type LandDemandRecordStatus = '0' | LandDemandStatus

export interface LandDemandForm {
  county: string
  region: string
  businessname: string
  creditcode: string
  area: string
  building_area: string
  expect_park: string
  expect_time: string
  is_deploy: YesNo | ''
  deploy_park: string[]
  is_specialuse: YesNo | ''
  deploy_landtype: string
  deploy_height: string
  deploy_weight: string
  investment: string
  project_hydm: string
  keyindustry: string
  futureindustry: string
  pred_ys: string
  pred_tax: string
  pred_rdex: string
  pred_unitenergy: string
  projectdata: string
  contact: string
  office: string
  phone: string
}

export interface LandDemandRecord extends Omit<LandDemandForm, 'deploy_park'> {
  deploy_park: string
  landusedemand: LandDemandRecordStatus
  updatetime: string
  lastSubmittedAt?: string
  updateuser: string
  newproject?: '1'
  industryCode?: string
  is_energy?: string
  energy?: string
  energy_time?: string
  qyhydm?: string
  registrationType?: number
}

export type LandDemandRecordInput = LandDemandRecord

export type SaveLandDemandPayload = Omit<
  LandDemandRecord,
  'updatetime' | 'updateuser' | 'newproject' | 'industryCode' | 'landusedemand'
> & { landusedemand: LandDemandStatus }

export type UpdateLandDemandPayload = Omit<
  LandDemandRecord,
  'county' | 'region' | 'businessname' | 'updatetime' | 'updateuser' | 'landusedemand'
> & { landusedemand: LandDemandStatus, newproject: '1' }

export interface LandDemandDraft {
  form: LandDemandForm
  currentStep: 1 | 2 | 3 | 4 | 5
  progressStep?: 1 | 2 | 3 | 4 | 5
  savedAt: number
}

export interface FieldError {
  field: keyof LandDemandForm
  step: 1 | 2 | 3 | 4
  message: string
}

export interface VerificationChallenge {
  phone: string
  expiresAt: number
  retryAt: number
  mockCode: string
}
