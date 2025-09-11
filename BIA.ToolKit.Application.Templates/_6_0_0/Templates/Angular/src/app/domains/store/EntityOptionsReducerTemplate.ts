import { EntityState, createEntityAdapter } from '@ngrx/entity';
import { createReducer, on } from '@ngrx/store';
import { OptionDto } from 'bia-ng/models';
import { DomainMyCountryOptionsActions } from './my-country-options-actions';

// This adapter will allow is to manipulate myCountries (mostly CRUD operations)
export const myCountryOptionsAdapter = createEntityAdapter<OptionDto>({
  selectId: (myCountry: OptionDto) => myCountry.id,
  sortComparer: false,
});

// -----------------------------------------
// The shape of EntityState
// ------------------------------------------
// interface EntityState<MyCountry> {
//   ids: string[] | number[];
//   entities: { [id: string]: MyCountry };
// }
// -----------------------------------------
// -> ids arrays allow us to sort data easily
// -> entities map allows us to access the data quickly without iterating/filtering though an array of objects

export type State = EntityState<OptionDto>;

export const INIT_STATE: State = myCountryOptionsAdapter.getInitialState({
  // additional props default values here
});

export const myCountryOptionReducers = createReducer<State>(
  INIT_STATE,
  on(DomainMyCountryOptionsActions.loadAllSuccess, (state, { myCountries }) =>
    myCountryOptionsAdapter.setAll(myCountries, state)
  )
);

export const getMyCountryOptionById = (id: number) => (state: State) =>
  state.entities[id];
