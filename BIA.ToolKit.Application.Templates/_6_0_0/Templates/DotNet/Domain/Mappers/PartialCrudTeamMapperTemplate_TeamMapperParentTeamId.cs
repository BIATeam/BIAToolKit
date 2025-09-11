                // BIAToolKit - Begin Partial TeamMapperParentTeamId MaintenanceTeam
                MaintenanceTeam maintenanceTeam => team.TeamTypeId == (int)TeamTypeId.MaintenanceTeam ? maintenanceTeam.AircraftMaintenanceCompanyId : 0,

                // BIAToolKit - End Partial TeamMapperParentTeamId MaintenanceTeam
