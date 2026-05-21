SHELL := /bin/zsh

BACKEND_DIR := backend
CLIENT_DIR := swift-client
IOS_DIR := ios-app

.PHONY: help build setup test backend-install backend-setup backend-migrate backend-serve backend-test client-build client-run ios-open clean

help:
	@echo "Available targets:"
	@echo "  make build           - build backend dependencies and Swift client"
	@echo "  make setup           - prepare backend env and run migrations"
	@echo "  make test            - run Laravel tests"
	@echo "  make backend-install - install PHP dependencies"
	@echo "  make backend-setup   - create .env if needed and generate app key"
	@echo "  make backend-migrate - run Laravel migrations"
	@echo "  make backend-serve   - start Laravel dev server"
	@echo "  make backend-test    - run Laravel tests"
	@echo "  make client-build    - build macOS Swift client (swift-client)"
	@echo "  make client-run      - run macOS Swift client"
	@echo "  make ios-open        - open iOS project in Xcode (ios-app)"
	@echo "  make clean           - remove Swift build artifacts"

build: backend-install client-build

setup: backend-setup backend-migrate

test: backend-test

backend-install:
	cd $(BACKEND_DIR) && composer install

backend-setup:
	cd $(BACKEND_DIR) && [ -f .env ] || cp .env.example .env
	cd $(BACKEND_DIR) && php artisan key:generate --force

backend-migrate:
	cd $(BACKEND_DIR) && php artisan migrate

backend-serve:
	cd $(BACKEND_DIR) && php artisan serve

backend-test:
	cd $(BACKEND_DIR) && php artisan test

client-build:
	cd $(CLIENT_DIR) && swift build

client-run:
	cd $(CLIENT_DIR) && swift run tree-client

ios-open:
	cd $(IOS_DIR) && open TreeCoursework.xcodeproj

clean:
	cd $(CLIENT_DIR) && rm -rf .build
