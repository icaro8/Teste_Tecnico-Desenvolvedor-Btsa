import type { Account, AccountDetails, PagedResponse, Problem, SortDirection, Transfer } from './types'

export class ApiError extends Error {
  readonly problem: Problem

  constructor(problem: Problem) {
    super(problem.detail ?? problem.title ?? 'Erro inesperado')
    this.problem = problem
  }

  get messages(): string[] {
    const fieldErrors = Object.values(this.problem.errors ?? {}).flat()
    return fieldErrors.length > 0 ? fieldErrors : [this.message]
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: { 'Content-Type': 'application/json', Accept: 'application/json', ...init.headers }
  })

  const body = response.status === 204 ? null : await response.json().catch(() => null)
  if (!response.ok) {
    throw new ApiError({ status: response.status, ...(body ?? {}) })
  }

  return body as T
}

const post = <T>(path: string, body?: unknown, headers?: HeadersInit) =>
  request<T>(path, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body), headers })

export interface ListParams {
  sortBy?: string
  sortDirection?: SortDirection
  page?: number
  pageSize?: number
}

const query = (params: Record<string, string | number | undefined>) => {
  const search = new URLSearchParams()
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== '') search.set(key, String(value))
  })
  return search.toString()
}

export interface OpenAccountInput {
  holderName: string
  holderDocument: string
  initialBalance: number
  overdraftLimit: number
}

export interface TransferInput {
  sourceAccountId: string
  destinationAccountId: string
  amount: number
  scheduledFor?: string
}

export interface HourlyLimitInput {
  maxAmount: number
  maxAttempts: number
}

export type AccountStatusAction = 'block' | 'activate' | 'deactivate'

export const api = {
  listAccounts: (params: ListParams & { search?: string }) => request<PagedResponse<Account>>(`/api/accounts?${query({ ...params })}`),
  getAccount: (id: string) => request<AccountDetails>(`/api/accounts/${id}`),
  openAccount: (input: OpenAccountInput) => post<Account>('/api/accounts', input),
  changeAccountStatus: (id: string, action: AccountStatusAction) => post<Account>(`/api/accounts/${id}/${action}`),
  changeOverdraftLimit: (id: string, overdraftLimit: number) =>
    request<Account>(`/api/accounts/${id}/overdraft-limit`, { method: 'PUT', body: JSON.stringify({ overdraftLimit }) }),
  changeTransferLimits: (id: string, daytimeLimit: HourlyLimitInput, nighttimeLimit: HourlyLimitInput) =>
    request<AccountDetails>(`/api/accounts/${id}/transfer-limits`, {
      method: 'PUT',
      body: JSON.stringify({ daytimeLimit, nighttimeLimit })
    }),
  listTransfers: (params: ListParams & { accountId?: string; status?: string }) =>
    request<PagedResponse<Transfer>>(`/api/transfers?${query({ ...params })}`),
  getTransfer: (id: string) => request<Transfer>(`/api/transfers/${id}`),
  createTransfer: (input: TransferInput, idempotencyKey: string) =>
    post<Transfer>(input.scheduledFor ? '/api/transfers/scheduled' : '/api/transfers', input, { 'Idempotency-Key': idempotencyKey }),
  cancelTransfer: (id: string) => post<Transfer>(`/api/transfers/${id}/cancel`)
}
