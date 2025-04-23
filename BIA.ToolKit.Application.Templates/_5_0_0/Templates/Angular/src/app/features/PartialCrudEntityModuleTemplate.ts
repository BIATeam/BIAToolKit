          // BIAToolKit - Begin Partial AircraftMaintenanceCompanyModuleChildPath MaintenanceTeam
          {
            path: 'maintenance-teams',
            data: {
              breadcrumb: 'app.maintenanceTeams',
              canNavigate: true,
              permission: Permission.MaintenanceTeam_List_Access,
              layoutMode: LayoutMode.fullPage,
            },
            loadChildren: () =>
              import('./children/maintenance-teams/maintenance-team.module').then(
                m => m.MaintenanceTeamModule
              ),
          },
          // BIAToolKit - End Partial AircraftMaintenanceCompanyModuleChildPath MaintenanceTeam
