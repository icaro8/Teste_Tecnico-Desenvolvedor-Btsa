# Transferências Financeiras — Frontend

Interface em React que consome a API REST de transferências: lista e abre contas, mostra saldo, cheque especial e limites por hora (e permite alterá-los), realiza transferências imediatas ou agendadas, consulta o status e cancela agendamentos.

| | |
| --- | --- |
| Stack | React 19, TypeScript, Vite, React Router |
| Servidor | nginx (arquivos estáticos + proxy de `/api` para a API configurada em `API_URL`) |
| Testes | Playwright (e2e), em desktop e mobile |

## Como executar

> Para subir **tudo** (banco, API e frontend) de uma vez, use o `docker compose` da raiz do repositório; veja o [README principal](../README.md). Os comandos abaixo sobem só o frontend, apontando para uma API já rodando.

Pré-requisito: Docker.

```bash
make build
# sem make: cp .env.example .env && docker compose up -d --build --wait
```

Acesse http://localhost:15173. O frontend sobe sozinho; as telas mostram os dados assim que a API configurada em `API_URL` responde.

| Variável (`.env`) | Para quê |
| --- | --- |
| `WEB_PORT` | porta local do frontend |
| `API_URL` | endereço da API REST (usado pelo nginx e pelo `npm run dev`) |
| `E2E_BASE_URL` | URL testada pelo Playwright |

### Comandos

| Comando | O que faz |
| --- | --- |
| `make build` | builda a imagem e sobe o frontend |
| `make up` / `make upd` | sobe anexado ao terminal / em segundo plano |
| `make down` | para o container |
| `make dev` | servidor de desenvolvimento do Vite |
| `make test` | type check + testes e2e com Playwright |

## Decisões

- **Sem regra de negócio:** valores formatados, rótulos e cores de status e as ações permitidas (`canCancel`, `actions.canBlock`…) vêm prontos da API. A tela só exibe e envia as ações do usuário.
- **Listas pela API:** ordenação em todas as colunas, filtro, busca e paginação são pedidos à API; o estado fica na URL (`?sortBy=…&sortDirection=…&page=…`), então dá para compartilhar o link.
- **Idempotência:** cada envio do formulário de transferência manda um `Idempotency-Key`; um duplo clique não gera duas transferências.
- **Recusas visíveis:** uma transferência recusada mostra o motivo e o link para a tentativa registrada.
- **Tema claro e escuro:** segue o sistema por padrão, e um seletor (Sistema / Claro / Escuro) guarda a escolha no navegador. Um script no `index.html` aplica o tema antes da primeira pintura, então a página não pisca. As cores são tokens CSS com `light-dark()`.
- **Layout responsivo:** no desktop, menu lateral fixo e conteúdo em largura total. Abaixo de 1024px, barra superior e uma barra de navegação flutuante embaixo, com atalho para nova transferência. No celular, as linhas das tabelas viram cards, e os cabeçalhos viram uma faixa de botões de ordenação.
- **Limites editáveis:** a página da conta permite alterar o cheque especial e os limites por hora (valor e tentativas, diurno e noturno).
- **Funciona fora de contexto seguro:** a chave de idempotência usa `crypto.randomUUID` quando disponível e, acessando pelo IP da rede (sem HTTPS), recorre a `crypto.getRandomValues`.

## Testes e2e

São 19 cenários executados em desktop e mobile (38 testes): listagem e ordenação de contas e transferências em todas as colunas, busca, abertura de conta, bloqueio/reativação, alteração do cheque especial e dos limites por hora, transferência imediata, uso do cheque especial, recusa por saldo, agendamento e cancelamento, agendamento recusado para conta bloqueada e erro de data no passado. Como são testes de ponta a ponta, eles exercitam a tela contra a API em `API_URL`; cada teste cria as próprias contas, então podem rodar em paralelo e repetidos.

Para rodá-los sem sujar o banco de desenvolvimento, use `make e2e` na **raiz** do repositório: ele sobe banco, API e frontend num projeto Docker separado, roda o Playwright num container e destrói tudo no final.
