  // BIAToolKit - Begin Partial AircraftMaintenanceCompanyIndexTsOnViewChild MaintenanceTeam
  onViewMaintenanceTeams() {
    if (this.selectedCrudItems.length === 1) {
      this.router.navigate(
        [this.selectedCrudItems[0].id, 'maintenance-teams'],
        { relativeTo: this.activatedRoute }
      );
    }
  }
  // BIAToolKit - End Partial AircraftMaintenanceCompanyIndexTsOnViewChild MaintenanceTeam
