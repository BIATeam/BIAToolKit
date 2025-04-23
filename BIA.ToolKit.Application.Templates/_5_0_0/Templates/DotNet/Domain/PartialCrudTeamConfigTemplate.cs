            // BIAToolKit - Begin Partial TeamConfig MaintenanceTeam
            new BiaTeamConfig<Team>()
            {
                TeamTypeId = (int)TeamTypeId.MaintenanceTeam,
                RightPrefix = "MaintenanceTeam",
                AdminRoleIds = new int[] { (int)RoleId.MaintenanceTeamAdmin },
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
