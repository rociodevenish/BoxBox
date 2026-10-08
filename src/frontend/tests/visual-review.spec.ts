import { test, expect } from "@playwright/test";
import { mkdir, writeFile, readFile } from "node:fs/promises";
const evidence = "../../.scratch/visual-design-foundation/evidence";
for (const [width, height] of [
  [360, 800],
  [768, 1024],
  [1920, 1080],
]) {
  test(`card gallery remains usable at ${width}px with reduced motion`, async ({
    page,
  }) => {
    await page.setViewportSize({ width, height });
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.goto("/");
    const compact = page.getByRole("region", { name: "Compact cards" });
    await expect(
      compact.getByRole("article", {
        name: "Alexandra Montgomery-Sutherland — compact driver card",
      }),
    ).toBeVisible();
    await page.evaluate(() =>
      Promise.all([...document.images].map((i) => i.decode())),
    );
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth),
    ).toBe(width);
    for (const button of await page.getByRole("button").all()) {
      const size = await button.boundingBox();
      expect(size?.height).toBeGreaterThanOrEqual(44);
      expect(size?.width).toBeGreaterThanOrEqual(44);
    }
    await mkdir(evidence, { recursive: true });
    const files = [];
    for (const [label, locator] of [
      ["compact", compact],
      [
        "expanded",
        page.getByRole("article", {
          name: "Lewis Hamilton — expanded driver card",
        }),
      ],
      [
        "tv",
        page.getByRole("article", { name: "Max Verstappen — tv driver card" }),
      ],
    ] as const) {
      const file = `angular-${label}-${width}.png`;
      await locator.screenshot({ path: `${evidence}/${file}` });
      files.push(file);
    }
    await compact
      .getByRole("button", {
        name: "View details for Alexandra Montgomery-Sutherland",
      })
      .click();
    const dialog = page.getByRole("dialog", {
      name: "Alexandra Montgomery-Sutherland — driver details",
    });
    await expect(dialog).toBeVisible();
    expect(await dialog.evaluate((e) => e.scrollWidth <= e.clientWidth)).toBe(
      true,
    );
    const file = `angular-dialog-${width}.png`;
    await dialog.screenshot({ path: `${evidence}/${file}` });
    files.push(file);
    const captures = [];
    for (const file of files) {
      const png = await readFile(`${evidence}/${file}`);
      captures.push({
        file,
        width: png.readUInt32BE(16),
        height: png.readUInt32BE(20),
      });
    }
    await writeFile(
      `${evidence}/angular-captures-${width}.json`,
      JSON.stringify(
        { viewport: { width, height }, reducedMotion: true, captures },
        null,
        2,
      ),
    );
  });
}
