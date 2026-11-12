SHELL := /bin/bash
COMPOSE := docker compose
E2E_PROJECT := transferencias-e2e
# Portas próprias para não conflitar com o ambiente de desenvolvimento rodando ao lado.
E2E_COMPOSE := DB_PORT=25432 API_PORT=25080 WEB_PORT=25173 $(COMPOSE) -p $(E2E_PROJECT)
PLAYWRIGHT_IMAGE := mcr.microsoft.com/playwright:v1.65.0-noble

.DEFAULT_GOAL := help

.PHONY: help up down logs reset test test-backend e2e

help: ## lista os comandos
	@grep -E '^[a-z-]+:.*## ' Makefile | awk -F':.*## ' '{printf "  make %-14s %s\n", $$1, $$2}'

up: ## builda e sobe Postgres + API + frontend
	$(COMPOSE) up -d --build --wait
	@echo
	@echo "  Frontend: http://localhost:$${WEB_PORT:-15173}"
	@echo "  Swagger:  http://localhost:$${API_PORT:-15080}/swagger"

down: ## para os containers (mantém o banco)
	$(COMPOSE) down

logs: ## acompanha os logs da API e do frontend
	$(COMPOSE) logs -f api web

reset: ## recria o banco do zero e sobe tudo com os dados de demonstração
	$(COMPOSE) down --volumes --remove-orphans
	$(MAKE) up

test: test-backend e2e ## roda todos os testes (backend + e2e)

test-backend: ## testes de unidade e integração do backend (Postgres via Testcontainers)
	$(MAKE) -C backend test

e2e: ## testes e2e (Playwright, desktop e mobile) num ambiente isolado e descartável
	$(E2E_COMPOSE) up -d --build --wait db api web
	status=0; \
	docker run --rm --network $(E2E_PROJECT)_default \
		-e E2E_BASE_URL=http://web \
		-v "$(CURDIR)/frontend":/app -v /app/node_modules -w /app \
		$(PLAYWRIGHT_IMAGE) \
		sh -c 'npm ci --no-audit --no-fund && npx playwright test --reporter=list' || status=$$?; \
	$(E2E_COMPOSE) down --volumes --remove-orphans; \
	exit $$status
