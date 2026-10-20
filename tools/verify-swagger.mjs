// Проверяет настоящий Swagger UI в отдельном временном профиле Chrome.
import { chromium } from 'playwright-core';
import assert from 'node:assert/strict';
import fs from 'node:fs/promises';

const baseUrl = process.argv[2] ?? 'http://127.0.0.1:5087';
const chromePath = process.env.CHROME_PATH ?? '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const browser = await chromium.launch({ executablePath: chromePath, headless: true });
try {
  const page = await browser.newPage();
  const failedRequests = [];
  page.on('requestfailed', request => failedRequests.push(request.url()));
  await page.goto(`${baseUrl}/swagger/index.html`, { waitUntil: 'networkidle' });
  await page.locator('.opblock').first().waitFor();
  assert.equal(await page.locator('.opblock').count(), 10, 'Ожидаются 10 операций API');
  assert.deepEqual(failedRequests, [], 'Все файлы Swagger должны загружаться локально');

  const createClient = page.locator('.opblock-post').filter({
    has: page.locator('.opblock-summary-path[data-path="/api/clients"]')
  });
  await createClient.locator('.opblock-summary').click();
  await createClient.getByRole('button', { name: 'Try it out' }).click();
  await createClient.locator('textarea').fill(JSON.stringify({
    firstName: 'Swagger', lastName: 'Student', birthDate: '1990-01-01',
    phoneNumber: 'swagger-phone', passportNumber: 'swagger-passport'
  }));
  const responseReady = page.waitForResponse(response =>
    response.url().endsWith('/api/clients') && response.request().method() === 'POST');
  await createClient.getByRole('button', { name: 'Execute', exact: true }).click();
  const response = await responseReady;
  assert.equal(response.status(), 201);
  const client = await response.json();
  assert.equal(client.accounts[0].currency, 'USD');

  await fs.mkdir('artifacts/verification', { recursive: true });
  await page.screenshot({ path: 'artifacts/verification/swagger.png', fullPage: true });
  const removed = await page.request.delete(`${baseUrl}/api/clients/${client.id}`);
  assert.equal(removed.status(), 204);
  console.log('Swagger browser: 10 operations rendered, Try it out POST 201, USD account and cleanup 204 passed.');
} finally {
  await browser.close();
}
