            // BIAToolKit - Begin Partial RoleModelBuilder MaintenanceTeam
            modelBuilder.Entity<Role>().HasData(new Role { Id = (int)RoleId.MaintenanceTeamAdmin, Code = "MaintenanceTeam_Admin", Label = "MaintenanceTeam administrator" });
            modelBuilder.Entity<Role>().HasData(new Role { Id = (int)RoleId.AircraftMaintenanceCompanyTeamLeader, Code = "AircraftMaintenanceCompany_TeamLeader", Label = "AircraftMaintenanceCompany Team leader" });

            // BIAToolKit - End Partial RoleModelBuilder MaintenanceTeam
