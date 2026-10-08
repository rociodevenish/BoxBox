import { test, expect } from "@playwright/test";
test("player can open and close driver details without losing their place", async ({
  page,
}) => {
  await page.goto("/");
  const open = page
    .getByRole("article", { name: "Max Verstappen — compact driver card" })
    .getByRole("button", {
      name: "View details for Max Verstappen",
      exact: true,
    });
  await open.click();
  const dialog = page.getByRole("dialog", {
    name: "Max Verstappen — driver details",
  });
  await expect(dialog).toBeVisible();
  await expect(
    dialog.getByText("Qualifying Pace", { exact: true }),
  ).toBeVisible();
  await dialog.getByRole("button", { name: "Close driver details" }).click();
  await expect(dialog).not.toBeVisible();
  await expect(open).toBeFocused();
});
test("failed portrait keeps driver identity and details usable", async ({
  page,
}) => {
  await page.route("**/portraits/max-verstappen.png", (route) =>
    route.fulfill({ status: 404, body: "Missing" }),
  );
  await page.goto("/");
  const card = page.getByRole("article", {
    name: "Max Verstappen — compact driver card",
  });
  await expect(
    card.getByText("Portrait unavailable", { exact: true }),
  ).toBeVisible();
  await expect(
    card.getByRole("heading", { name: "Verstappen", exact: true }),
  ).toBeVisible();
  await card
    .getByRole("button", { name: "View details for Max Verstappen" })
    .click();
  await expect(
    page.getByRole("dialog", { name: "Max Verstappen — driver details" }),
  ).toBeVisible();
});
test("unknown ratings and a long name remain distinct from real ratings", async ({
  page,
}) => {
  await page.goto("/");
  const card = page.getByRole("article", {
    name: "Alexandra Montgomery-Sutherland — compact driver card",
  });
  await expect(
    card.getByRole("heading", { name: "Montgomery-Sutherland", exact: true }),
  ).toBeVisible();
  await expect(card.getByText("Unknown", { exact: true })).toHaveCount(5);
  await expect(
    card.getByText("Portrait unavailable", { exact: true }),
  ).toBeVisible();
  await card
    .getByRole("button", {
      name: "View details for Alexandra Montgomery-Sutherland",
    })
    .click();
  await expect(
    page
      .getByRole("dialog", {
        name: "Alexandra Montgomery-Sutherland — driver details",
      })
      .getByText("Setup Feedback", { exact: true }),
  ).toBeVisible();
});

test("keyboard details stay modal and Escape returns focus to the initiating card", async ({
  page,
}) => {
  await page.goto("/");
  const open = page
    .getByRole("article", { name: "Max Verstappen — compact driver card" })
    .getByRole("button", { name: "View details for Max Verstappen" });
  await open.focus();
  await page.keyboard.press("Enter");
  const dialog = page.getByRole("dialog", {
    name: "Max Verstappen — driver details",
  });
  const close = dialog.getByRole("button", { name: "Close driver details" });
  await expect(close).toBeFocused();
  await page.keyboard.press("Tab");
  await expect(close).toBeFocused();
  await page.keyboard.press("Shift+Tab");
  await expect(close).toBeFocused();
  await page.keyboard.press("Escape");
  await expect(dialog).not.toBeVisible();
  await expect(open).toBeFocused();
});
test("touch users can open and close details on a phone", async ({
  browser,
}) => {
  const context = await browser.newContext({
    viewport: { width: 360, height: 800 },
    hasTouch: true,
  });
  const page = await context.newPage();
  await page.goto("http://127.0.0.1:4400");
  await page
    .getByRole("article", { name: "Lewis Hamilton — compact driver card" })
    .getByRole("button", { name: "View details for Lewis Hamilton" })
    .tap();
  const dialog = page.getByRole("dialog", {
    name: "Lewis Hamilton — driver details",
  });
  await expect(dialog).toBeVisible();
  await dialog.getByRole("button", { name: "Close driver details" }).tap();
  await expect(dialog).not.toBeVisible();
  await context.close();
});
