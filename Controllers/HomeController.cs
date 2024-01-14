using Azure;
using Azure.Communication.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using SendGrid;
using SendGrid.Helpers.Mail;
using SmartCarWebApp.Models;
using SmartCarWebApp.Models.ViewModels;
using SmartCarWebApp.Services;
using SmartCarWebApp.Shared;
using System.Diagnostics;

namespace SmartCarWebApp.Controllers;

public class HomeController : Controller
{
    #region Properties
    private ILogger<HomeController> Logger { get; init; } = default!;
    private IFeatureManager FeatureManager { get; init; } = default!;
    private SmartCarService Service { get; init; } = default!;
    private Settings Settings { get; init; } = default!;
    #endregion

    public HomeController(ILogger<HomeController> logger, SmartCarService service, 
        IFeatureManager featureManager, IOptionsSnapshot<Settings> options)
    {
        this.Logger = logger;
        this.Settings = options.Value;
        this.FeatureManager = featureManager;
        this.Service = service;

        this.Logger.LogTraceExt("HomeController initialized.");
    }

    /// <summary>
    /// This action method is invoked from the SmartCar service after the
    /// Connect flow is completed. If successful, a code is provided which
    /// is then exchanged for a token to be used for accessing SmartCar APIs.
    /// </summary>
    /// <param name="code">Authorization code.</param>
    /// <param name="state">[Unused]</param>
    /// <param name="error">Error info from SmartCar.</param>
    /// <param name="error_description">Error info from SmartCar.</param>
    /// <param name="vin">Error info from SmartCar (for incompatible vehicle).</param>
    /// <param name="make">Error info from SmartCar (for incompatible vehicle).</param>
    /// <param name="model">Error info from SmartCar (for incompatible vehicle).</param>
    /// <param name="year">Error info from SmartCar (for incompatible vehicle).</param>
    /// <returns>Redirect to "/Vehicles" if no errors, to "/" otherwise.</returns>
    [HttpGet]
    public async Task<IActionResult> Callback(string? code, string? state, string? error, string? error_description,
        string? vin, string? make, string? model, string? year)
    {
        // (keep this comment - hard to find this on SmartCar site)
        // see this page on how to test a few errors:
        // https://smartcar.com/docs/errors/testing-errors

        this.Logger.LogTraceExt(
            $"Callback() - Code: {code}, State: {state}, Error: {error}, Error Description: {error_description}, VIN: {vin}, Make: {make}, Model: {model}, Year: {year}");

        if (string.IsNullOrEmpty(code))
        {
            var errorMessage = $"Error while connecting:<br>{error}<br>{error_description}";
            this.Logger.LogErrorExt(errorMessage);
            this.TempData.ErrorResult(errorMessage);
            return Redirect("/");
        }

        try
        {
            await this.Service.TokenExchange(code, this.Settings.CallbackUri);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error while connecting to SmartCar.";
            this.Logger.LogErrorExt(ex, errorMessage);
            this.TempData.ErrorResult(errorMessage);
            return Redirect("/");
        }

        await this.SendEmail("SmartCar Test App", "SmartCar callback successfully completed.");

        return this.Redirect("/Vehicles");
    }

    /// <summary>
    /// Called via AJAX to refresh data for a single vehicle.
    /// Also used for attempts to lock or unlock vehicle, after
    /// which the data will be refresehed.
    /// </summary>
    /// <param name="id"><Vehicle ID./param>
    /// <param name="vehicleIndex">Index value of vehicle on page, zero-based.</param>
    /// <param name="lockOrUnlock">"LOCK" or "UNLOCK".</param>
    /// <returns>Partial view using "_VehicleSingle".</returns>
    [HttpGet]
    [Route("Vehicle")]
    public async Task<IActionResult> Vehicle(string id, string? vehicleIndex, string? lockOrUnlock)
    {
        this.Logger.LogTraceExt("Vehicle() HttpGet called.");

        // ajax call - from Refresh or Lock

        // clear any pre-existing error message
        this.TempData.ErrorResult("");

        if (string.IsNullOrEmpty(id))
        {
            // should not happen
            this.Logger.LogCriticalExt("Vehicle() - method called without required ID.");
            return PartialView("_VehicleSingle", new Vehicle());
        }

        this.Logger.LogInformationExt($"Vehicl() - id: {id}");

        if (string.IsNullOrEmpty(lockOrUnlock) == false)
        {
            bool lockVehicle = true;
            if (lockOrUnlock.ToLower() == "unlock")
            {
                lockVehicle = false;
            }

            try
            {
                await this.Service.LockOrUnlock(id, lockVehicle);
            }
            catch (LockOrUnlockException vehicleListEx)
            {
                var errorMessage = "Problem with attempting to Lock or Unlock vehicle. More info: " + vehicleListEx.Message;
                this.Logger.LogErrorExt(errorMessage);
                this.TempData.ErrorResult(errorMessage + ". Status information may be incorrect or incomplete.");
            }
            catch (Exception ex)
            {
                var errorMessage = "Error while attempting to Lock or Unlock vehicle.";
                this.Logger.LogErrorExt(ex, errorMessage);
                this.TempData.ErrorResult(errorMessage + " Status information may be incorrect or incomplete.");
            }
        }

        int vehicleIndexInteger = 0;
        if (string.IsNullOrEmpty(vehicleIndex) == false)
        {
            if (int.TryParse(vehicleIndex, out vehicleIndexInteger) == false)
            {
                this.Logger.LogCriticalExt($"Vehicle() - unable to parse expected integer value for vehicle index: {vehicleIndex}.");
            }
        }
        base.ViewData["index"] = vehicleIndexInteger;

        var viewModelPartial = await this.PopulateModel(id);

        return PartialView("_VehicleSingle", viewModelPartial);
    }

