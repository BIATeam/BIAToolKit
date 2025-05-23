            // BIAToolKit - Begin Partial TeamConfig MaintenanceTeam
            new BiaTeamConfig<Team>()
            {
                TeamTypeId = (int)TeamTypeId.MaintenanceTeam,
                RightPrefix = "MaintenanceTeam",
                AdminRoleIds = [
                    (int)RoleId.MaintenanceTeamAdmin
                    ],
                // BIAToolKit - Begin TeamConfigAircraftMaintenanceCompanyChildren
                // BIAToolKit - End TeamConfigAircraftMaintenanceCompanyChildren
                Parents = new ImmutableListBuilder<BiaTeamParentConfig<Team>>
                {
                    new BiaTeamParentConfig<Team>
                    {
                        TeamTypeId = (int)TeamTypeId.AircraftMaintenanceCompany,
                        GetParent = team => (team as MaintenanceCompanies.Entities.MaintenanceTeam).AircraftMaintenanceCompany,
                    },
                }
                .ToImmutable(),
            },

            // BIAToolKit - End Partial TeamConfig MaintenanceTeam
