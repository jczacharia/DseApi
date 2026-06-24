import {defineConfig} from '@hey-api/openapi-ts';

export default defineConfig({
  input: './src/Dse.Api/Dse.Api.json',
  output: {
    path: './ui/app/api',
    tsConfigPath: './tsconfig.app.json',
    postProcess: ['prettier'],
  },
  plugins: [
    {
      name: '@hey-api/client-angular',
      throwOnError: true,
    },
    {
      name: '@angular/common',
    },
    {
      name: '@hey-api/transformers',
      dates: 'temporal',
    },
  ],
});
