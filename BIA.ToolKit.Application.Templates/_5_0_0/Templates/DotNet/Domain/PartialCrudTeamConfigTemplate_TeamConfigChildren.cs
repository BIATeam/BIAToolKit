                // BIAToolKit - Begin Partial TeamConfigAircraftMaintenanceCompanyChildren MaintenanceTeam
                Children = new ImmutableListBuilder<BiaTeamChildrenConfig<Team>>
                {
                    new BiaTeamChildrenConfig<Team>
                    {
                        TeamTypeId = (int)TeamTypeId.MaintenanceTeam,
                        GetChilds = team => (team as AircraftMaintenanceCompany).MaintenanceTeams,
                    },
                }.ToImmutable(),

                // BIAToolKit - End Partial TeamConfigAircraftMaintenanceCompanyChildren MaintenanceTeam
