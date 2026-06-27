# syntax=docker/dockerfile:latest@sha256:87999aa3d42bdc6bea60565083ee17e86d1f3339802f543c0d03998580f9cb89

# datasource=github-releases のものはバージョン番号に v をつける

# renovate: datasource=node-version depName=node
ARG NODE_VERSION=24.16.0

# renovate: datasource=npm depName=@anthropic-ai/claude-code
ARG CLAUDE_CODE_VERSION=2.1.190

# renovate: datasource=npm depName=npm
ARG NPM_VERSION=11.17.0

# renovate: datasource=github-releases depName=bluesky-social/goat
ARG GOAT_VERSION=v0.2.3

# renovate: datasource=npm depName=@devcontainers/cli
ARG DEVCONTAINER_VERSION=0.87.0

# renovate: datasource=npm depName=renovate
ARG RENOVATE_VERSION=43.239.0

# ---- Plugin seed stage ----
FROM node:${NODE_VERSION}-slim AS plugin-seed

ARG CLAUDE_CODE_VERSION

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
      git \
      ca-certificates && \
    rm -rf /var/lib/apt/lists/*

RUN npm install -g @anthropic-ai/claude-code@${CLAUDE_CODE_VERSION}

RUN export CLAUDE_CODE_PLUGIN_CACHE_DIR=/claude-plugin-seed && \
    claude plugin marketplace add anthropics/claude-plugins-official && \
    claude plugin marketplace add aetos382/dotnet-skills#add-claude-plugin-manifests && \
    claude plugin marketplace add MicrosoftDocs/mcp && \
    for plugin in \
        claude-code-setup@claude-plugins-official \
        claude-md-management@claude-plugins-official \
        commit-commands@claude-plugins-official \
        feature-dev@claude-plugins-official \
        pr-review-toolkit@claude-plugins-official \
        skill-creator@claude-plugins-official \
        microsoft-docs@microsoft-docs-marketplace \
        dotnet@dotnet-agent-skills \
        dotnet-ai@dotnet-agent-skills \
        dotnet-aspnetcore@dotnet-agent-skills \
        dotnet-blazor@dotnet-agent-skills \
        dotnet-data@dotnet-agent-skills \
        dotnet-diag@dotnet-agent-skills \
        dotnet-maui@dotnet-agent-skills \
        dotnet-msbuild@dotnet-agent-skills \
        dotnet-nuget@dotnet-agent-skills \
        dotnet-template-engine@dotnet-agent-skills \
        dotnet-test@dotnet-agent-skills; do \
        claude plugin install "$plugin"; \
    done

# ---- Main stage ----
FROM mcr.microsoft.com/devcontainers/base:3-noble@sha256:6e5abd134eb1c2b747927487c3d59590bcc17815d5d7b3afd494e1be5de0f6b4

RUN sed -i.bak 's|http://archive.ubuntu.com/ubuntu/|https://ftp.udx.icscoe.jp/Linux/ubuntu/|g' /etc/apt/sources.list.d/ubuntu.sources

ARG NODE_VERSION
ARG NPM_VERSION
ARG GOAT_VERSION
ARG CLAUDE_CODE_VERSION
ARG DEVCONTAINER_VERSION
ARG RENOVATE_VERSION

RUN mkdir /usr/local/node && \
    curl -fqsLS https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-x64.tar.xz -o node.tar.xz && \
    tar -C /usr/local/node -xJf node.tar.xz --strip-components=1 && \
    rm node.tar.xz

RUN mkdir /usr/local/goat && \
    curl -fqsLS https://github.com/bluesky-social/goat/releases/download/${GOAT_VERSION}/goat_Linux_x86_64.tar.gz -o goat.tar.gz && \
    tar -C /usr/local/goat -xzf goat.tar.gz && \
    rm goat.tar.gz

RUN /usr/local/node/bin/node --completion-bash > /usr/share/bash-completion/completions/node

USER vscode

WORKDIR /home/vscode

RUN mkdir -p .nuget/packages .npm .npm-global .claude

ENV PATH=/usr/local/node/bin:/usr/local/goat:/home/vscode/.npm-global/bin:$PATH

# << と 'EOF' の間に空白を開けない
# https://github.com/rcjsuen/dockerfile-utils/issues/130
RUN cat >> /home/vscode/.npmrc <<'EOF'
cache=/home/vscode/.npm
prefix=/home/vscode/.npm-global
save-exact=true
EOF

RUN npm install -g npm@$NPM_VERSION

RUN npm install -g \
    @anthropic-ai/claude-code@$CLAUDE_CODE_VERSION \
    @devcontainers/cli@$DEVCONTAINER_VERSION \
    renovate@$RENOVATE_VERSION

COPY --from=plugin-seed --chown=vscode:vscode /claude-plugin-seed /home/vscode/.claude-plugin-seed
ENV CLAUDE_CODE_PLUGIN_SEED_DIR=/home/vscode/.claude-plugin-seed

WORKDIR /workspaces
