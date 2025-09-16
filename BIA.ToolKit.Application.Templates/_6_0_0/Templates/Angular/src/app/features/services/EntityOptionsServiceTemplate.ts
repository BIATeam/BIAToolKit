import { Injectable } from '@angular/core';
import {
  Observable,
  combineLatest,
} from 'rxjs';
import { map } from 'rxjs/operators';
import { CrudItemOptionsService, DictOptionDto } from 'bia-ng/shared';
import { Store } from '@ngrx/store';
import { OptionDto } from 'bia-ng/models';
import { AppState } from 'src/app/store/state';
import { getAllEngineOptions } from 'src/app/domains/engine-option/store/engine-option.state';
import { DomainEngineOptionsActions } from 'src/app/domains/engine-option/store/engine-options-actions';
import { getAllPlaneTypeOptions } from 'src/app/domains/plane-type-option/store/plane-type-option.state';
import { DomainPlaneTypeOptionsActions } from 'src/app/domains/plane-type-option/store/plane-type-options-actions';

@Injectable({
  providedIn: 'root',
})
export class MaintenanceTeamOptionsService extends CrudItemOptionsService {
  engineOptions$: Observable<OptionDto[]>;
  planeTypeOptions$: Observable<OptionDto[]>;

  constructor(
      private store: Store<AppState>
    ) {
    super();
    // TODO after creation of CRUD Team MaintenanceTeam : get all required option dto use in Table calc and create and edit form
    this.engineOptions$ = this.store.select(getAllEngineOptions);
    this.planeTypeOptions$ = this.store.select(getAllPlaneTypeOptions);
    let cpt = 0;
    const engine = cpt++;
    const planeType = cpt++;

    this.dictOptionDtos$ = combineLatest([
      this.engineOptions$,
      this.planeTypeOptions$,
    ]).pipe(
      map(options => {
        return <DictOptionDto[]>[
          new DictOptionDto('engines', options[engine]),
          new DictOptionDto('planeType', options[planeType]),
          new DictOptionDto('similarTypes', options[planeType]),
        ];
      })
    );
  }

  loadAllOptions() {
    this.store.dispatch(DomainEngineOptionsActions.loadAll());
    this.store.dispatch(DomainPlaneTypeOptionsActions.loadAll());
  }
}
