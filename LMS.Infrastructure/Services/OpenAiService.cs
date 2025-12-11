using LMS.Application.ServiceInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.Infrastructure.Services
{
    public class OpenAiService : IOpenAiService
    {
        private readonly ChatClient _chatClient;
        private readonly ILogger<OpenAiService> _logger;

        public OpenAiService(IConfiguration configuration, ILogger<OpenAiService> logger)
        {
            var apiKey = configuration.GetValue<string>("OpenAI:ApiKey")
                ?? throw new InvalidOperationException("OpenAI API key not found");

            var model = configuration.GetValue<string>("OpenAI:Model") ?? "gpt-4o-mini";

            _chatClient = new ChatClient(model, new ApiKeyCredential(apiKey));
            _logger = logger;
        }

        public async Task<string> AnalyzeQueryAsync(string userQuery, string context)
        {
            try
            {
                var systemPrompt = @"You are an AI assistant that helps analyze natural language queries about a library management system.
Given a user's question, determine what data they want to retrieve and respond with a structured JSON object.

Available query types:
1. USER_WITH_MOST_BOOKS - Find user who owns the most books
2. MOST_POPULAR_BOOK - Find most popular book (owned by most users)
3. EXPENSIVE_BOOKS - Find most expensive books
4. USER_BOOK_COUNT - Count books for a specific user
5. BOOKS_BY_GENRE - Find books by genre
6. BOOKS_BY_STATUS - Find books by reading status
7. USER_STATISTICS - Get statistics for a user
8. GENERAL_STATISTICS - Get overall library statistics

Respond ONLY with valid JSON in this format:
{
  ""queryType"": ""TYPE_NAME"",
  ""parameters"": {
    ""limit"": 5,
    ""genre"": ""value"",
    ""status"": ""value""
  }
}";

                var userPrompt = $@"Context: {context}

User Query: {userQuery}

Analyze this query and return the appropriate JSON response.";

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                };

                var completion = await _chatClient.CompleteChatAsync(messages);
                var response = completion.Value.Content[0].Text;

                _logger.LogInformation("OpenAI Analysis - Query: {Query}, Response: {Response}", userQuery, response);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing query with OpenAI");
                throw;
            }
        }

        public async Task<string> GenerateAnswerAsync(string query, string data)
        {
            try
            {
                var systemPrompt = @"You are a helpful assistant that generates human-readable answers from data.
Given a user's question and the retrieved data, create a clear, concise answer.
Keep the answer brief and natural, like you're talking to a friend.";

                var userPrompt = $@"User asked: {query}

Data retrieved: {data}

Generate a friendly, natural language answer.";

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                };

                var completion = await _chatClient.CompleteChatAsync(messages);
                var response = completion.Value.Content[0].Text;

                _logger.LogInformation(" Answer - Query: {Query}", query);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating answer ");
                throw;
            }
        }
    }
}
