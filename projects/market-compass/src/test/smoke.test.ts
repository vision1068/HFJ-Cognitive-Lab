import { describe, it, expect } from "vitest";

// Trivial test proving the Vitest runner + jsdom environment are wired up.
describe("test runner", () => {
  it("runs and asserts", () => {
    expect(1 + 1).toBe(2);
  });

  it("has a jsdom document", () => {
    expect(typeof document).toBe("object");
  });
});
