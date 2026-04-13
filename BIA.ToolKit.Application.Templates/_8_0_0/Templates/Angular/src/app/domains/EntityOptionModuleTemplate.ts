import { NgModule } from '@angular/core';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { storeKey } from './my-country-option.constants';
import { reducers } from './store/my-country-option.state';
import { MyCountryOptionsEffects } from './store/my-country-options-effects';

@NgModule({
  imports: [
    StoreModule.forFeature(storeKey, reducers),
    EffectsModule.forFeature([MyCountryOptionsEffects]),
  ],
})
export class MyCountryOptionModule {}
