import {
  BaseDto,
  BiaFieldConfig,
  BiaFieldsConfig,
  BiaFormLayoutConfig,
  OptionDto,
} from '@bia-team/bia-ng/models';
import { PropType } from '@bia-team/bia-ng/models/enum';

// TODO after creation of CRUD Plane : adapt the model
export interface Plane
  extends BaseDto {
  siteId: number | null;
  msn: string | null;
  isActive: boolean | null;
  firstFlightDate: Date | null;
  motorsCount: number | null;
  someDecimal: number | null;
  engines: OptionDto[] | null;
  planeType: OptionDto | null;
  similarTypes: OptionDto[] | null;
}

// TODO after creation of CRUD Plane : adapt the field configuration
export const planeFieldsConfiguration: BiaFieldsConfig<Plane> = {
  columns: [
    Object.assign(new BiaFieldConfig('siteId', 'plane.siteId'), {
      type: PropType.Number,
    }),
    Object.assign(new BiaFieldConfig('msn', 'plane.msn'), {
      type: PropType.String,
    }),
    Object.assign(new BiaFieldConfig('isActive', 'plane.isActive'), {
      type: PropType.Boolean,
    }),
    Object.assign(new BiaFieldConfig('firstFlightDate', 'plane.firstFlightDate'), {
      type: PropType.Date,
    }),
    Object.assign(new BiaFieldConfig('motorsCount', 'plane.motorsCount'), {
      type: PropType.Number,
    }),
    Object.assign(new BiaFieldConfig('someDecimal', 'plane.someDecimal'), {
      type: PropType.Number,
    }),
    Object.assign(new BiaFieldConfig('engines', 'plane.engines'), {
      type: PropType.ManyToMany,
    }),
    Object.assign(new BiaFieldConfig('planeType', 'plane.planeType'), {
      type: PropType.OneToMany,
    }),
    Object.assign(new BiaFieldConfig('similarTypes', 'plane.similarTypes'), {
      type: PropType.ManyToMany,
    }),
  ],
};

// TODO after creation of CRUD Plane : adapt the form layout configuration
export const planeFormLayoutConfiguration: BiaFormLayoutConfig<Plane> =
  new BiaFormLayoutConfig([
  ]);
