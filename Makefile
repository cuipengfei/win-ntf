# win-ntf 开发任务入口
# dotnet 不在 PATH 时自动回退到 $HOME/.dotnet，所以 make build / make test 总能跑。

DOTNET := $(shell command -v dotnet 2>/dev/null || echo $(HOME)/.dotnet/dotnet)
SLN := WinNtf.sln
TESTS := tests/WinNtf.Core.Tests/WinNtf.Core.Tests.csproj
APP := src/WinNtf.App/WinNtf.App.csproj
CONFIG ?= Release
DIST := dist
SELF_CONTAINED_DIR := $(DIST)/win-ntf-self-contained
FRAMEWORK_DEPENDENT_DIR := $(DIST)/win-ntf-framework-dependent

.DEFAULT_GOAL := help

.PHONY: help build test restore clean publish-self-contained publish-framework-dependent verify-build-contract

help: ## 显示可用命令
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-12s\033[0m %s\n", $$1, $$2}'

build: ## 编译整个解决方案（CONFIG=Release）
	$(DOTNET) build $(SLN) --configuration $(CONFIG)

test: ## 运行自定义测试运行器
	$(DOTNET) run --project $(TESTS) --configuration $(CONFIG)

restore: ## 还原 NuGet 依赖
	$(DOTNET) restore $(SLN)

clean: ## 清理 build 产物
	$(DOTNET) clean $(SLN) --configuration $(CONFIG)

publish-self-contained: ## 发布自包含 Windows x64 版本（目标机无需预装 .NET）
	$(DOTNET) publish $(APP) --configuration $(CONFIG) --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o $(SELF_CONTAINED_DIR)
	@cp scripts/smoke-win.ps1 $(SELF_CONTAINED_DIR)/smoke-win.ps1

publish-framework-dependent: ## 发布框架依赖 Windows x64 版本（目标机需预装 .NET Desktop Runtime + ASP.NET Core Runtime）
	$(DOTNET) publish $(APP) --configuration $(CONFIG) --runtime win-x64 --self-contained false -p:PublishSingleFile=false -p:EnableCompressionInSingleFile=false -o $(FRAMEWORK_DEPENDENT_DIR)
	@cp scripts/smoke-win.ps1 $(FRAMEWORK_DEPENDENT_DIR)/smoke-win.ps1

verify-build-contract: ## 验证发布入口与 smoke 脚本合同
	bash scripts/verify-build-contract.sh
