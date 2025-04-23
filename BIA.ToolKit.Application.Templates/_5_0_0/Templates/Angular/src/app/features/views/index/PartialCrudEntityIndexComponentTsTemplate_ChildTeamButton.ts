      // Begin Partial AircraftMaintenanceCompanyIndexTsChildTeamButton MaintenanceTeam
      new BiaButtonGroupItem(
        this.translateService.instant(
          'aircraftMaintenanceCompany.maintenanceTeams'
        ),
        () => this.onViewMaintenanceTeams(),
        this.canViewMaintenanceTeams,
        this.selectedCrudItems.length !== 1,
        this.translateService.instant(
          'aircraftMaintenanceCompany.maintenanceTeams'
        )
      ),
      // End Partial AircraftMaintenanceCompanyIndexTsChildTeamButton MaintenanceTeam
