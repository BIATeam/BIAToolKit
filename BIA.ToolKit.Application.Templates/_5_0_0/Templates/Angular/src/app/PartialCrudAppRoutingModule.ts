          // BIAToolKit - Begin Partial Routing MaintenanceTeam
          {
            path: 'maintenance-teams',
            data: {
              breadcrumb: 'app.maintenanceTeams',
              canNavigate: true,
            },
            loadChildren: () =>
              import(
                './features/aircraft-maintenance-companies/children/maintenance-teams/maintenance-team.module'
              ).then(m => m.MaintenanceTeamModule),
          },
          // BIAToolKit - End Partial Routing MaintenanceTeam