import {statePipeline, type StatePipelineInterceptOptions} from '#shared/state-pipeline';
import {SuppressTransitions} from '#shared/suppress-transitions';
import {Directive, input} from '@angular/core';

export type DseStyles = Record<string, string | number | boolean | null | undefined>;

/**
 * Composable style-merging host directive.
 */
@Directive({
  selector: '[dseStyle],[style]',
  hostDirectives: [SuppressTransitions],
  host: {'[style]': 'state() || null'},
})
export class DseStyle {
  readonly style = input<DseStyles>({});
  readonly state = statePipeline.deep<DseStyles>(this.style);
  intercept(fn: () => Partial<DseStyles>, opts?: StatePipelineInterceptOptions): () => void {
    return this.state.intercept(({next}) => ({...next(), ...fn()}), opts);
  }
}
