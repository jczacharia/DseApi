import {defineConfig} from '@hey-api/openapi-ts';

export default defineConfig({
  input: './src/Dse.Api/Dse.Api.json',
  output: {
    path: './ui/app/api',
    tsConfigPath: './tsconfig.app.json',
    postProcess: ['prettier'],
  },
  // plugins: [
  //   {name: '@angular/common', httpResources: 'flat'},
  //   // {name: '@hey-api/client-angular', throwOnError: true},
  //   {name: '@hey-api/schemas'},
  //   {name: '@hey-api/transformers'},
  //   // {name: 'zod'},
  //   // {name: '@hey-api/sdk', transformer: true},
  //   // {name: '@hey-api/typescript'},
  // ],
  plugins: [
    {
      name: '@hey-api/client-angular',
      throwOnError: true,
    },
    {
      httpRequests: true,
      httpResources: 'flat',
      name: '@angular/common',
    },
    '@hey-api/schemas',
    '@hey-api/transformers',
    {
      name: '@hey-api/sdk',
      responseStyle: 'data',
      transformer: true,
    },
    {
      enums: 'javascript',
      name: '@hey-api/typescript',
    },
  ],
});
