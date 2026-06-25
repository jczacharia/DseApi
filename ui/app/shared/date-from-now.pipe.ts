import type {PipeTransform} from '@angular/core';
import {Pipe} from '@angular/core';
import {formatDistanceToNowStrict, type FormatDistanceToNowStrictOptions} from 'date-fns/formatDistanceToNowStrict';

@Pipe({name: 'dateFromNow'})
export class DateFromNowPipe implements PipeTransform {
  transform(value: Date, opts?: FormatDistanceToNowStrictOptions): string {
    return formatDistanceToNowStrict(value, opts);
  }
}
