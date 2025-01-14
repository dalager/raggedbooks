using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

#pragma warning disable SKEXP0050

namespace RaggedBooks.Core;

#pragma warning disable SKEXP0001

public class ChatService(Kernel kernel)
{
    public async Task<string> AskRaggedQuestion(string question, string[] contexts)
    {
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var chat = new ChatHistory(
            """
            You can use only the information provided in this chat to answer questions. If you don't know the answer, reply suggesting to refine the question.
            For example, if the user asks "What is the capital of France?" and in this chat there isn't information about France, you should reply something like "This information isn't available in the given context".
            Never answer to questions that are not related to this chat.
            You must answer in the same language of the user's question.
            """
        );

        var prompt = new StringBuilder(
            """
            Using the following information:

            """
        );
        foreach (var text in contexts)
        {
            prompt.AppendLine("---");
            prompt.Append(text);
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

        // Add question and answer to the chat history.
#pragma warning disable S125
        //await SetChatHistoryAsync(conversationId, question, answer.Content!);
#pragma warning restore S125

        return answer.Content!;
    }
}
