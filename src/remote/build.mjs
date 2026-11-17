import { build } from 'esbuild';
import { cp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { basename, dirname, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

// The remote is a static page: bundle local TS/CSS and copy its public assets.
// No development server or glob-pattern dependency is needed in the build.
const root = fileURLToPath(new URL('.', import.meta.url));
const output = resolve(root, 'dist');
if (dirname(output) !== resolve(root) || basename(output) !== 'dist') throw new Error('Unexpected build directory.');
await rm(output, { recursive: true, force: true });
await mkdir(output, { recursive: true });
await cp(resolve(root, 'public'), output, { recursive: true });
const result = await build({
  absWorkingDir: root, entryPoints: ['src/main.ts'], outdir: output,
  entryNames: 'assets/[name]-[hash]', bundle: true, format: 'esm', platform: 'browser',
  target: ['es2022', 'safari17'], minify: true, metafile: true, logLevel: 'info',
});
const entry = Object.entries(result.metafile.outputs).find(([name, info]) => name.endsWith('.js') && info.entryPoint === 'src/main.ts');
if (!entry || !entry[1].cssBundle) throw new Error('Missing browser script or stylesheet.');
const assetUrl = name => '/' + relative(output, resolve(root, name)).replaceAll('\\', '/');
let html = await readFile(resolve(root, 'index.html'), 'utf8');
const script = '<script type="module" src="/src/main.ts"></script>';
if (!html.includes(script)) throw new Error('Missing HTML script entry.');
html = html.replace(script, `<script type="module" src="${assetUrl(entry[0])}"></script>`)
  .replace('</head>', `  <link rel="stylesheet" href="${assetUrl(entry[1].cssBundle)}">\n</head>`);
await writeFile(resolve(output, 'index.html'), html);
