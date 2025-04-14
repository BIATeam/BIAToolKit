            // BIAToolKit - Begin Partial RoleModelBuilder Plane
            modelBuilder.Entity<Role>().HasData(new Role { Id = (int)RoleId.PlaneAdmin, Code = "Plane_Admin", Label = "Plane administrator" });
            modelBuilder.Entity<Role>().HasData(new Role { Id = (int)RoleId.SiteTeamLeader, Code = "Site_TeamLeader", Label = "Site Team leader" });

            // BIAToolKit - End Partial RoleModelBuilder Plane
