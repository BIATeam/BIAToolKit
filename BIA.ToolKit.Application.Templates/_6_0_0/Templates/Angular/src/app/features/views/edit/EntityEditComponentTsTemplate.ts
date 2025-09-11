import { Component, Injector, OnInit } from '@angular/core';
import {
  CrudItemEditComponent,
  FormReadOnlyMode,
  SpinnerComponent,
} from 'bia-ng/shared';
import { AsyncPipe, NgIf } from '@angular/common';
import { MaintenanceTeamFormComponent } from '../../components/maintenance-team-form/maintenance-team-form.component';
import { MaintenanceTeam } from '../../model/maintenance-team';
import { maintenanceTeamCRUDConfiguration } from '../../maintenance-team.constants';
import { MaintenanceTeamService } from '../../services/maintenance-team.service';
import { filter } from 'rxjs';
import { Permission } from 'src/app/shared/permission';
import { MaintenanceTeamOptionsService } from '../../services/maintenance-team-options.service';

@Component({
  selector: 'app-maintenance-team-edit',
  templateUrl: './maintenance-team-edit.component.html',
  imports: [NgIf, MaintenanceTeamFormComponent, AsyncPipe, SpinnerComponent],
})
export class MaintenanceTeamEditComponent extends CrudItemEditComponent<MaintenanceTeam> implements OnInit {
  constructor(
    protected injector: Injector,
    protected maintenanceTeamOptionsService: MaintenanceTeamOptionsService,
    public maintenanceTeamService: MaintenanceTeamService
  ) {
    super(injector, maintenanceTeamService);
    this.crudConfiguration = maintenanceTeamCRUDConfiguration;
  }

}
