import { Component, Injector } from '@angular/core';
import { SpinnerComponent } from 'src/app/shared/bia-shared/components/spinner/spinner.component';
import { AuthService } from 'src/app/core/bia-core/services/auth.service';
import { CrudItemReadComponent } from 'src/app/shared/bia-shared/feature-templates/crud-items/views/crud-item-read/crud-item-read.component';
import { AsyncPipe, NgIf } from '@angular/common';
import { MaintenanceTeamFormComponent } from '../../components/maintenance-team-form/maintenance-team-form.component';
import { MaintenanceTeam } from '../../model/maintenance-team';
import { maintenanceTeamCRUDConfiguration } from '../../maintenance-team.constants';
import { MaintenanceTeamService } from '../../services/maintenance-team.service';

@Component({
  selector: 'app-maintenance-team-read',
  templateUrl: './maintenance-team-read.component.html',
  imports: [NgIf, MaintenanceTeamFormComponent, AsyncPipe, SpinnerComponent],
})
export class MaintenanceTeamReadComponent extends CrudItemReadComponent<MaintenanceTeam> {
  constructor(
    protected injector: Injector,
    public maintenanceTeamService: MaintenanceTeamService,
    protected authService: AuthService
  ) {
    super(injector, maintenanceTeamService, authService);
    this.crudConfiguration = maintenanceTeamCRUDConfiguration;
  }
}
