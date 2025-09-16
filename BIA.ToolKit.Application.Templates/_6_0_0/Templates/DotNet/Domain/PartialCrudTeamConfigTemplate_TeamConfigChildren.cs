                // BIAToolKit - Begin Partial TeamConfigAircraftMaintenanceCompanyChildren MaintenanceTeam
                    new ()
                    {
                        TeamTypeId = (int)TeamTypeId.MaintenanceTeam,
                        GetChilds = team => (team as MaintenanceCompanies.Entities.AircraftMaintenanceCompany).MaintenanceTeams,
                    },

                // BIAToolKit - End Partial TeamConfigAircraftMaintenanceCompanyChildren MaintenanceTeam
