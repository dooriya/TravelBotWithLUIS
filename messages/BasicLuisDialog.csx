using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

// For more information about this template visit http://aka.ms/azurebots-csharp-luis
//[LuisModel("9a002f0f-9a17-4f23-8c0c-fac8d0bf3f20", "c043256de3914185a74eea8fc16d0ef5", domain: "westus.api.cognitive.microsoft.com")]
[Serializable]
public class BasicLuisDialog : LuisDialog<object>
{
    public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(Utils.GetAppSetting("LuisAppId"), Utils.GetAppSetting("LuisAPIKey"))))
    {
    }

    private string deviceEntity;

    #region Intent Handler
    [LuisIntent("")]
    public async Task NoneIntent(IDialogContext context, LuisResult result)
    {
        string message = $"Sorry I did not understand: " + string.Join(", ", result.Intents.Select(i => i.Intent));
        await context.PostAsync(message);

        context.Wait(MessageReceived);
    }

    // Go to https://luis.ai and create a new intent, then train/publish your luis app.
    // Finally replace "MyIntent" with the name of your newly created intent in the following handler
    [LuisIntent("BookFlight")]
    public async Task BookFlightIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the BookFlight intent. You said: {result.Query}"); //
        context.Wait(MessageReceived);
    }

    [LuisIntent("Weather.GetForecast")]
    public async Task GetWeatherForecastIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the Weather.GetForecast intent. You said: {result.Query}"); //
        context.Wait(MessageReceived);
    }

    [LuisIntent("Weather.GetCondition")]
    public async Task GetWeatherConditionIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the Weather.GetCondition intent. You said: {result.Query}"); //
        context.Wait(MessageReceived);
    }

    [LuisIntent("HomeAutomation.TurnOn")]
    public async Task HomeAutomationTurnOnIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the HomeAutomation.TurnOn intent. You said: {result.Query}");

        if (TryFindEntity(result, "HomeAutomation.Device", out this.deviceEntity))
        {
            PromptDialog.Confirm(context, AfterConfirming_TurnOn, "Are you sure?", promptStyle: PromptStyle.Auto);
        }
        else
        {
            await context.PostAsync($"Did not find the object to be turned on: {result.Query}");
            context.Wait(MessageReceived);
        }
    }

    [LuisIntent("HomeAutomation.TurnOff")]
    public async Task HomeAutomationTurnOffIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the HomeAutomation.TurnOff intent. You said: {result.Query}");

        if (TryFindEntity(result, "HomeAutomation.Device", out this.deviceEntity))
        {
            PromptDialog.Confirm(context, AfterConfirming_TurnOff, "Are you sure?", promptStyle: PromptStyle.Auto);
        }
        else
        {
            await context.PostAsync($"Did not find the object to be turned off: {result.Query}");
            context.Wait(MessageReceived);
        }
    }
    #endregion

    #region Action and Reply
    private async Task AfterConfirming_TurnOn(IDialogContext context, IAwaitable<bool> confirmation)
    {
        if (await confirmation)
        {
            await context.PostAsync($"Ok, turn on {this.deviceEntity} successfully.");
        }
        else
        {
            await context.PostAsync($"Ok! We haven't turned of {this.deviceEntity}!");
        }

        context.Wait(MessageReceived);
    }

    private async Task AfterConfirming_TurnOff(IDialogContext context, IAwaitable<bool> confirmation)
    {
        if (await confirmation)
        {
            await context.PostAsync($"Ok, turn off {this.deviceEntity} successfully.");
        }
        else
        {
            await context.PostAsync($"Ok! We haven't turned of {this.deviceEntity}!");
        }

        context.Wait(MessageReceived);
    }

    private bool TryFindEntity(LuisResult result, string entityType, out string entityValue)
    {
        EntityRecommendation entity;
        if (result.TryFindEntity(entityType, out entity))
        {
            entityValue = entity.Entity;
            return true;
        }
        else
        {
            entityValue = "default";
            return false;
        }
    }
    #endregion
}