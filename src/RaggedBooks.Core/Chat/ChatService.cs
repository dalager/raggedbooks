using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

#pragma warning disable SKEXP0050

namespace RaggedBooks.Core.Chat;

#pragma warning disable SKEXP0001

public interface IChatService
{
    Task<string> AskRaggedQuestion(string question, string[] contexts);
}

public class ChatService(Kernel kernel) : IChatService
{
    public async Task<string> AskRaggedQuestion(string question, string[] contexts)
    {
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var chat = new ChatHistory(
            """
            You can use only the information provided in this chat to answer questions. If you don't know the answer, reply suggesting to refine the question.
            For example, if the user asks "What is the capital of France?" and in this chat there isn't information about France, you should reply something like "This information isn't available in the given context".
            You will be given chunks of text from different books to use as context.
            You must answer in the same language of the user's question.
            You will repond in markdown.

            """
        );

        var prompt = new StringBuilder(
            """
            Using the following information:
            =====

            """
        );
        foreach (var text in contexts)
        {
            prompt.AppendLine("---");
            prompt.AppendLine(text);
        }

        prompt.AppendLine(
            $"""

=====
Answer the following question:
---
{question}
"""
        );

        chat.AddUserMessage(prompt.ToString());
        var answer = await chatCompletionService.GetChatMessageContentAsync(chat)!;

        return answer.Content!;
    }
}
