import {Component} from '@angular/core';
import {NgIcon, provideIcons} from '@ng-icons/core';
import {lucideArrowRight} from '@ng-icons/lucide';
import {HlmButton} from '@spartan-ng/helm/button';

// Living example of the spartan/ui starting blocks.
@Component({
  selector: 'dse-home',
  imports: [HlmButton, NgIcon],
  providers: [provideIcons({lucideArrowRight})],
  template: `
    <main class="flex flex-1 flex-col items-start gap-6 p-8">
      <h1 class="font-serif text-2xl font-semibold">DSE UI — spartan</h1>
      <div class="flex flex-wrap items-center gap-3">
        <button hlmBtn>Primary</button>
        <button hlmBtn variant="secondary">Secondary</button>
        <button hlmBtn variant="outline">Outline</button>
        <button hlmBtn variant="ghost">Ghost</button>
        <button hlmBtn variant="destructive">Destructive</button>
        <button hlmBtn variant="link">Link</button>
        <button hlmBtn disabled>Disabled</button>
        <button hlmBtn>
          Next
          <ng-icon name="lucideArrowRight" />
        </button>
      </div>
    </main>
  `,
})
export default class Home {}
