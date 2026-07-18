# syntax=docker/dockerfile:1.25@sha256:0adf442eae370b6087e08edc7c50b552d80ddf261576f4ebd6421006b2461f12

# datasource=github-releases のものはバージョン番号に v をつける

# renovate: datasource=node-version depName=node
ARG NODE_VERSION=24.18.0

# renovate: datasource=npm depName=@anthropic-ai/claude-code
ARG CLAUDE_CODE_VERSION=2.1.210

# renovate: datasource=npm depName=npm
ARG NPM_VERSION=12.0.1

# renovate: datasource=github-releases depName=bluesky-social/goat
ARG GOAT_VERSION=v0.2.3

# renovate: datasource=npm depName=@devcontainers/cli
ARG DEVCONTAINER_VERSION=0.87.0

# renovate: datasource=npm depName=renovate
ARG RENOVATE_VERSION=43.263.6

# ---- op (1Password CLI) stage ----
FROM 1password/op:2.35.0@sha256:40f3e68389deee432455f647710ea6947b8cbc0b03f51218e5934c01f3ab73d6 AS op

# ---- Main stage ----
FROM mcr.microsoft.com/devcontainers/base:3.0.3-noble@sha256:c5cc2b45afe06a1df3aba17e58ba0dc4a02b999493198dab37dd0ccd4e2b0705

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

COPY --from=op /usr/local/bin/op /usr/local/bin/op

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

WORKDIR /workspaces
