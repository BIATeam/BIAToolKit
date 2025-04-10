import {
  Action,
  combineReducers,
  createFeatureSelector,
  createSelector,
} from '@ngrx/store';
import { storeKey } from '../my-country-option.constants';
import * as fromMyCountryOptions from './my-country-options-reducer';

export interface MyCountryOptionsState {
  myCountryOptions: fromMyCountryOptions.State;
}

/** Provide reducers with AoT-compilation compliance */
export function reducers(
  state: MyCountryOptionsState | undefined,
  action: Action
) {
  return combineReducers({
    myCountryOptions: fromMyCountryOptions.myCountryOptionReducers,
  })(state, action);
}

/**
 * The createFeatureSelector function selects a piece of state from the root of the state object.
 * This is used for selecting feature states that are loaded eagerly or lazily.
 */

export const getMyCountriesState =
  createFeatureSelector<MyCountryOptionsState>(storeKey);

export const getMyCountryOptionsEntitiesState = createSelector(
  getMyCountriesState,
  state => state.myCountryOptions
);

export const { selectAll: getAllMyCountryOptions } =
  fromMyCountryOptions.myCountryOptionsAdapter.getSelectors(
    getMyCountryOptionsEntitiesState
  );

export const getMyCountryOptionById = (id: number) =>
  createSelector(
    getMyCountryOptionsEntitiesState,
    fromMyCountryOptions.getMyCountryOptionById(id)
  );