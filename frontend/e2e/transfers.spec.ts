import { chooseAccount, expect, expectSortableByEveryColumn, localDateTime, openAccount, test } from './fixtures'

test.describe('Transferências', () => {
  test('transferência imediata conclui e atualiza os saldos', async ({ page, request }) => {
    const source = await openAccount(request, { balance: 1000 })
    const destination = await openAccount(request)

    await page.goto(`/transferencias/nova?source=${source.id}`)
    await chooseAccount(page, 'Conta de destino', destination)
    await page.getByLabel('Valor (R$)').fill('300')
    await page.getByRole('button', { name: 'Transferir agora' }).click()

    await expect(page.getByTestId('transfer-status')).toHaveText('Concluída')
    await expect(page.getByTestId('amount')).toHaveText(/R\$\s300,00/)

    await page.getByRole('link', { name: source.holderName }).click()
    await expect(page.getByTestId('balance')).toHaveText(/R\$\s700,00/)
  })

  test('transferência usando cheque especial deixa o saldo negativo', async ({ page, request }) => {
    const source = await openAccount(request, { balance: 500, overdraftLimit: 1000 })
    const destination = await openAccount(request)

    await page.goto(`/transferencias/nova?source=${source.id}`)
    await chooseAccount(page, 'Conta de destino', destination)
    await page.getByLabel('Valor (R$)').fill('1000')
    await page.getByRole('button', { name: 'Transferir agora' }).click()
    await expect(page.getByTestId('transfer-status')).toHaveText('Concluída')

    await page.goto(`/contas/${source.id}`)
    await expect(page.getByTestId('balance')).toHaveText(/-R\$\s500,00/)
    await expect(page.getByTestId('overdraft-in-use')).toHaveText(/R\$\s500,00/)
  })

  test('transferência sem saldo é recusada e a tentativa fica registrada', async ({ page, request }) => {
    const source = await openAccount(request, { balance: 100 })
    const destination = await openAccount(request)

    await page.goto(`/transferencias/nova?source=${source.id}`)
    await chooseAccount(page, 'Conta de destino', destination)
    await page.getByLabel('Valor (R$)').fill('500')
    await page.getByRole('button', { name: 'Transferir agora' }).click()

    const alert = page.getByRole('alert')
    await expect(alert).toContainText('Transferência recusada')
    await expect(alert).toContainText('insuficiente')

    await alert.getByRole('link', { name: 'Ver tentativa registrada' }).click()
    await expect(page.getByTestId('transfer-status')).toHaveText('Falhou')
  })

  test('filtra as contas de origem e destino pelo CPF', async ({ page, request }) => {
    const source = await openAccount(request, { balance: 50 })
    const other = await openAccount(request)

    await page.goto('/transferencias/nova')
    await page.getByRole('combobox', { name: 'Conta de origem' }).click()
    await page.getByRole('searchbox', { name: 'Filtrar conta de origem por CPF' }).fill(source.holderDocument)

    const options = page.getByRole('option')
    await expect(options).toHaveCount(1)
    await expect(options.first()).toContainText(source.holderName)
    await expect(options.first()).toContainText(source.holderDocument)
    await expect(page.getByRole('option', { name: other.holderName })).toHaveCount(0)

    await page.getByRole('searchbox', { name: 'Filtrar conta de origem por CPF' }).press('Enter')
    await expect(page.getByRole('combobox', { name: 'Conta de origem' })).toContainText(source.holderName)
    await expect(page.getByRole('listbox')).toHaveCount(0)
  })

  test('escolhe as contas de origem e destino pelo select', async ({ page, request }) => {
    const source = await openAccount(request, { balance: 50 })
    const destination = await openAccount(request)

    await page.goto('/transferencias/nova')
    await chooseAccount(page, 'Conta de origem', source)
    await chooseAccount(page, 'Conta de destino', destination)
    await page.getByLabel('Valor (R$)').fill('20')
    await page.getByRole('button', { name: 'Transferir agora' }).click()

    await expect(page.getByTestId('transfer-status')).toHaveText('Concluída')
  })

  test('agenda e cancela uma transferência', async ({ page, request }) => {
    const source = await openAccount(request, { balance: 1000 })
    const destination = await openAccount(request)
    const tomorrow = new Date(Date.now() + 24 * 60 * 60 * 1000)

    await page.goto(`/transferencias/nova?source=${source.id}`)
    await chooseAccount(page, 'Conta de destino', destination)
    await page.getByLabel('Valor (R$)').fill('250')
    await page.getByLabel('Agendar para uma data futura').check()
    await page.getByLabel('Data e hora da execução').fill(localDateTime(tomorrow))
    await page.getByRole('button', { name: 'Agendar transferência' }).click()

    await expect(page.getByTestId('transfer-status')).toHaveText('Agendada')
    await page.getByRole('button', { name: 'Cancelar agendamento' }).click()

    await expect(page.getByTestId('transfer-status')).toHaveText('Cancelada')
    await expect(page.getByRole('button', { name: 'Cancelar agendamento' })).toHaveCount(0)
  })

  test('agendamento no passado mostra o erro', async ({ page, request }) => {
    const source = await openAccount(request, { balance: 1000 })
    const destination = await openAccount(request)

    await page.goto(`/transferencias/nova?source=${source.id}`)
    await chooseAccount(page, 'Conta de destino', destination)
    await page.getByLabel('Valor (R$)').fill('10')
    await page.getByLabel('Agendar para uma data futura').check()
    await page.getByLabel('Data e hora da execução').fill(localDateTime(new Date(Date.now() - 60 * 60 * 1000)))
    await page.getByRole('button', { name: 'Agendar transferência' }).click()

    await expect(page.getByRole('alert')).toContainText('data do agendamento deve ser futura')
  })

  test('agendamento a partir de conta bloqueada é recusado na hora', async ({ page, request }) => {
    const source = await openAccount(request, { balance: 1000 })
    const destination = await openAccount(request)
    expect((await request.post(`/api/accounts/${source.id}/block`)).ok()).toBeTruthy()

    await page.goto(`/transferencias/nova?source=${source.id}`)
    await chooseAccount(page, 'Conta de destino', destination)
    await page.getByLabel('Valor (R$)').fill('50')
    await page.getByLabel('Agendar para uma data futura').check()
    await page.getByLabel('Data e hora da execução').fill(localDateTime(new Date(Date.now() + 24 * 60 * 60 * 1000)))
    await page.getByRole('button', { name: 'Agendar transferência' }).click()

    await expect(page.getByRole('alert')).toContainText('não está ativa')
  })

  test('lista de transferências ordena por todas as colunas e filtra por status', async ({ page }) => {
    await page.goto('/transferencias')
    await expect(page.getByRole('table', { name: 'Transferências' }).locator('tbody tr').first()).toBeVisible()

    await expectSortableByEveryColumn(page, 'Transferências')

    const filtered = page.waitForResponse((response) => response.url().includes('/api/transfers?') && response.url().includes('status=Scheduled'))
    await page.getByLabel('Filtrar por status').selectOption('Scheduled')
    await filtered
    await expect(page).toHaveURL(/status=Scheduled/)
    const badges = page.getByRole('table', { name: 'Transferências' }).getByTestId('badge')
    await expect(badges.first()).toHaveText('Agendada')
    for (const text of await badges.allInnerTexts()) {
      expect(text).toBe('Agendada')
    }
  })

  test('movimentações da conta ordenam por todas as colunas', async ({ page, request }) => {
    const source = await openAccount(request, { balance: 1000 })
    const destination = await openAccount(request)
    for (const amount of [10, 20]) {
      await request.post('/api/transfers', { data: { sourceAccountId: source.id, destinationAccountId: destination.id, amount } })
    }

    await page.goto(`/contas/${source.id}`)
    await expect(page.getByRole('table', { name: 'Transferências' }).locator('tbody tr')).toHaveCount(2)

    await expectSortableByEveryColumn(page, 'Transferências')
  })
})
