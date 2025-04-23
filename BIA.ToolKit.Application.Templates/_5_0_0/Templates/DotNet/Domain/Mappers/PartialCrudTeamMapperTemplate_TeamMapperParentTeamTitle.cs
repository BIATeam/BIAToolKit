                // BIAToolKit - Begin Partial TeamMapperParentTeamTitle MaintenanceTeam
                MaintenanceTeam maintenanceTeam => team.TeamTypeId == (int)TeamTypeId.MaintenanceTeam ? maintenanceTeam.AircraftMaintenanceCompany.Title : string.Empty,

                // BIAToolKit - End Partial TeamMapperParentTeamTitle MaintenanceTeam
