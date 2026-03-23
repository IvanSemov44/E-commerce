import type { ChangeEvent, FocusEvent } from 'react';
import type { ProfileFormValues } from '../ProfileFormCard/profileSchemas';

export type ProfileFieldErrors = Partial<Record<keyof ProfileFormValues, string>>;

export interface ProfileFormProps {
  values: ProfileFormValues;
  fieldErrors: ProfileFieldErrors;
  isEditMode: boolean;
  isPending: boolean;
  action: (payload?: unknown) => void;
  onCancel: () => void;
  onChange: (e: ChangeEvent<HTMLInputElement>) => void;
  onBlur: (e: FocusEvent<HTMLInputElement>) => void;
}
