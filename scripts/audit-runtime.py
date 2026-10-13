#!/usr/bin/env python3
"""Query GitHub's public advisory feed for the pinned bundled runtime package versions."""
import json
import os
from pathlib import Path
import urllib.parse
import urllib.request

VERSION = '10.0.13'  # SDK 10.0.402 runtime; packaging independently verifies actual runtime.
PACKAGES = ['Microsoft.NETCore.App', 'Microsoft.AspNetCore.App', 'Microsoft.WindowsDesktop.App'] + [
    'Microsoft.NETCore.App.Runtime.' + rid for rid in ['win-x64', 'osx-arm64', 'osx-x64', 'linux-x64']]


def main():
    results = {}
    for package in PACKAGES:
        url = 'https://api.github.com/advisories?' + urllib.parse.urlencode({'ecosystem': 'nuget', 'affects': package + '@' + VERSION, 'per_page': 100})
        headers = {'Accept': 'application/vnd.github+json', 'User-Agent': 'PCRemote-runtime-audit'}
        if os.environ.get('GITHUB_TOKEN'):  # CI's read-only job token avoids shared-runner anonymous rate limits.
            headers['Authorization'] = 'Bearer ' + os.environ['GITHUB_TOKEN']
        request = urllib.request.Request(url, headers=headers)
        with urllib.request.urlopen(request, timeout=30) as response:
            records = json.load(response)
            if response.headers.get('Link') and 'rel="next"' in response.headers['Link']:
                raise RuntimeError('Advisory response requires pagination; cannot declare passed.')
        if not isinstance(records, list):
            raise RuntimeError('Unusable advisory response for ' + package)
        results[package] = [a['ghsa_id'] for a in records]
        if records:
            raise RuntimeError('Runtime advisory for ' + package + ': ' + ', '.join(results[package]))
    print(json.dumps({'runtimeVersion': VERSION, 'githubAdvisories': results}, indent=2))
    print('No matching advisories in the queried public feed. npm/NuGet audits and current OS security updates remain required.')


if __name__ == '__main__':
    main()
