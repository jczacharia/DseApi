import {relative} from 'node:path';

/**
 * @type {import('lint-staged').Configuration}
 */
export default {
  '*.{ts,js,html}': ['eslint --fix'],
  '*.cs': (files) => `dotnet format Dse.slnx --include ${files.map((file) => relative(process.cwd(), file)).join(' ')}`,
  '*.{ts,js,html,json,css,scss,md,svg,csproj}': ['prettier --ignore-path .gitignore --write'],
  '*.{ts,js,html,md,cs}': ['cspell --quiet --no-must-find-files'],
};
