  // BIAToolKit - Begin Partial AircraftMaintenanceCompanyIndexTsOnViewChild MaintenanceTeam
  onViewMaintenanceTeams(crudItemId: any) {
    if (crudItemId && crudItemId > 0) {
      this.router.navigate([crudItemId, 'maintenance-teams'], {
        relativeTo: this.activatedRoute,
      });
    }
  }
  // BIAToolKit - End Partial AircraftMaintenanceCompanyIndexTsOnViewChild MaintenanceTeam