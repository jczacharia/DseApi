import {inject, Injectable, type Signal} from '@angular/core';
import {isActive, Router} from '@angular/router';

const SOURCE_BRAND = Symbol(ngDevMode ? 'SOURCE_BRAND' : undefined);

export interface SourceOptions {
  readonly name: string;
}

@Injectable()
export abstract class Source {
  declare private readonly [SOURCE_BRAND]: true;
  readonly searchPath: string;
  readonly isActive: Signal<boolean>;

  constructor(
    readonly key: string,
    readonly options: SourceOptions,
  ) {
    Object.defineProperty(this, SOURCE_BRAND, {value: true});
    this.searchPath = `api/sources/${this.key}/search`;
    this.isActive = isActive(`/${this.key}`, inject(Router), {
      paths: 'subset',
      fragment: 'ignored',
      matrixParams: 'ignored',
      queryParams: 'ignored',
    });
  }
}

export function isSource(value: unknown): value is Source {
  return typeof value === 'object' && value !== null && SOURCE_BRAND in value;
}

export type SourceKey<S extends Source> = S extends Source ? S['key'] : never;
