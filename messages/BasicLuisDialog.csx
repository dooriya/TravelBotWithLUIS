#load "OpenWeatherResponse.csx"

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Newtonsoft.Json;

//using Iveonik.Stemmers;

// For more information about this template visit http://aka.ms/azurebots-csharp-luis
[LuisModel("9a002f0f-9a17-4f23-8c0c-fac8d0bf3f20", "c043256de3914185a74eea8fc16d0ef5", domain: "westus.api.cognitive.microsoft.com")]
[Serializable]
public class BasicLuisDialog : LuisDialog<object>
{
    /*
    public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(Utils.GetAppSetting("LuisAppId"), Utils.GetAppSetting("LuisAPIKey"))))
    {
    }
    */

    private static readonly string CurrentWeatherReplyTemplate = "It's {0} in {1} with temprature {2} deg c.";
    private static readonly string WeatherForecastReplyTemplate = "For {0}, it'll be {1} in {2}..with low temperature at {3} and high at {4}.";
    private static readonly string YesWeatherTemplate = "Hi..actually yes, it's {0} in {1}";
    private static readonly string NoWeatherTemplate = "Hi..actually no, it's {0} in {1}";

    private string deviceEntity;
    private string deviceLocation;
    private string deviceOperation;

    private static readonly string IoTHubConnectionString = "HostName=dol-iot-hub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=AQWXy4/JElSRMLngUJDUu+zYVDzJPU6EM/DXeR9FKCU=";
    private static readonly ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(IoTHubConnectionString);
    private static readonly string deviceId = "myDevice1";

    #region Intent Handler
    [LuisIntent("None")]
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


