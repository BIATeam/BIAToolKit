import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PermissionGuard } from 'bia-ng/core';
import {
  DynamicLayoutComponent,
  LayoutMode,
} from 'bia-ng/shared';
import { Permission } from 'src/app/shared/permission';
import { PlaneItemComponent } from './views/plane-item/plane-item.component';
import { PlanesIndexComponent } from './views/planes-index/planes-index.component';
import { EngineOptionModule } from 'src/app/domains/engine-option/engine-option.module';
import { PlaneTypeOptionModule } from 'src/app/domains/plane-type-option/plane-type-option.module';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { planeCRUDConfiguration } from './plane.constants';
import { FeaturePlanesStore } from './store/plane.state';
import { PlanesEffects } from './store/planes-effects';
import { PlaneEditComponent } from './views/plane-edit/plane-edit.component';
import { PlaneImportComponent } from './views/plane-import/plane-import.component';
import { PlaneNewComponent } from './views/plane-new/plane-new.component';
import { PlaneHistoricalComponent } from './views/plane-historical/plane-historical.component';

export const ROUTES: Routes = [
  {
    path: '',
    data: {
      breadcrumb: null,
      permission: Permission.Plane_List_Access,
      injectComponent: PlanesIndexComponent,
      configuration: planeCRUDConfiguration,
    },
    component: DynamicLayoutComponent,
    canActivate: [PermissionGuard],
    // [Calc] : The children are not used in calc
    children: [
      {
        path: 'create',
        data: {
          breadcrumb: 'bia.add',
          canNavigate: false,
          permission: Permission.Plane_Create,
          title: 'plane.add',
        },
        component: PlaneNewComponent,
        canActivate: [PermissionGuard],
      },
      {
        path: ':crudItemId',
        data: {
          breadcrumb: '',
          canNavigate: false,
        },
        component: PlaneItemComponent,
        canActivate: [PermissionGuard],
        children: [
          {
            path: 'edit',
            data: {
              breadcrumb: 'bia.edit',
              canNavigate: true,
              permission: Permission.Plane_Update,
              title: 'plane.edit',
            },
            component: PlaneEditComponent,
            canActivate: [PermissionGuard],
          },
          {
            path: 'historical',
            data: {
              breadcrumb: 'bia.historical',
              canNavigate: false,
              layoutMode: LayoutMode.popup,
              style: {
                minWidth: '50vw',
              },
              title: 'bia.historical',
              permission: Permission.Plane_Read,
            },
            component: PlaneHistoricalComponent,
            canActivate: [PermissionGuard],
          },
          {
            path: '',
            pathMatch: 'full',
            redirectTo: 'edit',
          },
          // BIAToolKit - Begin PlaneModuleChildPath
          // BIAToolKit - End PlaneModuleChildPath
        ],
      },
    ],
  },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [
    RouterModule.forChild(ROUTES),
    StoreModule.forFeature(
      planeCRUDConfiguration.storeKey,
      FeaturePlanesStore.reducers
    ),
    EffectsModule.forFeature([PlanesEffects]),
    // Domain Modules:
    EngineOptionModule,
    PlaneTypeOptionModule,
  ],
})
export class PlaneModule {}
