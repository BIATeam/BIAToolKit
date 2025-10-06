import { BaseDto } from 'src/app/shared/bia-shared/model/dto/base-dto';
import {
  BiaFieldConfig,
  BiaFieldNumberFormat,
  BiaFieldsConfig,
  NumberMode,
  PrimeNGFiltering,
  PropType,
} from 'src/app/shared/bia-shared/model/bia-field-config';
import { 
  BiaFormLayoutConfig,
  BiaFormLayoutConfigColumnSize,
  BiaFormLayoutConfigField,
  BiaFormLayoutConfigGroup,
  BiaFormLayoutConfigRow,
} from 'src/app/shared/bia-shared/model/bia-form-layout-config';
import { OptionDto } from 'src/app/shared/bia-shared/model/option-dto';
import {
  TeamDto,
  teamFieldsConfigurationColumns,
} from 'src/app/shared/bia-shared/model/dto/team-dto';

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
