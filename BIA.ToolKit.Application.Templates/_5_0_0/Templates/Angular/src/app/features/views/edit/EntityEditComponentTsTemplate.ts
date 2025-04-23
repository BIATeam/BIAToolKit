import { Component, Injector } from '@angular/core';
import { SpinnerComponent } from 'src/app/shared/bia-shared/components/spinner/spinner.component';
import { CrudItemEditComponent } from 'src/app/shared/bia-shared/feature-templates/crud-items/views/crud-item-edit/crud-item-edit.component';
import { AsyncPipe, NgIf } from '@angular/common';
import { MaintenanceTeamFormComponent } from '../../components/maintenance-team-form/maintenance-team-form.component';
import { MaintenanceTeam } from '../../model/maintenance-team';
import { maintenanceTeamCRUDConfiguration } from '../../maintenance-team.constants';
import { MaintenanceTeamService } from '../../services/maintenance-team.service';

@Component({
  selector: 'app-maintenance-team-edit',
  templateUrl: './maintenance-team-edit.component.html',
  imports: [NgIf, MaintenanceTeamFormComponent, AsyncPipe, SpinnerComponent],
})
export class MaintenanceTeamEditComponent extends CrudItemEditComponent<MaintenanceTeam> {
  constructor(
    protected injector: Injector,
    public maintenanceTeamService: MaintenanceTeamService
  ) {
    super(injector, maintenanceTeamService);
    this.crudConfiguration = maintenanceTeamCRUDConfiguration;
  }
}
