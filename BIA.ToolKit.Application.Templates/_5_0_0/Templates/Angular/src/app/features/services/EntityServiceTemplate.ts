import { Injectable } from '@angular/core';
import { Store } from '@ngrx/store';
import { TableLazyLoadEvent } from 'primeng/table';
import { map, Observable } from 'rxjs';
import { AuthService } from 'src/app/core/bia-core/services/auth.service';
import { CrudItemSignalRService } from 'src/app/shared/bia-shared/feature-templates/crud-items/services/crud-item-signalr.service';
import { CrudItemService } from 'src/app/shared/bia-shared/feature-templates/crud-items/services/crud-item.service';
import { TeamTypeId } from 'src/app/shared/constants';
import { AppState } from 'src/app/store/state';
import { MaintenanceTeam } from '../model/maintenance-team';
import { maintenanceTeamCRUDConfiguration } from '../maintenance-team.constants';
import { FeatureMaintenanceTeamsStore } from '../store/maintenance-team.state';
import { FeatureMaintenanceTeamsActions } from '../store/maintenance-teams-actions';
import { MaintenanceTeamDas } from './maintenance-team-das.service';
import { MaintenanceTeamOptionsService } from './maintenance-team-options.service';

@Injectable({
  providedIn: 'root',
})
export class MaintenanceTeamService extends CrudItemService<MaintenanceTeam> {
  _updateSuccessActionType = FeatureMaintenanceTeamsActions.loadAllByPost.type;
  _createSuccessActionType = FeatureMaintenanceTeamsActions.loadAllByPost.type;
  _updateFailureActionType = FeatureMaintenanceTeamsActions.failure.type;

  constructor(
    private store: Store<AppState>,
    public dasService: MaintenanceTeamDas,
    public signalRService: CrudItemSignalRService<MaintenanceTeam>,
    public optionsService: MaintenanceTeamOptionsService,
    // required only for parent key
    protected authService: AuthService
  ) {
    super(dasService, signalRService, optionsService);
  }

  public getParentIds(): any[] {
    // TODO after creation of CRUD MaintenanceTeam : adapt the parent Key tothe context. It can be null if root crud
    return [this.authService.getCurrentTeamId(TeamTypeId.AircraftMaintenanceCompany)];
  }

  public getFeatureName() {
    return maintenanceTeamCRUDConfiguration.featureName;
  }

  public crudItems$: Observable<MaintenanceTeam[]> = this.store.select(
    FeatureMaintenanceTeamsStore.getAllMaintenanceTeams
  );
  public totalCount$: Observable<number> = this.store.select(
    FeatureMaintenanceTeamsStore.getMaintenanceTeamsTotalCount
  );
  public loadingGetAll$: Observable<boolean> = this.store.select(
    FeatureMaintenanceTeamsStore.getMaintenanceTeamLoadingGetAll
  );
  public lastLazyLoadEvent$: Observable<TableLazyLoadEvent> = this.store.select(
    FeatureMaintenanceTeamsStore.getLastLazyLoadEvent
  );

  public crudItem$: Observable<MaintenanceTeam> = this.store.select(
    FeatureMaintenanceTeamsStore.getCurrentMaintenanceTeam
  );

  public displayItemName$: Observable<string> = this.crudItem$.pipe(
    map(maintenanceTeam => maintenanceTeam?.name?.toString() ?? '')
  );

  public loadingGet$: Observable<boolean> = this.store.select(
    FeatureMaintenanceTeamsStore.getMaintenanceTeamLoadingGet
  );

  public load(id: any) {
    this.store.dispatch(FeatureMaintenanceTeamsActions.load({ id }));
  }
  public loadAllByPost(event: TableLazyLoadEvent) {
    this.store.dispatch(FeatureMaintenanceTeamsActions.loadAllByPost({ event }));
  }
  public create(crudItem: MaintenanceTeam) {
    // TODO after creation of CRUD MaintenanceTeam : map parent Key on the corresponding field
    let indexParent = 0;
    crudItem.aircraftMaintenanceCompanyId = this.getParentIds()[indexParent++];
    this.store.dispatch(FeatureMaintenanceTeamsActions.create({ maintenanceTeam: crudItem }));
  }
  public update(crudItem: MaintenanceTeam) {
    this.store.dispatch(FeatureMaintenanceTeamsActions.update({ maintenanceTeam: crudItem }));
  }
  public save(crudItems: MaintenanceTeam[]) {
    let indexParent = 0;
    const aircraftMaintenanceCompanyIdIndexParent = indexParent++;
    crudItems
      .filter(x => !x.id)
      .map(x => (x.aircraftMaintenanceCompanyId = this.getParentIds()[aircraftMaintenanceCompanyIdIndexParent]));
    this.store.dispatch(FeatureMaintenanceTeamsActions.save({ maintenanceTeams: crudItems }));
  }
  public remove(id: any) {
    this.store.dispatch(FeatureMaintenanceTeamsActions.remove({ id }));
  }
  public multiRemove(ids: any[]) {
    this.store.dispatch(FeatureMaintenanceTeamsActions.multiRemove({ ids }));
  }
  public clearAll() {
    this.store.dispatch(FeatureMaintenanceTeamsActions.clearAll());
  }
  public clearCurrent() {
    this._currentCrudItem = <MaintenanceTeam>{};
    this._currentCrudItemId = 0;
    this.store.dispatch(FeatureMaintenanceTeamsActions.clearCurrent());
  }
}

