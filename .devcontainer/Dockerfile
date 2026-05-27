ARG CLAUDE_CODE_PATH=/opt/claude-code

FROM ubuntu:resolute@sha256:f3d28607ddd78734bb7f71f117f3c6706c666b8b76cbff7c9ff6e5718d46ff64 AS claude-code-install

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
      curl \
      gnupg \
      jq \
      ca-certificates && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /workspaces

COPY ["install-claude-code.sh", "."]

ARG CLAUDE_CODE_PATH
RUN INSTALL_DIR=$CLAUDE_CODE_PATH bash ./install-claude-code.sh

FROM mcr.microsoft.com/devcontainers/base:3-resolute@sha256:bb779775cfc7d2e65f9b0f80f17770878f5a50c00d15cc873b627c5c214ce37e

USER vscode

RUN mkdir -p /home/vscode/.nuget/packages && \
    mkdir -p /home/vscode/.claude

ARG CLAUDE_CODE_PATH
COPY --from=claude-code-install --chown=vscode:vscode --chmod=+x $CLAUDE_CODE_PATH /home/vscode/.local/bin/

WORKDIR /workspaces
