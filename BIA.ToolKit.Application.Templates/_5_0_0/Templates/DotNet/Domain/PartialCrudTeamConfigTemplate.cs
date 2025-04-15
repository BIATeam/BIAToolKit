            // BIAToolKit - Begin Partial TeamConfig Plane
            new BiaTeamConfig<Team>()
            {
                TeamTypeId = (int)TeamTypeId.Plane,
                RightPrefix = "Plane",
                AdminRoleIds = new int[] { (int)RoleId.PlaneAdmin },
                Parents = new ImmutableListBuilder<BiaTeamParentConfig<Team>>
                {
                    new BiaTeamParentConfig<Team>
                    {
                        TeamTypeId = (int)TeamTypeId.Site,
                        GetParent = team => (team as Fleet.Entities.Plane).Site,
                    },
                }
                .ToImmutable(),
            },

            // BIAToolKit - End Partial TeamConfig Plane