    /// <summary>
    /// This gets called as a Redirect from Callback() after token exchange is complete.
    /// Service is called to get a list of vehicles, then information and status for each one.
    /// </summary>
    /// <returns>View using "Vehicles".</returns>
    [HttpGet]
    [Route("Vehicles")]
    public async Task<IActionResult> VehicleList()
    {
        this.Logger.LogTraceExt("VehicleList() HttpGet called.");
        var ipv4Address = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        if (string.IsNullOrEmpty(ipv4Address) == false)
        {
            this.Logger.LogInformationExt($"Home Controller->Index() - Calling IPv4: {ipv4Address}");
        }

        // clear any pre-existing error message
        this.TempData.NoVehicles("");

        // init model with list of vehicles
        VehicleList vehicleList = new VehicleList();
        var viewModel = new List<Vehicle>();

        try
        {
            vehicleList = await this.Service.GetVehicleList();
        }
        catch (VehicleListException vehicleListEx)
        {
            var errorMessage = "Problem with retrieving list of vehicles. More info: " + vehicleListEx.Message;
            this.Logger.LogErrorExt(errorMessage);
            this.TempData.NoVehicles(errorMessage);
            return View("Vehicles", viewModel);
        }
        catch(Exception ex)
        {
            var errorMessage = "Problem with retrieving list of vehicles.";
            this.Logger.LogErrorExt(ex, "Unexpected error in service.");
            this.TempData.NoVehicles(errorMessage);
            return View("Vehicles", viewModel);
        }

        if (vehicleList.VehicleIDs.Count == 0)
        {
            this.TempData.NoVehicles("true");
        }
        else
        {
            foreach (string vehicleID in vehicleList.VehicleIDs)
            {
                viewModel.Add(await this.PopulateModel(vehicleID));
            }
        }

        return View("Vehicles", viewModel);
    }

    /// <summary>
    /// This is the Home Page (entry point), which allows a user to initiate 
    /// a Connect flow with SmartCar. As the HttpGet method, nothing is needed
    /// here other than returning the view.
    /// </summary>
    /// <returns>Default view.</returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        this.Logger.LogTraceExt("Index() HttpGet called.");
        var ipv4Address = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        if (string.IsNullOrEmpty(ipv4Address) == false)
        {
            this.Logger.LogInformationExt($"Home Controller->Index() - Calling IPv4: {ipv4Address}");
        }

