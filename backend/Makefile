SHELL := /bin/bash
COMPOSE := docker compose
TEST_IMAGE := transferencias-backend-tests
MIGRATIONS := src/Transfers.Infrastructure/Persistence/Migrations

.DEFAULT_GOAL := help

.env:
	cp .env.example .env

-include .env
export

.PHONY: help build up upd down logs test db-seed db-reset db-check db-migration

help:
	@grep -E '^[a-z-]+:' Makefile | cut -d: -f1 | sort | uniq

build: .env
	$(COMPOSE) up -d --build --wait

up: .env
	$(COMPOSE) up

upd: .env
	$(COMPOSE) up -d --wait

down:
	$(COMPOSE) down

logs:
	$(COMPOSE) logs -f api

test:
	docker build --target test -t $(TEST_IMAGE) .
	docker run --rm \
		-v /var/run/docker.sock:/var/run/docker.sock \
		--add-host=host.docker.internal:host-gateway \
		-e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal \
		$(TEST_IMAGE)

db-seed: .env
	$(COMPOSE) run --rm --no-deps api seed

db-reset: .env
	$(COMPOSE) down --volumes --remove-orphans
	$(COMPOSE) up -d --wait db
	$(COMPOSE) run --rm --no-deps api seed
	$(COMPOSE) up -d --build --wait

db-check: .env
	$(COMPOSE) exec db pg_isready -U $(POSTGRES_USER) -d $(POSTGRES_DB)
	curl -fsS http://localhost:$(API_PORT)/health && echo

db-migration:
	@test -n "$(name)" || (echo "usage: make db-migration name=MigrationName" && exit 1)
	docker run --rm -v "$(CURDIR)":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 bash -c '\
		dotnet tool install --global dotnet-ef --version 10.0.15 >/dev/null && \
		dotnet restore src/Transfers.Api/Transfers.Api.csproj >/dev/null && \
		/root/.dotnet/tools/dotnet-ef migrations add $(name) -p src/Transfers.Infrastructure -s src/Transfers.Api -o Persistence/Migrations'
	perl -i -ne 's/^\x{EF}\x{BB}\x{BF}//; print unless m{^\s*//}' $(MIGRATIONS)/*.cs
