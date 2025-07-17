using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

// --- Application Setup (This part sets up the web server) ---

var builder = WebApplication.CreateBuilder(args);

// This adds the Swagger UI, the website you use to test the API.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// This section configures the web server.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // This creates the UI at /swagger
}
app.UseHttpsRedirection();


// --- Data Storage (Using simple dictionaries to store data in memory) ---

// A dictionary to hold all the workflow "blueprints" (definitions).
// The "key" is the string ID of the definition (e.g., "doc-approval").
// The "value" is the WorkflowDefinition object itself.
var allWorkflowDefinitions = new Dictionary<string, WorkflowDefinition>();

// A dictionary to hold all the running workflow "instances".
// The "key" is the unique ID (a Guid) of the instance.
// The "value" is the WorkflowInstance object.
var allWorkflowInstances = new Dictionary<Guid, WorkflowInstance>();


// --- API Endpoints (The "front door" of your application) ---

// Endpoint to create a new workflow definition (a blueprint)
app.MapPost("/workflows", (WorkflowDefinitionDto dto) =>
{
    // Check if a definition with this ID already exists.
    if (allWorkflowDefinitions.ContainsKey(dto.Id))
    {
        return Results.BadRequest(new { message = $"Workflow definition with ID '{dto.Id}' already exists." });
    }

    // Check that the definition has exactly one starting state.
    int initialStates = 0;
    foreach (var state in dto.States)
    {
        if (state.IsInitial)
        {
            initialStates++;
        }
    }

    if (initialStates != 1)
    {
        return Results.BadRequest(new { message = "A workflow definition must have exactly one initial state." });
    }

    // If everything is okay, create the new definition and save it.
    var definition = new WorkflowDefinition();
    definition.Id = dto.Id;
    definition.States = dto.States;
    definition.Actions = dto.Actions;

    allWorkflowDefinitions.Add(definition.Id, definition);

    return Results.Created($"/workflows/{definition.Id}", definition);
});

// Endpoint to start a new workflow instance from a blueprint
app.MapPost("/workflows/{definitionId}/instances", (string definitionId) =>
{
    // Find the blueprint (definition) for this workflow.
    if (!allWorkflowDefinitions.ContainsKey(definitionId))
    {
        return Results.NotFound(new { message = $"Workflow definition with ID '{definitionId}' not found." });
    }
    var definition = allWorkflowDefinitions[definitionId];

    // Find the starting state from the blueprint.
    string initialStateId = "";
    foreach (var state in definition.States)
    {
        if (state.IsInitial)
        {
            initialStateId = state.Id;
            break; // Exit the loop once we find it.
        }
    }

    // Create a new instance and set its starting state.
    var instance = new WorkflowInstance();
    instance.DefinitionId = definitionId;
    instance.CurrentStateId = initialStateId;

    // Save the new instance to our dictionary.
    allWorkflowInstances.Add(instance.Id, instance);

    return Results.Created($"/instances/{instance.Id}", instance);
});

// Endpoint to execute an action on a running instance
app.MapPost("/instances/{instanceId}/actions/{actionId}", (Guid instanceId, string actionId) =>
{
    // 1. Find the running instance.
    if (!allWorkflowInstances.ContainsKey(instanceId))
    {
        return Results.BadRequest(new { message = $"Workflow instance with ID '{instanceId}' not found." });
    }
    var instance = allWorkflowInstances[instanceId];

    // 2. Find the blueprint (definition) for this instance.
    if (!allWorkflowDefinitions.ContainsKey(instance.DefinitionId))
    {
        return Results.Conflict(new { message = "Critical error: Could not find definition for this instance." });
    }
    var definition = allWorkflowDefinitions[instance.DefinitionId];

    // 3. Find the instance's current state from the blueprint.
    State? currentState = null; // The '?' means it can be null
    foreach (var state in definition.States)
    {
        if (state.Id == instance.CurrentStateId)
        {
            currentState = state;
            break;
        }
    }

    // 4. Check if the instance is already finished.
    if (currentState == null || currentState.IsFinal)
    {
        return Results.Conflict(new { message = "Cannot execute actions on an instance that is in a final state." });
    }

    // 5. Find the action the user wants to perform from the blueprint.
    Action? actionToPerform = null;
    foreach (var action in definition.Actions)
    {
        if (action.Id == actionId)
        {
            actionToPerform = action;
            break;
        }
    }

    // 6. Check if the action exists and is allowed from the current state.
    if (actionToPerform == null)
    {
        return Results.BadRequest(new { message = $"Action '{actionId}' not found in the workflow definition." });
    }
    if (!actionToPerform.FromStates.Contains(currentState.Id))
    {
        return Results.Conflict(new { message = $"Action '{actionId}' cannot be executed from the current state '{currentState.Id}'." });
    }

    // 7. If all checks pass, update the instance's state.
    instance.CurrentStateId = actionToPerform.ToState;
    instance.History.Add( (actionId, DateTime.UtcNow) );

    return Results.Ok(instance);
});

// Endpoint to get the details of a running instance
app.MapGet("/instances/{instanceId}", (Guid instanceId) =>
{
    if (allWorkflowInstances.ContainsKey(instanceId))
    {
        return Results.Ok(allWorkflowInstances[instanceId]);
    }
    return Results.NotFound();
});


// This command starts the web server.
app.Run();


// --- Core "Nouns" (Using simple classes) ---

// Represents a single step in a process (e.g., "Draft", "Approved").
public class State
{
    public string Id { get; set; } = "";   // visibility datatype name {get; set;} = default value
    public bool IsInitial { get; set; } = false;
    public bool IsFinal { get; set; } = false;
    public bool Enabled { get; set; } = true;
}

// Represents the action to move between states (e.g., "Submit for Review").
public class Action
{
    public string Id { get; set; } = "";
    public List<string> FromStates { get; set; } = new List<string>();
    public string ToState { get; set; } = "";
    public bool Enabled { get; set; } = true;   //enabled by default
}

// The "blueprint" for a process, containing all possible states and actions.
public class WorkflowDefinition
{
    public string Id { get; set; } = "";
    public List<State> States { get; set; } = new List<State>();    //default empty list of states
    public List<Action> Actions { get; set; } = new List<Action>();
}

// A single, live run of a workflow blueprint.
public class WorkflowInstance
{
    public Guid Id { get; } = Guid.NewGuid(); // Automatically get a new unique ID.
    public string DefinitionId { get; set; } = "";
    public string CurrentStateId { get; set; } = "";
    public List<(string ActionId, DateTime Timestamp)> History { get; } = new List<(string, DateTime)>();
}

// A helper class to receive the data when you create a new workflow.
public class WorkflowDefinitionDto
{
    public string Id { get; set; } = "";
    public List<State> States { get; set; } = new List<State>();
    public List<Action> Actions { get; set; } = new List<Action>();
}
