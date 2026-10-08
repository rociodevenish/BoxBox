import { test, expect } from "@playwright/test";
test("public Classification keeps time gaps, lap deficits and stop states separate", async ({
  page,
}) => {
  await page.goto("/");
  const tower = page.getByRole("table", {
    name: "Classification — gap to leader",
  });
  await expect(tower.getByRole("row")).toHaveCount(23);
  const lewis = tower.getByRole("row").filter({ hasText: "Lewis Hamilton" });
  await expect(lewis.getByText("+1.374 s", { exact: true })).toBeVisible();
  const carlos = tower.getByRole("row").filter({ hasText: "Carlos Sainz" });
  await expect(carlos.getByText("+8.244 s", { exact: true })).toBeVisible();
  await expect(
    carlos.getByText("Stop pending", { exact: true }).filter({ visible: true }),
  ).toBeVisible();
  const fernando = tower
    .getByRole("row")
    .filter({ hasText: "Fernando Alonso" });
  await expect(fernando.getByText("+9.618 s", { exact: true })).toBeVisible();
  await expect(
    fernando
      .getByText("In pit lane", { exact: true })
      .filter({ visible: true }),
  ).toBeVisible();
  await expect(
    tower
      .getByRole("row")
      .filter({ hasText: "Sergio Pérez" })
      .getByText("+2 laps", { exact: true }),
  ).toBeVisible();
  await expect(
    tower
      .getByRole("row")
      .filter({ hasText: "Valtteri Bottas" })
      .getByText("Unavailable", { exact: true }),
  ).toBeVisible();
});

test("shared display keeps all 22 cars below the phase header within 1080px", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1920, height: 1080 });
  await page.goto("/?view=public");
  await expect(
    page.getByRole("heading", { name: "Race Phase 4", exact: true }),
  ).toBeVisible();
  const tower = page.getByRole("table", {
    name: "Classification — gap to leader",
  });
  await expect(tower.getByRole("row")).toHaveCount(23);
  const last = tower.getByRole("row").filter({ hasText: "Sergio Pérez" });
  const bounds = await last.boundingBox();
  expect(bounds).not.toBeNull();
  expect(bounds!.y + bounds!.height).toBeLessThanOrEqual(1080);
  await expect(
    tower.getByText("Alexandra Montgomery-Sutherland", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText(/Tyre estimate|Pace Instruction|Pit Instruction/),
  ).toHaveCount(0);
});
test("public leader portrait failure preserves race identity and Classification", async ({
  page,
}) => {
  await page.route("**/portraits/max-verstappen.png", (route) =>
    route.fulfill({ status: 404, body: "Missing" }),
  );
  await page.goto("/?view=public");
  const leader = page.getByRole("complementary", { name: "Race leader" });
  await expect(
    leader.getByText("Portrait unavailable", { exact: true }),
  ).toBeVisible();
  await expect(
    leader.getByRole("heading", { name: "Verstappen", exact: true }),
  ).toBeVisible();
  await expect(
    page
      .getByRole("table", { name: "Classification — gap to leader" })
      .getByRole("row"),
  ).toHaveCount(23);
});
test("full compound labels are not clipped at phone width", async ({
  page,
}) => {
  await page.setViewportSize({ width: 360, height: 800 });
  await page.goto("/?view=public");
  const tower = page.getByRole("table", {
    name: "Classification — gap to leader",
  });
  const bounds = await tower.boundingBox();
  for (const label of await tower
    .getByLabel("Tyre compound Medium", { exact: true })
    .all()) {
    const box = await label.boundingBox();
    expect(box!.x + box!.width).toBeLessThanOrEqual(bounds!.x + bounds!.width);
  }
});
