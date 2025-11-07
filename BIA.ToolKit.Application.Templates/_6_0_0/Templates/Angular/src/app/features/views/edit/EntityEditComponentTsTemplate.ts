import { Component, Injector, OnInit } from '@angular/core';
import {
  CrudItemEditComponent,
  FormReadOnlyMode,
  SpinnerComponent,
} from 'bia-ng/shared';
import { AsyncPipe } from '@angular/common';
import { PlaneFormComponent } from '../../components/plane-form/plane-form.component';
import { Plane } from '../../model/plane';
import { planeCRUDConfiguration } from '../../plane.constants';
import { PlaneService } from '../../services/plane.service';
import { filter } from 'rxjs';
import { Permission } from 'src/app/shared/permission';
import { PlaneOptionsService } from '../../services/plane-options.service';

@Component({
  selector: 'app-plane-edit',
  templateUrl: './plane-edit.component.html',
  imports: [PlaneFormComponent, AsyncPipe, SpinnerComponent],
})
export class PlaneEditComponent extends CrudItemEditComponent<Plane> implements OnInit {
  constructor(
    protected injector: Injector,
    protected planeOptionsService: PlaneOptionsService,
    public planeService: PlaneService
  ) {
    super(injector, planeService);
    this.crudConfiguration = planeCRUDConfiguration;
  }

}
