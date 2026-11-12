SHELL := /bin/bash
COMPOSE := docker compose

.DEFAULT_GOAL := help

.env:
	cp .env.example .env

-include .env
export

.PHONY: help install build up upd down logs dev test e2e

help:
	@grep -E '^[a-z-]+:' Makefile | cut -d: -f1 | sort | uniq

install:
	npm ci
	npx playwright install chromium

build: .env
	$(COMPOSE) up -d --build --wait

up: .env
	$(COMPOSE) up

upd: .env
	$(COMPOSE) up -d --wait

down:
	$(COMPOSE) down

logs:
	$(COMPOSE) logs -f web

dev: .env
	npm run dev

test: .env install
	npm run typecheck
	npm run e2e

e2e: test
