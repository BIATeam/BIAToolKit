            // BIAToolKit - Begin Partial TeamConfig MaintenanceTeam
            new()
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
                    new()
                    {
                        TeamTypeId = (int)TeamTypeId.AircraftMaintenanceCompany,
                        GetParent = team => (team as MaintenanceCompanies.Entities.MaintenanceTeam).AircraftMaintenanceCompany,
                    },
                }
                .ToImmutable(),
                Label = "maintenanceTeam.headerLabel",
            },

            // BIAToolKit - End Partial TeamConfig MaintenanceTeam
