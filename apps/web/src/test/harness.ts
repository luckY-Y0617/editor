type TestFunction = () => void | Promise<void>;

type TestCase = {
  name: string;
  run: TestFunction;
};

const tests: TestCase[] = [];
const suiteStack: string[] = [];

export function describe(name: string, defineSuite: () => void) {
  suiteStack.push(name);

  try {
    defineSuite();
  } finally {
    suiteStack.pop();
  }
}

export function test(name: string, run: TestFunction) {
  tests.push({
    name: [...suiteStack, name].join(" > "),
    run,
  });
}

export function expect<T>(actual: T) {
  return {
    toBe(expected: T) {
      if (!Object.is(actual, expected)) {
        fail(`Expected ${formatValue(actual)} to be ${formatValue(expected)}`);
      }
    },
    toEqual(expected: unknown) {
      if (!isEqual(actual, expected)) {
        fail(`Expected ${formatValue(actual)} to equal ${formatValue(expected)}`);
      }
    },
    toContain(expected: unknown) {
      if (!Array.isArray(actual) && typeof actual !== "string") {
        fail(`Expected ${formatValue(actual)} to support containment`);
      }

      if (!(actual as Array<unknown> | string).includes(expected as never)) {
        fail(`Expected ${formatValue(actual)} to contain ${formatValue(expected)}`);
      }
    },
    toBeGreaterThanOrEqual(expected: number) {
      if (typeof actual !== "number" || actual < expected) {
        fail(`Expected ${formatValue(actual)} to be >= ${expected}`);
      }
    },
    toBeLessThan(expected: number) {
      if (typeof actual !== "number" || actual >= expected) {
        fail(`Expected ${formatValue(actual)} to be < ${expected}`);
      }
    },
    toMatchObject(expected: Record<string, unknown>) {
      if (!isRecord(actual)) {
        fail(`Expected ${formatValue(actual)} to be an object`);
      }

      for (const [key, expectedValue] of Object.entries(expected)) {
        if (!isEqual((actual as Record<string, unknown>)[key], expectedValue)) {
          fail(`Expected property ${key} to equal ${formatValue(expectedValue)}`);
        }
      }
    },
  };
}

export async function runTests() {
  const failures: { name: string; error: unknown }[] = [];

  for (const testCase of tests) {
    try {
      await testCase.run();
      console.log(`ok ${testCase.name}`);
    } catch (error) {
      failures.push({ name: testCase.name, error });
      console.error(`not ok ${testCase.name}`);
      console.error(error);
    }
  }

  if (failures.length > 0) {
    throw new Error(`${failures.length} of ${tests.length} comment regression tests failed`);
  }

  console.log(`\n${tests.length} comment regression tests passed`);
}

function fail(message: string): never {
  throw new Error(message);
}

function isEqual(leftValue: unknown, rightValue: unknown): boolean {
  return JSON.stringify(leftValue) === JSON.stringify(rightValue);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function formatValue(value: unknown) {
  return typeof value === "string" ? `"${value}"` : JSON.stringify(value);
}
