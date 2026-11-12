import { test as base, expect, type APIRequestContext, type Page } from '@playwright/test'

export interface CreatedAccount {
  id: string
  holderName: string
  holderDocument: string
}

const checkDigit = (digits: number[]) => {
  const weight = digits.length + 1
  const sum = digits.reduce((total, digit, index) => total + digit * (weight - index), 0)
  const remainder = sum % 11
  return remainder < 2 ? 0 : 11 - remainder
}

export const randomCpf = () => {
  const digits = Array.from({ length: 9 }, () => Math.floor(Math.random() * 10))
  digits.push(checkDigit(digits))
  digits.push(checkDigit(digits))
  return digits.join('')
}

export const uniqueName = (prefix: string) => `${prefix} ${Math.random().toString(36).slice(2, 8)}`

export async function openAccount(
  request: APIRequestContext,
  options: { name?: string; balance?: number; overdraftLimit?: number } = {}
): Promise<CreatedAccount> {
  const response = await request.post('/api/accounts', {
    data: {
      holderName: options.name ?? uniqueName('E2E'),
      holderDocument: randomCpf(),
      initialBalance: options.balance ?? 0,
      overdraftLimit: options.overdraftLimit ?? 0
    }
  })
  expect(response.status()).toBe(201)
  return response.json()
}

export const parseMoney = (text: string) => {
  const negative = text.trim().startsWith('-')
  const value = Number(text.replace(/[^\d,]/g, '').replace(',', '.'))
  return negative ? -value : value
}

export async function columnValues(page: Page, table: string, column: string) {
  const grid = page.getByRole('table', { name: table })
  const headers = await grid.locator('thead th').allInnerTexts()
  const index = headers.findIndex((header) => header.toLowerCase().includes(column.toLowerCase()))
  return grid.locator(`tbody tr td:nth-child(${index + 1})`).allInnerTexts()
}

export async function expectSortableByEveryColumn(page: Page, table: string) {
  const grid = page.getByRole('table', { name: table })
  const headers = grid.locator('thead th')
  const count = await headers.count()
  for (let index = 0; index < count; index++) {
    const header = headers.nth(index)
    const wasAscending = (await header.getAttribute('aria-sort')) === 'ascending'
    const [first, second] = wasAscending ? (['descending', 'ascending'] as const) : (['ascending', 'descending'] as const)
    await header.getByRole('button').click()
    await expect(header).toHaveAttribute('aria-sort', first)
    await expect(page).toHaveURL(first === 'ascending' ? /sortDirection=Asc/ : /sortDirection=Desc/)
    await header.getByRole('button').click()
    await expect(header).toHaveAttribute('aria-sort', second)
    await expect(page).toHaveURL(second === 'ascending' ? /sortDirection=Asc/ : /sortDirection=Desc/)
  }
}

export const localDateTime = (date: Date) => {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

export const cpfDigits = (account: CreatedAccount) => account.holderDocument.replace(/\D/g, '')

export async function chooseAccount(page: Page, label: 'Conta de origem' | 'Conta de destino', account: CreatedAccount) {
  await page.getByRole('combobox', { name: label }).click()
  await page.getByRole('searchbox', { name: `Filtrar ${label.toLowerCase()} por CPF` }).fill(cpfDigits(account))
  await page.getByRole('option', { name: account.holderName }).click()
  await expect(page.getByRole('combobox', { name: label })).toContainText(account.holderName)
}

export const test = base
export { expect }
