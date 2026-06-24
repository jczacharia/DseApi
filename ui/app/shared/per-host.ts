import {injectElement} from '#shared/inject-element';
import {inject, InjectionToken, Injector, runInInjectionContext, type ProviderToken, type Type} from '@angular/core';

const HOST_MAP = new InjectionToken(ngDevMode ? 'HOST_MAP' : '', {
  factory: () => new WeakMap<HTMLElement, Map<ProviderToken<unknown>, unknown>>(),
});

function resolver<T>(token: ProviderToken<T>, factory: () => T): T {
  const element = injectElement();
  const injector = inject(Injector);
  const map = inject(HOST_MAP).getOrInsertComputed(element, () => new Map()) as Map<ProviderToken<T>, T>;
  return map.getOrInsertComputed(token, () => runInInjectionContext(injector, factory));
}

/**
 * Factory whose instances should be shared between host HTML elements.
 */
export function perHost<T>(factory: () => T): InjectionToken<T> {
  const token = new InjectionToken<T>(ngDevMode ? factory.name : '');
  Object.defineProperty(token, '__NG_ELEMENT_ID__', {
    value: (): T => resolver<T>(token, factory),
    configurable: true,
    enumerable: true,
    writable: true,
  });
  return token;
}

/**
 * Class decorator whose instances should be shared between host HTML elements.
 */
export function PerHost() {
  return <T extends object, C extends Type<T>>(type: C) => perHost(() => new type()) as unknown as C;
}
