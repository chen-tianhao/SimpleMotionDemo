using UnityEngine;
using O2DESNet;
using System;

public class Simulator : Sandbox
{
    public Simulator(int seed = 0) : base(seed)
    {
    }
    
    public void Arrive(int id)
    {
        Debug.Log($" ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Entity {id} arrived at time {ClockTime}");
        Schedule(() => Process(id), TimeSpan.FromSeconds(1.0));
        Schedule(() => Arrive(id + 1), TimeSpan.FromSeconds(10.0 + DefaultRS.NextDouble() * 10.0));
    }

    void Process(int id)
    {
        Debug.Log($" ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Entity {id} processed at time {ClockTime}");
        Schedule(() => Depart(id), TimeSpan.FromSeconds(1.0));
    }

    void Depart(int id)
    {
        Debug.Log($" ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Entity {id} departed at time {ClockTime}");
    }
}
