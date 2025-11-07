import { AsyncPipe, NgClass } from '@angular/common';
import { Component, Injector, OnInit, ViewChild } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from 'bia-ng/core';
import { TeamAdvancedFilterDto } from 'bia-ng/models';
import {
  BiaTableBehaviorControllerComponent,
  BiaTableComponent,
  BiaTableControllerComponent,
  BiaTableHeaderComponent,
  CrudItemService,
  CrudItemsIndexComponent,
} from 'bia-ng/shared';
import { PrimeTemplate } from 'primeng/api';
import { filter } from 'rxjs';
import { Permission } from 'src/app/shared/permission';
import { PlaneTableComponent } from '../../components/plane-table/plane-table.component';
import { Plane } from '../../model/plane';
import { planeCRUDConfiguration } from '../../plane.constants';
import { PlaneService } from '../../services/plane.service';
import { PlaneOptionsService } from '../../services/plane-options.service';

@Component({
  selector: 'app-planes-index',
  templateUrl: './planes-index.component.html',
  styleUrls: ['./planes-index.component.scss'],
  imports: [
    NgClass,
    PrimeTemplate,
    PlaneTableComponent,
    AsyncPipe,
    TranslateModule,
    BiaTableHeaderComponent,
    BiaTableControllerComponent,
    BiaTableBehaviorControllerComponent,
    BiaTableComponent,
  ],
  providers: [{ provide: CrudItemService, useExisting: PlaneService }],
})
export class PlanesIndexComponent extends CrudItemsIndexComponent<Plane> implements OnInit {
  @ViewChild(PlaneTableComponent, { static: false })
  crudItemTableComponent: PlaneTableComponent;

  // BIAToolKit - Begin PlaneIndexTsCanViewChildDeclaration
  // BIAToolKit - End PlaneIndexTsCanViewChildDeclaration


  constructor(
    protected injector: Injector,
    public planeService: PlaneService,
    protected planeOptionsService: PlaneOptionsService,
    protected authService: AuthService
  ) {
    super(injector, planeService);
    this.crudConfiguration = planeCRUDConfiguration;
  }


  protected setPermissions() {
    this.canEdit = this.authService.hasPermission(Permission.Plane_Update);
    this.canDelete = this.authService.hasPermission(Permission.Plane_Delete);
    this.canAdd = this.authService.hasPermission(Permission.Plane_Create);
    this.canSelect = this.canDelete;
    // BIAToolKit - Begin PlaneIndexTsCanViewChildSet
    // BIAToolKit - End PlaneIndexTsCanViewChildSet
  }


  // BIAToolKit - Begin PlaneIndexTsOnViewChild
  // BIAToolKit - End PlaneIndexTsOnViewChild

  protected initSelectedButtonGroup() {
    this.selectionActionsMenuItems = [
      // BIAToolKit - Begin PlaneIndexTsSelectedButtonViewChild
      // BIAToolKit - End PlaneIndexTsSelectedButtonViewChild
    ];
  }
}
