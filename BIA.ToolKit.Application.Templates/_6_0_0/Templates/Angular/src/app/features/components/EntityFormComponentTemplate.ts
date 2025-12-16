import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BiaFormComponent, CrudItemFormComponent } from 'bia-ng/shared';
import { MaintenanceTeam } from '../../model/maintenance-team';

@Component({
  selector: 'app-maintenance-team-form',
  templateUrl:
    '@bia-team/bia-ng/templates/feature-templates/crud-items/components/crud-item-form/crud-item-form.component.html',
  styleUrls: [
    '@bia-team/bia-ng/templates/feature-templates/crud-items/components/crud-item-form/crud-item-form.component.scss',
  ],
  imports: [BiaFormComponent],
})
export class MaintenanceTeamFormComponent extends CrudItemFormComponent<MaintenanceTeam> {}
