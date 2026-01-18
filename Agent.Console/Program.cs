using Agent.Console;
using Agent.Console.Models;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenAI.Chat;
using Shared;
using System.ClientModel;
using System.Text;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;


var endpoint = "";
var apiKey = "";
var modelDeploymentName = "";
var embeddingDeploymentName = "";

var listingDataFile = "property_listings.json";

var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
string jsonData = await File.ReadAllTextAsync(listingDataFile);
var propertyListings = JsonSerializer.Deserialize<IEnumerable<PropertyDetail>>(jsonData);

var embeddingGenerator = client.GetEmbeddingClient(embeddingDeploymentName).AsIEmbeddingGenerator();
InMemoryVectorStore vectorStore = new(new InMemoryVectorStoreOptions { EmbeddingGenerator = embeddingGenerator });

InMemoryCollection<Guid, PropertyVectorRecord> collection = vectorStore.GetCollection<Guid, PropertyVectorRecord>("property");
await collection.EnsureCollectionExistsAsync();

#region other db vector stores
//Microsoft.SemanticKernel.Connectors.AzureAISearch.AzureAISearchVectorStore vectorStoreFromAzureAiSearch = new AzureAISearchVectorStore(
//    new SearchIndexClient(new Uri("azureAiSearchEndpoint"),
//        new AzureKeyCredential("azureAiSearchKey")
//    ));

//Microsoft.SemanticKernel.Connectors.SqlServer.SqlServerVectorStore vectorStoreFromSqlServer2025 = new SqlServerVectorStore(
//    "connectionString");

//Microsoft.SemanticKernel.Connectors.CosmosNoSql.CosmosNoSqlVectorStore vectorStoreFromCosmosDb = new CosmosNoSqlVectorStore(
//    "connectionString",
//    "databaseName",
//    new CosmosClientOptions
//    {
//        UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default,
//    });
#endregion

foreach (var property in propertyListings)
{
    await collection.UpsertAsync(new PropertyVectorRecord
    {
        Id = Guid.NewGuid(),
        Address = property.Address,
        AgentName = property.AgentName,
        Description = property.Description,
        NearbySchools = property.NearbySchools,
        Rooms = property.Rooms,
        Status = property.Status,
        Title = property.Title
    });
}


var orchestratorAgent = client.GetChatClient(modelDeploymentName)
    .CreateAIAgent(name: "OrchestratorAgent", instructions:
        "You are a coordinator. " +
        "1. For property searches, use DataBaseAgent. " +
        "2. If the user asks to translate the results or 'translate that', hand off to TranslatorAgent. " +
        "3. Always summarize and present the final output from any agent to the user.");

SearchTool searchTool = new(collection);

var dataBaseAgent = client.GetChatClient(modelDeploymentName).CreateAIAgent(
    name: "DataBaseAgent",
    instructions: "You are a retrieval specialist. " +
                  "Use the SearchVectorStore tool for any property inquiry. " +
                  "If the tool returns 'NO_PROPERTIES_FOUND' or an empty list, " +
                  "tell the Orchestrator exactly this: 'No property listings were found for this query.'",
    tools: [AIFunctionFactory.Create(searchTool.SearchVectorStore)])
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();


var translatorAgent = client.GetChatClient(modelDeploymentName)
    .CreateAIAgent(name: "TranslatorAgent", instructions:
        "You are a translation assistant. " +
        "Look at the conversation history. If the user asks to translate the listings or 'translate that', " +
        "find the property listings provided by the DataBaseAgent earlier in the chat and translate them into Spanish. " +
        "Provide only the translated text.");


List<ChatMessage> messages = [];

while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine();
    messages.Add(new(ChatRole.User, input));    
    var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(orchestratorAgent)
        .WithHandoffs(orchestratorAgent, [dataBaseAgent, translatorAgent])
        .WithHandoffs([dataBaseAgent, translatorAgent], orchestratorAgent)
        .Build();

    var responseMessages = await RunWorkflowAsync(workflow, messages);
    messages.AddRange(responseMessages);
}

static async Task<List<ChatMessage>> RunWorkflowAsync(Workflow workflow, List<ChatMessage> messages)
{
    string lastExecutorId = null;

    StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);
    await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
    await foreach (WorkflowEvent @event in run.WatchStreamAsync())
    {
        switch (@event)
        {
            case AgentRunUpdateEvent e:
                {
                    if (e.ExecutorId != lastExecutorId)
                    {
                        lastExecutorId = e.ExecutorId;
                        Utils.WriteLineGreen(e.Update.AuthorName ?? e.ExecutorId);
                    }

                    Console.Write(e.Update.Text);
                    if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                    {
                        Utils.WriteLineDarkGray($"Call '{call.Name}' with arguments: {JsonSerializer.Serialize(call.Arguments)}]");
                    }

                    break;
                }
            case WorkflowOutputEvent output:
                Utils.Separator();
                return output.As<List<ChatMessage>>()!;
            case ExecutorFailedEvent failedEvent:
                if (failedEvent.Data is Exception ex)
                {
                    Utils.WriteLineRed($"Error in agent {failedEvent.ExecutorId}: " + ex);
                }

                break;
        }
    }

    return [];
}

async ValueTask<object> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
{
    StringBuilder functionCallDetails = new();
    functionCallDetails.Append($"- Tool Call: '{context.Function.Name}'");
    if (context.Arguments.Count > 0)
    {
        functionCallDetails.Append($" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))}");
    }

    Utils.WriteLineDarkGray(functionCallDetails.ToString());

    return await next(context, cancellationToken);
}

class SearchTool(InMemoryCollection<Guid, PropertyVectorRecord> collection)
{
    public async Task<string> SearchVectorStore(string question)
    {
        List<string> result = [];
        await foreach (VectorSearchResult<PropertyVectorRecord> searchResult in collection.SearchAsync(question, 10,
                           new VectorSearchOptions<PropertyVectorRecord>
                           {
                               IncludeVectors = false
                           }))
        {
            PropertyVectorRecord record = searchResult.Record;
            result.Add(record.SearchDocument);
        }

        if (result.Count == 0)
        {
            return "NO_PROPERTIES_FOUND"; // Return a specific keyword
        }

        return string.Join("\n", result); ;
    }
}