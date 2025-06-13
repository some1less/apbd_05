using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DeviceManager.Applicatio;
using DeviceManager.Application;
using DeviceManager.DTOs;
using DeviceManager.DTOs.embeddeddevice;
using DeviceManager.DTOs.personalcomputer;
using DeviceManager.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DeviceDatabase");
builder.Services.AddSingleton<IDeviceService, DeviceService>(DeviceService => new DeviceService(new DeviceRepository(connectionString)));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("api/devices", (IDeviceService deviceService) =>
{
    try
    {
        var result = deviceService.GetAllDevices();
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Max is a cool guy!");
        return Results.BadRequest(ex.Message);
    }
});

app.MapGet("api/devices/{id}", (IDeviceService deviceService, string id) =>
{
    try
    {
        var result = deviceService.GetDeviceById(id);
        if (result == null)
            return Results.NotFound($"Device with id = {id} not found");
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapPost("api/devices", async (HttpRequest request, IDeviceService deviceService) =>
{
    string? contentType = request.ContentType?.ToLower();
        
    switch (contentType)
    {
        case "application/json":
        {
            using var reader = new StreamReader(request.Body);
            string rawJson = await reader.ReadToEndAsync();
            
            var json = JsonNode.Parse(rawJson);
            
            var deviceType = json["deviceType"];

            if (deviceType == null)
            {
                return Results.BadRequest("You have to provide a device type.");

            }
            
            if (deviceType.ToString() == "smartwatch")
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                
                // if we get a plain of text smthg like: name, isTurnedOn, deviceType 
                // it will be converted to the obj of Smartwatch 
                // sounds smart
                
                /*{
                    "Name": "hello",
                    "IsTurnedOn": true,
                    "deviceType": "smartwatch",
                    "BatteryLevel": 80
                }*/
                Smartwatch? smartwatch;
                try
                {
                    smartwatch = JsonSerializer.Deserialize<Smartwatch>(json.ToString(), options);
                }
                catch (JsonException)
                {
                    return Results.BadRequest("Invalid JSON.");
                }

                if (smartwatch == null)
                {
                    return Results.BadRequest("Invalid JSON.");
                }

                if (smartwatch.BatteryLevel is < 0 or > 100)
                {
                    return Results.BadRequest("Battery level must be between 0 and 100.");
                }

                var swDto = new SmartwatchDto
                {
                    Id = smartwatch.Id,
                    Name = smartwatch.Name,
                    IsTurnedOn = smartwatch.IsTurnedOn,
                    BatteryLevel = smartwatch.BatteryLevel,
                };
                
                swDto = deviceService.AddSmartwatch(swDto);
                
                return Results.Created($"SmartWatch created successfully!", swDto);
            }
            if (deviceType.ToString() == "personal computer")
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                
                /*{
                    "Name": "hello",
                    "IsTurnedOn": true,
                    "deviceType": "personal computer",
                    "OperationSystem": "Windows"
                }*/
                PersonalComputer? pc;
                try
                {
                    pc = json.Deserialize<PersonalComputer>(options);
                }
                catch (JsonException)
                {
                    return Results.BadRequest("Invalid JSON.");
                }

                if (pc == null)
                {
                    return Results.BadRequest("Invalid JSON.");
                }

                if (pc.IsTurnedOn && pc.OperationSystem.IsNullOrEmpty())
                {
                    return Results.BadRequest("Personal Computer has to have operating system to be turned on");
                }

                var pcDto = new PersonalComputerDto
                {
                    Id = pc.Id,
                    Name = pc.Name,
                    IsTurnedOn = pc.IsTurnedOn,
                    OperationSystem = pc.OperationSystem
                };
                
                pcDto = deviceService.AddPersonalComputer(pcDto);
                
                return Results.Created($"Personal Computer created successfully!]", pcDto);
            }

            if (deviceType.ToString() == "embedded device")
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                
                /*{
                    "Name": "hello",
                    "IsTurnedOn": true,
                    "IpAddress": "22.142.81.231",
                    "NetworkName": "capybara",
                    "deviceType": "embedded device"
                }*/

                EmbeddedDevice? ed;
                try
                {
                    ed = json.Deserialize<EmbeddedDevice>(options);

                }
                catch (JsonException)
                {
                    return Results.BadRequest("Invalid JSON.");
                }

                if (ed == null)
                {
                    return Results.BadRequest("Invalid JSON.");
                }

                if (!Regex.IsMatch(ed.IpAddress,
                        @"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$"))
                {
                    return Results.BadRequest("Invalid IP address.");
                }

                if (!ed.NetworkName.Contains("MD Ltd.") && ed.IsTurnedOn)
                {
                    return Results.BadRequest("Embedded Device cannot be turned on when it does not have MD Ltd.");
                }

                var edDto = new EmbeddedDeviceDto
                {
                    Id = ed.Id,
                    Name = ed.Name,
                    IsTurnedOn = ed.IsTurnedOn,
                    IpAddress = ed.IpAddress,
                    NetworkName = ed.NetworkName
                };
                edDto = deviceService.AddEmbeddedDevice(edDto);
                return Results.Created($"Embedded Device created successfully! [/api/devices/{{ed.Id}}]", edDto);
            }
            else
            {
                return Results.BadRequest("Cannot see device type -.-");
            }
        }
        case "text/plain":
        {
            // OK, user will insert data like in .../textfile/input.txt
            // SW-1,Apple Watch SE2,true,27%
            // P-1,LinuxPC,false,Linux Mint
            // I will ignore [0] value and will add after [1]
            
            using var reader = new StreamReader(request.Body);
            string rawJson = await reader.ReadToEndAsync();
            
            var lines = rawJson.Split('\n');
            
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length < 4)
                {
                    return Results.BadRequest("Error! You can't create a device with less than 4 attributes.");
                }
                
                // ignore id
                var name = parts[1];
                if (!bool.TryParse(parts[2], out var isTurnedOn))
                {
                    return Results.BadRequest("Error while Device parsing: expecting a boolean value for 3rd parameter ;<");
                }
                
                if (parts[0].StartsWith("SW-"))
                { 
                    if(!int.TryParse(parts[3].TrimEnd('%'), out var  batteryLevel))
                    {
                        return Results.BadRequest("Error while SmartWatch parsing: expecting a number value for batteryLevel ;<");
                    }
                    var sw = new Smartwatch
                    {
                        Name = name,
                        IsTurnedOn = isTurnedOn,
                        BatteryLevel = batteryLevel
                    };
                    
                    if (sw.BatteryLevel is < 0 or > 100)
                    {
                        return Results.BadRequest("Battery level must be between 0 and 100.");
                    }
                    
                    var swDto = new SmartwatchDto
                    {
                        Id = sw.Id,
                        Name = sw.Name,
                        IsTurnedOn = sw.IsTurnedOn,
                        BatteryLevel = sw.BatteryLevel,
                    };
                    
                    deviceService.AddSmartwatch(swDto);
                }
                else if(parts[0].StartsWith("PC-"))
                {
                    var operationSystem = parts[3];
                    var pc = new PersonalComputer
                    {
                        Name = name,
                        IsTurnedOn = isTurnedOn,
                        OperationSystem = operationSystem,
                    };
                    
                    if (pc.IsTurnedOn && pc.OperationSystem.IsNullOrEmpty())
                    {
                        return Results.BadRequest("Personal Computer has to have operating system to be turned on");
                    }
                    
                    var pcDto = new PersonalComputerDto
                    {
                        Id = pc.Id,
                        Name = pc.Name,
                        IsTurnedOn = pc.IsTurnedOn,
                        OperationSystem = pc.OperationSystem
                    };
                    deviceService.AddPersonalComputer(pcDto);
                }
                else if (parts[0].StartsWith("ED-"))
                {
                    if (parts.Length < 5)
                    {
                        return Results.BadRequest("Error while Embedded Device parsing: expecting 5 attributes for creating ;<");
                    }
                    
                    var ipAddress = parts[3];
                    var networkName = parts[4];
                    var ed = new EmbeddedDevice
                    {
                        Name = name,
                        IsTurnedOn = isTurnedOn,
                        IpAddress = ipAddress,
                        NetworkName = networkName,
                    };
                    
                    if (!Regex.IsMatch(ed.IpAddress,
                            @"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$"))
                    {
                        return Results.BadRequest("Invalid IP address.");
                    }

                    if (!ed.NetworkName.Contains("MD Ltd.") && ed.IsTurnedOn)
                    {
                        return Results.BadRequest("Embedded Device cannot be turned on when it does not have MD Ltd.");
                    }
                    
                    var edDto = new EmbeddedDeviceDto
                    {
                        Id = ed.Id,
                        Name = ed.Name,
                        IsTurnedOn = ed.IsTurnedOn,
                        IpAddress = ed.IpAddress,
                        NetworkName = ed.NetworkName
                    };
                    deviceService.AddEmbeddedDevice(edDto);
                }
            }
            
            return Results.Ok("Devices from text/plain created successfully!");
            
        }
        default:
            return Results.Conflict();
    }

}).Accepts<string>("application/json", ["text/plain"]);

