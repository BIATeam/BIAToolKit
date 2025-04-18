    // BIAToolKit - Begin Partial AircraftMaintenanceCompanyIndexTsCanViewChildSet MaintenanceTeam
    this.canViewMaintenanceTeams = this.authService.hasPermission(
      Permission.MaintenanceTeam_List_Access
    );
    this.canSelect = this.canSelect || this.canViewMaintenanceTeams;
    // BIAToolKit - End Partial AircraftMaintenanceCompanyIndexTsCanViewChildSet MaintenanceTeam