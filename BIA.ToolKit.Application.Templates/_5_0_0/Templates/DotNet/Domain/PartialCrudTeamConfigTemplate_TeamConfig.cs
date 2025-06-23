            // BIAToolKit - Begin Partial TeamConfig MaintenanceTeam
            new BiaTeamConfig<BaseEntityTeam>()
            {
                TeamTypeId = (int)TeamTypeId.MaintenanceTeam,
                RightPrefix = "MaintenanceTeam",
                AdminRoleIds = [
                    (int)RoleId.MaintenanceTeamAdmin
                    ],
                Children = new ImmutableListBuilder<BiaTeamChildrenConfig<BaseEntityTeam>>
                {
                // BIAToolKit - Begin TeamConfigMaintenanceTeamChildren
                // BIAToolKit - End TeamConfigMaintenanceTeamChildren
                }.ToImmutable(),
                Parents = new ImmutableListBuilder<BiaTeamParentConfig<BaseEntityTeam>>
                {
                    new BiaTeamParentConfig<BaseEntityTeam>
                    {
                        TeamTypeId = (int)TeamTypeId.AircraftMaintenanceCompany,
                        GetParent = team => (team as MaintenanceCompanies.Entities.MaintenanceTeam).AircraftMaintenanceCompany,
                    },
                }
                .ToImmutable(),
            },

            // BIAToolKit - End Partial TeamConfig MaintenanceTeam
