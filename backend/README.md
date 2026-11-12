# Transferências Financeiras — API

API REST para realizar, agendar, consultar e cancelar transferências entre contas, com cheque especial, limites por hora (diurno/noturno) e proteção contra transferências simultâneas.

| Camada | Tecnologia |
| --- | --- |
| Backend | .NET 10 (LTS), ASP.NET Core, C# |
| Banco | PostgreSQL 17 + Entity Framework Core 10 (migrations) |
| Documentação | Swagger / OpenAPI |
| Testes | xUnit (unidade), WebApplicationFactory + Testcontainers (integração com Postgres real) |
| Infra | Docker Compose, Makefile |

---

## Como executar

> Para subir **tudo** (banco, API e frontend) de uma vez, use o `docker compose` da raiz do repositório; veja o [README principal](../README.md). Os comandos abaixo sobem só o backend.

Pré-requisito: Docker. (`make` é opcional; os comandos equivalentes estão ao lado.)

```bash
make build
# sem make: cp .env.example .env && docker compose up -d --build --wait
```

| O quê | URL |
| --- | --- |
| Swagger | http://localhost:15080/swagger |
| Health check | http://localhost:15080/health |

As migrations são aplicadas na subida da API. Com `RUN_SEEDS=true` (padrão do `.env.example`) o banco recebe contas e transferências de demonstração. Em produção use `RUN_SEEDS=false`.

### Comandos

| Comando | O que faz |
| --- | --- |
| `make build` | builda a imagem e sobe API + Postgres (aplica as migrations) |
| `make up` / `make upd` | sobe anexado ao terminal / em segundo plano |
| `make down` | para os containers |
| `make test` | testes de unidade e integração (em container, Postgres via Testcontainers) |
| `make db-seed` | insere os dados de demonstração |
| `make db-reset` | recria o banco, aplica as migrations e insere os seeds |
| `make db-check` | verifica o Postgres e o health check da API |
| `make db-migration name=X` | gera uma nova migration |

### Sem Docker (opcional)

```bash
ConnectionStrings__Transfers="Host=localhost;Port=15432;Database=transfers;Username=transfers;Password=<senha do .env>" \
RUN_SEEDS=true dotnet run --project src/Transfers.Api
```

`dotnet test` funciona direto na máquina se houver Docker rodando (o Testcontainers sobe um Postgres descartável).

---

## Endpoints

| Método | Rota | Descrição |
| --- | --- | --- |
| POST | `/api/transfers` | Transferência imediata (aceita `Idempotency-Key`) |
| POST | `/api/transfers/scheduled` | Agenda transferência (aceita `Idempotency-Key`) |
| POST | `/api/transfers/{id}/cancel` | Cancela um agendamento |
| GET | `/api/transfers/{id}` | Consulta status e dados |
| GET | `/api/transfers` | Lista com filtros (`accountId`, `status`), ordenação e paginação |
| GET | `/api/accounts/{id}` | Conta, limites e uso na última hora |
| GET | `/api/accounts` | Lista com busca (`search`), ordenação e paginação |
| POST | `/api/accounts` | Abre conta |
| PUT | `/api/accounts/{id}/overdraft-limit` | Configura o cheque especial |
| PUT | `/api/accounts/{id}/transfer-limits` | Configura os limites por hora (valor e tentativas, diurno e noturno) |
| POST | `/api/accounts/{id}/block` · `/deactivate` · `/activate` | Altera o status da conta |

Respostas de erro seguem RFC 7807 (`ProblemDetails`):

| Situação | Status |
| --- | --- |
| Dados inválidos (valor ≤ 0, mesma conta, data no passado) | 400 |
| Conta ou transferência inexistente | 404 |
| Cancelar algo que não está agendado, chave de idempotência reutilizada com outros dados, CPF duplicado | 409 |
| Transferência recusada por regra de negócio (saldo, limites, conta inativa) | 422, com a transferência `Failed` no corpo |
| Agendamento com conta de origem ou destino bloqueada/inativa | 422 (nada é gravado) |

---

## Decisões arquiteturais

### Camadas (Clean Architecture)

```
Transfers.Api ──► Transfers.Infrastructure ──► Transfers.Application ──► Transfers.Domain
 (HTTP, Swagger)   (EF Core, Postgres,          (casos de uso, portas,     (agregados, regras,
                    worker, seeds)               apresentação)              eventos)
```

- **Domain** não depende de nada. Contém os agregados `Account` e `Transfer`, value objects (`HourlyLimit`, `TransferLimitPolicy`, `TransferPeriodSchedule`), o serviço de domínio `FundsTransferService` e os eventos de domínio.
- **Application** contém um handler por caso de uso (`ExecuteTransferHandler`, `ScheduleTransferHandler`, `CancelTransferHandler`, `ProcessDueTransferHandler`…), expostos por `ICommandHandler`/`IQueryHandler`. Define as portas (`IAccountRepository`, `ITransferRepository`, `IUnitOfWork`) que a infraestrutura implementa (inversão de dependência).
- **Infrastructure** implementa persistência, travas de concorrência, despacho de eventos, o worker de agendamentos e os seeds.
- **Api** só traduz HTTP ⇄ comandos e `Result` ⇄ status code. Os controllers não têm regra de negócio.

