import { test, expect } from "@playwright/test";
test("both cars retain selected choices in confirmation before submission", async ({
  page,
}) => {
  await page.goto("/?view=controller");
  const car = page.getByRole("region", { name: "Car 1 — Max Verstappen" });
  await car.getByRole("button", { name: "Push", exact: true }).click();
  await car.getByRole("button", { name: "Pit", exact: true }).click();
  await car.getByRole('button',{name:'New compound Hard',exact:true}).click();
  await page.getByRole("button", { name: "Approve Plan" }).click();
  const dialog = page.getByRole("dialog", { name: "Confirm both cars" });
  await expect(dialog).toContainText("Max Verstappen");
  await expect(dialog).toContainText("Push · Pit · Hard · Neutral");
  await expect(dialog).toContainText("Alexandra Montgomery-Sutherland");
  await expect(dialog).toContainText("Standard · Stay Out · Neutral");
  await expect(dialog.getByRole("button", { name: "Submit both cars" })).toBeEnabled();
  await dialog.getByRole("button", { name: "Submit both cars" }).click();
  await expect(page.getByRole("status")).toContainText("Submitted");
});

test("rejection preserves both drafts and permits correction while acknowledgement locks them", async ({
  page,
}) => {
  await page.goto("/?view=controller");
  const car = page.getByRole("region", { name: "Car 1 — Max Verstappen" });
  await car.getByRole("button", { name: "Push", exact: true }).click();
  await page.getByRole("button", { name: "Approve Plan" }).click();
  await page.getByRole("button", { name: "Submit both cars" }).click();
  await expect(
    car.getByRole("button", { name: "Push", exact: true }),
  ).toBeDisabled();
  await page.getByRole("button", { name: "Reject submission" }).click();
  await expect(page.getByRole("alert")).toContainText("Submission rejected");
  await expect(
    car.getByRole("button", { name: "Push", exact: true }),
  ).toHaveAttribute("aria-pressed", "true");
  await car.getByRole("button", { name: "Conserve", exact: true }).click();
  await page.getByRole("button", { name: "Approve Plan" }).click();
  await page.getByRole("button", { name: "Submit both cars" }).click();
  await page.getByRole("button", { name: "Acknowledge and lock" }).click();
  await expect(page.getByRole("status")).toContainText("Locked");
  await expect(
    page.getByRole("button", { name: "Approve Plan" }),
  ).toBeDisabled();
});

test("fixture availability retains known pending compound and disables unavailable actions", async ({
  page,
}) => {
  await page.goto("/?view=controller");
  const car2 = page.getByRole("region", {
    name: "Car 2 — Alexandra Montgomery-Sutherland",
  });
  await expect(
    car2.getByRole("button", { name: "Attack", exact: true }),
  ).toHaveCount(0);
  await page.getByRole("button", { name: "Show pending stop" }).click();
  await expect(car2).toContainText("Pit stop pending · Hard");
  await expect(car2.getByRole('button',{name:'New compound Hard',exact:true})).toHaveAttribute('aria-pressed','true');
  await expect(car2.getByRole("button",{name:"New compound Hard",exact:true})).toBeDisabled();
  for (const label of ["Show loading", "Show resolving"]) {
    await page.getByRole("button", { name: label }).click();
    await expect(
      page.getByRole("button", { name: "Approve Plan" }),
    ).toBeDisabled();
    await expect(page.getByRole("status")).toContainText(
      label === "Show loading" ? "Loading" : "resolving",
    );
  }
});

test("keyboard confirmation traps focus and returns to review on Escape", async ({
  page,
}) => {
  await page.goto("/?view=controller");
  const review = page.getByRole("button", { name: "Approve Plan" });
  await review.focus();
  await page.keyboard.press("Enter");
  await expect(
    page.getByRole("button", { name: "Keep editing" }),
  ).toBeFocused();
  await page.keyboard.press("Shift+Tab");
  await expect(
    page.getByRole("button", { name: "Submit both cars" }),
  ).toBeFocused();
  await page.keyboard.press("Tab");
  await expect(
    page.getByRole("button", { name: "Keep editing" }),
  ).toBeFocused();
  await page.keyboard.press("Escape");
  await expect(review).toBeFocused();
});
