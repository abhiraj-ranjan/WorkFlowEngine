# ⚙️ Configurable Workflow Engine

A minimal but robust backend service built with .NET 8 that implements a configurable state-machine API. This project was created as a take-home exercise for the Infonetica Software Engineer Intern role.

---

## ✨ Features

-   **Define Workflows**: Dynamically create workflow blueprints with custom states and actions.
-   **Instantiate & Execute**: Start instances from any definition and execute actions to transition between states.
-   **Full Validation**: The engine enforces all state-machine rules, such as ensuring actions are valid from the current state and that workflows have exactly one initial state.
-   **Inspect State**: Retrieve the current state and a complete history of actions for any running instance.

---

## 🚀 Getting Started

### Prerequisites

-   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Installation & Running

1.  **Clone the repository:**
    ```bash
    git clone [https://github.com/abhiraj-ranjan/WorkFlowEngine.git](https://github.com/abhiraj-ranjan/WorkFlowEngine.git)
    ```
2.  **Navigate to the project directory:**
    ```bash
    cd WorkFlowEngine
    ```
3.  **Run the application:**
    ```bash
    dotnet run
    ```
    The API will now be running and listening on the port specified in the terminal (e.g., `http://localhost:5054`).

---

## 🛠️ API Usage

You can interact with the API using `curl` or any API client.

*(Note: Replace `[your_port]` in the examples below with the actual port number from your terminal.)*

### 1. Create a Workflow Definition

Send a `POST` request to `/workflows` to define a new workflow.

```bash
curl -X POST "http://localhost:[your_port]/workflows" \
-H "Content-Type: application/json" \
-d '{
  "id": "doc-approval",
  "states": [
    { "id": "draft", "isInitial": true },
    { "id": "in-review" },
    { "id": "approved", "isFinal": true },
    { "id": "rejected", "isFinal": true }
  ],
  "actions": [
    { "id": "submit-for-review", "fromStates": ["draft"], "toState": "in-review" },
    { "id": "approve", "fromStates": ["in-review"], "toState": "approved" },
    { "id": "reject", "fromStates": ["in-review"], "toState": "rejected" }
  ]
}'
```

### 2. Start a Workflow Instance

Send a `POST` request to `/workflows/{definitionId}/instances`.

```bash
curl -X POST "http://localhost:[your_port]/workflows/doc-approval/instances"
```
> **Response:** This will return a new instance with a unique ID. Copy this ID for the next steps.

### 3. Execute an Action

Send a `POST` request to `/instances/{instanceId}/actions/{actionId}`.

*(Replace `[INSTANCE_ID]` with the ID you copied.)*
```bash
curl -X POST "http://localhost:[your_port]/instances/[INSTANCE_ID]/actions/submit-for-review"
```
> **Response:** The instance will now be in the `in-review` state.

---

## 📝 Design & Assumptions

-   **In-Memory Persistence**: As required, all data is stored in-memory. Data is lost on application restart.
-   **Single-File Simplicity**: The entire application is contained within `Program.cs`. This was a pragmatic choice to maximize readability and focus on the core logic, avoiding the overhead of a more complex project structure for an exercise of this scope.
-   **Minimal API**: The project uses .NET Minimal APIs for clean and concise endpoint definitions.
-   **No Unit Tests**: To adhere to the suggested time frame, unit tests were not included. In a production scenario, the service logic would be thoroughly tested to ensure correctness.