Não usei MediatR nem FluentAssertions: os dois passaram a ter licença comercial, e um handler injetado direto resolve o problema sem indireção extra.

### Modelagem e regras

- **Conta** guarda um único `Balance`, que pode ficar negativo até `-OverdraftLimit`. Assim, *"o valor recebido cobre primeiro o cheque especial"* acontece por construção: `-800 + 1000 = 200`. `AvailableToTransfer = Balance + OverdraftLimit`.
- `Withdraw`/`Deposit` são `internal`: o dinheiro só se move pelo `FundsTransferService`, que valida tudo antes e só depois debita e credita. Uma transferência recusada não altera nenhum saldo.
- **Transferência** é uma máquina de estados explícita: `Scheduled → Processing → Completed | Failed` e `Scheduled → Cancelled`. Transições inválidas lançam exceção (são bugs, não erros de usuário).
- **Sobre o `Processing`:** o estado existe e é uma transição obrigatória do domínio (`StartProcessing`), mas a passagem por ele acontece **dentro da mesma transação** que liquida a transferência. Por isso ninguém observa uma transferência "em processamento" pela API. Foi uma escolha consciente: gravar `Processing` num commit separado criaria transferências presas nesse estado caso a API caísse no meio, exigindo um processo de recuperação. Com tudo numa transação, ou a transferência termina (`Completed`/`Failed`) ou volta a ser `Scheduled` e o worker tenta de novo. Se a execução passar a ser assíncrona via fila, o `Processing` passa a ser persistido entre o consumo da mensagem e a liquidação.
- **Toda tentativa vira registro.** Uma recusa por regra de negócio (saldo, limite, conta inativa) é persistida como `Failed`, com o motivo. É isso que permite contar *"transferências rejeitadas como tentativa"*. Erros de formato (valor ≤ 0, mesma conta, conta inexistente) são 400/404 e não geram tentativa.
- **Limites por hora** usam uma janela deslizante de 60 minutos sobre `processed_at`, por conta de origem. O período (diurno 06:00–22:00, noturno no restante, fuso `America/Sao_Paulo`) é o do instante da tentativa. Valor: soma das concluídas na janela + valor atual ≤ máximo do período. Tentativas: concluídas + recusadas na janela < máximo. Os limites padrão (R$ 5.000/5 de dia, R$ 1.000/3 à noite) vêm do `appsettings.json`, podem ser definidos por conta na abertura e alterados depois em `PUT /api/accounts/{id}/transfer-limits`.
- **Virada de período:** a janela olha os últimos 60 minutos inteiros, mesmo que parte deles seja do outro período, e compara com o limite **do período atual**. Exemplo: R$ 3.000 transferidos às 21:30 ainda contam às 22:10, quando o limite noturno é R$ 1.000, então uma nova transferência às 22:10 é recusada. É a interpretação mais conservadora (impede "estourar" o limite noturno logo após a virada) e é coberta pelo teste `Daytime_transfers_still_count_against_the_nighttime_limit_after_22h`.
- **Ordem das verificações:** tentativas → conta de origem ativa → conta de destino ativa → limite de valor → saldo + cheque especial.
- **Agendamento** valida a data futura, a existência das contas e se **as duas estão ativas** (conta bloqueada/inativa não agenda nem recebe; resposta 422 e nada é gravado). Saldo e limites **não** são exigidos no agendamento, porque podem mudar até a data. No momento da execução, status, saldo, cheque especial e limites são verificados de novo pelo mesmo `FundsTransferService` da transferência imediata; se a conta for bloqueada depois de agendar, a execução falha com o motivo.
- **Erros esperados** usam o padrão `Result` (sem exceção para controlar fluxo). As mensagens ao usuário são em pt-BR e vêm do backend.
- **Respostas prontas para exibição:** a API devolve valores já formatados (`R$ 1.500,00`, datas no fuso do banco), rótulos e cores de status, e flags (`canCancel`, `actions.canBlock`…). Ordenação, filtro, busca e paginação são feitos no servidor. Qualquer cliente consome a API sem reimplementar regra.

### Concorrência (duas transferências ao mesmo tempo)

1. **Trava pessimista por linha:** cada operação abre uma transação e executa `SELECT … FOR UPDATE` nas contas envolvidas, **sempre ordenadas por id**. A segunda transferência que usa o mesmo saldo espera a primeira terminar e então lê o saldo já atualizado. A ordem fixa evita deadlock entre A→B e B→A (há teste para isso).
2. A mesma trava serializa a **contagem de tentativas/valor por hora** da conta de origem, então requisições simultâneas não estouram o limite.
3. **Worker de agendamentos** pega uma transferência vencida por vez com `FOR UPDATE SKIP LOCKED`. Várias instâncias da API podem rodar o worker sem processar o mesmo item duas vezes. O cancelamento trava a mesma linha, então *cancelar* e *executar* nunca acontecem os dois.
4. **Defesa em profundidade:** token de concorrência otimista (`xmin` do Postgres) em contas e transferências, `CHECK` constraints no banco (`balance >= -overdraft_limit`, `amount > 0`, origem ≠ destino) e retry automático do EF/Npgsql em deadlock ou serialization failure.

