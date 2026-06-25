// hey-api hardcodes `import {Temporal} from 'temporal-polyfill'` into its generated
// types (the source is not configurable). We target native Temporal — TS 6 + lib
// "ESNext" expose `Temporal` as a global — so that import is dead weight and pulls
// in an undeclared package. Strip it post-generation. Wired into `api:gen`.
import {readdirSync, readFileSync, writeFileSync} from 'node:fs';
import {join} from 'node:path';

const dir = 'ui/app/api';
const polyfillImport = /^import\s*\{[^}]*}\s*from\s*['"]temporal-polyfill['"];?\r?\n/m;

for (const file of readdirSync(dir)) {
  if (!file.endsWith('.gen.ts')) continue;
  const path = join(dir, file);
  const before = readFileSync(path, 'utf8');
  const after = before.replace(polyfillImport, '');
  if (after !== before) writeFileSync(path, after);
}
