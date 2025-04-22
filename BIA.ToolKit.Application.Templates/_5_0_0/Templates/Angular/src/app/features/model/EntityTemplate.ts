import { Validators } from '@angular/forms';
import { BaseDto } from 'src/app/shared/bia-shared/model/base-dto';
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
export class MaintenanceTeam extends BaseDto {
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
    Object.assign(new BiaFieldConfig('aircraftMaintenanceCompanyId', 'maintenanceTeam.aircraftMaintenanceCompanyId'), {
      isRequired: true,
      type: PropType.Number,
      validators: [Validators.required],
    }),
    Object.assign(new BiaFieldConfig('msn', 'maintenanceTeam.msn'), {
      isRequired: true,
      type: PropType.String,
      validators: [Validators.required],
    }),
    Object.assign(new BiaFieldConfig('isActive', 'maintenanceTeam.isActive'), {
      isRequired: true,
      type: PropType.Boolean,
      validators: [Validators.required],
    }),
    Object.assign(new BiaFieldConfig('firstFlightDate', 'maintenanceTeam.firstFlightDate'), {
      isRequired: true,
      type: PropType.Date,
      validators: [Validators.required],
    }),
    Object.assign(new BiaFieldConfig('motorsCount', 'maintenanceTeam.motorsCount'), {
      type: PropType.Number,
    }),
    Object.assign(new BiaFieldConfig('someDecimal', 'maintenanceTeam.someDecimal'), {
      isRequired: true,
      type: PropType.Number,
      displayFormat: Object.assign(new BiaFieldNumberFormat(), {
        mode: NumberMode.Decimal,
        minFractionDigits: 2,
        maxFractionDigits: 2,
      }),
      validators: [Validators.required],
    }),
    Object.assign(new BiaFieldConfig('engines', 'maintenanceTeam.engines'), {
      isRequired: true,
      type: PropType.ManyToMany,
      validators: [Validators.required],
    }),
    Object.assign(new BiaFieldConfig('planeType', 'maintenanceTeam.planeType'), {
      isRequired: true,
      type: PropType.OneToMany,
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
};

// TODO after creation of CRUD MaintenanceTeam : adapt the form layout configuration
export const maintenanceTeamFormLayoutConfiguration: BiaFormLayoutConfig<MaintenanceTeam> =
  new BiaFormLayoutConfig([]);
