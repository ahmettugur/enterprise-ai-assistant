using AI.Application.Common.Helpers;
using AI.Application.Configuration;
using AI.Application.DTOs.ReAct;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.Common;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AI.Application.UseCases;

/// <summary>
/// ReAct (Reasoning + Acting) pattern merkezi servis implementasyonu.
/// Tüm kullanıcı-facing akışlarda THOUGHT ve OBSERVATION adımlarını yönetir.
/// </summary>
public class ReActUseCase : IReActUseCase
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly Kernel _kernel;
    private readonly ISignalRHubContext _hubContext;
    private readonly ReActSettings _reactSettings;
    private readonly ILogger<ReActUseCase> _logger;

    public ReActUseCase(
        IChatCompletionService chatCompletionService,
        Kernel kernel,
        ISignalRHubContext hubContext,
        ReActSettings reactSettings,
        ILogger<ReActUseCase> logger)
    {
        _chatCompletionService = chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _reactSettings = reactSettings ?? throw new ArgumentNullException(nameof(reactSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsEnabled => _reactSettings.Enabled && _reactSettings.SendStepsToFrontend;

    /// <inheritdoc />
    public async Task SendThoughtAsync(string connectionId, string userPrompt, string flowContext)
    {
        if (!IsEnabled) return;

        try
        {
            var thought = await GetThoughtAsync(userPrompt, flowContext).ConfigureAwait(false);

            await SendReActStepAsync(connectionId, new ReActStep
            {
                StepNumber = 1,
                StepType = "thought",
                Content = thought
            }).ConfigureAwait(false);

            // Kullanıcının adımı görebilmesi için kısa gecikme
            // await Task.Delay(5000).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SendThoughtAsync sırasında hata oluştu - ConnectionId: {ConnectionId}", connectionId);
        }
    }

    /// <inheritdoc />
    public async Task SendObservationAsync(string connectionId, string observationMessage)
    {
        if (!IsEnabled) return;

        try
        {
            await SendReActStepAsync(connectionId, new ReActStep
            {
                StepNumber = 2,
                StepType = "observation",
                Content = observationMessage
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SendObservationAsync sırasında hata oluştu - ConnectionId: {ConnectionId}", connectionId);
        }
    }

    #region Private Methods

    /// <summary>
    /// ReAct THOUGHT adımı - Ayrı LLM çağrısı ile düşünce üretir
    /// </summary>
    private async Task<string> GetThoughtAsync(string userPrompt, string flowContext)
    {
        try
        {
            var thoughtPrompt = Helper.ReadFileContent("Common/Resources/Prompts", "react-thought.md");
            thoughtPrompt = thoughtPrompt
                .Replace("{{USER_PROMPT}}", userPrompt)
                .Replace("{{FLOW_CONTEXT}}", flowContext);

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(thoughtPrompt);
            chatHistory.AddUserMessage(userPrompt);

            var openAiSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.3F,
                MaxTokens = 150
            };

            var result = await _chatCompletionService.GetChatMessageContentsAsync(
                chatHistory, openAiSettings, _kernel).ConfigureAwait(false);

            var thought = result[0].ToString()?.Trim() ?? "Düşünce alınamadı.";
            _logger.LogInformation("ReAct THOUGHT: {Thought}", thought);
            return thought;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetThoughtAsync metodunda hata oluştu");
            return "İstek analiz ediliyor...";
        }
    }

    /// <summary>
    /// ReAct adımını frontend'e SignalR ile gönderir
    /// </summary>
    private async Task SendReActStepAsync(string connectionId, ReActStep step)
    {
        if (_reactSettings.VerboseLogging)
        {
            _logger.LogInformation("ReAct Step {StepNumber}: {StepType} - {Content}",
                step.StepNumber, step.StepType, step.Content);
        }

        await _hubContext.Clients.Client(connectionId)
            .SendAsync("ReceiveReActStep", step)
            .ConfigureAwait(false);
    }

    #endregion
}
