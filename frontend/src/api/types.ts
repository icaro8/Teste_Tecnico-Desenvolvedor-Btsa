export type Tone = 'Neutral' | 'Info' | 'Success' | 'Warning' | 'Danger'
export type SortDirection = 'Asc' | 'Desc'

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
  sortBy: string
  sortDirection: SortDirection
}

export interface AccountActions {
  canBlock: boolean
  canActivate: boolean
  canDeactivate: boolean
  canSendTransfers: boolean
}

export interface Account {
  id: string
  holderName: string
  holderDocument: string
  status: string
  statusLabel: string
  statusTone: Tone
  balance: number
  balanceDisplay: string
  overdraftLimit: number
  overdraftLimitDisplay: string
  overdraftInUse: number
  overdraftInUseDisplay: string
  availableToTransfer: number
  availableToTransferDisplay: string
  openedAt: string
  openedAtDisplay: string
  actions: AccountActions
}

export interface HourlyLimit {
  maxAmount: number
  maxAmountDisplay: string
  maxAttempts: number
}

export interface AccountDetails {
  account: Account
  limits: {
    currentPeriod: string
    currentPeriodLabel: string
    daytime: HourlyLimit
    nighttime: HourlyLimit
    usage: {
      attemptsInLastHour: number
      remainingAttempts: number
      transferredInLastHourDisplay: string
      remainingAmountDisplay: string
    }
  }
}

export interface Transfer {
  id: string
  kind: string
  kindLabel: string
  status: string
  statusLabel: string
  statusTone: Tone
  source: { accountId: string; holderName: string }
  destination: { accountId: string; holderName: string }
  amount: number
  amountDisplay: string
  failureReason: string | null
  failureMessage: string | null
  timeline: {
    createdAtDisplay: string
    scheduledForDisplay: string | null
    processedAtDisplay: string | null
    completedAtDisplay: string | null
    failedAtDisplay: string | null
    cancelledAtDisplay: string | null
  }
  canCancel: boolean
}

export interface Problem {
  status: number
  title?: string
  detail?: string
  errors?: Record<string, string[]>
  transfer?: Transfer
}
