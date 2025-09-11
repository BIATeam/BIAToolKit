import {
  BaseDto,
  BiaFieldConfig,
  BiaFieldsConfig,
  BiaFormLayoutConfig,
  OptionDto,
  TeamDto,
  teamFieldsConfigurationColumns,
} from 'bia-ng/models';
import { PropType } from 'bia-ng/models/enum';

// TODO after creation of CRUD Team MaintenanceTeam : adapt the model
export interface MaintenanceTeam
  extends BaseDto, TeamDto {
  aircraftMaintenanceCompanyId: number | null;
  msn: string | null;
  isActive: boolean | null;
  firstFlightDate: Date | null;
  motorsCount: number | null;
  someDecimal: number | null;
  engines: OptionDto[] | null;
  planeType: OptionDto | null;
  similarTypes: OptionDto[] | null;
}

// TODO after creation of CRUD Team MaintenanceTeam : adapt the field configuration
export const maintenanceTeamFieldsConfiguration: BiaFieldsConfig<MaintenanceTeam> = {
  columns: [
      ...teamFieldsConfigurationColumns,
      ...[
    Object.assign(new BiaFieldConfig('aircraftMaintenanceCompanyId', 'maintenanceTeam.aircraftMaintenanceCompanyId'), {
      type: PropType.Number,
    }),
    Object.assign(new BiaFieldConfig('msn', 'maintenanceTeam.msn'), {
      type: PropType.String,
    }),
    Object.assign(new BiaFieldConfig('isActive', 'maintenanceTeam.isActive'), {
      type: PropType.Boolean,
    }),
    Object.assign(new BiaFieldConfig('firstFlightDate', 'maintenanceTeam.firstFlightDate'), {
      type: PropType.Date,
    }),
    Object.assign(new BiaFieldConfig('motorsCount', 'maintenanceTeam.motorsCount'), {
      type: PropType.Number,
    }),
    Object.assign(new BiaFieldConfig('someDecimal', 'maintenanceTeam.someDecimal'), {
      type: PropType.Number,
    }),
    Object.assign(new BiaFieldConfig('engines', 'maintenanceTeam.engines'), {
      type: PropType.ManyToMany,
    }),
    Object.assign(new BiaFieldConfig('planeType', 'maintenanceTeam.planeType'), {
      type: PropType.OneToMany,
    }),
    Object.assign(new BiaFieldConfig('similarTypes', 'maintenanceTeam.similarTypes'), {
      type: PropType.ManyToMany,
    }),
    ],
  ],
};

// TODO after creation of CRUD Team MaintenanceTeam : adapt the form layout configuration
export const maintenanceTeamFormLayoutConfiguration: BiaFormLayoutConfig<MaintenanceTeam> =
  new BiaFormLayoutConfig([
  ]);
