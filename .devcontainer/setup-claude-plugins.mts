import { execSync } from 'node:child_process';
import { readFileSync } from 'node:fs';

const settings = JSON.parse(readFileSync('.claude/settings.json', 'utf8'));

for (const marketplace of Object.values<{ source?: { source: string; repo?: string; ref?: string } }>(settings.extraKnownMarketplaces ?? {})) {
    if (marketplace.source?.source === 'github' && marketplace.source.repo) {
        const repoArg = marketplace.source.ref
            ? `${marketplace.source.repo}#${marketplace.source.ref}`
            : marketplace.source.repo;
        execSync(`claude plugin marketplace add ${repoArg}`, { stdio: 'inherit' });
    }
}

for (const [name, enabled] of Object.entries<boolean>(settings.enabledPlugins ?? {})) {
    if (enabled) {
        execSync(`claude plugin install --scope project ${name}`, { stdio: 'inherit' });
    }
}