    [LuisIntent("greetings")]
    public async Task GreetingsIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"Hi, what can I do for you?");
        context.Wait(MessageReceived);
    }

    [LuisIntent("Weather.GetForecast")]
    public async Task GetWeatherForecastIntent(IDialogContext context, LuisResult result)
    {
        //await context.PostAsync($"Your intent: Weather.GetForecast.");

        string city;
        if (TryFindEntity(result, "Weather.Location", out city))
        {
            var weatherResponse = await this.GetCurrentWeatherByCityName(city);
            string replyMessage = string.Format(CurrentWeatherReplyTemplate,
                weatherResponse.Summary,
                weatherResponse.City,
                weatherResponse.Temp);

            //await context.PostAsync(replyMessage);
            await context.SayAsync(text: replyMessage, speak: replyMessage);

            SendCloudToDeviceMessageAsync(deviceId, $"GetWeather:{replyMessage}");
        }
        else
        {
            //await context.PostAsync("Sorry, no information!");
            await context.SayAsync(text: "Sorry, no information!", speak: "Sorry, no information!");
        }

        context.Wait(MessageReceived);
    }

    [LuisIntent("Weather.GetCondition")]
    public async Task GetWeatherConditionIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"Your intent: Weather.GetCondition.");

        string city;
        string condition;
        if (TryFindEntity(result, "Weather.Location", out city)
            && TryFindEntity(result, "Weather.Condition", out condition))
        {
            string replyMessage;
            /*
             * Handle condition words with stemmer
            EnglishStemmer enStemmer = new EnglishStemmer();
            condition = enStemmer.Stem(condition);
            condition.Remove(condition.Length - 1);
            */

            var weatherResponse = await this.GetCurrentWeatherByCityName(city);
            if (weatherResponse.Summary.IndexOf(condition, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                replyMessage = string.Format(YesWeatherTemplate, condition, weatherResponse.City);
            }
            else
            {
                replyMessage = string.Format(NoWeatherTemplate, weatherResponse.Summary, weatherResponse.City);
            }
            await context.PostAsync(replyMessage);
        }
        else
        {
            await context.PostAsync("Sorry, no information!");
        }

        context.Wait(MessageReceived);
    }

    [LuisIntent("RGBLed.SetColor")]
    public async Task SetRGBLedColorIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"Your intent: RGBLed.SetColor.");

        string color;
        this.TryFindEntity(result, "RGBLed.Color", out color);
        if (!string.IsNullOrEmpty(color))
        {
            SendCloudToDeviceMessageAsync(deviceId, $"SetColor:{color.ToLowerInvariant()}");
        }
        else
        {
            await context.PostAsync($"Cannot find the RGBLed.Color entity from your query.");
        }

        context.Wait(MessageReceived);
    }

    [LuisIntent("RGBLed.Off")]
    public async Task ClearRGBLedColorIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"Your intent: RGBLed.Off.");
        SendCloudToDeviceMessageAsync(deviceId, "SetColor:none");
        context.Wait(MessageReceived);
    }

    [LuisIntent("Sensor.GetStatus")]
    public async Task GetSensorStatusIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"Your intent: Sensor.GetStatus.");

        string sensorType;
        this.TryFindEntity(result, "SensorType", out sensorType);
        if (!string.IsNullOrEmpty(sensorType))
        {
            sensorType = sensorType.ToLowerInvariant();
            switch (sensorType)
            {
                case "temperature":
                case "humidity":
                    SendCloudToDeviceMessageAsync(deviceId, $"Sensor:HumidTemp");
                    break;
                case "pressure":
                    SendCloudToDeviceMessageAsync(deviceId, $"Sensor:Pressure");
                    break;
                case "motion":
                    SendCloudToDeviceMessageAsync(deviceId, $"Sensor:Motion");
                    break;
                case "magnetic":
                    SendCloudToDeviceMessageAsync(deviceId, $"Sensor:Motion");
                    break;
                default:
                    await context.PostAsync($"Not supported sensor type: {sensorType}");
                    break;
            }
        }
        else
        {
            await context.PostAsync($"Cannot find the SensorType entity from your query.");
        }

        context.Wait(MessageReceived);
    }

    [LuisIntent("HomeAutomation.TurnOn")]
    public async Task HomeAutomationTurnOnIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"Your intent: HomeAutomation.TurnOn.");

        this.deviceEntity = null;
        this.deviceLocation = null;
        this.deviceOperation = null;

        TryFindEntity(result, "HomeAutomation.Device", out this.deviceEntity);
        TryFindEntity(result, "HomeAutomation.Operation", out this.deviceOperation);
        TryFindEntity(result, "HomeAutomation.Room", out this.deviceLocation);
        await context.PostAsync($"device: {this.deviceEntity}, location: {this.deviceLocation}");

        if (string.IsNullOrEmpty(this.deviceEntity))
        {
            await context.PostAsync($"Did not find the object to be turned on.");
            context.Wait(MessageReceived);
        }
        else
        {
            /*
            if (string.IsNullOrEmpty(this.deviceLocation))
            {
                await context.PostAsync($"Did not find the device location.");
                PromptDialog.Text(context, OnDeviceHomeReply_TurnOn, "where is the device?");
            }
            */

            if (!string.IsNullOrEmpty(this.deviceLocation)
                && this.deviceLocation.ToLowerInvariant().Contains("devkit"))
            {
                SendCloudToDeviceMessageAsync(deviceId, result.Query);                
            }

            string replyMesage;
            if (!string.IsNullOrEmpty(this.deviceLocation))
            {
                replyMesage = $"Ok, turn on {this.deviceEntity} in {this.deviceLocation} successfully.";
            }
            else
            {
                replyMesage = $"Ok, turn on {this.deviceEntity} successfully.";
            }

            await context.PostAsync(replyMesage);
            context.Wait(MessageReceived);
        }
    }

    [LuisIntent("HomeAutomation.TurnOff")]
    public async Task HomeAutomationTurnOffIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"Your intent: HomeAutomation.TurnOff.");

        this.deviceEntity = null;
        this.deviceLocation = null;
        this.deviceOperation = null;

        TryFindEntity(result, "HomeAutomation.Device", out this.deviceEntity);
        TryFindEntity(result, "HomeAutomation.Operation", out this.deviceOperation);
        TryFindEntity(result, "HomeAutomation.Room", out this.deviceLocation);

        if (string.IsNullOrEmpty(this.deviceEntity))
        {
            await context.PostAsync($"Did not find the object to be turned off.");
            context.Wait(MessageReceived);
        }
        else
        {
            if (string.IsNullOrEmpty(this.deviceLocation))
            {
                await context.PostAsync($"Did not find the device location.");
                PromptDialog.Text(context, OnDeviceHomeReply_TurnOff, "where is the device?");
            }
            else
            {
                PromptDialog.Confirm(context, AfterConfirming_TurnOff, "Are you sure?", promptStyle: PromptStyle.Auto);
            }
        }
    }
    #endregion

    #region Action and Reply
    private async Task AfterConfirming_TurnOn(IDialogContext context, IAwaitable<bool> confirmation)
    {
        if (await confirmation)
        {
            await context.PostAsync($"Ok, turn on {this.deviceEntity} in {this.deviceLocation} successfully.");
        }
        else
        {
            await context.PostAsync($"Ok! We haven't turned on {this.deviceEntity}!");
        }

        context.Wait(MessageReceived);
    }

    private async Task AfterConfirming_TurnOff(IDialogContext context, IAwaitable<bool> confirmation)
    {
        if (await confirmation)
        {
            await context.PostAsync($"Ok, turn off {this.deviceEntity} in {this.deviceLocation} successfully.");
        }
        else
        {
            await context.PostAsync($"Ok! We haven't turned off {this.deviceEntity}!");
        }

        context.Wait(MessageReceived);
    }

    private async Task OnDeviceEntityReply(IDialogContext context, IAwaitable<string> result)
    {
        this.deviceEntity = await result;

        PromptDialog.Confirm(context, AfterConfirming_TurnOff, "Are you sure?", promptStyle: PromptStyle.Auto);
    }

    private async Task OnDeviceHomeReply_TurnOff(IDialogContext context, IAwaitable<string> result)
    {
        this.deviceLocation = await result;
        PromptDialog.Confirm(context, AfterConfirming_TurnOff, $"Are you sure to turn off {this.deviceEntity} in {this.deviceLocation}?", promptStyle: PromptStyle.Auto);
    }

    private async Task OnDeviceHomeReply_TurnOn(IDialogContext context, IAwaitable<string> result)
    {
        this.deviceLocation = await result;
        PromptDialog.Confirm(context, AfterConfirming_TurnOn, $"Are you sure to turn on {this.deviceEntity} in {this.deviceLocation}?", promptStyle: PromptStyle.Auto);
    }

    public async Task<GetCurrentWeatherResponse> GetCurrentWeatherByCityName(string city)
    {
        using (var client = new HttpClient())
        {
            try
            {
                client.BaseAddress = new Uri("http://api.openweathermap.org");
                HttpResponseMessage response = await client.GetAsync($"/data/2.5/weather?q={city}&units=metric&APPID=d01499238c7a7f3173938f86b7ad1fc8");
                response.EnsureSuccessStatusCode();

                var stringResult = await response.Content.ReadAsStringAsync();

                var rawWeather = JsonConvert.DeserializeObject<OpenWeatherResponse>(stringResult);
                string summary = string.Join(",", rawWeather.Weather.Select(x => x.Main));

                return new GetCurrentWeatherResponse
                {
                    Temp = rawWeather.Main.Temp,
                    Summary = string.Join(",", rawWeather.Weather.Select(x => x.Main)),
                    City = rawWeather.Name
                };
            }
            catch (HttpRequestException httpRequestException)
            {
                Console.WriteLine($"Error getting weather from OpenWeather: {httpRequestException.Message}");
                throw;
            }
        }
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
            entityValue = string.Empty;
            return false;
        }
    }
    #endregion

    #region Device Control
    private async void SendCloudToDeviceMessageAsync(string deviceName, string message)
    {
        var commandMessage = new Message(Encoding.ASCII.GetBytes(message));
        await serviceClient.SendAsync(deviceName, commandMessage);
    }
    #endregion
}
