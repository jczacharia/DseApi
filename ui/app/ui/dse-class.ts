import {hostAttr} from '#shared/attr';
import {cn} from '#shared/cn';
import {statePipeline, type StatePipelineInterceptOptions} from '#shared/state-pipeline';
import {SuppressTransitions} from '#shared/suppress-transitions';
import {computed, Directive, input} from '@angular/core';
import {type ClassValue} from 'clsx';

/**
 * Composable tailwind class-merging host directive.
 */
@Directive({
  selector: '[dseClass],[class],[className]',
  hostDirectives: [SuppressTransitions],
  host: {'[class]': 'state()'},
})
export class DseClass {
  readonly class = input<ClassValue>(hostAttr('class'));
  readonly className = input<ClassValue>(hostAttr('className'));
  readonly state = statePipeline(computed(() => cn(this.class(), this.className())));
  intercept(fn: () => ClassValue[] | string, opts?: StatePipelineInterceptOptions): () => void {
    return this.state.intercept(({next}) => cn(next, fn()), opts);
  }
}
