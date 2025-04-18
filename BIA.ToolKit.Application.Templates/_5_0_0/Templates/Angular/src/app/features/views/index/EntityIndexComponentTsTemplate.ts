import { AsyncPipe, NgClass, NgIf } from '@angular/common';
import { Component, Injector, ViewChild } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { PrimeTemplate } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { AuthService } from 'src/app/core/bia-core/services/auth.service';
import { BiaTableBehaviorControllerComponent } from 'src/app/shared/bia-shared/components/table/bia-table-behavior-controller/bia-table-behavior-controller.component';
import { BiaTableControllerComponent } from 'src/app/shared/bia-shared/components/table/bia-table-controller/bia-table-controller.component';
import { BiaTableHeaderComponent } from 'src/app/shared/bia-shared/components/table/bia-table-header/bia-table-header.component';
import { BiaTableComponent } from 'src/app/shared/bia-shared/components/table/bia-table/bia-table.component';
import { CrudItemService } from 'src/app/shared/bia-shared/feature-templates/crud-items/services/crud-item.service';
import { CrudItemsIndexComponent } from 'src/app/shared/bia-shared/feature-templates/crud-items/views/crud-items-index/crud-items-index.component';
import { Permission } from 'src/app/shared/permission';
import { MaintenanceTeamTableComponent } from '../../components/maintenance-team-table/maintenance-team-table.component';
import { MaintenanceTeam } from '../../model/maintenance-team';
import { maintenanceTeamCRUDConfiguration } from '../../maintenance-team.constants';
import { MaintenanceTeamService } from '../../services/maintenance-team.service';

@Component({
  selector: 'app-maintenance-teams-index',
  templateUrl: './maintenance-teams-index.component.html',
  styleUrls: ['./maintenance-teams-index.component.scss'],
  imports: [
    NgClass,
    PrimeTemplate,
    NgIf,
    ButtonDirective,
    MaintenanceTeamTableComponent,
    AsyncPipe,
    TranslateModule,
    BiaTableHeaderComponent,
    BiaTableControllerComponent,
    BiaTableBehaviorControllerComponent,
    BiaTableComponent,
  ],
  providers: [{ provide: CrudItemService, useExisting: MaintenanceTeamService }],
})
export class MaintenanceTeamsIndexComponent extends CrudItemsIndexComponent<MaintenanceTeam> {
  @ViewChild(MaintenanceTeamTableComponent, { static: false })
  crudItemTableComponent: MaintenanceTeamTableComponent;
  // BIAToolKit - Begin MaintenanceTeamIndexTsCanViewChildDeclaration
  // BIAToolKit - End MaintenanceTeamIndexTsCanViewChildDeclaration

  constructor(
    protected injector: Injector,
    public maintenanceTeamService: MaintenanceTeamService,
    protected authService: AuthService
  ) {
    super(injector, maintenanceTeamService);
    this.crudConfiguration = maintenanceTeamCRUDConfiguration;
  }

  protected setPermissions() {
    this.canEdit = this.authService.hasPermission(Permission.MaintenanceTeam_Update);
    this.canDelete = this.authService.hasPermission(Permission.MaintenanceTeam_Delete);
    this.canAdd = this.authService.hasPermission(Permission.MaintenanceTeam_Create);
    this.canSave = this.authService.hasPermission(Permission.MaintenanceTeam_Save);
    this.canSelect = this.canDelete;
    // BIAToolKit - Begin MaintenanceTeamIndexTsCanViewChildSet
    // BIAToolKit - End MaintenanceTeamIndexTsCanViewChildSet
  }

  // BIAToolKit - Begin MaintenanceTeamIndexTsOnViewChild
  // BIAToolKit - End MaintenanceTeamIndexTsOnViewChild
}
