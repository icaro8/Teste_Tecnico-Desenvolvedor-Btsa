import { columnValues, expect, expectSortableByEveryColumn, openAccount, parseMoney, randomCpf, test, uniqueName } from './fixtures'

test.describe('Contas', () => {
  test('lista as contas e ordena por todas as colunas', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('heading', { name: 'Contas' })).toBeVisible()
    await expect(page.getByRole('table', { name: 'Contas' }).locator('tbody tr').first()).toBeVisible()

    await expectSortableByEveryColumn(page, 'Contas')
  })

  test('ordena pelo saldo no servidor', async ({ page }) => {
    await page.goto('/?sortBy=Balance&sortDirection=Desc')
    const grid = page.getByRole('table', { name: 'Contas' })
    await expect(grid.locator('thead th').filter({ hasText: 'Saldo' })).toHaveAttribute('aria-sort', 'descending')
    await expect(grid.locator('tbody tr').first()).toBeVisible()

    const balances = (await columnValues(page, 'Contas', 'Saldo')).map(parseMoney)
    expect(balances).toEqual([...balances].sort((left, right) => right - left))
  })

  test('busca contas por nome no servidor', async ({ page, request }) => {
    const account = await openAccount(request, { name: uniqueName('Busca') })
    await page.goto('/')

    await page.getByRole('searchbox', { name: 'Buscar contas' }).fill(account.holderName)

    await expect(page).toHaveURL(/search=/)
    const rows = page.getByRole('table', { name: 'Contas' }).locator('tbody tr')
    await expect(rows).toHaveCount(1)
    await expect(rows.first()).toContainText(account.holderName)
  })

  test('abre uma conta pelo formulário', async ({ page }) => {
    const name = uniqueName('Nova Conta')
    await page.goto('/')
    await page.getByRole('link', { name: 'Nova conta' }).click()

    await page.getByLabel('Nome do titular').fill(name)
    await page.getByLabel('CPF').fill(randomCpf())
    await page.getByLabel('Saldo inicial (R$)').fill('500')
    await page.getByLabel('Limite de cheque especial (R$)').fill('1000')
    await page.getByRole('button', { name: 'Abrir conta' }).click()

    await expect(page.getByRole('heading', { name })).toBeVisible()
    await expect(page.getByTestId('balance')).toHaveText(/R\$\s500,00/)
    await expect(page.getByTestId('available')).toHaveText(/R\$\s1\.500,00/)
  })

  test('mostra o erro de CPF inválido', async ({ page }) => {
    await page.goto('/contas/nova')
    await page.getByLabel('Nome do titular').fill('Teste')
    await page.getByLabel('CPF').fill('111.111.111-11')
    await page.getByRole('button', { name: 'Abrir conta' }).click()

    await expect(page.getByRole('alert')).toContainText('CPF válido')
  })

  test('mostra limites e uso por hora na conta', async ({ page, request }) => {
    const account = await openAccount(request, { balance: 100 })
    await page.goto(`/contas/${account.id}`)

    await expect(page.getByRole('heading', { name: 'Limites por hora' })).toBeVisible()
    await expect(page.getByTestId('current-period')).toHaveText(/Diurno|Noturno/)
    await expect(page.getByTestId('remaining-attempts')).toHaveText(/\d+/)
  })

  test('bloqueia, inativa e reativa a conta', async ({ page, request }) => {
    const account = await openAccount(request, { balance: 100 })
    await page.goto(`/contas/${account.id}`)

    await page.getByRole('button', { name: 'Bloquear' }).click()
    await expect(page.getByTestId('badge').first()).toHaveText('Bloqueada')
    await expect(page.getByRole('link', { name: 'Transferir' })).toHaveCount(0)

    await page.getByRole('button', { name: 'Inativar' }).click()
    await expect(page.getByTestId('badge').first()).toHaveText('Inativa')

    await page.getByRole('button', { name: 'Ativar' }).click()
    await expect(page.getByTestId('badge').first()).toHaveText('Ativa')
    await expect(page.getByRole('link', { name: 'Transferir' })).toBeVisible()
  })

  test('altera o limite de cheque especial', async ({ page, request }) => {
    const account = await openAccount(request, { balance: 200 })
    await page.goto(`/contas/${account.id}`)

    await page.getByLabel('Novo limite (R$)').fill('800')
    await page.getByRole('button', { name: 'Salvar limite' }).click()

    await expect(page.getByTestId('available')).toHaveText(/R\$\s1\.000,00/)
  })

  test('altera os limites de transferência por hora', async ({ page, request }) => {
    const account = await openAccount(request, { balance: 100 })
    await page.goto(`/contas/${account.id}`)

    await page.getByLabel('Diurno: valor por hora (R$)').fill('8000')
    await page.getByLabel('Diurno: tentativas por hora').fill('7')
    await page.getByLabel('Noturno: valor por hora (R$)').fill('1500')
    await page.getByLabel('Noturno: tentativas por hora').fill('4')
    await page.getByRole('button', { name: 'Atualizar limites por hora' }).click()

    await expect(page.getByText('Limites por hora atualizados.')).toBeVisible()
    await expect(page.locator('.details')).toContainText(/R\$\s8\.000,00 · 7 tentativas/)
    await expect(page.locator('.details')).toContainText(/R\$\s1\.500,00 · 4 tentativas/)
  })
})
