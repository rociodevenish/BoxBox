import { test, expect } from "@playwright/test";
test("phase results show public overtake and pit feedback separately from the final Classification", async ({
  page,
}) => {
  await page.goto("/?view=results");
  await expect(
    page.getByRole("heading", { name: "Phase 4 results", exact: true }),
  ).toBeVisible();
  const events = page.getByRole("region", { name: "Public phase events" });
  await expect(events).toContainText("Lewis Hamilton passed Max Verstappen");
  await expect(events).toContainText("Fernando Alonso entered the pit lane");
  await expect(events).toContainText("Fernando Alonso changed Medium to Hard");
  await expect(events).toContainText("Fernando Alonso exited the pit lane");
  await page.getByRole("button", { name: "Show race finish" }).click();
  await expect(
    page.getByRole("heading", { name: "Race finished", exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Final Classification", exact: true }),
  ).toBeVisible();
  await expect(page.getByRole("status")).toContainText(
    "No further decision phases",
  );
  await expect(
    page.getByRole("heading", { name: "Phase 4 results", exact: true }),
  ).toHaveCount(0);
});

test("crowded published pit entries retain matching ongoing stops in Classification", async ({
  page,
}) => {
  await page.goto("/?view=results");
  await page.getByRole("button", { name: "Show crowded phase" }).click();
  const feed = page.getByRole("region", { name: "Public phase events" });
  await expect(feed.getByRole("article")).toHaveCount(14);
  const row = page.getByRole("row").filter({ hasText: "Pierre Gasly" });
  await expect(row).toContainText("In pit lane");
  await page.getByRole("button", { name: "Show quiet phase" }).click();
  await expect(feed).toContainText("No overtakes or pit events reported");
  await page.getByRole("button", { name: "Show resolving" }).click();
  await expect(page.getByRole("status")).toContainText(
    "Resolution in progress",
  );
  await expect(
    page.getByRole("heading", {
      name: "Last published Classification",
      exact: true,
    }),
  ).toBeVisible();
  await expect(feed).toContainText("Waiting for published phase events");
});

test("failed supplied images preserve public identities and event information", async ({
  page,
}) => {
  await page.route("**/portraits/*.png", (route) =>
    route.fulfill({ status: 404, body: "missing" }),
  );
  await page.goto("/?view=results");
  await expect(
    page.getByRole("region", { name: "Result context" }),
  ).toContainText("Image unavailable");
  await expect(
    page.getByRole("region", { name: "Public phase events" }),
  ).toContainText("Lewis Hamilton passed Max Verstappen");
  await expect(
    page.getByRole("heading", { name: "Lewis Hamilton", exact: true }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Show race finish" }).click();
  await expect(
    page.getByRole("heading", { name: "Max Verstappen", exact: true }),
  ).toBeVisible();
  await expect(page.getByRole("status")).toContainText("Chequered flag");
  await expect(
    page.getByText(
      /tyre condition estimate|random draw|opponent decision|locked instruction/i,
    ),
  ).toHaveCount(0);
});

test("crowded shared-display events explain scrolling and keyboard reaches the last event", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1920, height: 1080 });
  await page.goto("/?view=results");
  await page.getByRole("button", { name: "Show crowded phase" }).click();
  await expect(
    page.getByText(
      "Scroll for all events · focus list and use arrow keys or End",
    ),
  ).toBeVisible();
  const list = page.getByRole("list", {
    name: "Chronological public event list",
  });
  await list.focus();
  await page.keyboard.press("End");
  await expect
    .poll(async () => {
      const last = await list.getByRole("article").last().boundingBox();
      const box = await list.boundingBox();
      return last!.y + last!.height - (box!.y + box!.height);
    })
    .toBeLessThanOrEqual(1);
});
