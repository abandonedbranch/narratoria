# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`  
**Created**: [DATE]  
**Status**: Draft  
**Input**: User description: "$ARGUMENTS"


## Scope *(mandatory)*

### In Scope

- [Bullet list of behaviors/capabilities that ARE included]

### Out of Scope

- [Bullet list of behaviors/capabilities that are explicitly NOT included]

### Assumptions

- [Assumption that must hold true; if unknown, mark as NEEDS CLARIFICATION]

### Open Questions *(mandatory)*

- [NEEDS CLARIFICATION: unanswered requirement/question]

## User Scenarios & Testing *(mandatory)*

**Constitution note**: If the feature changes UI components, acceptance scenarios MUST be coverable via Playwright for .NET E2E tests in addition to any applicable unit tests.

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - [Brief Title] (Priority: P1)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently - e.g., "Can be fully tested by [specific action] and delivers [specific value]"]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]
2. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 2 - [Brief Title] (Priority: P2)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 3 - [Brief Title] (Priority: P3)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

[Add more user stories as needed, each with an assigned priority]

### Edge Cases

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right edge cases.
-->

- What happens when [boundary condition]?
- How does system handle [error scenario]?


## Interface Contract *(mandatory)*

List the externally observable surface area this feature introduces or changes. Avoid implementation details.

### New/Changed Public APIs

- [API/endpoint/command/query name] — [brief contract summary]

### Events / Messages *(if applicable)*

- [Event/message name] — [producer -> consumer(s)] — [intent]

### Data Contracts *(if applicable)*

- [DTO/record name] — [fields at a high level; omit types if unknown]

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

### Error Handling *(mandatory)*

- **EH-001**: System MUST [expected failure] and respond with [observable behavior]
- **EH-002**: System MUST log [what] at [level] including [context fields]

- **FR-001**: System MUST [specific capability, e.g., "allow users to create accounts"]
- **FR-002**: System MUST [specific capability, e.g., "validate email addresses"]  
- **FR-003**: Users MUST be able to [key interaction, e.g., "reset their password"]
- **FR-004**: System MUST [data requirement, e.g., "persist user preferences"]
- **FR-005**: System MUST [behavior, e.g., "log all security events"]

*Example of marking unclear requirements:*

- **FR-006**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]
- **FR-007**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]


### State & Data *(mandatory if feature involves data)*

- **Persistence**: [What is stored/updated; where conceptually]
- **Invariants**: [Rules that must always hold true]
- **Migration/Compatibility**: [Any required migrations, backfills, versioning, or backward-compat expectations]

### Key Entities *(include if feature involves data)*

- **[Entity 1]**: [What it represents, key attributes without implementation]
- **[Entity 2]**: [What it represents, relationships to other entities]


## Test Matrix *(mandatory)*

Map each requirement to the minimum required test coverage. If UI behavior changes, include Playwright E2E coverage.

| Requirement ID | Unit Tests | Integration Tests | E2E (Playwright) | Notes |
|---|---|---|---|---|
| FR-001 | [Y/N] | [Y/N] | [Y/N] | [What must be proven] |

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: [Measurable metric, e.g., "Users can complete account creation in under 2 minutes"]
- **SC-002**: [Measurable metric, e.g., "System handles 1000 concurrent users without degradation"]
- **SC-003**: [User satisfaction metric, e.g., "90% of users successfully complete primary task on first attempt"]
- **SC-004**: [Business metric, e.g., "Reduce support tickets related to [X] by 50%"]
