import { relative } from 'node:path';

// dotnet format --include matches paths relative to the solution; lint-staged passes
// absolute paths, so convert them or the formatter silently no-ops.
const toRelative = (files) => files.map((file) => relative(process.cwd(), file));

export default {
  '*.{ts,js,html}': ['eslint --fix'],
  '*.cs': (files) =>
    `dotnet format whitespace Dse.slnx --no-restore --include ${toRelative(files).join(' ')}`,
  '*.{ts,js,html,json,css,scss,md,svg,csproj}': ['prettier --ignore-path .gitignore --write'],
};
