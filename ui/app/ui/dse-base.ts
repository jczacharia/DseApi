import {hostAttr, nonEmptyStringAttribute, softDisabledAttribute} from '#shared/attr';
import {supportsDisabled} from '#shared/dom-validators';
import {IdGenerator} from '#shared/id-generator';
import {injectElement} from '#shared/inject-element';
import {statePipeline} from '#shared/state-pipeline';
import {computed, Directive, inject, input, output} from '@angular/core';
import {watcher} from '@signality/core';
import {DseClass} from './dse-class';
import {DseStyle} from './dse-style';

/** Composable base host directive for interactive components. */
@Directive({
  selector: '[dseBase]',
  exportAs: 'dseBase',
  hostDirectives: [
    {directive: DseStyle, inputs: ['style']},
    {directive: DseClass, inputs: ['class', 'className']},
  ],
  host: {
    '[attr.id]': 'resolvedId()',
    '[attr.role]': 'resolvedRole()',
    '[attr.disabled]': 'disabledAttr()',
    '[attr.aria-disabled]': 'ariaDisabledAttr()',
    '[attr.data-disabled]': 'disabledVariant()',
  },
})
export class DseBase {
  readonly #idGenerator = inject(IdGenerator);
  readonly #element = injectElement();

  readonly #generatedId = this.#idGenerator();
  readonly #initId = nonEmptyStringAttribute(hostAttr('id'));
  readonly id = input(this.#initId, {transform: nonEmptyStringAttribute});
  readonly resolvedId = computed(() => this.id() || this.#generatedId);

  readonly #initRole = nonEmptyStringAttribute(hostAttr('role'));
  readonly role = input(this.#initRole, {transform: nonEmptyStringAttribute});
  readonly resolvedRole = statePipeline(this.role);
  readonly roleChange = output<string | null>();

  readonly nativeDisable = supportsDisabled(this.#element);
  readonly disabled = input(false, {transform: softDisabledAttribute});
  readonly resolvedDisabled = statePipeline(this.disabled);
  readonly disabledChange = output<boolean | 'soft'>();

  readonly softDisabled = computed(() => this.resolvedDisabled() === 'soft');
  readonly hardDisabled = computed(() => this.resolvedDisabled() === true);
  readonly disabledVariant = computed(() => {
    if (this.softDisabled()) return 'soft';
    if (this.hardDisabled()) return 'hard';
    return null;
  });

  protected readonly disabledAttr = computed(() => (this.nativeDisable && this.hardDisabled() ? '' : null));
  protected readonly ariaDisabledAttr = computed(() => {
    if (this.nativeDisable) return this.softDisabled() ? 'true' : null;
    return this.resolvedDisabled() ? 'true' : null;
  });

  constructor() {
    watcher(this.resolvedDisabled, (disabled) => this.disabledChange.emit(disabled));
    watcher(this.resolvedRole, (role) => this.roleChange.emit(role));
  }
}
