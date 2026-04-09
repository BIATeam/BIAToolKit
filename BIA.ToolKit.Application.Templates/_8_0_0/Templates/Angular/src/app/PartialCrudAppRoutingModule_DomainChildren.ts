              // BIAToolKit - Begin Partial RoutingDomainFleetChildren Plane
              {
                path: 'planes',
                data: {
                  breadcrumb: 'app.planes',
                  canNavigate: true,
                },
                loadChildren: () =>
                  import(
                    './features/planes/plane.module'
                  ).then(m => m.PlaneModule),
              },
              // BIAToolKit - End Partial RoutingDomainFleetChildren Plane
