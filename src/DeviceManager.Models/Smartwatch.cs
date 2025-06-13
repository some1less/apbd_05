﻿namespace Models;

public class Smartwatch : Device
{
    private int _batteryLevel;

    public int BatteryLevel { get; set; }
    public byte[] SwRowVersion { get; set; }

    public Smartwatch(){}
    public Smartwatch(string id, string name, bool isTurnedOn, byte[] originalVersion, int batteryLevel)
        : base(id, name, isTurnedOn, originalVersion)
    {
        BatteryLevel = batteryLevel;
    }
    

    public void TurnMode()
    {
        if (IsTurnedOn)
        {
            IsTurnedOn = false;
        }
        else
        {
            IsTurnedOn = true;
        }
    }

    public object GetInfo()
    {
        return new
        {
            Type = "Smartwatch",
            Id,
            Name,
            IsTurnedOn,
            BatteryLevel = _batteryLevel
        };
    }
}