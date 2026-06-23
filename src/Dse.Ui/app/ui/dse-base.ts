import {softDisabledAttribute, hostAttr, nonEmptyStringAttribute} from '#shared/attr';
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
    '[attr.id]': 'id()',
    '[attr.role]': 'role()',
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
  readonly idInput = input(this.#initId, {alias: 'id', transform: nonEmptyStringAttribute});
  readonly id = computed(() => this.idInput() || this.#generatedId);

  readonly #initRole = nonEmptyStringAttribute(hostAttr('role'));
  readonly roleInput = input(this.#initRole, {alias: 'role', transform: nonEmptyStringAttribute});
  readonly role = statePipeline(this.roleInput);
  readonly roleChange = output<string | null>();

  readonly nativeDisable = supportsDisabled(this.#element);
  readonly disabledInput = input(false, {alias: 'disabled', transform: softDisabledAttribute});
  readonly disabled = statePipeline(this.disabledInput);
  readonly disabledChange = output<boolean | 'soft'>();

  readonly softDisabled = computed(() => this.disabled() === 'soft');
  readonly hardDisabled = computed(() => this.disabled() === true);
  readonly disabledVariant = computed(() => {
    if (this.softDisabled()) return 'soft';
    if (this.hardDisabled()) return 'hard';
    return null;
  });

  protected readonly disabledAttr = computed(() => (this.nativeDisable && this.hardDisabled() ? '' : null));
  protected readonly ariaDisabledAttr = computed(() => {
    if (this.nativeDisable) return this.softDisabled() ? 'true' : null;
    return this.disabled() ? 'true' : null;
  });

  constructor() {
    watcher(this.disabled, (disabled) => this.disabledChange.emit(disabled));
    watcher(this.role, (role) => this.roleChange.emit(role));
  }
}
