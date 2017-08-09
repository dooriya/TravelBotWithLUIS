using System;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

// For more information about this template visit http://aka.ms/azurebots-csharp-luis
[Serializable]
public class BasicLuisDialog : LuisDialog<object>
{
    public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(Utils.GetAppSetting("LuisAppId"), Utils.GetAppSetting("LuisAPIKey"))))
    {
    }

    [LuisIntent("None")]
    public async Task NoneIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the none intent. You said: {result.Query}"); //
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
    
    [LuisIntent("GetWeather")]
    public async Task GetWeatherIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the GetWeather intent. You said: {result.Query}"); //
        context.Wait(MessageReceived);
    }
    
    [LuisIntent("HomeAutomation.TurnOn")]
    public async Task HomeAutomationTurnOnIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the HomeAutomation.TurnOn intent. You said: {result.Query}"); //
        context.Wait(MessageReceived);
    }

    [LuisIntent("HomeAutomation.TurnOff")]
    public async Task HomeAutomationTurnOffIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"You have reached the HomeAutomation.TurnOff intent. You said: {result.Query}"); //
        context.Wait(MessageReceived);
    }
}