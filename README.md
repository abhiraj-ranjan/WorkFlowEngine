Configurable Workflow Engine (State-Machine API)
This project is a minimal ASP.NET Core backend service that implements a configurable workflow engine, built as a single-file application for simplicity and clarity.

How to Run
Prerequisites: You must have the .NET 8 SDK installed.

Clone the repository or place the source code in a directory.

Open a terminal and navigate into the project directory.

Run the application using the command:

dotnet run

The API will be available at the URL shown in the terminal (e.g., http://localhost:5054).

How to Test
You can test the running application using curl in a new terminal window.

(Note: Replace [your_port] with the actual port number from your terminal when you run the application, for example 5054)

1. Create a Workflow Definition
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

2. Start a Workflow Instance
curl -X POST "http://localhost:[your_port]/workflows/doc-approval/instances"

(This will return a unique instance ID. Copy it for the next step.)

3. Execute an Action
(Replace [INSTANCE_ID] with the ID you copied.)

curl -X POST "http://localhost:[your_port]/instances/[INSTANCE_ID]/actions/submit-for-review"

Assumptions & Design Decisions
In-Memory Persistence: As per the requirements, all workflow definitions and instances are stored in memory. All data will be lost when the application stops.

Single-File Project: The entire application (models, services, and endpoints) is contained within Program.cs for maximum readability and simplicity, avoiding the need for a complex project structure.

Minimal API: The project uses the .NET Minimal API style for concise endpoint definitions.

No Unit Tests: Unit tests were not included to stay within the suggested time frame, but in a production environment, the service logic would be thoroughly tested.
