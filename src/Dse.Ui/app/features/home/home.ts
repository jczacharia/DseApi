import {Component} from '@angular/core';
import {DseClass} from '../../ui/dse-class';

// Placeholder landing — also a living example of the design-system starting blocks.
@Component({
  selector: 'dse-home',
  imports: [DseClass],
  template: `
    <main class="flex flex-1 flex-col items-start gap-6 p-8">
      <h1 class="font-heading text-2xl">DSE UI — starting blocks</h1>
      <div class="flex flex-wrap items-center gap-3">
        <button>Primary</button>
        <button variant="secondary">Secondary</button>
        <button variant="ghost">Ghost</button>
        <button disabled>Disabled</button>
      </div>
    </main>
  `,
})
export default class Home {}
