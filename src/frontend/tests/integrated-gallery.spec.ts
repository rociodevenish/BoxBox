import { test, expect } from "@playwright/test";
test("initial race is distinguishable from completed phase and has no invented event history", async ({
  page,
}) => {
  await page.goto("/?view=results");
  await page.getByRole("button", { name: "Show initial race" }).click();
  await expect(
    page.getByRole("heading", { name: "Race ready", exact: true }),
  ).toBeVisible();
  await expect(page.getByRole("status")).toContainText(
    "Waiting for first decisions",
  );
  await expect(
    page.getByRole("heading", { name: "Initial Classification", exact: true }),
  ).toBeVisible();
  await expect(page.getByText("LAP 0 / 40", { exact: true })).toBeVisible();
  await expect(
    page.getByRole("region", { name: "Public race events" }),
  ).toContainText("No race events published yet");
  await expect(page.getByRole("article")).toHaveCount(0);
});

test("rejected submission announces a recovery alert with retained selected choices", async ({
  page,
}) => {
  await page.goto("/?view=controller");
  await page.getByRole("button", { name: "Approve Plan" }).click();
  await page.getByRole("button", { name: "Submit both cars" }).click();
  await page.getByRole("button", { name: "Reject submission" }).click();
  await expect(page.getByRole("alert")).toContainText(
    "Review or change your retained choices",
  );
  await expect(
    page
      .getByRole("region", { name: "Car 1 — Max Verstappen" })
      .getByRole("button", { name: "Standard", exact: true }),
  ).toHaveAttribute("aria-pressed", "true");
});

test("keyboard compound control has the same visible focus baseline as primary buttons", async ({
  page,
}) => {
  await page.goto("/?view=controller");
  const car = page.getByRole("region", { name: "Car 1 — Max Verstappen" });
  await car.getByRole("button", { name: "Pit", exact: true }).click();
  const compound = car.getByRole("button",{name:"New compound Hard",exact:true});
  await page.keyboard.press("Tab");await compound.focus();
  await expect(compound).toBeFocused();
  const focus = await compound.evaluate((element) => ({
    width: parseFloat(getComputedStyle(element).outlineWidth),
    style: getComputedStyle(element).outlineStyle,
  }));
  expect(focus.width).toBeGreaterThanOrEqual(3);
  expect(focus.style).not.toBe("none");
  await page.keyboard.press("Space");
  await expect(compound).toHaveAttribute('aria-pressed','true');
});
