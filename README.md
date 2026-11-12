# Transferências Financeiras

Teste técnico para **Desenvolvedor .NET Pleno**: uma API REST e uma interface web para realizar, agendar, consultar e cancelar transferências entre contas. O sistema trata cheque especial, limites por hora diferentes de dia e de noite, e transferências simultâneas.

| | |
| --- | --- |
| **Backend** | .NET 10, ASP.NET Core, EF Core 10, PostgreSQL 17, Swagger: [`backend/`](backend/README.md) |
| **Frontend** | React 19, TypeScript, Vite, React Router: [`frontend/`](frontend/README.md) |
| **Testes** | 111 de unidade e integração (xUnit + Testcontainers) e 19 cenários e2e (Playwright, desktop e mobile) |
| **Infra** | Docker Compose; um comando sobe tudo |

## Como executar

Pré-requisito: **Docker**. Nada mais precisa estar instalado.

```bash
docker compose up -d --build --wait
# ou: make up
```

| O quê | Endereço |
| --- | --- |
| Frontend | http://localhost:15173 |
| Swagger (API) | http://localhost:15080/swagger |
| Health check | http://localhost:15080/health |

As migrations são aplicadas quando a API sobe, e o banco recebe contas e transferências de demonstração. Para mudar portas, senha ou desligar os dados de demonstração, copie `.env.example` para `.env` e ajuste. Todas as variáveis têm valor padrão, então o `.env` é opcional.

### Comandos (`make`)

| Comando | O que faz |
| --- | --- |
| `make up` | builda e sobe Postgres, API e frontend |
| `make down` | para os containers (o banco é mantido) |
| `make reset` | recria o banco do zero e sobe tudo de novo |
| `make logs` | acompanha os logs da API e do frontend |
| `make test` | roda **todos** os testes: backend e e2e |
| `make test-backend` | testes de unidade e integração (Postgres descartável via Testcontainers) |
| `make e2e` | testes e2e num ambiente isolado (projeto `transferencias-e2e`, portas próprias), destruído no final |

`backend/` e `frontend/` também têm `docker-compose.yml` e `Makefile` próprios, para trabalhar em cada parte separadamente.

## Estrutura

```
.
├── docker-compose.yml        # banco + API + frontend
├── backend/
│   ├── src/
│   │   ├── Transfers.Domain/          # agregados, regras, eventos (sem dependências)
│   │   ├── Transfers.Application/     # casos de uso (handlers), portas, apresentação
│   │   ├── Transfers.Infrastructure/  # EF Core, Postgres, travas, outbox, workers, seeds
│   │   └── Transfers.Api/             # controllers, Swagger, ProblemDetails
│   └── tests/                         # unidade + integração com Postgres real
└── frontend/
    ├── src/                           # páginas, componentes, cliente da API
    └── e2e/                           # Playwright
```

## Requisitos do enunciado

| Requisito | Como foi atendido |
| --- | --- |
| Transferir, agendar, consultar e cancelar | `POST /api/transfers`, `POST /api/transfers/scheduled`, `GET /api/transfers/{id}`, `POST /api/transfers/{id}/cancel`, com as telas correspondentes |
| Conta com saldo e cheque especial configurável | `GET /api/accounts/{id}`, `PUT /api/accounts/{id}/overdraft-limit`; um CPF, uma conta |
| Conta bloqueada/inativa não envia nem recebe | Recusado na transferência imediata, **no agendamento** e de novo na execução do agendamento |
| Origem ≠ destino, valor > 0, saldo + cheque especial | Validado no domínio e reforçado por `CHECK` constraints no banco |
| Débito e crédito atômicos | Uma transação por operação; o dinheiro só se move pelo `FundsTransferService` |
| Recebimento cobre primeiro o cheque especial | Saldo único que pode ser negativo: `-800 + 1000 = 200` |
| Limite de valor e de tentativas por hora, dia ≠ noite, recusas contam | Janela deslizante de 60 min; toda recusa é gravada como `Failed`. Limites editáveis por conta |
| Agendamento: data futura, cancelável, revalidado na execução | Worker em background com `FOR UPDATE SKIP LOCKED`; as mesmas regras da transferência imediata |
| Status Scheduled, Processing, Completed, Failed, Cancelled | Máquina de estados explícita no agregado `Transfer` |
| .NET 8+, REST, EF Core com banco relacional, Swagger | .NET 10, PostgreSQL, migrations versionadas, `/swagger` |
| Duas transferências simultâneas | Trava pessimista ordenada por id (sem deadlock), token otimista e testes de concorrência |
| README, migrations, decisões arquiteturais | Este arquivo e o [README do backend](backend/README.md#decisões-arquiteturais) |

### Diferenciais

| Diferencial | Situação |
| --- | --- |
| Docker | ✅ Um `docker compose up` sobe tudo; imagens com healthcheck e limites de recurso |
| Testes de integração | ✅ API real contra Postgres real (Testcontainers), com relógio controlado |
| Background worker para agendamentos | ✅ `ScheduledTransfersWorker`, seguro com várias instâncias |
| Domain Events | ✅ Gravados num **outbox transacional** e entregues por um worker (*at-least-once*, com retentativa) |
| Idempotência | ✅ Header `Idempotency-Key`; o frontend envia uma chave por envio do formulário |
| Mensageria | ◑ O outbox é a base: publicar em RabbitMQ/Kafka é só adicionar um handler. Não há broker no compose, para manter a solução simples |

## Principais decisões

Detalhes e justificativas estão no [README do backend](backend/README.md#decisões-arquiteturais). Em resumo:

- **Clean Architecture com DDD:** o domínio não depende de nada; regras ficam nos agregados (`Account`, `Transfer`) e num serviço de domínio (`FundsTransferService`). Os controllers não têm regra de negócio.
- **Toda tentativa vira registro:** uma recusa por regra de negócio é gravada como `Failed`, com o motivo. É assim que as recusas contam para o limite de tentativas, e o usuário consegue consultar o que aconteceu.
- **Concorrência resolvida no banco:** `SELECT … FOR UPDATE` nas contas, sempre na mesma ordem, dentro de uma transação. A segunda transferência que usa o mesmo saldo espera a primeira terminar e enxerga o saldo atualizado.
- **Erros esperados como `Result`**, não exceções; respostas de erro em RFC 7807 (`ProblemDetails`) com mensagens em pt-BR.
- **Frontend sem regra de negócio:** a API devolve valores formatados, rótulos, cores de status e as ações permitidas. Ordenação, filtro e paginação acontecem no servidor.
