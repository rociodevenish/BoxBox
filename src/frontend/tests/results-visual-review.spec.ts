import { test, expect } from "@playwright/test";
import { mkdirSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
const evidence = resolve("../../.scratch/visual-design-foundation/evidence");
for (const [width, height] of [
  [360, 800],
  [768, 1024],
  [1920, 1080],
]) {
  test(`public phase/finish review ${width}x${height}`, async ({ page }) => {
    await page.setViewportSize({ width, height });
    await page.goto("/?view=results");
    const events = page.getByRole("region", { name: "Public phase events" });
    const original = await events.innerText();
    await page.emulateMedia({ reducedMotion: "reduce" });
    await expect(events).toHaveText(original, { useInnerText: true });
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth),
    ).toBeLessThanOrEqual(width);
    await expect(page.getByRole("table")).toBeVisible();
    mkdirSync(evidence, { recursive: true });
    const dimensions: Record<string, unknown> = { viewport: { width, height } };
    for (const [state, button] of [
      ["phase", null],
      ["crowded", "Show crowded phase"],
      ["finished", "Show race finish"],
    ] as const) {
      if (button) await page.getByRole("button", { name: button }).click();
      await expect(
        page.getByRole("heading", {
          name: state === "finished" ? "Race finished" : "Phase 4 results",
          exact: true,
        }),
      ).toBeVisible();
      await page.evaluate(() => {
        window.scrollTo(0, 0);
        return new Promise<void>((resolve) =>
          requestAnimationFrame(() => resolve()),
        );
      });
      if (width === 1920) {
        const last = await page
          .getByRole("table")
          .getByRole("row")
          .last()
          .boundingBox();
        expect(last!.y + last!.height).toBeLessThanOrEqual(height);
      }
      dimensions[state] = await page.evaluate(() => ({
        width: document.documentElement.scrollWidth,
        height: document.documentElement.scrollHeight,
      }));
      await page.screenshot({
        path: resolve(evidence, `results-${state}-${width}.png`),
        fullPage: width !== 1920,
      });
    }
    writeFileSync(
      resolve(evidence, `results-captures-${width}.json`),
      JSON.stringify(dimensions, null, 2) + "\n",
    );
  });
}
