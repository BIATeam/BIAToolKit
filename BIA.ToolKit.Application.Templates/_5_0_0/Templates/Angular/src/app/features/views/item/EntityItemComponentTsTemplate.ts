import { AsyncPipe, NgIf } from '@angular/common';
import { Component, Injector } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SpinnerComponent } from 'src/app/shared/bia-shared/components/spinner/spinner.component';
import { CrudItemItemComponent } from 'src/app/shared/bia-shared/feature-templates/crud-items/views/crud-item-item/crud-item-item.component';
import { MaintenanceTeam } from '../../model/maintenance-team';
import { MaintenanceTeamService } from '../../services/maintenance-team.service';

@Component({
  selector: 'app-maintenance-teams-item',
  templateUrl:
    '../../../../../../shared/bia-shared/feature-templates/crud-items/views/crud-item-item/crud-item-item.component.html',
  styleUrls: [
    '../../../../../../shared/bia-shared/feature-templates/crud-items/views/crud-item-item/crud-item-item.component.scss',
  ],
  imports: [RouterOutlet, NgIf, AsyncPipe, SpinnerComponent],
})
export class MaintenanceTeamItemComponent extends CrudItemItemComponent<MaintenanceTeam> {
  constructor(
    protected injector: Injector,
    public maintenanceTeamService: MaintenanceTeamService
  ) {
    super(injector, maintenanceTeamService);
  }
}
