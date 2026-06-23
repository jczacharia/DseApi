/* eslint-disable @typescript-eslint/prefer-function-type */
/* eslint-disable @typescript-eslint/no-unsafe-declaration-merging */

import {inject, Injector, runInInjectionContext} from '@angular/core';

export interface Lazy<T> {
  (): T;
}

/**
 * Lazily computes and caches a value on first access.
 */
export class Lazy<T> {
  #cached?: T;
  constructor(factory: () => T) {
    const injector = inject(Injector);
    return () => (this.#cached ??= runInInjectionContext(injector, factory));
  }
}
