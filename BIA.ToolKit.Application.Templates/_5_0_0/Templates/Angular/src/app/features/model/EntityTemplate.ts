import { Validators } from '@angular/forms';
import {
  BaseTeamDto,
  teamFieldsConfigurationColumns,
} from 'src/app/shared/bia-shared/model/base-team-dto';
import {
  BiaFieldConfig,
  BiaFieldNumberFormat,
  BiaFieldsConfig,
  NumberMode,
  PrimeNGFiltering,
  PropType,
} from 'src/app/shared/bia-shared/model/bia-field-config';
import { BiaFormLayoutConfig } from 'src/app/shared/bia-shared/model/bia-form-layout-config';
import { OptionDto } from 'src/app/shared/bia-shared/model/option-dto';

// TODO after creation of CRUD MaintenanceTeam : adapt the model
export class MaintenanceTeam extends BaseTeamDto {
  aircraftMaintenanceCompanyId: number;
  msn: string;
  isActive: boolean;
  firstFlightDate: Date;
  motorsCount: number | null;
  someDecimal: number;
  engines: OptionDto[];
  planeType: OptionDto;
  similarTypes: OptionDto[] | null;
}

// TODO after creation of CRUD MaintenanceTeam : adapt the field configuration
export const maintenanceTeamFieldsConfiguration: BiaFieldsConfig<MaintenanceTeam> = {
  columns: [
      ...teamFieldsConfigurationColumns,
      ...[
    Object.assign(new BiaFieldConfig('aircraftMaintenanceCompanyId', 'maintenanceTeam.aircraftMaintenanceCompanyId'), {
      type: PropType.Number,
      isRequired: true,
      validators: [Validators.required],
    }),
    Object.assign(new BiaFieldConfig('msn', 'maintenanceTeam.msn'), {
      type: PropType.String,
      isRequired: true,
      validators: [Validators.required],
    }),
    Object.assign(new BiaFieldConfig('isActive', 'maintenanceTeam.isActive'), {
      type: PropType.Boolean,
      isRequired: true,
      validators: [Validators.required],
    }),
    Object.assign(new BiaFieldConfig('firstFlightDate', 'maintenanceTeam.firstFlightDate'), {
      type: PropType.Date,
      isRequired: true,
      validators: [Validators.required],
    }),
    Object.assign(new BiaFieldConfig('motorsCount', 'maintenanceTeam.motorsCount'), {
      type: PropType.Number,
    }),
    Object.assign(new BiaFieldConfig('someDecimal', 'maintenanceTeam.someDecimal'), {
      type: PropType.Number,
      isRequired: true,
      validators: [Validators.required],
    }),
    Object.assign(new BiaFieldConfig('engines', 'maintenanceTeam.engines'), {
      type: PropType.ManyToMany,
      isRequired: true,
      validators: [Validators.required],
    }),
    Object.assign(new BiaFieldConfig('planeType', 'maintenanceTeam.planeType'), {
      type: PropType.OneToMany,
      isRequired: true,
      validators: [Validators.required],
    }),
    Object.assign(new BiaFieldConfig('similarTypes', 'maintenanceTeam.similarTypes'), {
      type: PropType.ManyToMany,
    }),
    Object.assign(new BiaFieldConfig('rowVersion', 'maintenanceTeam.rowVersion'), {
      isVisible: false,
      isHideByDefault: true,
      isVisibleInTable: false,
    }),
    ],
  ],
};

// TODO after creation of CRUD MaintenanceTeam : adapt the form layout configuration
export const maintenanceTeamFormLayoutConfiguration: BiaFormLayoutConfig<MaintenanceTeam> =
  new BiaFormLayoutConfig([]);