### Idempotência

O header opcional `Idempotency-Key` é gravado na transferência, com índice único por `(conta de origem, chave)`. Uma repetição com os mesmos dados devolve a transferência original (`200` + `Idempotent-Replayed: true`) sem debitar de novo. A mesma chave reutilizada com outros dados resulta em `409`. Como a verificação ocorre sob a trava da conta de origem, retentativas simultâneas também geram uma única transferência.

### Domain Events e outbox transacional

`TransferScheduled`, `TransferCompleted`, `TransferFailed` e `TransferCancelled` são levantados pelos agregados. A unit of work grava cada evento na tabela `outbox_messages` **na mesma transação** da mudança de estado: ou a transferência e o evento são gravados juntos, ou nenhum dos dois. Se a API cair logo depois do commit, nenhum evento se perde.

O `OutboxWorker` (background service) lê os pendentes em lotes com `FOR UPDATE SKIP LOCKED` e os entrega aos `IDomainEventHandler<T>`. A entrega é *at-least-once*: uma falha incrementa `attempts`, guarda o erro em `last_error` e a mensagem é tentada de novo no próximo ciclo, até `Outbox:MaxAttempts` (depois disso fica parada para análise, como uma dead letter). Por isso os handlers devem ser idempotentes. Hoje um handler registra logs estruturados; publicar em RabbitMQ/Kafka é trocar ou adicionar um handler, sem mexer no domínio.

### Banco

- `numeric(18,2)` para dinheiro; valores com mais de 2 casas são rejeitados.
- `timestamptz` em UTC; a conversão para o fuso do banco só ocorre na apresentação.
- Enums gravados como texto (legíveis em consultas).
- IDs `Guid v7` (ordenáveis por tempo, bons para índice).
- Índices: `(source_account_id, processed_at)` para a janela de limites, índice parcial em `scheduled_for WHERE status = 'Scheduled'` para o worker, índice parcial em `outbox_messages WHERE processed_at IS NULL` para o outbox, índice único parcial para idempotência e índice único para o CPF (uma pessoa, uma conta).

### Por que PostgreSQL

Banco relacional robusto, leve em container, com `FOR UPDATE SKIP LOCKED` nativo (ideal para fila de agendamentos sem broker) e excelente suporte no EF Core via Npgsql.

---

## Cenários do enunciado e onde estão testados

| Cenário | Teste |
| --- | --- |
| Transferência com saldo suficiente | `ImmediateTransferTests.Transfer_with_sufficient_balance_completes` |
| Usando cheque especial | `ImmediateTransferTests.Transfer_using_overdraft_leaves_negative_balance` |
| Ultrapassa o cheque especial | `ImmediateTransferTests.Transfer_beyond_overdraft_is_rejected_and_recorded_as_failed` |
| Recebimento com cheque especial em uso | `ImmediateTransferTests.Receiving_while_in_overdraft_covers_it_first` |
| Acima do limite da hora | `ImmediateTransferTests.Transfer_above_hourly_amount_limit_is_rejected` |
| Excesso de tentativas na hora | `ImmediateTransferTests.Rejected_transfers_count_towards_the_attempt_limit` |
| Período noturno | `ImmediateTransferTests.Nighttime_transfer_uses_nighttime_limits` |
| Agendada sem saldo na execução | `ScheduledTransferTests.Scheduled_transfer_without_funds_at_execution_fails` |
| Duas simultâneas com o mesmo saldo | `ConcurrencyTests.Two_simultaneous_transfers_cannot_spend_the_same_balance` |

Também cobertos: agendamento recusado na hora para conta bloqueada/inativa e execução que falha quando a conta é bloqueada depois de agendar, limites por hora editáveis, eventos gravados no outbox e entregues uma única vez (inclusive com retentativa após falha), 20 transferências paralelas que nunca passam do cheque especial, transferências cruzadas sem deadlock, limite de tentativas sob concorrência, cancelamento disputando com o worker, idempotência (inclusive com retentativas simultâneas), contas bloqueadas/inativas, janela deslizante, ordenação em todas as colunas e busca. São **111 testes** (52 de unidade + 59 de integração com Postgres real).

Os testes de integração usam um `FakeTimeProvider`, então "avançar uma hora" ou "ser 23h" é determinístico.

---

## Evoluções possíveis

- **Mensageria:** o outbox já existe; falta um handler que publique os eventos em RabbitMQ/Kafka e, se fizer sentido, trocar o polling do worker de agendamentos pelo consumo de uma fila com mensagens atrasadas.
- **Livro-razão (ledger):** registrar lançamentos de débito/crédito imutáveis e derivar o saldo, para auditoria e extrato.
- **Autenticação/autorização:** hoje qualquer cliente movimenta qualquer conta.
- **Feriados e fusos por cliente** na definição de dia/noite.
- **Observabilidade:** OpenTelemetry (traces e métricas por motivo de recusa).
