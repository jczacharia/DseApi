import {client} from '#api/client.gen';
import {provideHeyApiClient} from '#api/client/client.gen';
import {Source} from '#core/source/source';
import {pageTitle} from '#core/state/page-title';
import {Theme} from '#core/theme/theme';
import {provideHttpClient} from '@angular/common/http';
import {
  ErrorHandler,
  inject,
  injectAsync,
  provideBrowserGlobalErrorListeners,
  provideEnvironmentInitializer,
  type ApplicationConfig,
  type DefaultExport,
  type ProviderToken,
} from '@angular/core';
import {
  provideRouter,
  RouteReuseStrategy,
  withComponentInputBinding,
  withExperimentalAutoCleanupInjectors,
  withInMemoryScrolling,
  withRouterConfig,
  type Route,
} from '@angular/router';
import {AppErrorHandler} from './app-error-handler';
import {AppReuseStrategy} from './app-reuse-strategy';

export const appConfig: ApplicationConfig = {
  providers: [
    {provide: ErrorHandler, useExisting: AppErrorHandler},
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(),
    provideHeyApiClient(client),
    provideEnvironmentInitializer(() => inject(Theme)),
    {provide: RouteReuseStrategy, useClass: AppReuseStrategy},
    provideRouter(
      [
        {
          path: '',
          loadComponent: () => import('./app-shell'),
          children: [
            {
              path: '',
              children: [
                {path: '', outlet: 'sidebar', loadComponent: () => import('./app-sidebar')},
                {
                  path: '',
                  pathMatch: 'full',
                  title: () => pageTitle.from('Home'),
                  loadComponent: () => import('#features/home/home'),
                },
              ],
            },
            routeSource(() => import('#sources/confluence/confluence')),
            routeSource(() => import('#sources/jira/jira')),
          ],
        },
      ],
      withComponentInputBinding(),
      withExperimentalAutoCleanupInjectors(),
      withInMemoryScrolling({anchorScrolling: 'enabled', scrollPositionRestoration: 'enabled'}),
      withRouterConfig({defaultQueryParamsHandling: 'merge', paramsInheritanceStrategy: 'always'}),
    ),
  ],
};

export function routeSource<S extends Source>(sourceFn: () => Promise<DefaultExport<ProviderToken<S>>>): Route {
  return {
    path: '',
    loadChildren: () =>
      injectAsync(sourceFn)().then((source): Route[] => [
        {
          path: source.key,
          data: {source},
          providers: [{provide: Source, useValue: source}],
          title: () => pageTitle(source.options.name)(),
        },
      ]),
  };
}
