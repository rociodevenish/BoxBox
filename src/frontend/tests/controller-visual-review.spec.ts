import { test, expect } from "@playwright/test";
import { mkdirSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
const evidence = resolve("../../.scratch/visual-design-foundation/evidence");
for (const [width, height] of [
  [360, 800],
  [800, 360],
  [768, 1024],
  [1024, 768],
]) {
  test(`controller containment and reviewed capture ${width}x${height}`, async ({
    page,
  }) => {
    await page.setViewportSize({ width, height });
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.goto("/?view=controller");
    await expect(
      page.getByRole("heading", { name: "Alexandra Montgomery-Sutherland" }),
    ).toBeVisible();
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth),
    ).toBeLessThanOrEqual(width);
    const controls = page.locator("app-two-car-controller button");
    for (const button of await controls.all()) {
      if (!(await button.isVisible())) continue;
      const box = await button.boundingBox();
      expect(box!.height).toBeGreaterThanOrEqual(44);
      expect(box!.width).toBeGreaterThanOrEqual(44);
      expect(box!.x).toBeGreaterThanOrEqual(0);
      expect(box!.x + box!.width).toBeLessThanOrEqual(width);
    }
    mkdirSync(evidence, { recursive: true });
    await page.screenshot({
      path: resolve(evidence, `controller-${width}x${height}.png`),
      fullPage: true,
    });
    await page.getByRole("button", { name: "Review team plan" }).click();
    const dialog = page.getByRole("dialog", { name: "Confirm both cars" });
    const box = await dialog.boundingBox();
    expect(box!.x).toBeGreaterThanOrEqual(0);
    expect(box!.y).toBeGreaterThanOrEqual(0);
    expect(box!.x + box!.width).toBeLessThanOrEqual(width);
    expect(box!.y + box!.height).toBeLessThanOrEqual(height);
    await page.screenshot({
      path: resolve(evidence, `controller-confirm-${width}x${height}.png`),
    });
    await page.getByRole("button", { name: "Submit both cars" }).click();
    await page.getByRole("button", { name: "Reject submission" }).click();
    await page.getByRole("status").scrollIntoViewIfNeeded();
    await page.screenshot({
      path: resolve(evidence, `controller-rejected-${width}x${height}.png`),
      fullPage: true,
    });
    writeFileSync(
      resolve(evidence, `controller-${width}x${height}.json`),
      JSON.stringify(
        {
          viewport: { width, height },
          fullPage: await page.evaluate(() => ({
            width: document.documentElement.scrollWidth,
            height: document.documentElement.scrollHeight,
          })),
          confirmation: box,
        },
        null,
        2,
      ) + "\n",
    );
  });
}
