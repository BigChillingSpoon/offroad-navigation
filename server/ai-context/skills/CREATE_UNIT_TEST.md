# Skill: Create Unit Test

This skill defines the procedure for creating a new unit test for a class or method.

## Procedure

1.  **Identify Target**: The user specifies the class or method to be tested (e.g., `SegmentBuilder`).

2.  **Grill Me First**: Before writing code, I will state my understanding of the target's purpose, inputs, and expected outputs.
    - *Example*: "I will test the `SegmentBuilder`. My understanding is that it takes a `ProviderRoute` and slices it into `Segments`, correctly calculating off-road distance."

3.  **Seek Confirmation**: I will ask for confirmation before proceeding. I will only continue after you approve my understanding.

4.  **Adhere to DRY**: I will check if any logic within the method is duplicated from elsewhere or could be reused. If so, I will propose refactoring it into a shared method first.

5.  **Create Test File**: I will create a new test file in the `Offroad.Tests` project (e.g., `SegmentBuilderTests.cs`).

6.  **Write Test Cases**: I will write focused unit tests using the "Arrange, Act, Assert" pattern.
    - **Arrange**: Set up test data and mocks.
    - **Act**: Execute the method under test.
    - **Assert**: Verify the result is correct. I will assert against expected values and ensure methods do not return `null`.

7.  **Use Mocks**: I will use a mocking framework to isolate the class under test from its external dependencies.