        await this.SendEmail("SmartCar Test App", $"Home Page accessed Calling IPv4: {ipv4Address}.");
        return View();
    }

    /// <summary>
    /// This HttpPost method is called when the user initiates a
    /// Connect flow with SmartCar. A URL is retrieved from the service,
    /// which is then used for redirection.
    /// </summary>
    /// <param name="model">Viewmodel used to get TestMode checkbox value.</param>
    /// <returns>Redirect to URL for Connect flow.</returns>
    [HttpPost]
    public async Task<IActionResult> Index(HomeViewModel model)
    {
        // todo - remove js function from _layout if not needed after DB and Identity are implemented.
        this.Logger.LogTraceExt("Index() HttpPost called.");

        string url;
        if (await this.FeatureManager.IsEnabledAsync(FeatureFlags.EnableLiveMode))
        {
            this.Logger.LogInformationExt($"Index() HttpPost - live mode enabled, test mode value is {model.TestMode}.");
            url = this.Service.GetConnectUrl(model.TestMode, this.Settings.CallbackUri);
        }
        else
        {
            // when live mode not enabled, always use Test Mode
            url = this.Service.GetConnectUrl(true, this.Settings.CallbackUri);
        }

        return Redirect(url);
    }

    /// <summary>
    /// Privacy Statement page.
    /// </summary>
    /// <returns>Default view.</returns>
    [HttpGet]
    public IActionResult Privacy()
    {
        this.Logger.LogTraceExt("Privacy() HttpGet called.");
        return View();
    }

    /// <summary>
    /// Error page.
    /// </summary>
    /// <returns>Default view.</returns>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        this.Logger.LogErrorExt("Error() HttpGet called.");

        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    #region Helper Methods
    /// <summary>
    /// Uses SendGrid API to send an email using configuration data.
    /// </summary>
    /// <param name="subject">Subject of the email.</param>
    /// <param name="message">Body of the email.</param>
    private async Task SendEmail(string subject, string message)
    {
        if (await this.FeatureManager.IsEnabledAsync(FeatureFlags.SendEmails))
        {
            this.Logger.LogTraceExt("SendEmail() called, feature enabled.");

            if (this.Settings.Email.SendToAddress != null)
            {
                #region Azure Email Service
                var emailConnectionString = this.Settings.Email.AzureEmailConnectionString;
                var emailClient = new EmailClient(emailConnectionString);

                try
                {
                    EmailSendOperation emailSendOperation = await emailClient.SendAsync(
                        WaitUntil.Started,
                        senderAddress: "DoNotReply@zuras.com",
                        recipientAddress: this.Settings.Email.SendToAddress,
                        subject: this.Settings.Email.Subject,
                        htmlContent: message,
                        plainTextContent: message);
                    this.Logger.LogTraceExt($"Azure email sent.");
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarningExt(ex, "Exception attempting to send email with Azure.");
                }
                #endregion

                #region SendGrid Code
                /*
                var apiKey = this.Settings.Email.SendGridKey;
                var client = new SendGridClient(apiKey);
                var msg = new SendGridMessage()
                {
                    From = new EmailAddress(this.Settings.Email.FromAddress, this.Settings.Email.FromName),
                    Subject = this.Settings.Email.Subject,
                    PlainTextContent = message
                };
                msg.AddTo(new EmailAddress(this.Settings.Email.SendToAddress, this.Settings.Email.SendToName));

                try
                {
                    var response = await client.SendEmailAsync(msg);
                    this.Logger.LogInformationExt($"SendGrid email sent - status code: {response.StatusCode}.");
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarningExt(ex, "Exception attempting to send email with SendGrid.");
                }
                */
                #endregion
            }
        }
        else
        {
            this.Logger.LogTraceExt("SendEmail() called, feature disabled.");
        }
    }

    /// <summary>
    /// Creates and populates a Vehicle after retrieving data from Service.
    /// </summary>
    /// <param name="vehicleID">Vehicle ID.</param>
    /// <returns>Vehicle object.</returns>
    private async Task<Vehicle> PopulateModel(string vehicleID, bool testing = false)
    {
        var viewModel = new Vehicle();
        var vehicleInfo = new VehicleInfo();

        try
        {
            vehicleInfo = await this.Service.GetVehicleInfo(vehicleID);
        }
        catch (VehicleInfoException ex)
        {
            var errorMessage = "Error while retrieving vehicle information and status. More info: " + ex.Message;
            this.Logger.LogErrorExt(ex, errorMessage);
            this.TempData.ErrorResult(errorMessage + ". Status information may be incorrect or incomplete.");
            return viewModel;
        }
        catch (Exception ex)
        {
            var errorMessage = "Error while retrieving vehicle information and status.";
            this.Logger.LogErrorExt(ex, "Unexpected error in service.");
            this.TempData.ErrorResult(errorMessage + " Status information may be incorrect or incomplete.");
            return viewModel;
        }

        viewModel.VehicleInfo.ID = vehicleInfo.ID;
        viewModel.VehicleInfo.Make = vehicleInfo.Make;
        viewModel.VehicleInfo.Model = vehicleInfo.Model;
        viewModel.VehicleInfo.Year = vehicleInfo.Year;

        try
        {
            var lockStatus = await this.Service.GetLockStatus(vehicleID);
            viewModel.Security = lockStatus;
            viewModel.SetStatus(lockStatus);
        }
        catch (GetLockStatusException ex)
        {
            var errorMessage = "Error while retrieving vehicle status. More info: " + ex.Message;
            this.Logger.LogErrorExt(ex, errorMessage);
            this.TempData.ErrorResult(errorMessage + ". Status information may be incorrect or incomplete.");
        }
        catch (Exception ex)
        {
            var errorMessage = "Error while retrieving vehicle status.";
            this.Logger.LogErrorExt(ex, "Unexpected error in service.");
            this.TempData.ErrorResult(errorMessage + " Status information may be incorrect or incomplete.");
        }

        return viewModel;
    }
    #endregion 
}
