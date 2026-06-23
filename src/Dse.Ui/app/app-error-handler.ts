import {environment} from '#environment';
import {ErrorHandler, Service} from '@angular/core';

@Service()
export class AppErrorHandler extends ErrorHandler {
  override handleError(error: unknown): void {
    console.error(`[${environment.name}] DSE App Error:`, error); // eslint-disable-line no-console
  }
}
