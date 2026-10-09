import { test, expect } from "@playwright/test";
import { mkdir, readFile, writeFile } from "node:fs/promises";
const out = "../../.tools/reports/gallery";
for (const [width, height] of [
  [360, 800],
  [768, 1024],
  [1920, 1080],
]) {
  test(`public tower is readable at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height });
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.goto("/?view=public");
    const tower = page.getByRole("table", {
      name: "Classification — Gap to leader",
    });
    await expect(tower.getByRole("row")).toHaveCount(23);
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth),
    ).toBe(width);
    const identity = tower.getByText("Alexandra Montgomery-Sutherland", {
      exact: true,
    });
    await expect(identity).toBeVisible();
    const nameBox = await identity.boundingBox();
    expect(nameBox?.width).toBeGreaterThan(0);
    await expect(
      tower
        .getByRole("row")
        .filter({ hasText: "Carlos Sainz" })
        .getByText("Stop pending", { exact: true })
        .filter({ visible: true }),
    ).toBeVisible();
    await expect(
      tower
        .getByRole("row")
        .filter({ hasText: "Fernando Alonso" })
        .getByText("In pit lane", { exact: true })
        .filter({ visible: true }),
    ).toBeVisible();
    await expect(
      tower
        .getByRole("row")
        .filter({ hasText: "Lewis Hamilton" })
        .getByText("+1.374 s", { exact: true }),
    ).toBeVisible();
    await expect(
      tower
        .getByRole("row")
        .filter({ hasText: "Lewis Hamilton" })
        .getByLabel("Tyre compound Medium", { exact: true }),
    ).toBeVisible();
    await page.evaluate(() =>
      Promise.all([...document.images].map((i) => i.decode())),
    );
    await mkdir(out, { recursive: true });
    const crop = `angular-tower-${width}.png`;
    await tower.screenshot({ path: `${out}/${crop}` });
    const captures = [];
    for (const file of [crop]) {
      const png = await readFile(`${out}/${file}`);
      captures.push({
        file,
        width: png.readUInt32BE(16),
        height: png.readUInt32BE(20),
      });
    }
    let lastBounds = null;
    if (width === 1920) {
      await page.evaluate(() => scrollTo(0, 0));
      const file = "angular-public-1920x1080.png";
      await page.screenshot({ path: `${out}/${file}` });
      const png = await readFile(`${out}/${file}`);
      captures.push({
        file,
        width: png.readUInt32BE(16),
        height: png.readUInt32BE(20),
      });
      lastBounds = await tower
        .getByRole("row")
        .filter({ hasText: "Sergio Pérez" })
        .boundingBox();
      expect(lastBounds!.y + lastBounds!.height).toBeLessThanOrEqual(1080);
    }
    await writeFile(
      `${out}/tower-captures-${width}.json`,
      JSON.stringify(
        {
          viewport: { width, height },
          reducedMotion: true,
          captures,
          lastRowBounds: lastBounds,
        },
        null,
        2,
      ),
    );
  });
}
