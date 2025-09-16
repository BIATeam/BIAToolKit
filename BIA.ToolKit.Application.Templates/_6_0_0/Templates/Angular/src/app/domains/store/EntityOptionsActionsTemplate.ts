import { createAction, props } from '@ngrx/store';
import { OptionDto } from 'bia-ng/models';
import { storeKey } from '../my-country-option.constants';

export namespace DomainMyCountryOptionsActions {
  export const loadAll = createAction('[' + storeKey + '] Load all');

  export const loadAllSuccess = createAction(
    '[' + storeKey + '] Load all success',
    props<{ myCountries: OptionDto[] }>()
  );

  export const failure = createAction(
    '[' + storeKey + '] Failure',
    props<{ error: any }>()
  );
}
