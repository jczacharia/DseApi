import {Source} from '#core/source/source';
import {Service} from '@angular/core';

@Service()
export default class Confluence extends Source {
  constructor() {
    super('confluence', {name: 'Confluence'});
  }
}