app.MapPut("api/devices", async (HttpRequest request, IDeviceService deviceService) =>
{
    string? contentType = request.ContentType?.ToLower();

    switch (contentType)
    {
        case "application/json":
        {
            using var reader = new StreamReader(request.Body);
            string rawJson = await reader.ReadToEndAsync();
            
            var json = JsonNode.Parse(rawJson);
            
            // now we don't care about which device we are adding
            // it will be id, name, isTurnOn for all devices + specific for separated ones
            // but still it's important to have ID column to update device
            var deviceId = json["Id"];

            if (deviceId == null)
            {
                return Results.BadRequest("You have to provide a device Id.");
            }

            if (deviceId.ToString().StartsWith("SW-"))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                
                /*{
                    "Id": "SW-1",
                    "Name": "hello",
                    "IsTurnedOn": true,
                    "BatteryLevel": 80
                }*/
                
                Smartwatch? smartwatch;
                try
                {
                    smartwatch = json.Deserialize<Smartwatch>(options);
                }
                catch (JsonException)
                {
                    return Results.BadRequest("Invalid JSON.");
                }

                if (smartwatch == null)
                {
                    return Results.BadRequest("Invalid JSON.");
                }
                
                if (smartwatch.BatteryLevel is < 0 or > 100)
                {
                    return Results.BadRequest("Battery level must be between 0 and 100.");
                }
                
                var ok = await deviceService.ModifySmartwatch(smartwatch);
                if (!ok) 
                    return Results.Conflict("Concurrent update failed or device not found.");
                return Results.Ok("SmartWatch updated successfully!");
                
            }

            if (deviceId.ToString().StartsWith("PC-"))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                
                /*{
                    "Id": "PC-1",
                    "Name": "hello",
                    "IsTurnedOn": true,
                    "deviceType": "personal computer",
                    "OperationSystem": "Apple"
                }*/
                
                PersonalComputer? pc;
                try
                {
                    pc = json.Deserialize<PersonalComputer>(options);
                }
                catch (JsonException)
                {
                    return Results.BadRequest("Invalid JSON.");
                }

                if (pc == null)
                {
                    return Results.BadRequest("Invalid JSON.");
                }

                if (pc.IsTurnedOn && pc.OperationSystem.IsNullOrEmpty())
                {
                    return Results.BadRequest("Personal Computer has to have operating system to be turned on");
                }
                
                var ok = await deviceService.ModifyPersonalComputer(pc);
                if (!ok) 
                    return Results.Conflict("Concurrent update failed or device not found.");
                return Results.Ok("Personal Computer updated successfully!");
            }

            if (deviceId.ToString().StartsWith("ED-"))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                
                /*{
                    "Id": "ED-1",
                    "Name": "hello",
                    "IsTurnedOn": true,
                    "IpAddress": "182.86.135.104",
                    "NetworkName": "different",
                    "deviceType": "embedded device"
                }*/
                
                EmbeddedDevice? ed;
                try
                {
                    ed = json.Deserialize<EmbeddedDevice>(options);

                }
                catch (JsonException)
                {
                    return Results.BadRequest("Invalid JSON.");
                }

                if (ed == null)
                {
                    return Results.BadRequest("Invalid JSON.");
                }

                if (!Regex.IsMatch(ed.IpAddress,
                        @"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$"))
                {
                    return Results.BadRequest("Invalid IP address.");
                }

                if (!ed.NetworkName.Contains("MD Ltd.") && ed.IsTurnedOn)
                {
                    return Results.BadRequest("Embedded Device cannot be turned on when it does not have MD Ltd.");
                }
                
                var ok = await deviceService.ModifyEmbeddedDevice(ed);
                if (!ok) 
                    return Results.Conflict("Concurrent update failed or device not found.");
                return Results.Ok("Embedded Device updated successfully!");
            }
            else
            {
                return Results.NotFound($"Device with id = {deviceId} not found.");
            }
        }
        default:
            return Results.Conflict("Something went wrong :(");
    }

}).Accepts<string>("application/json");

app.MapDelete("api/devices/{id}", async (IDeviceService deviceService, string id) =>
{
    try
    {
        var result = await deviceService.RemoveDevice(id);
        if(!result)
            return Results.NotFound($"Device with id = {id} not found."); 
        
        return Results.Ok("Device deleted successfully!");
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.Run();