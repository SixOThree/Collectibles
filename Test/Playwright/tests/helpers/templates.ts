import { expect, Locator, Page } from '@playwright/test';

export type TemplateFieldType = 'text' | 'number' | 'dropdown';

export type TemplateFieldConfig = {
  name: string;
  label: string;
  type?: TemplateFieldType;
  required?: boolean;
  placeholder?: string;
  helpText?: string;
  defaultValue?: string;
  options?: string[];
};

export type CreateShowcaseTemplateOptions = {
  showcaseName: string;
  templateName: string;
  description?: string;
  hideAttachments?: boolean;
  allowMultipleEntries?: boolean;
  fields: TemplateFieldConfig[];
};

const fieldTypeLabels: Record<TemplateFieldType, string> = {
  text: 'Text (Single Line)',
  number: 'Number',
  dropdown: 'Dropdown',
};

export async function createShowcaseTemplate(page: Page, options: CreateShowcaseTemplateOptions): Promise<void> {
  await page.goto('/templates/new', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Create New Template' })).toBeVisible({ timeout: 15000 });

  await page.getByLabel(/Template Name/).fill(options.templateName);
  await page.getByLabel(/^Description$/).fill(options.description ?? `${options.templateName} description`);
  await getFieldContainer(page.locator('.basic-info-section'), 'Select Showcase').locator('select').selectOption({
    label: options.showcaseName,
  });

  await setCheckbox(page.getByLabel('Show Related Items Only'), options.hideAttachments ?? false);
  await setCheckbox(page.getByLabel('Allow Multiple Entries'), options.allowMultipleEntries ?? false);

  for (const field of options.fields) {
    await addTemplateField(page, field);
  }

  // The dropdown editor schedules a delayed focus on new option inputs.
  // Give those UI updates a moment to settle before submitting the form.
  await page.waitForTimeout(200);
  await page.getByRole('button', { name: 'Create Template' }).click();
  await expect(page).toHaveURL(/\/templates$/);
  await expect(page.getByText(options.templateName, { exact: true })).toBeVisible({ timeout: 15000 });
}

export async function addTemplateField(page: Page, field: TemplateFieldConfig): Promise<void> {
  await page.getByRole('button', { name: 'Add Field' }).click();

  const editor = page.locator('.field-editor.expanded').last();
  await expect(editor).toBeVisible({ timeout: 15000 });

  const fieldNameInput = getFieldContainer(editor, 'Field Name').locator('input');
  await fieldNameInput.fill(field.name);
  await expect(fieldNameInput).toHaveValue(field.name);
  await fieldNameInput.press('Tab');

  const displayLabelInput = getFieldContainer(editor, 'Display Label').locator('input');
  await displayLabelInput.fill(field.label);
  await expect(displayLabelInput).toHaveValue(field.label);
  await displayLabelInput.press('Tab');

  const fieldType = field.type ?? 'text';
  if (fieldType !== 'text') {
    await getFieldContainer(editor, 'Field Type').locator('select').selectOption({ label: fieldTypeLabels[fieldType] });
  }

  await setCheckbox(
    editor.locator('.form-check').filter({ hasText: 'Required Field' }).locator('input[type="checkbox"]'),
    field.required ?? false
  );

  if (field.placeholder) {
    const placeholderInput = getFieldContainer(editor, 'Placeholder Text').locator('input');
    await placeholderInput.fill(field.placeholder);
    await expect(placeholderInput).toHaveValue(field.placeholder);
    await placeholderInput.press('Tab');
  }

  if (field.helpText) {
    const helpTextInput = getFieldContainer(editor, 'Help Text').locator('input');
    await helpTextInput.fill(field.helpText);
    await expect(helpTextInput).toHaveValue(field.helpText);
    await helpTextInput.press('Tab');
  }

  if (field.defaultValue) {
    const defaultValueInput = getFieldContainer(editor, 'Default Value').locator('input');
    await defaultValueInput.fill(field.defaultValue);
    await expect(defaultValueInput).toHaveValue(field.defaultValue);
    await defaultValueInput.press('Tab');
  }

  if (fieldType === 'dropdown') {
    const options = field.options ?? [];
    await expect(editor.getByRole('button', { name: 'Add Option' })).toBeVisible({ timeout: 15000 });

    for (const option of options) {
      const optionInputs = editor.locator('.dropdown-option-item input');
      const existingCount = await optionInputs.count();
      await editor.getByRole('button', { name: 'Add Option' }).click();
      await expect(optionInputs).toHaveCount(existingCount + 1);
      await optionInputs.last().fill(option);
      await expect(optionInputs.last()).toHaveValue(option);
      await optionInputs.last().press('Tab');
    }
  }
}

async function setCheckbox(locator: Locator, desiredValue: boolean): Promise<void> {
  const isChecked = await locator.isChecked();
  if (desiredValue && !isChecked) {
    await locator.check();
  }

  if (!desiredValue && isChecked) {
    await locator.uncheck();
  }
}

function getFieldContainer(scope: Locator, labelText: string): Locator {
  return scope.locator('.mb-3').filter({ hasText: labelText }).first();
}
