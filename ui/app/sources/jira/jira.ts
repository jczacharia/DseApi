import {Source} from '#core/source/source';
import {Service} from '@angular/core';

@Service()
export default class Jira extends Source {
  constructor() {
    super('jira', {name: 'Jira'});
  }
}
