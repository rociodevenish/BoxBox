import { test, expect } from "@playwright/test";
import { mkdirSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
const evidence = resolve("../../.tools/reports/gallery");
test("integrated initial and two-car compositions fit the actual shared-display viewport", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1920, height: 1080 });
  await page.emulateMedia({ reducedMotion: "reduce" });
  mkdirSync(evidence, { recursive: true });
  await page.goto("/?view=results");
  await page.getByRole("button", { name: "Show initial race" }).click();
  await expect(
    page.getByRole("heading", { name: "Race ready", exact: true }),
  ).toBeVisible();
  await page.evaluate(() => window.scrollTo(0, 0));
  const last = await page
    .getByRole("table")
    .getByRole("row")
    .last()
    .boundingBox();
  expect(last!.y + last!.height).toBeLessThanOrEqual(1080);
  await page.screenshot({
    path: resolve(evidence, "integrated-initial-1920x1080.png"),
  });
  await page.goto("/?view=controller");
  await expect(
    page.getByRole("heading", { name: "Alexandra Montgomery-Sutherland" }),
  ).toBeVisible();
  const review = page.getByRole("button", { name: "Approve Plan" });
  const box = await review.boundingBox();
  expect(box!.y + box!.height).toBeLessThanOrEqual(1080);
  await review.focus();
  await page.screenshot({
    path: resolve(evidence, "integrated-controller-1920x1080.png"),
  });
  writeFileSync(
    resolve(evidence, "integrated-captures.json"),
    JSON.stringify(
      {
        viewport: { width: 1920, height: 1080 },
        png: { width: 1920, height: 1080 },
        initialLastRow: last,
        controllerReview: box,
      },
      null,
      2,
    ) + "\n",
  );
});
