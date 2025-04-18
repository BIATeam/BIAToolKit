import { AsyncPipe } from '@angular/common';
import { Component, Injector } from '@angular/core';
import { BiaFormComponent } from 'src/app/shared/bia-shared/components/form/bia-form/bia-form.component';
import { CrudItemImportFormComponent } from 'src/app/shared/bia-shared/feature-templates/crud-items/components/crud-item-import-form/crud-item-import-form.component';
import { CrudItemImportComponent } from 'src/app/shared/bia-shared/feature-templates/crud-items/views/crud-item-import/crud-item-import.component';
import { Permission } from 'src/app/shared/permission';
import { MaintenanceTeam } from '../../model/maintenance-team';
import { maintenanceTeamCRUDConfiguration } from '../../maintenance-team.constants';
import { MaintenanceTeamService } from '../../services/maintenance-team.service';

@Component({
  selector: 'app-maintenance-team-import',
  templateUrl:
    '../../../../../../shared/bia-shared/feature-templates/crud-items/views/crud-item-import/crud-item-import.component.html',
  imports: [CrudItemImportFormComponent, AsyncPipe, BiaFormComponent],
})
export class MaintenanceTeamImportComponent extends CrudItemImportComponent<MaintenanceTeam> {
  constructor(
    protected injector: Injector,
    private maintenanceTeamService: MaintenanceTeamService
  ) {
    super(injector, maintenanceTeamService);
    this.crudConfiguration = maintenanceTeamCRUDConfiguration;
    this.setPermissions();
  }

  setPermissions() {
    this.canEdit = this.authService.hasPermission(Permission.MaintenanceTeam_Update);
    this.canDelete = this.authService.hasPermission(Permission.MaintenanceTeam_Delete);
    this.canAdd = this.authService.hasPermission(Permission.MaintenanceTeam_Create);
  }

  save(toSaves: MaintenanceTeam[]): void {
    this.maintenanceTeamService.save(toSaves);
  }
}
