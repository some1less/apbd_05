using Microsoft.AspNetCore.Mvc;
using Logic;
using Models;

namespace APBD_05;

[ApiController]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    
    // GET api/devices
    [HttpGet]
    public IResult GetDevices()
    {
        return Results.Ok(DeviceData.Devices);
    }

    // GET api/devices/SW-1
    [HttpGet(":id")]
    public IResult GetDevice(string id)
    {
        try
        {
            return Results.Ok(DeviceManager.Instance.GetDeviceById(id));
        }
        catch
        {
            return Results.NotFound();
        }
    }
    
    // POST api/devices/
    [HttpPost("smartwatches")]
    public IResult AddDevice(Smartwatch device)
    {
        DeviceManager.Instance.AddDevice(device);
        return Results.Ok(device);
    }
    
    [HttpPost("personal-computers")]
    public IResult AddDevice(PersonalComputer device)
    {
        DeviceManager.Instance.AddDevice(device);
        return Results.Ok(device);
    }
    [HttpPost("embedded-devices")]
    public IResult AddDevice(EmbeddedDevice device)
    {
        DeviceManager.Instance.AddDevice(device);
        return Results.Ok(device);
    }
    
    [HttpPut("smartwatches/:id")]
    public IResult UpdateSmartWatch(string id, [FromBody] Smartwatch device)
    {
        DeviceManager.Instance.EditDeviceData(id, device);
        try
        {
            return Results.Ok(DeviceManager.Instance.GetDeviceById(id));
        }
        catch
        {
            return Results.NotFound();
        }
    }
    
    [HttpPut("personal-computers/:id")]
    public IResult UpdatePersonalComputer(string id, [FromBody] PersonalComputer device)
    {
        DeviceManager.Instance.EditDeviceData(id, device);
        try
        {
            return Results.Ok(DeviceManager.Instance.GetDeviceById(id));
        }
        catch
        {
            return Results.NotFound();
        }
    }
    
    
    [HttpPut("embedded-devices/:id")]
    public IResult UpdateEmbeddedDevice(string id, [FromBody] EmbeddedDevice device)
    {
        DeviceManager.Instance.EditDeviceData(id, device);
        try
        {
            return Results.Ok(DeviceManager.Instance.GetDeviceById(id));
        }
        catch
        {
            return Results.NotFound();
        }
    }
    
    // DELETE api/devices/SW-1
    [HttpDelete(":id")]
    public IResult DeleteDevice(string id)
    {
        try
        {
            DeviceManager.Instance.RemoveDevice(id);
            return Results.Ok();
        }
        catch
        {
            return Results.NotFound();
        }
        
    }
}