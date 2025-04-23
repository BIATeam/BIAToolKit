import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PermissionGuard } from 'src/app/core/bia-core/guards/permission.guard';
import { Permission } from 'src/app/shared/permission';
import { MaintenanceTeamItemComponent } from './views/maintenance-team-item/maintenance-team-item.component';
import { MaintenanceTeamsIndexComponent } from './views/maintenance-teams-index/maintenance-teams-index.component';
import { EngineOptionModule } from 'src/app/domains/engine-option/engine-option.module';
import { PlaneTypeOptionModule } from 'src/app/domains/plane-type-option/plane-type-option.module';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import {
  DynamicLayoutComponent,
  LayoutMode,
} from 'src/app/shared/bia-shared/components/layout/dynamic-layout/dynamic-layout.component';

import { MaintenanceTeamReadComponent } from '../maintenance-teams/views/maintenance-team-read/maintenance-team-read.component';
import { maintenanceTeamCRUDConfiguration } from './maintenance-team.constants';
import { FeatureMaintenanceTeamsStore } from './store/maintenance-team.state';
import { MaintenanceTeamsEffects } from './store/maintenance-teams-effects';
import { MaintenanceTeamEditComponent } from './views/maintenance-team-edit/maintenance-team-edit.component';
import { MaintenanceTeamImportComponent } from './views/maintenance-team-import/maintenance-team-import.component';
import { MaintenanceTeamNewComponent } from './views/maintenance-team-new/maintenance-team-new.component';

export const ROUTES: Routes = [
  {
    path: '',
    data: {
      breadcrumb: null,
      permission: Permission.MaintenanceTeam_List_Access,
      injectComponent: MaintenanceTeamsIndexComponent,
      configuration: maintenanceTeamCRUDConfiguration,
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
          permission: Permission.MaintenanceTeam_Create,
          title: 'maintenanceTeam.add',
        },
        component: MaintenanceTeamNewComponent,
        canActivate: [PermissionGuard],
      },
      {
        path: 'import',
        data: {
          breadcrumb: 'maintenanceTeam.import',
          canNavigate: false,
          layoutMode: LayoutMode.popup,
          style: {
            minWidth: '80vw',
            maxWidth: '80vw',
            maxHeight: '80vh',
          },
          permission: Permission.MaintenanceTeam_Save,
          title: 'maintenanceTeam.import',
        },
        component: MaintenanceTeamImportComponent,
        canActivate: [PermissionGuard],
      },
      {
        path: ':crudItemId',
        data: {
          breadcrumb: '',
          canNavigate: true,
        },
        component: MaintenanceTeamItemComponent,
        canActivate: [PermissionGuard],
        children: [
          {
            path: 'members',
            data: {
              breadcrumb: 'app.members',
              canNavigate: true,
              permission:
                Permission.MaintenanceTeam_Member_List_Access,
              layoutMode: LayoutMode.fullPage,
            },
            loadChildren: () =>
              import(
                './children/members/maintenance-team-member.module'
              ).then(m => m.MaintenanceTeamMemberModule),
          },
          {
            path: 'read',
            data: {
              breadcrumb: 'bia.read',
              canNavigate: true,
              permission: Permission.MaintenanceTeam_Read,
              readOnlyMode: maintenanceTeamCRUDConfiguration.formEditReadOnlyMode,
              title: 'maintenanceTeam.read',
            },
            component: MaintenanceTeamReadComponent,
            canActivate: [PermissionGuard],
          },
          {
            path: 'edit',
            data: {
              breadcrumb: 'bia.edit',
              canNavigate: true,
              permission: Permission.MaintenanceTeam_Update,
              title: 'maintenanceTeam.edit',
            },
            component: MaintenanceTeamEditComponent,
            canActivate: [PermissionGuard],
          },
          {
            path: '',
            pathMatch: 'full',
            redirectTo: 'read',
          },
          // BIAToolKit - Begin MaintenanceTeamModuleChildPath
          // BIAToolKit - End MaintenanceTeamModuleChildPath
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
      maintenanceTeamCRUDConfiguration.storeKey,
      FeatureMaintenanceTeamsStore.reducers
    ),
    EffectsModule.forFeature([MaintenanceTeamsEffects]),
    // TODO after creation of CRUD MaintenanceTeam : select the optioDto dommain module required for link
    // Domain Modules:
    EngineOptionModule,
    PlaneTypeOptionModule,
  ],
})
export class MaintenanceTeamModule {}
